namespace CShredis.RESP
{
	public class BulkStringParseHandler : IParseHandler, IPartialParseHandler
	{
		public BulkStringParseHandler() { }

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			if (!Utilities.TryParseType(span, RESPType.BulkString.Qualifier(), RESPType.BulkString.Name()))
				return ParseResult.Incomplete;

			if (!Utilities.TryParseLength(span, out int declaredLength, out int lengthBytesConsumed))
				return ParseResult.Incomplete;

			if (declaredLength == -1)
				return ParseResult.Complete(new RESPNullBulkString(), lengthBytesConsumed);

			var lengthWithCRLF = declaredLength + 2;

			var bodyStart = lengthBytesConsumed;
			int bytesConsumed;

			if (span.Length < lengthBytesConsumed + lengthWithCRLF)
			{
				var partialBodyLength = span.Length - lengthBytesConsumed;
				var partialParsedValue = data.Slice(bodyStart, partialBodyLength);

				bytesConsumed = lengthBytesConsumed + partialBodyLength;

				return ParseResult.Partial(new RESPBulkString(partialParsedValue, declaredLength), bytesConsumed);
			}

			var parsedValue = data.Slice(bodyStart, lengthWithCRLF);
			bytesConsumed = lengthBytesConsumed + parsedValue.Length;

			return ParseResult.Complete(new RESPBulkString(parsedValue), bytesConsumed);
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, RESPObject partial)
		{
			var respBulkString = (RESPBulkString)partial;

			ReadOnlySpan<byte> span = data.Span;

			int bytesConsumed = Math.Min(respBulkString.BytesMissing, span.Length);

			var slice = data.Slice(0, bytesConsumed);

			respBulkString.Append(slice);

			if (respBulkString.IsComplete)
				return ParseResult.Complete(respBulkString, bytesConsumed);
			else
				return ParseResult.Partial(respBulkString, bytesConsumed);
		}
	}
}
