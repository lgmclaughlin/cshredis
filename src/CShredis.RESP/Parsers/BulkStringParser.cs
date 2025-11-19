namespace CShredis.RESP
{
	public class BulkStringParser : IParser, IPartialParser
	{
		public BulkStringParser() { }

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			if (!RESPUtilities.TryParseType(span, RESPType.BulkString.Qualifier(), RESPType.BulkString.Name()))
				return ParseResult.Incomplete;

			if (!RESPUtilities.TryParseLength(span, out int declaredLength, out int lengthBytesConsumed))
				return ParseResult.Incomplete;

			if (declaredLength == -1)
				return new ParseResult(RESPObject.NullBulkString(), lengthBytesConsumed);

			var declaredLengthAndCrlf = declaredLength + 2;

			var bodyStart = lengthBytesConsumed;
			int bytesConsumed;

			if (span.Length < lengthBytesConsumed + declaredLengthAndCrlf)
			{
				var partialBodyLength = span.Length - lengthBytesConsumed;
				var partialParsedValue = data.Slice(bodyStart, partialBodyLength);

				bytesConsumed = lengthBytesConsumed + partialBodyLength;

				return new ParseResultBulkString(partialParsedValue, declaredLength, lengthBytesConsumed);
			}

			var parsedValue = data.Slice(bodyStart, declaredLengthAndCrlf);
			bytesConsumed = lengthBytesConsumed + parsedValue.Length;

			return new ParseResultBulkString(parsedValue, declaredLength, lengthBytesConsumed);
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, ParseResult parseResult)
		{
			var partialParseResultBulkString = (ParseResultBulkString)parseResult;
			ReadOnlySpan<byte> span = data.Span;

			int lengthToAppend = Math.Min(partialParseResultBulkString.BytesMissing, span.Length);
			var slice = data.Slice(0, lengthToAppend);

			partialParseResultBulkString.Append(slice);

			return partialParseResultBulkString;
		}
	}
}
