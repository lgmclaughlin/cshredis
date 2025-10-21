namespace CShredis.RESP
{
	public interface IPartialParseHandler
	{
		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, RESPObject partial);
	}
}
