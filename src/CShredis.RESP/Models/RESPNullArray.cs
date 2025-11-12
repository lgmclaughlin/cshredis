using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPNullArray() : RESPObject
	{
		public override RESPType Type => RESPType.NullArray;

		public override ReadOnlyMemory<byte> Encode()
			=> Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString() => "$-1\r\n";

		public override string Print() => "(nil)";
	}
}
