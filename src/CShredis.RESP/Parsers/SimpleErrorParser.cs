using System.Text;

namespace CShredis.RESP
{
	public class SimpleErrorParser : IParser
	{
		public SimpleErrorParser() { }

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			RESPUtilities.TryParseType(span, RESPType.SimpleError.Qualifier(), RESPType.SimpleError.Name());

			var crlfIndex = RESPUtilities.GetCrlfIndex(span);
			if (crlfIndex < 0)
				return ParseResult.Incomplete;

			var parsedValue = data.Slice(1, crlfIndex - 1);
			var respObject = RESPObject.SimpleError(parsedValue);

			var bytesConsumed = crlfIndex + 2; // add \n and correct for 0-index

			return new ParseResult(respObject, bytesConsumed, ParseStatus.Complete);
		}
	}
}
