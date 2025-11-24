using System.Text;

namespace CShredis.Core
{
	public sealed record RedisObject(RedisType Type, ReadOnlyMemory<byte> Value)
	{
		public static readonly RedisObject Null = new(RedisType.Null, ReadOnlyMemory<byte>.Empty);

		public RedisObject(RedisType type, string value)
			: this(type, Encoding.UTF8.GetBytes(value).AsMemory()) { }
	}
}
