using System;
using System.Text;

namespace CShredis.RESP
{
	public record RESPObject
	{
		public RESPType Type { get; set; }
		public ReadOnlyMemory<byte> Value { get; set; }

		public string ValueString => Encoding.UTF8.GetString(Value.Span);
		public int Length => Value.Length;
		public bool IsError =>
			Type == RESPType.SimpleError ||
			Type == RESPType.BulkError;

		public RESPObject(RESPType type, string value)
			: this(type, Encoding.UTF8.GetBytes(value).AsMemory()) { }

		public RESPObject(RESPType type, ReadOnlyMemory<byte>? value = null)
		{
			Type = type;
			Value = value ?? ReadOnlyMemory<byte>.Empty;
		}

		public static RESPObject BulkError(ReadOnlyMemory<byte>? value = null)
			=> new(RESPType.BulkError, value);

		public static RESPObject BulkError(string value)
			=> new(RESPType.BulkError, value);

		public static RESPObject BulkString(ReadOnlyMemory<byte>? value = null)
			=> new(RESPType.BulkString, value);

		public static RESPObject BulkString(string value)
			=> new(RESPType.BulkString, value);

		public static RESPObject Integer(ReadOnlyMemory<byte>? value)
			=> new(RESPType.Integer, value);

		public static RESPObject Integer(string value)
			=> new(RESPType.Integer, value);

		public static RESPObject NullArray()
			=> new(RESPType.NullArray);

		public static RESPObject NullBulkString()
			=> new(RESPType.NullBulkString);

		public static RESPObject SimpleError(ReadOnlyMemory<byte>? value)
			=> new(RESPType.SimpleError, value);

		public static RESPObject SimpleError(string value)
			=> new(RESPType.SimpleError, value);

		public static RESPObject SimpleString(ReadOnlyMemory<byte>? value)
			=> new(RESPType.SimpleString, value);

		public static RESPObject SimpleString(string value)
			=> new(RESPType.SimpleString, value);
	}
}
