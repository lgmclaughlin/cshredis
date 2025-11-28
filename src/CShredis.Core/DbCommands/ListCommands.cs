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
	}
}
