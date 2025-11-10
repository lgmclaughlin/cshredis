using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPBulkString : RESPObject
	{
		public override RESPType Type => RESPType.BulkString;

		public ReadOnlyMemory<byte> Value { get; private set; }

		public int DeclaredLength { get; }

		private byte[]? _buffer;

		private int _bytesWritten = 0;

		public int BytesMissing => IsComplete ? 0 : _buffer.Length - _bytesWritten;

		public bool IsComplete => _buffer == null;

		public RESPBulkString(ReadOnlyMemory<byte> data)
			: this(data, data.Length - 2) { }

		public RESPBulkString(ReadOnlyMemory<byte> data, int declaredLength)
		{
			DeclaredLength = declaredLength;
			var lengthWithCRLF = declaredLength + 2;

			if (data.Length < lengthWithCRLF)
			{
				_buffer = new byte[lengthWithCRLF];
				Append(data);
			}
			else
			{
				Freeze(data);
			}
		}

		public RESPBulkString(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory(), value.Length) { }

		public override ReadOnlyMemory<byte> Encode()
		{
			var encodedValue = Type.Qualifier() + DeclaredLength.ToString() + "\r\n"
				+ Encoding.UTF8.GetString(Value.Span) + "\r\n";

			return Encoding.UTF8.GetBytes(encodedValue).AsMemory();
		}

		public void Append(ReadOnlyMemory<byte> data)
		{
			if (IsComplete)
				throw new InvalidOperationException("Cannot append to completed Bulk String.");

			var lengthWithCRLF = DeclaredLength + 2;
			var remaining = lengthWithCRLF - _bytesWritten;
			var lengthToCopy = Math.Min(remaining, data.Length);

			data.Span.Slice(0, lengthToCopy).CopyTo(_buffer.AsSpan(_bytesWritten));
			_bytesWritten += lengthToCopy;

			if (_bytesWritten == lengthWithCRLF)
			{
				Freeze(_buffer);
				_buffer = null;
			}
		}

		private void Freeze(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			if (span[^2] != (byte)'\r' || span[^1] != (byte)'\n')
				throw new InvalidOperationException(
						$"Invalid CRLF after body, expect '\\r\\n', saw '{(char)span[^2]}{(char)span[^1]}'.");

			Value = data.Slice(0, DeclaredLength);
		}
	}
}
