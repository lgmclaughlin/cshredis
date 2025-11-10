using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPBulkError(ReadOnlyMemory<byte> Value) : RESPError
	{
		public override RESPType Type => RESPType.BulkError;

		public RESPBulkError(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory()) { }

		public override ReadOnlyMemory<byte> Encode() => ReadOnlyMemory<byte>.Empty;
	}
}
