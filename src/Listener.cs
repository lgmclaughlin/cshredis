using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Mono.Unix.Native;

namespace CShredis
{
	public class Listener : IDisposable
	{
		private int _fd = -1;

		/// <summary>
		/// Initializes a Listener.
		/// </summary>
		public Listener()
		{
		}
		
		/// <summary>
		/// Prepares the socket with passed family.
		/// </summary>
		private void SetupSocket(UnixAddressFamily family)
		{
			if (_fd > -1)
				throw new InvalidOperationException("The socket is already initialized.");

			_fd = Syscall.socket(family, UnixSocketType.SOCK_STREAM, 0);

			if (_fd < 0)
				throw new IOException($"Failed to open socket: {Stdlib.GetLastError()}");

			Syscall.setsockopt(_fd, UnixSocketProtocol.SOL_SOCKET, UnixSocketOptionName.SO_REUSEADDR, 1);
		}

		/// <summary>
		/// Binds the socket to an endpoint and begins listening
		/// </summary>
		public void Bind(EndPoint endPoint, int backlog)
		{
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

		/// <summary>
		/// Accepts an incoming connection and returns the file descriptor.
		/// </summary>
		public int Accept()
		{
			var addr = new SockaddrIn();
			int fd = Syscall.accept(_fd, addr);

			if (fd < 0)
				throw new IOException($"Failed to accept: {Stdlib.GetLastError()}");

			return fd;
		}
		
		/// <summary>
		/// Closes the socket.
		/// </summary>
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
