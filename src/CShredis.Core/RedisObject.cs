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

		public List<RedisObject> LPop(long howMany)
		{
			ValidateList();
			
			// TODO: List capacity is max int, custom type later
			var removed = ((List<RedisObject>)_value).GetRange(0, (int)howMany);
			((List<RedisObject>)_value).RemoveRange(0, (int)howMany);

			return removed;
		}

		public List<RedisObject> RPop(long howMany)
		{
			ValidateList();

			// TODO: List capacity is max int, custom type later
			var start = (int)Count! - (int)howMany;
			var removed = ((List<RedisObject>)_value).GetRange(start, (int)howMany);
			removed.Reverse();
			((List<RedisObject>)_value).RemoveRange(start, (int)howMany);

			return removed;
		}

		private void ValidateList()
		{
			if (Type != RedisType.List)
				throw new InvalidCastException();
		}
	}
}
