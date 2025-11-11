using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPBulkError(ReadOnlyMemory<byte> Value) : RESPError
	{
		private string? _value;

		private string ValueString =>
			(_value != null) ? _value : Encoding.UTF8.GetString(Value.Span);

		public override RESPType Type => RESPType.BulkError;

		public RESPBulkError(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory())
		{
			_value = value;
		}

		public override ReadOnlyMemory<byte> Encode()
			=> Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString() => "";

		public override string Print() => ValueString;
	}
}
