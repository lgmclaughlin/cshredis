using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPInteger(ReadOnlyMemory<byte> Value) : RESPObject
	{
		public override RESPType Type => RESPType.Integer;

		public RESPInteger(long value) : this(BitConverter.GetBytes(value).AsMemory()) { }

		public override ReadOnlyMemory<byte> Encode() => ReadOnlyMemory<byte>.Empty;
	}
}
