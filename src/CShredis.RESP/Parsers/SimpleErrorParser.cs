using System.Text;

namespace CShredis.RESP
{
	public class SimpleErrorParser : IParser
	{
		public SimpleErrorParser() { }

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			_ = ParseUtilities.TryParseType(span, RESPType.SimpleError.Qualifier(), RESPType.SimpleError.Name());

			ParseUtilities.VerifyCRLF(span);

			var parsedValue = ReadOnlyMemory<byte>.Empty;

			int messageLength;
			var firstSpace = span.IndexOf((byte)' ');
			if (firstSpace > -1)
			{
				var messageBodyStart = firstSpace + 1;
				messageLength = data.Length - 2 - messageBodyStart; 
				parsedValue = data.Slice(messageBodyStart, messageLength);
			}
			else
			{
				messageLength = data.Length - 3;
				parsedValue = data.Slice(1, messageLength);
			}

			var bytesConsumed = data.Length;

			return ParseResult.Complete(new RESPSimpleError(parsedValue), bytesConsumed);
		}

	}
}
