namespace CShredis.RESP
{
	public interface IPartialParser
	{
		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, RESPObject partial);
	}
}
