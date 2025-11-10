using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPSimpleString(ReadOnlyMemory<byte> Value) : RESPObject
	{
		public override RESPType Type => RESPType.SimpleString;

		public RESPSimpleString(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory()) { }

		public override ReadOnlyMemory<byte> Encode()
		{
			var encodedValue = Type.Qualifier() + Encoding.UTF8.GetString(Value.Span) + "\r\n";

			return Encoding.UTF8.GetBytes(encodedValue).AsMemory();
		}
	}
}
