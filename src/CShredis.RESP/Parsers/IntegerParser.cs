using System.Text;

namespace CShredis.RESP
{
	public class IntegerParser : IParser
	{
		public IntegerParser() { }

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			RESPUtilities.TryParseType(span, RESPType.Integer.Qualifier(), RESPType.Integer.Name());

			var crlfIndex = RESPUtilities.GetCrlfIndex(data.Span);
			if (crlfIndex < 0)
				return ParseResult.Incomplete;

			var bytesConsumed = crlfIndex + 2; // add \n and correct for 0-index
			
			var parsedValue = data.Slice(1, crlfIndex - 1);
			var respObject = RESPObject.Integer(parsedValue);

			return new ParseResult(respObject, bytesConsumed, ParseStatus.Complete);
		}
	}
}
