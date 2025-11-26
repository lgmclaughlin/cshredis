using System;
using System.Text;
using CShredis.Core.DbCommands;

namespace CShredis.Core
{
	public sealed class RedisDb
	{
		private BasicCommands _basicCommands = new();

		public Dictionary<ReadOnlyMemory<byte>, RedisObject> Db { get; } = new(new ByteMemoryComparer()); 
		public Dictionary<ReadOnlyMemory<byte>, long> Expiry { get; } = new(new ByteMemoryComparer());

		public RedisDb() { }

		public (bool Success, RedisObject? PreviousValue) Set(
				ReadOnlyMemory<byte> key,
				RedisObject value,
				SetOptions setOptions)
			=> _basicCommands.Set(this, key, value, setOptions);

		public void SetExpiry(ReadOnlyMemory<byte> key, long expires)
			=> _basicCommands.SetExpiry(this, key, expires);

		public RedisObject? Get(ReadOnlyMemory<byte> key)
			=> _basicCommands.Get(this, key);

		public void Delete(ReadOnlyMemory<byte> key)
			=> _basicCommands.Delete(this, key);
	}
}
