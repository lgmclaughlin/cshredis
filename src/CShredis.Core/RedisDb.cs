using System;
using System.Text;
using CShredis.Core.DbCommands;

namespace CShredis.Core
{
	public sealed class RedisDb
	{
		private BasicCommands _basicCommands = new();
		private ListCommands _listCommands = new();

		public Dictionary<ReadOnlyMemory<byte>, RedisObject> Db { get; } = new(new ByteMemoryComparer()); 
		public Dictionary<ReadOnlyMemory<byte>, long> Expiry { get; } = new(new ByteMemoryComparer());

		public RedisDb() { }

		public (DbResult Result, RedisObject? PreviousValue) Set(
				ReadOnlyMemory<byte> key,
				RedisObject value)
			=> Set(key, value, new SetOptions());

		public (DbResult Result, RedisObject? PreviousValue) Set(
				ReadOnlyMemory<byte> key,
				RedisObject value,
				SetOptions setOptions)
			=> _basicCommands.Set(this, key, value, setOptions);

		public void SetExpiry(ReadOnlyMemory<byte> key, long expires)
			=> _basicCommands.SetExpiry(this, key, expires);

		public (DbResult Result, RedisObject? Value) Get(ReadOnlyMemory<byte> key)
			=> _basicCommands.Get(this, key);

		public RedisObject? GetAny(ReadOnlyMemory<byte> key)
			=> _basicCommands.GetAny(this, key);

		public void Delete(ReadOnlyMemory<byte> key)
			=> _basicCommands.Delete(this, key);

		public (DbResult Result, int? Length) LLen(ReadOnlyMemory<byte> key)
			=> _listCommands.LLen(this, key);

		public (DbResult Result, int? NewLength) LRPush(ReadOnlyMemory<byte> key, RedisObject list, bool rPush)
			=> _listCommands.LRPush(this, key, list, rPush);
		
		public (DbResult Result, RedisObject? Value) LRange(ReadOnlyMemory<byte> key, long left, long right)
			=> _listCommands.LRange(this, key, left, right);
	}
}
