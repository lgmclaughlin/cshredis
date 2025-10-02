using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Mono.Unix.Native;

namespace CShredis
{
	public class Client : IDisposable
	{
		private const int MAX_READ_SIZE = 4096;

		private int _fd = -1;

		public int Fd { get { return _fd; } }

		private LinkedList<byte[]> _writeQueue = new LinkedList<byte[]>();

		/// <summary>
		/// Gets the current count of the write queue. 
		/// </summary>
		public int GetWriteQueueCount() => _writeQueue.Count;

		/// <summary>
		/// Initializes a client.
		/// </summary>
		public Client (int fd)
		{
			_fd = fd;
		}

		/// <summary>
		/// Reads from client file.
		/// </summary>
		public string Read()
		{
			var buffer = new byte[MAX_READ_SIZE];
			long bytesRead = Syscall.read(_fd, buffer, (ulong)buffer.Length);

			if (bytesRead == 0)
			{
				return null;
			}
			else if (bytesRead < 0)
			{
				var errno = Stdlib.GetLastError();
				if (errno == Errno.EAGAIN || errno == Errno.EWOULDBLOCK)
					return string.Empty;

				throw new IOException($"Read from client {_fd} failed: {errno}");
			}

			return Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
		}

		/// <summary>
		/// Adds a response to the end of the response queue.
		/// </summary>
		public void EnqueueResponseToWriteQueue(string response) => _writeQueue.AddLast(Encoding.UTF8.GetBytes(response));

		/// <summary>
		/// Writes a response to the client file. 
		/// </summary>
		public void Write()
		{
			while (_writeQueue.Count > 0)
			{
				byte[] responseBytes = _writeQueue.First.Value;

				var bytesWritten = Syscall.write(_fd, responseBytes, (ulong)responseBytes.Length);

				if (bytesWritten < 0)
				{
					var errno = Stdlib.GetLastError();
					if (errno == Errno.EAGAIN || errno == Errno.EWOULDBLOCK)
						return;

					throw new IOException($"Write to client {_fd} failed: {errno}");
				}
				else if (bytesWritten < responseBytes.Length)
				{
					_writeQueue.First.Value = responseBytes[bytesWritten..];
					return;
				}

				_writeQueue.RemoveFirst();
			}
		}

		/// <summary>
		/// Closes the client connection.
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
