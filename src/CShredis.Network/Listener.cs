using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Mono.Unix.Native;

namespace CShredis.Network
{
	public class Listener : IDisposable
	{
		private static readonly NetworkLogger _log = NetworkLogger.For(nameof(Listener));
		private int _fd = -1;

		public int Fd { get { return _fd; } }

		public Listener() { }
		
		private void SetupSocket(UnixAddressFamily family)
		{
			if (_fd > -1)
				throw new InvalidOperationException("The socket is already initialized.");

			_log.Info("Setting up listener socket.");

			_fd = Syscall.socket(family, UnixSocketType.SOCK_STREAM, 0);

			if (_fd < 0)
				throw new IOException($"Failed to open socket: {Stdlib.GetLastError()}");

			_log.Trace($"Setting socket options for socket fd {_fd}.");

			Syscall.setsockopt(_fd, UnixSocketProtocol.SOL_SOCKET, UnixSocketOptionName.SO_REUSEADDR, 1);
		}

		public void Bind(EndPoint endPoint, int backlog)
		{
			_log.Trace($"Binding listener to endpoint with backlog {backlog}.");
			
			try
			{
				Sockaddr servAddr = null;
				var filePath = string.Empty;

				// Support only IP_V4 for now
				// Revisit and add implementation for other families
				var family = UnixAddressFamily.AF_INET;

				if (endPoint is IPEndPoint ipEndPoint) {
					servAddr = new SockaddrIn()
					{
						sa_family = UnixAddressFamily.AF_INET,
						sin_family = UnixAddressFamily.AF_INET,
						sin_addr = new InAddr() { s_addr = BitConverter.ToUInt32(ipEndPoint.Address.GetAddressBytes(), 0) },
						sin_port = Syscall.htons((ushort)ipEndPoint.Port)
					};

					SetupSocket(family);
				}

				int bind = Syscall.bind(_fd, servAddr);

				if (bind < 0)
					throw new IOException($"Failed to bind to endpoint: {Stdlib.GetLastError()}");

				int listen = Syscall.listen(_fd, backlog);

				if (listen < 0)
					throw new IOException($"Failed to set socket to listen: {Stdlib.GetLastError()}");
			}
			catch
			{
				if (_fd != -1)
				{
					try
					{ 
						Syscall.close(_fd);
					}
					catch 
					{
					}

					_fd = -1;
				}

				throw;
			}
		}

		public int Accept()
		{
			_log.Info("Listener accepting incoming connection.");

			var addr = new SockaddrIn();
			int fd = Syscall.accept(_fd, addr);

			if (fd < 0)
				throw new IOException($"Failed to accept: {Stdlib.GetLastError()}");

			_log.Trace($"Connection successfully accepted with fd {fd}.");

			return fd;
		}
		
		public void Dispose()
		{
			if (_fd >= 0)
			{
				try { Syscall.close(_fd); } catch { }
				_fd = -1;
			}
		}
	}
}
