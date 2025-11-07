namespace CShredis.RESP
{
	public interface IParseHandler
	{
		public ParseResult Parse(ReadOnlyMemory<byte> data);
	}
}
