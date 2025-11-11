using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPSimpleError(ReadOnlyMemory<byte> Value) : RESPError
	{
		private string? _value;

		private string _prefix = "ERR";

		public string ValueString =>
			(_value != null) ? _value : Encoding.UTF8.GetString(Value.Span);

		public override RESPType Type => RESPType.SimpleError;
		
		public RESPSimpleError(string value)
			: this(Encoding.UTF8.GetBytes(value).AsMemory())
		{
			_value = value; 
			Console.WriteLine($"***** Simple Error Value: {_value}");
		}

		public override ReadOnlyMemory<byte> Encode() =>
			Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString() =>
			$"{Type.QualifierString()}{_prefix} {ValueString}\r\n";

		public override string Print() => $"(error) {_prefix} {ValueString}";
	}
}
