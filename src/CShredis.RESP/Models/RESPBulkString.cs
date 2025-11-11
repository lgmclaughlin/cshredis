using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPBulkString : RESPObject
	{
		private byte[]? _buffer;

		private int _bytesWritten = 0;

		private string? _value;

		public string ValueString =>
			(_value != null) ? _value : Encoding.UTF8.GetString(Value.Span);

		public ReadOnlyMemory<byte> Value { get; private set; }

		public override RESPType Type => RESPType.BulkString;

		public int DeclaredLength { get; }

		public int BytesMissing => IsComplete ? 0 : _buffer!.Length - _bytesWritten;

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
			: this(Encoding.UTF8.GetBytes(value).AsMemory(), value.Length)
		{
			_value = value;
		}

		public override ReadOnlyMemory<byte> Encode()
			=> Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString()
			=> $"{Type.QualifierString()}{DeclaredLength.ToString()}\r\n{ValueString}\r\n";

		public override string Print() => ValueString;

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
			ParseUtilities.VerifyCRLF(data.Span);

			Value = data.Slice(0, DeclaredLength);
		}
	}
}
