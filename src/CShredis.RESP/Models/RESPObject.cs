using System;
using System.Collections.Generic;
using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPObject
	{
		public RESPType Type { get; set; }
		public ReadOnlyMemory<byte> Value { get; set; }
		public List<RESPObject>? Elements { get; private set; }

		public string? ValueString => Encoding.UTF8.GetString(Value.Span);
		public int? Length => (Value.IsEmpty && Type == RESPType.BulkString) ? null : Value.Length;
		public int? Count => Elements?.Count;
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

		public RESPObject(RESPType type, List<RESPObject> elements)
		{
			Type = type;
			Elements = elements;
		}

		public void Add(RESPObject element)
        {
            if (Elements is null)
                throw new InvalidOperationException("Cannot add elements to a non-array RESP object.");

            Elements.Add(element);
        }

		public static RESPObject Array(int declaredLength)
			=> new(RESPType.Array, new List<RESPObject>(declaredLength));

		public static RESPObject Array(List<RESPObject> elements)
			=> new(RESPType.Array, elements);

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
