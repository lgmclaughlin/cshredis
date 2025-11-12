using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Mono.Unix.Native;

namespace CShredis.Network
{
	public class Client : IDisposable
	{
		private static readonly NetworkLogger _log = NetworkLogger.For(nameof(Client));
		private const int MAX_READ_SIZE = 4096;
		private int _fd = -1;
		private LinkedList<byte[]> _writeQueue = new LinkedList<byte[]>();

		public int Fd { get { return _fd; } }
		public int GetWriteQueueCount() => _writeQueue.Count;

		public Client (int fd)
		{
			_fd = fd;
		}

		public ReadOnlyMemory<byte>? Read()
		{
			var readBytes = new byte[MAX_READ_SIZE];
			long readCount;

			_log.Info("Reading from client.");

			unsafe
			{
				fixed (byte* readBytesPtr = readBytes)
					readCount = Syscall.read(_fd, (IntPtr)readBytesPtr, (ulong)readBytes.Length);
			}

			if (readCount == 0)
			{
				return null;
			}
			else if (readCount < 0)
			{
				var errno = Stdlib.GetLastError();
				if (errno == Errno.EAGAIN || errno == Errno.EWOULDBLOCK)
				{
					_log.Info("Read from client would block - returning.");
					return ReadOnlyMemory<byte>.Empty;
				}

				throw new IOException($"Reading from client {_fd} failed: {errno}");
			}

			_log.Info("Read from client succesful.");

			return readBytes[..(int)readCount].AsMemory();
		}

		public void EnqueueResponseToWriteQueue(ReadOnlyMemory<byte> response) => _writeQueue.AddLast(response.ToArray());

		public void Write()
		{
			_log.Info("Writing messages back to client.");
			
			while (_writeQueue.Count > 0)
			{
				byte[] writeBytes = _writeQueue.First.Value;
				long writeCount;

				unsafe
				{
					fixed (byte* writeBytesPtr = writeBytes)
						writeCount = Syscall.write(_fd, (IntPtr)writeBytesPtr, (ulong)writeBytes.Length);
				}

				if (writeCount < 0)
				{
					var errno = Stdlib.GetLastError();
					if (errno == Errno.EAGAIN || errno == Errno.EWOULDBLOCK)
					{
						_log.Info("Writing to client would block - returning.");
						return;
					}

					throw new IOException($"Write to client {_fd} failed: {errno}");
				}
				else if (writeCount < writeBytes.Length)
				{
					_writeQueue.First.Value = writeBytes[(int)writeCount..];
					return;
				}

				_writeQueue.RemoveFirst();

				_log.Info("Write to client successful.");
				_log.Debug($"Messages remaining in queue: {_writeQueue.Count}"); 
			}
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
