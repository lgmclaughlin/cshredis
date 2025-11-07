namespace CShredis.RESP
{
	public interface IParser
	{
		public ParseResult Parse(ReadOnlyMemory<byte> data);
	}
}
