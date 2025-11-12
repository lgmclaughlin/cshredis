using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPNullBulkString() : RESPObject
	{
		public override RESPType Type => RESPType.NullBulkString;

		public override ReadOnlyMemory<byte> Encode()
			=> Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString() => "$-1\r\n";

		public override string Print() => "(nil)";
	}
}
