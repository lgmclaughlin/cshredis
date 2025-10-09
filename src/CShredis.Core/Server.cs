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

namespace CShredis.Core
{
	public class Server
	{
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

		/// <summary>
		/// Initializes a Server
		/// </summary>
		public Server()
		{
			Console.WriteLine("Creating epoll.");

			_epollFd = Syscall.epoll_create(1);

			if (_epollFd < 0)
				throw new IOException($"Call to {nameof(Syscall.epoll_create)} failed with code: {Stdlib.GetLastError()}");

			Console.WriteLine($"Epoll created successfully with fd {_epollFd}.");
		}

		/// <summary>
		/// Starts the server. Sets up listeners and runs the event loop.
		/// </summary>
		public void Start()
		{
			try
			{
				Console.WriteLine($"Starting server on port {PORT}.");

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

		/// <summary>
		/// Stops the server. Closes the epoll and disposes of all listeners and clients. 
		/// </summary>
		public void Stop()
		{
			if (!_running)
				return;

			Console.WriteLine("Stopping server.");

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

			Console.WriteLine("Server stopped.");
		}

		private void SetupWakeUpPipe()
		{
			Console.WriteLine("Setting up wake up pipe.");

			var pipeFds = new int[2];
			var ret = Syscall.pipe(pipeFds);
			if (ret < 0)
				throw new IOException($"Call to {nameof(Syscall.pipe)} failed with code {ret}: {Stdlib.GetLastError()}");

			_wakeUpReadFd = pipeFds[0];
			_wakeUpWriteFd = pipeFds[1];

			Console.WriteLine($"Wake up pipe successfully set up with read fd {_wakeUpReadFd} and write fd {_wakeUpWriteFd}.");

			RegisterFd(_wakeUpReadFd, WAKE_UP_READ_EVENTS);
		}

		/// <summary>
		/// Sets up listener sockets and registers them with epoll.
		/// </summary>
		private void SetupListeners()
		{
			Console.WriteLine("Setting up listeners.");

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

			Console.WriteLine("Listeners set up successfully.");
		}

		/// <summary>
		/// Sets the passed fd to Nonblocking. 
		/// </summary>
		private void SetNonBlocking(int fd)
		{
			Console.WriteLine($"Setting fd {fd} to nonblocking.");

			int opts = Syscall.fcntl(fd, FcntlCommand.F_GETFL);
			if (opts < 0)
				throw new IOException($"Failed to get openflags for fd {fd}: {Stdlib.GetLastError()}");

			opts |= (int)OpenFlags.O_NONBLOCK;

			int ret = Syscall.fcntl(fd, FcntlCommand.F_SETFL, opts);
			if (ret < 0)
				throw new IOException($"Failed to set socket O_NONBLOCK: {Stdlib.GetLastError()}"); 
		
			Console.WriteLine($"Successfully set fd {fd} to nonblocking.");
		}

		/// <summary>
		/// Manage a given fd with epoll events and given operation. 
		/// </summary>
		private void ManageFd(int fd, EpollEvents events, EpollOp op) 
		{
			var ev = new EpollEvent()
			{
				fd = fd,
				events = events
			};

			Console.WriteLine($"Modifying epoll events and operation for fd {fd}, events {events}, and operation {op}.");

			var ret = Syscall.epoll_ctl(_epollFd, op, fd, ref ev);
			if (ret < 0)
				throw new IOException($"Call to {nameof(Syscall.epoll_ctl)} failed with code {ret}: {Stdlib.GetLastError()}");
		}

		/// <summary>
		/// Registers an fd with epoll for the given events.
		/// </summary>
		private void RegisterFd(int fd, EpollEvents events) => ManageFd(fd, events, EpollOp.EPOLL_CTL_ADD);

		/// <summary>
		/// Modifies events of an already regististered fd. 
		/// </summary>
		private void ModifyFd(int fd, EpollEvents events) => ManageFd(fd, events, EpollOp.EPOLL_CTL_MOD);

		/// <summary>
		/// Deregister an fd with epoll for the given events. 
		/// </summary>
		private void DeregisterFd(int fd) => ManageFd(fd, 0, EpollOp.EPOLL_CTL_DEL);

		/// <summary>
		/// Sets new client fd to nonblock and registers with the epoll.
		/// </summary>
		private void HandleNewClient(int fd)
		{
			try
			{
				Console.WriteLine($"Handling new client with fd {fd}.");

				SetNonBlocking(fd);
				RegisterFd(fd, CLIENT_EVENTS);

				var client = new Client(fd);
				_clients[fd] = client;

				Console.WriteLine($"Succesfully handled new client with fd {fd}.");
			}
			catch
			{
				try { Syscall.close(fd); } catch { };
				throw;
			}
		}

		private void RemoveClient(Client client)
		{
			Console.WriteLine($"Removing client with fd {client.Fd}."); 

			DeregisterFd(client.Fd);
			_clients.Remove(client.Fd);
			client.Dispose();
		}

		/// <summary>
		/// Wakes up the epoll_wait with a write to the wake up pipe. 
		/// </summary>
		private void WakeUpPoll()
		{
			Console.WriteLine("Waking up poll.");

			var writeBytes = new byte[1] { 1 };

			unsafe
			{
				fixed (byte* writeBytesPtr = writeBytes)
					_ = Syscall.write(_wakeUpWriteFd, (IntPtr)writeBytesPtr, 1);
			}
		}

		/// <summary>
		/// Empties the wake up pipe after receiving a wake up. 
		/// </summary>
		private void EmptyWakeUpPipe()
		{
			Console.WriteLine("Wake up received. Emptying wake up pipe.");

			var readBytes = new byte[1];

			unsafe
			{
				fixed (byte* readBytesPtr = readBytes)
					_ = Syscall.read(_wakeUpReadFd, (IntPtr)readBytesPtr, 1);
			}
		}

		/// <summary>
		/// Starts the event loop that runs through epoll events and handles new clients and client requests. 
		/// </summary>
		private void RunEventLoop()
		{
			var events = new EpollEvent[MAX_EVENTS];

			try
			{
				while (_running)
				{
					Console.WriteLine("Waiting for epoll socket.");
					var count = Syscall.epoll_wait(_epollFd, events, events.Length, -1);

					if (count < 0)
					{
						Console.WriteLine($"Invalid event count returned from {nameof(Syscall.epoll_wait)}. Stopping.");
						_running = false;
						break;
					}

					for (int i = 0; i < count; i++)
					{
						var ev = events[i];
						Console.WriteLine($"Epoll[{i}] got events {ev.events} from fd {ev.fd}");

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
								Console.WriteLine($"Failed to accept new client: {ex}");
							}
						}
						else if (_clients.TryGetValue(ev.fd, out Client client))
						{
							if ((ev.events & EpollEvents.EPOLLIN) != 0)
							{
								try
								{
									string data = client.Read();
									if (!string.IsNullOrEmpty(data))
									{
										string escapedData = data.Replace("\r", "\\r").Replace("\n", "\\n");
										Console.WriteLine($@"Received from client fd {ev.fd}: {escapedData}");

										client.EnqueueResponseToWriteQueue("+PONG\r\n");

										// Client previously had 0 responses queued and was
										// not listening for writes, so listen now
										if (client.GetWriteQueueCount() == 1)
											ModifyFd(ev.fd, CLIENT_EVENTS | EpollEvents.EPOLLOUT);
									}
									else if (data == null)
									{
										Console.WriteLine($"Client with fd {ev.fd} closed connection. Removing client.");
										RemoveClient(client);
										continue;
									}
								}
								catch (Exception ex)
								{
									Console.WriteLine($"Error reading from client fd {ev.fd}: {ex}");
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
									Console.WriteLine($"Error writing to client fd {ev.fd}: {ex}");
									RemoveClient(client);
								}
							}

							if ((ev.events & EpollEvents.EPOLLHUP) != 0 || (ev.events & EpollEvents.EPOLLRDHUP) != 0)
							{
								Console.WriteLine($"Client fd {ev.fd} disconnected.");
								RemoveClient(client);
							}
						}
						else
						{
							Console.WriteLine($"Unknown fd {ev.fd} with events {ev.events}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Event Loop crashed.");
				Console.WriteLine(ex.ToString());
			}
			finally
			{
				Console.WriteLine("Event Loop stopping.");
				if (_epollFd >= 0) {
					try { Syscall.close(_epollFd); } catch { }
					_epollFd = -1;
				}
			}
		}
	}
}
