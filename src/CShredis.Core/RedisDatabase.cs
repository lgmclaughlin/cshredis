using System;
using System.Text;

namespace CShredis.Core
{
	public sealed class RedisDatabase
	{
		private Dictionary<string, RedisObject> _db = new(); 

		public RedisDatabase() { }

		public (bool Success, RedisObject? PreviousValue) Set(
				string key,
				RedisObject value,
				long? ttlMs = null,
				bool nx = false,
				bool xx = false,
				bool keepTtl = false,
				bool get = false)
		{
			// TODO
			//    Check expiry, remove if needed
			//    Eval NX/XX
			//    Retrieve old value if 'get'
			//    Update _db and expiry depending on ttlMs and keepTtl

			_db[key] = value;

			return (true, null);
		}

		public RedisObject? Get(string key)
		{
			// TODO check expiry, remove if needed

			if (!_db.TryGetValue(key, out var redisObject))
				return null;

			return redisObject;
		}

	}
}
