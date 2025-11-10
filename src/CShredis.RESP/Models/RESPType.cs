using System.Text;

namespace CShredis.RESP
{
	public enum RESPType
	{
		SimpleString,
		BulkString,
		NullBulkString,
		Integer,
		Array,
		NullArray,
		SimpleError,
		BulkError
	}

	public static class RESPTypeExtension
	{
		private static readonly string[] _names = Enum.GetNames(typeof(RESPType));

		public static string Name(this RESPType type) => _names[(int)type];

		public static string QualifierString(this RESPType type) => ((char)type.Qualifier()).ToString();

		public static byte Qualifier(this RESPType type) => type switch
		{
			RESPType.SimpleString => (byte)'+',
			RESPType.BulkString   => (byte)'$',
			RESPType.Integer      => (byte)':',
			RESPType.Array        => (byte)'*',
			RESPType.SimpleError  => (byte)'-',
			RESPType.BulkError    => (byte)'!',
			_ => throw new InvalidOperationException("Unknown RESPType: {type}")
		};

		public static RESPType FromQualifier(byte qualifier) => qualifier switch
		{
			(byte)'+' => RESPType.SimpleString,
			(byte)'$' => RESPType.BulkString,
			(byte)':' => RESPType.Integer,
			(byte)'*' => RESPType.Array,
			(byte)'-' => RESPType.SimpleError,
			(byte)'!' => RESPType.BulkError,
			_ => throw new InvalidOperationException("Unknown qualifier: {qualifier}")
		};
	}
}
