using System.Text;

namespace CShredis.Core
{
	public sealed record RedisObject
	{
		private readonly object _value;

		public RedisType Type { get; }

		public static readonly RedisObject Null = new(RedisType.Null, null);
		public int? Count
			=> (Type == RedisType.List) ? ((List<RedisObject>)_value).Count : null;

		public RedisObject(RedisType type, ReadOnlyMemory<byte> value)
		{
			Type = type;
			_value = value;
		}

		public RedisObject(ReadOnlyMemory<byte> value)
		{
			Type = RedisType.String;
			_value = value;
		}

		public RedisObject(List<RedisObject> elements)
		{
			Type = RedisType.List;
			_value = elements;
		}

		public ReadOnlyMemory<byte> AsString()
		{
			if (Type != RedisType.String)
				throw new InvalidCastException();

			return (ReadOnlyMemory<byte>)_value;
		}

		public List<RedisObject> AsList()
		{
			ValidateList();
			return (List<RedisObject>)_value;
		}

		public void Append(List<RedisObject> elements)
		{
			ValidateList();
			((List<RedisObject>)_value).AddRange(elements);
		}

		public void Prepend(List<RedisObject> elements)
		{
			ValidateList();
			((List<RedisObject>)_value).InsertRange(0, elements);
		}

		private void ValidateList()
		{
			if (Type != RedisType.List)
				throw new InvalidCastException();
		}
	}
}
