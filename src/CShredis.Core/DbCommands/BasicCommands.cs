using System;

namespace CShredis.Core.DbCommands
{
	public class BasicCommands
	{
		public BasicCommands() { }

		public (DbResult Result, RedisObject? PreviousValue) Set(
				RedisDb db,
				ReadOnlyMemory<byte> key,
				RedisObject value,
				SetOptions setOptions)
		{
			RedisObject? previousValue = null;
			if (setOptions.GetPrevious)
			{
				var result = Get(db, key);

				if (result.Result != DbResult.Success)
					return (result.Result, null);

				previousValue = result.Value ?? RedisObject.Null;
			}

			db.Db[key] = value;

			if (setOptions.ExpiryMs.HasValue)
			{
				var expires = setOptions.ExpiryMs.Value + CurrentTimeMs();
				SetExpiry(db, key, expires);
			}
			else
			{
				_ = db.Expiry.Remove(key);
			}

			return (DbResult.Success, previousValue);
		}

		public void SetExpiry(RedisDb db, ReadOnlyMemory<byte> key, long expires)
			=> db.Expiry[key] = expires;

		public (DbResult Result, RedisObject? Value) Get(RedisDb db, ReadOnlyMemory<byte> key)
		{
			var redisObject = GetAny(db, key);
			
			if (redisObject is null)
				return (DbResult.Success, null);

			if (redisObject.Type != RedisType.String)
				return (DbResult.WrongType, null);

			return (DbResult.Success, redisObject);
		}

		public RedisObject? GetAny(RedisDb db, ReadOnlyMemory<byte> key)
		{
			if (db.Expiry.TryGetValue(key, out long expires))
				if (expires <= CurrentTimeMs())
					Delete(db, key);

			if (!db.Db.TryGetValue(key, out var redisObject))
				return null;

			return redisObject;
		}

		public void Delete(RedisDb db, ReadOnlyMemory<byte> key)
		{
			_ = db.Expiry.Remove(key);
			_ = db.Db.Remove(key);
		}

		private long CurrentTimeMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
