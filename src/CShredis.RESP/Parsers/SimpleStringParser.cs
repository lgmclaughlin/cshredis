using System.Text;

namespace CShredis.RESP
{
	public class SimpleStringParser : IParser
	{
		public SimpleStringParser() { }

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			_ = ParseUtilities.TryParseType(span, RESPType.SimpleString.Qualifier(), RESPType.SimpleString.Name());

			ParseUtilities.VerifyCRLF(span);

			var bytesConsumed = data.Length - 3;
			var parsedValue = data.Slice(1, bytesConsumed);

			return ParseResult.Complete(new RESPSimpleString(parsedValue), bytesConsumed);
		}

	}
}
