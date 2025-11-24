using System.Text;

namespace CShredis.RESP
{
	public sealed class ParseResultBulkString : ParseResult
	{
		private byte[]? _buffer;
		private int _bytesWritten = 0;

		public int DeclaredLength { get; }

		public int TotalLength => DeclaredLength + 2;
		public int BytesMissing => IsComplete ? 0 : TotalLength - _bytesWritten;
		public bool IsComplete => _buffer is null;

		public ParseResultBulkString(
				ReadOnlyMemory<byte> data,
				int declaredLength,
				int lengthBytesConsumed
			)
			: base(RESPObject.BulkString(), 0, ParseStatus.Incomplete)
		{
			DeclaredLength = declaredLength;

			_buffer = new byte[TotalLength];
			if (data.Length < TotalLength)
			{
				data.Span.CopyTo(_buffer.AsSpan());
				_bytesWritten = data.Length;
			}
			else
			{
				data.Span.Slice(0, TotalLength).CopyTo(_buffer.AsSpan()); 
				_bytesWritten = TotalLength;
				Freeze();
			}

			BytesConsumed = lengthBytesConsumed + _bytesWritten;

			UpdateStatus();
		}

		public void Append(ReadOnlyMemory<byte> data)
		{
			if (IsComplete)
				throw new InvalidOperationException("Cannot append to completed Bulk String.");

			var remaining = TotalLength - _bytesWritten;
			var lengthToCopy = Math.Min(remaining, data.Length);
			data.Span.Slice(0, lengthToCopy).CopyTo(_buffer.AsSpan(_bytesWritten));

			BytesConsumed = lengthToCopy;
			_bytesWritten += lengthToCopy;

			if (_bytesWritten == TotalLength)
				Freeze();
			
			UpdateStatus();
		}

		private void Freeze()
		{
			if (_buffer is null)
				throw new InvalidOperationException("Freeze called on Bulk String after buffer was cleared.");

			RESPUtilities.VerifyCRLF(_buffer.AsSpan());

			var value = new ReadOnlyMemory<byte>(_buffer, 0, DeclaredLength);
			ParsedObject!.Value = value;

			_buffer = null;
		}

		private void UpdateStatus()
		{
			Status = (IsComplete)
				? ParseStatus.Complete
				: ParseStatus.Partial;
		}
	}
}
