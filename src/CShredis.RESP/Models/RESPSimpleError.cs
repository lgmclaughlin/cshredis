using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPSimpleError(ReadOnlyMemory<byte> Value) : RESPError
	{
		public override RESPType Type => RESPType.SimpleError;
		
		public RESPSimpleError(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory()) { }

		public override ReadOnlyMemory<byte> Encode()
		{
			var encodedValue = Type.Qualifier() + Encoding.UTF8.GetString(Value.Span) + "\r\n";

			return Encoding.UTF8.GetBytes(encodedValue).AsMemory();
		}
	}
}
