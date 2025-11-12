using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Mono.Unix.Native;
using CShredis.RESP;
using CShredis.Commands;

namespace CShredis.Network
{
	public class Server
	{
		private static readonly NetworkLogger _log = NetworkLogger.For(nameof(Server));
		private static readonly EpollEvents LISTENER_EVENTS = EpollEvents.EPOLLIN;
		private static readonly EpollEvents WAKE_UP_READ_EVENTS = EpollEvents.EPOLLIN;
		private static readonly EpollEvents CLIENT_EVENTS = 
											EpollEvents.EPOLLIN  |
											EpollEvents.EPOLLHUP |
											EpollEvents.EPOLLRDHUP;
		private const int BACKLOG = 128;
		private const int MAX_EVENTS = 100;
		private const int PORT = 6379;
		private int _epollFd;
		private int _wakeUpWriteFd;
		private int _wakeUpReadFd;
		private Dictionary<int, Listener> _listeners = new Dictionary<int, Listener>();
		private Dictionary<int, Client> _clients = new Dictionary<int, Client>();
		private bool _running = false;
		private static readonly ParseDispatcher _parseDispatcher = new();
		private static readonly CommandDispatcher _commandDispatcher = new();

		public Server()
		{
			_log.Info("Creating epoll.");

			_epollFd = Syscall.epoll_create(1);

			if (_epollFd < 0)
				throw new IOException($"Call to {nameof(Syscall.epoll_create)} failed with code: {Stdlib.GetLastError()}");

			_log.Trace($"Epoll created successfully with fd {_epollFd}.");
		}

		public void Start()
		{
			try
			{
				_log.Info($"Starting server on port {PORT}.");

				_running = true;
				SetupWakeUpPipe();
				SetupListeners();
				RunEventLoop();
			}	
			finally
			{
				Stop();
			}
		}

		public void Stop()
		{
			if (!_running)
				return;

			_log.Info("Stopping server.");

			_running = false;

			WakeUpPoll();

			if (_epollFd >= 0)
			{
				try { Syscall.close(_epollFd); } catch { }
				_epollFd = -1;
			}

			foreach (var listener in _listeners.Values)
			{
				try { listener.Dispose(); } catch { }
			}

			foreach (var client in _clients.Values)
			{
				try { client.Dispose(); } catch { }
			}

			_listeners.Clear();
			_clients.Clear();

			_log.Info("Server stopped.");
		}

		private void SetupWakeUpPipe()
		{
			_log.Info("Setting up wake up pipe.");

			var pipeFds = new int[2];
			var ret = Syscall.pipe(pipeFds);
			if (ret < 0)
				throw new IOException($"Call to {nameof(Syscall.pipe)} failed with code {ret}: {Stdlib.GetLastError()}");

			_wakeUpReadFd = pipeFds[0];
			_wakeUpWriteFd = pipeFds[1];

			_log.Trace($"Wake up pipe successfully set up with read fd {_wakeUpReadFd} and write fd {_wakeUpWriteFd}.");

			RegisterFd(_wakeUpReadFd, WAKE_UP_READ_EVENTS);
		}

		private void SetupListeners()
		{
			_log.Info("Setting up listeners.");

			var ipv4Endpoint = new IPEndPoint(IPAddress.Any, PORT);
			var ipv4Listener = new Listener();

			try
			{
				ipv4Listener.Bind(ipv4Endpoint, BACKLOG);

				SetNonBlocking(ipv4Listener.Fd);
				RegisterFd(ipv4Listener.Fd, LISTENER_EVENTS);

				_listeners[ipv4Listener.Fd] = ipv4Listener;
			}
			catch
			{
				if (ipv4Listener.Fd >= 0)
				{
					try { Syscall.close(ipv4Listener.Fd); } catch { }
				}
				throw;
			}

			_log.Info("Listeners set up successfully.");
		}

		private void SetNonBlocking(int fd)
		{
			_log.Trace($"Setting fd {fd} to nonblocking.");

			int opts = Syscall.fcntl(fd, FcntlCommand.F_GETFL);
			if (opts < 0)
				throw new IOException($"Failed to get openflags for fd {fd}: {Stdlib.GetLastError()}");

			opts |= (int)OpenFlags.O_NONBLOCK;

			int ret = Syscall.fcntl(fd, FcntlCommand.F_SETFL, opts);
			if (ret < 0)
				throw new IOException($"Failed to set socket O_NONBLOCK: {Stdlib.GetLastError()}"); 
		
			_log.Trace($"Successfully set fd {fd} to nonblocking.");
		}

		private void ManageFd(int fd, EpollEvents events, EpollOp op) 
		{
			var ev = new EpollEvent()
			{
				fd = fd,
				events = events
			};

			_log.Trace($"Modifying epoll events and operation for fd {fd}, events {events}, and operation {op}.");

			var ret = Syscall.epoll_ctl(_epollFd, op, fd, ref ev);
			if (ret < 0)
				throw new IOException($"Call to {nameof(Syscall.epoll_ctl)} failed with code {ret}: {Stdlib.GetLastError()}");
		}

		private void RegisterFd(int fd, EpollEvents events) => ManageFd(fd, events, EpollOp.EPOLL_CTL_ADD);

		private void ModifyFd(int fd, EpollEvents events) => ManageFd(fd, events, EpollOp.EPOLL_CTL_MOD);

		private void DeregisterFd(int fd) => ManageFd(fd, 0, EpollOp.EPOLL_CTL_DEL);

		private void HandleNewClient(int fd)
		{
			try
			{
				_log.Trace($"Handling new client with fd {fd}.");

				SetNonBlocking(fd);
				RegisterFd(fd, CLIENT_EVENTS);

				var client = new Client(fd);
				_clients[fd] = client;

				_log.Trace($"Succesfully handled new client with fd {fd}.");
			}
			catch
			{
				try { Syscall.close(fd); } catch { };
				throw;
			}
		}

		private void RemoveClient(Client client)
		{
			_log.Trace($"Removing client with fd {client.Fd}."); 

			DeregisterFd(client.Fd);
			_clients.Remove(client.Fd);
			client.Dispose();
		}

		private void WakeUpPoll()
		{
			_log.Info("Waking up poll.");

			var writeBytes = new byte[1] { 1 };

			unsafe
			{
				fixed (byte* writeBytesPtr = writeBytes)
					_ = Syscall.write(_wakeUpWriteFd, (IntPtr)writeBytesPtr, 1);
			}
		}

		private void EmptyWakeUpPipe()
		{
			_log.Info("Wake up received. Emptying wake up pipe.");

			var readBytes = new byte[1];

			unsafe
			{
				fixed (byte* readBytesPtr = readBytes)
					_ = Syscall.read(_wakeUpReadFd, (IntPtr)readBytesPtr, 1);
			}
		}

		private void RunEventLoop()
		{
			var events = new EpollEvent[MAX_EVENTS];

			try
			{
				while (_running)
				{
					_log.Info("Waiting for epoll socket.");

					var count = Syscall.epoll_wait(_epollFd, events, events.Length, -1);

					if (count < 0)
					{
						_log.Error($"Invalid event count returned from {nameof(Syscall.epoll_wait)}. Stopping.");
						_running = false;
						break;
					}

					for (int i = 0; i < count; i++)
					{
						var ev = events[i];
						_log.Debug($"Epoll[{i}] got events {ev.events} from fd {ev.fd}");

						if (ev.fd == _wakeUpReadFd)
						{
							EmptyWakeUpPipe();
							break;
						}
						else if (_listeners.TryGetValue(ev.fd, out Listener listener))
						{
							try
							{
								int newClientFd = listener.Accept();
								if (newClientFd > 0)
									HandleNewClient(newClientFd);
							}
							catch (Exception ex)
							{
								_log.Error($"Failed to accept new client: {ex}");
							}
						}
						else if (_clients.TryGetValue(ev.fd, out Client client))
						{
							if ((ev.events & EpollEvents.EPOLLIN) != 0)
							{
								try
								{
									ReadOnlyMemory<byte>? dataNullable = client.Read();
									if (dataNullable == null)
									{
										_log.Trace($"Client with fd {ev.fd} closed connection. Removing client.");
										RemoveClient(client);
										continue;
									}

									var data = (ReadOnlyMemory<byte>)dataNullable;
									if (data.Length > 0)
									{
										string escapedData = Encoding.UTF8.GetString(data.Span)
											.Replace("\r", "\\r").Replace("\n", "\\n");
										_log.Debug($@"Received from client fd {ev.fd}: {escapedData}");

										var dataRespObject = _parseDispatcher.Parse(data).ParsedObject;
										var commandResult = _commandDispatcher.Execute(dataRespObject);
										var encodedResponse = commandResult.Result.Encode();

										client.EnqueueResponseToWriteQueue(encodedResponse);

										// Client previously had 0 responses queued and was
										// not listening for writes, so listen now
										if (client.GetWriteQueueCount() == 1)
											ModifyFd(ev.fd, CLIENT_EVENTS | EpollEvents.EPOLLOUT);
									}
								}
								catch (Exception ex)
								{
									_log.Error($"Error reading from client fd {ev.fd}: {ex}");
									RemoveClient(client);
								}
							}
							if ((ev.events & EpollEvents.EPOLLOUT) != 0)
							{
								try
								{
									client.Write();

									// No writes queued, stop listening for write-ready
									if (client.GetWriteQueueCount() == 0)
										ModifyFd(ev.fd, CLIENT_EVENTS);
								}
								catch (Exception ex)
								{
									_log.Error($"Error writing to client fd {ev.fd}: {ex}");
									RemoveClient(client);
								}
							}

							if ((ev.events & EpollEvents.EPOLLHUP) != 0 || (ev.events & EpollEvents.EPOLLRDHUP) != 0)
							{
								_log.Trace($"Client fd {ev.fd} disconnected.");
								RemoveClient(client);
							}
						}
						else
						{
							_log.Trace($"Unknown fd {ev.fd} with events {ev.events}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				_log.Error("Event Loop crashed.");
				_log.Error($"Exception: {ex.ToString()}");
			}
			finally
			{
				_log.Info("Event Loop stopping.");
				if (_epollFd >= 0) {
					try { Syscall.close(_epollFd); } catch { }
					_epollFd = -1;
				}
			}
		}
	}
}
