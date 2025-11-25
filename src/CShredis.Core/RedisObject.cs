using System.Text;

namespace CShredis.Core
{
	public sealed record RedisObject(RedisType Type, ReadOnlyMemory<byte> Value)
	{
		public static readonly RedisObject Null = new(RedisType.Null, ReadOnlyMemory<byte>.Empty);
	}
}
