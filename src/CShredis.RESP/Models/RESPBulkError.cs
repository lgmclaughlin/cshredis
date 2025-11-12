using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPBulkError : RESPError
	{
		private string? _value;

		private string ValueString =>
			(_value != null) ? _value : Encoding.UTF8.GetString(Value.Span);

		public ReadOnlyMemory<byte> Value { get; private set; }

		public int Length { get; private set; }

		public override RESPType Type => RESPType.BulkError;

		public RESPBulkError(ReadOnlyMemory<byte> value)
		{
			Value = value;
			Length = value.Length;
		}

		public RESPBulkError(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory())
		{
			_value = value;
		}

		public override ReadOnlyMemory<byte> Encode()
			=> Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString()
			=> $"{Type.QualifierString()}{Length.ToString()}\r\n{ValueString}\r\n";

		public override string Print() => ValueString;
	}
}
