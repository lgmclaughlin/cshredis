using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPSimpleError(ReadOnlyMemory<byte> Value) : RESPError
	{
		private string? _value;

		public string ValueString =>
			(_value != null) ? _value : Encoding.UTF8.GetString(Value.Span);

		public override RESPType Type => RESPType.SimpleError;
		
		public RESPSimpleError(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory())
		{
			_value = value;
		}

		public override ReadOnlyMemory<byte> Encode() =>
			Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString() =>
			Type.QualifierString() + "ERR " + ValueString + "\r\n";

		public override string Print() => ValueString;
	}
}
