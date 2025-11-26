using System;

namespace CShredis.Core.DbCommands
{
	public class BasicCommands
	{
		public BasicCommands() { }

		public (bool Success, RedisObject? PreviousValue) Set(
				RedisDb db,
				ReadOnlyMemory<byte> key,
				RedisObject value,
				SetOptions setOptions)
		{
			RedisObject? previousValue = null;
			if (setOptions.GetPrevious)
				previousValue = Get(db, key);

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

			return (true, previousValue);
		}

		public void SetExpiry(RedisDb db, ReadOnlyMemory<byte> key, long expires)
			=> db.Expiry[key] = expires;

		public RedisObject? Get(RedisDb db, ReadOnlyMemory<byte> key)
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
