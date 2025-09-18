using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using Mono.Unix.Native;

namespace CShredis
{
	public class ListenerSocket : IDisposable
	{
		private int _socket = -1;

		public int Fd => _socket;

		public ListenerSocket()
		{
		}
		
		/// <summary>
		/// Prepares the socket with passed family
		/// </summary>
		private void SetupSocket(UnixAddressFamily family)
		{
			if (_socket > -1)
				throw new InvalidOperationException("The socket is already initialized.");

			_socket = Syscall.socket(family, UnixSocketType.SOCK_STREAM, 0);

			if (_socket < 0)
				throw new IOException($"Failed to open socket: {Stdlib.GetLastError()}");

			Syscall.setsockopt(_socket, UnixSocketProtocol.SOL_SOCKET, UnixSocketOptionName.SO_REUSEADDR, 1);
		}

		/// <summary>
		/// Bind the socket to an endpoint and begin listening
		/// </summary>
		public void Bind(EndPoint endpoint, int backlog)
		{
			try
			{
				Sockaddr servAddr;
				var filePath = string.Empty;

				// Support only IP_V4 for now
				// Revisit and add implementation for other families
				var family = UnixAddressFamily.AF_INET;

				servAddr = new SockaddrIn()
				{
					sa_family = UnixAddressFamily.AF_INET,
					sin_family = UnixAddressFamily.AF_INET,
					sin_addr = new InAddr() { s_addr = BitConverter.ToUInt32(endpoint.Address.GetAddressBytes(), 0) },
					sin_port = Syscall.htons((ushort)endpoint.Port)
				};

				SetupSocket(family);

				int bind = Syscall.bind(_socket, servAddr);

				if (bind < 0)
					throw new IOException($"Failed to bind to endpoint: {Stdlib.GetLastError()}");

				int listen = Syscall.listen(_socket, backlog);

				if (listen < 0)
					throw new IOException($"Failed to set socket to listen: {Stdlib.GetLastError()}");
			}
			catch
			{
				if (_socket != -1)
				{
					try
					{ 
						Syscall.close(_socket);
					}
					catch 
					{
					}

					_socket = -1;
				}

				throw;
			}
		}

		/// <summary>
		/// Accept an incoming connection and return the file descriptor
		/// </summary>
		public int Accept()
		{
			var addr = new SockaddrIn();
			int fd = Syscall.accept(_socket, addr);

			if (fd < 0)
				throw new IOException($"Failed to accept: {Stdlib.GetLastError()}");

			return fd;
		}
		
		/// <summary>
		/// Close the socket
		/// </summary>
		public  void Dispose()
		{
			if (_socket != -1)
			{
				try
				{
					Syscall.close(_socket);
				}
				finally	
				{
					_socket = -1;
				}
			}
		}
	}
}
