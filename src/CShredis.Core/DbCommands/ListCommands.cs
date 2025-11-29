using System;

namespace CShredis.Core.DbCommands
{
	public class ListCommands
	{
		public ListCommands() { }

		public (DbResult Result, int? Length) LLen(
				RedisDb db,
				ReadOnlyMemory<byte> key)
		{
			RedisObject? value = db.GetAny(key);

			if (value is null)
				return (DbResult.Success, 0);

			if (value.Type != RedisType.List)
				return (DbResult.WrongType, null);

			return (DbResult.Success, value.Count);
		}

		public (DbResult Result, int? NewLength) LRPush(
				RedisDb db,
				ReadOnlyMemory<byte> key,
				RedisObject list,
				bool rPush)
		{
			var count = list.Count;
			var listElements = list.AsList();
			if (!rPush)
				listElements.Reverse();

			RedisObject? previousValue = db.GetAny(key);

			if (previousValue is null)
			{
				db.Set(key, list);
				return (DbResult.Success, count);
			}

			if (previousValue.Type != RedisType.List)
				return (DbResult.WrongType, null);

			if (rPush)
				previousValue.Append(listElements);
			else
				previousValue.Prepend(listElements);

			db.Set(key, previousValue);

			return (DbResult.Success, previousValue.Count);
		}

		public (DbResult Result, RedisObject? Value) LRange(
				RedisDb db,
				ReadOnlyMemory<byte> key,
				long left,
				long right)
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

		public (DbResult Result, RedisObject? RemovedValues) LRPop(
				RedisDb db,
				ReadOnlyMemory<byte> key,
				long howMany,
				bool rPop)
		{
			RedisObject? value = db.GetAny(key);

			if (value is null)
				return (DbResult.Success, null);

			if (value.Type != RedisType.List)
				return (DbResult.WrongType, null);

			var count = (int)value.Count!;
			howMany = Math.Min(howMany, count);

			var removed = (rPop)
				? value.RPop(howMany)
				: value.LPop(howMany);

			if (value.Count! > 0)
				db.Set(key, value);
			else
				db.Delete(key);

			return (DbResult.Success, new RedisObject(removed));
		}
	}
}
