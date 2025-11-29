using System;

namespace CShredis.Core.DbCommands
{
	public class ListCommands
	{
		public ListCommands() { }

		public (DbResult Result, int? NewLength) RPush(RedisDb db, ReadOnlyMemory<byte> key, RedisObject list)
		{
			var count = list.Count;
			RedisObject? previousValue = db.GetAny(key);

			if (previousValue is null)
			{
				db.Set(key, list);
				return (DbResult.Success, count);
			}

			if (previousValue.Type != RedisType.List)
				return (DbResult.WrongType, null);

			previousValue.Append(list.AsList());
			db.Set(key, previousValue);

			return (DbResult.Success, previousValue.Count);
		}

		public (DbResult Result, RedisObject? Value) LRange(RedisDb db, ReadOnlyMemory<byte> key, long left, long right)
		{
			RedisObject? currentValue = db.GetAny(key);

			if (currentValue is null)
				return (DbResult.Success, new RedisObject(new List<RedisObject>()));

			if (currentValue.Type != RedisType.List)
				return (DbResult.WrongType, null);

			var list = currentValue.AsList();
			var count = list.Count;

			if (left < 0) left += count;
			if (right < 0) right += count;

			if (left < 0) left = 0;
			if (right > count - 1) right = count - 1;

			if (left > right || right >= count)
				return (DbResult.Success, new RedisObject(new List<RedisObject>()));

			var rangeLength = right - left + 1;
			var range = list.GetRange((int)left, (int)rangeLength); // TODO: List<T> doesn't hold past int capacity

			return (DbResult.Success, new RedisObject(range));
		}
	}
}
