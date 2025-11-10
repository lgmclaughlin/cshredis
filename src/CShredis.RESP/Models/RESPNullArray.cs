using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPNullArray() : RESPObject
	{
		public override RESPType Type => RESPType.NullArray;

		public override ReadOnlyMemory<byte> Encode() => ReadOnlyMemory<byte>.Empty;
	}
}
