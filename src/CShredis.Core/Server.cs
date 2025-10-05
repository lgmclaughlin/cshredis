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

		private static readonly EpollEvents CLIENT_EVENTS = 
											EpollEvents.EPOLLIN  |
											EpollEvents.EPOLLHUP |
											EpollEvents.EPOLLRDHUP;

		private const int BACKLOG = 128;

		private const int MAX_EVENTS = 100;

		private const int PORT = 6379;

		private int _epollFd;

		private Dictionary<int, Listener> _listeners = new Dictionary<int, Listener>();

		private Dictionary<int, Client> _clients = new Dictionary<int, Client>();
	
		private bool _running = false;

		/// <summary>
		/// Initializes a Server
		/// </summary>
		public Server()
		{
			_epollFd = Syscall.epoll_create(1);

			if (_epollFd < 0)
				throw new IOException($"Call to {nameof(Syscall.epoll_create)} failed with code: {Stdlib.GetLastError()}");
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

			_running = false;

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
		}

		/// <summary>
		/// Sets up listener sockets and registers them with epoll.
		/// </summary>
		private void SetupListeners()
		{
			var ipv4Endpoint = new IPEndPoint(IPAddress.Any, PORT);
			var ipv4Listener = new Listener();	
			var ipv4ListenerFd = ipv4Listener.Fd;

			try
			{
				ipv4Listener.Bind(ipv4Endpoint, BACKLOG);

				SetNonBlocking(ipv4ListenerFd);

				RegisterFd(ipv4ListenerFd, LISTENER_EVENTS);

				_listeners[ipv4ListenerFd] = ipv4Listener;
			}
			catch
			{
				if (ipv4ListenerFd >= 0)
				{
					try { Syscall.close(ipv4ListenerFd); } catch { }
				}
				throw;
			}
		}

		/// <summary>
		/// Sets the passed fd to Nonblocking. 
		/// </summary>
		private void SetNonBlocking(int fd)
		{
			int opts = Syscall.fcntl(fd, FcntlCommand.F_GETFL);
			if (opts < 0)
				throw new IOException($"Failed to get openflags for fd: {Stdlib.GetLastError()}");

			opts |= (int)OpenFlags.O_NONBLOCK;

			int ret = Syscall.fcntl(fd, FcntlCommand.F_SETFL, opts);
			if (ret < 0)
				throw new IOException($"Failed to set socket O_NONBLOCK: {Stdlib.GetLastError()}"); 
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
				SetNonBlocking(fd);
				RegisterFd(fd, CLIENT_EVENTS);

				var client = new Client(fd);
				_clients[fd] = client;
			}
			catch
			{
				try { Syscall.close(fd); } catch { };
				throw;
			}
		}

		private void RemoveClient(Client client)
		{
			DeregisterFd(client.Fd);
			_clients.Remove(client.Fd);
			client.Dispose();
		}

		/// <summary>
		/// Starts the event loop that runs through epoll events and handles new clients and client requests. 
		/// </summary>
		private void RunEventLoop()
		{
			var events = new EpollEvent[MAX_EVENTS];
			var running = true;
			
			try
			{
				while (running)
				{
					Debug.WriteLine("Waiting for epoll socket.");
					var count = Syscall.epoll_wait(_epollFd, events, events.Length, -1);

					if (count < 0)
					{
						Debug.WriteLine($"Invalid event count returned from {nameof(Syscall.epoll_wait)}. Stopping.");
						running = false;
						break;
					}

					for (int i = 0; i < count; i++)
					{
						var ev = events[i];
						Debug.WriteLine($"Epoll[{i}] got events {ev.events} from fd {ev.fd}");

						if (_listeners.TryGetValue(ev.fd, out Listener listener))
						{
							try
							{
								int newClientFd = listener.Accept();
								if (newClientFd > 0)
									HandleNewClient(newClientFd);
							}
							catch (Exception ex)
							{
								Debug.WriteLine($"Failed to accept new client: {ex}");
							}
						}
						else if (_clients.TryGetValue(ev.fd, out Client client))
						{
							// Read ready
							if ((ev.events & EpollEvents.EPOLLIN) != 0)
							{
								try
								{
									string data = client.Read();
									if (!string.IsNullOrEmpty(data))
									{
										Debug.WriteLine($"Received from client fd {ev.fd}: {data}");
										client.EnqueueResponseToWriteQueue("+PONG\r\n");

										// Client previously had 0 responses queued and was
										// not listening for writes, so listen now
										if (client.GetWriteQueueCount() == 1)
											ModifyFd(ev.fd, CLIENT_EVENTS | EpollEvents.EPOLLOUT);
									}
									else if (data == null)
									{
										Debug.WriteLine($"Clien with fd {ev.fd} closed connection. Removing client.");
										RemoveClient(client);
										continue;
									}
								}
								catch (Exception ex)
								{
									Debug.WriteLine($"Error reading from client fd {ev.fd}: {ex}");
									RemoveClient(client);
								}
							}

							// Write ready
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
									Debug.WriteLine($"Error writing to client fd {ev.fd}: {ex}");
									RemoveClient(client);
								}
							}

							// Client disconnect
							if ((ev.events & EpollEvents.EPOLLHUP) != 0 || (ev.events & EpollEvents.EPOLLRDHUP) != 0)
							{
								Debug.WriteLine($"Client fd {ev.fd} disconnected.");
								RemoveClient(client);
							}
						}
						else
						{
							Debug.WriteLine($"Unknown fd {ev.fd} with events {ev.events}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Event Loop crashed.");
				Debug.WriteLine(ex.ToString());
				Debug.Flush();
			}
			finally
			{
				Debug.WriteLine("Event Loop stopping.");
				if (_epollFd >= 0) {
					try { Syscall.close(_epollFd); } catch { }
					_epollFd = -1;
				}
			}
		}
	}
}
