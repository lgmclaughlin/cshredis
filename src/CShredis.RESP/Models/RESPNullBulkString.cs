using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPNullBulkString() : RESPObject
	{
		public override RESPType Type => RESPType.NullBulkString;

		public override ReadOnlyMemory<byte> Encode() => ReadOnlyMemory<byte>.Empty;
	}
}
