using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPInteger(ReadOnlyMemory<byte> Value) : RESPObject
	{
		public string ValueString => Encoding.UTF8.GetString(Value.Span);

		public override RESPType Type => RESPType.Integer;

		public RESPInteger(long value)
			: this(Encoding.UTF8.GetBytes(value.ToString()).AsMemory()) { }

		public override ReadOnlyMemory<byte> Encode()
			=> Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString() =>
			Type.QualifierString() + ValueString + "\r\n";

		public override string Print() => ValueString;
	}
}
