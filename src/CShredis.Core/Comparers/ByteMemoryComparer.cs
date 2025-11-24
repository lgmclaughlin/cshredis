namespace CShredis.Core
{
	public class ByteMemoryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
	{
		public bool Equals(ReadOnlyMemory<byte> a, ReadOnlyMemory<byte> b)
			=> a.Span.SequenceEqual(b.Span);

		public int GetHashCode(ReadOnlyMemory<byte> bytes)
		{
			unchecked
			{
				int hash = 17;
				foreach (var b in bytes.Span)
					hash = hash * 31 + b;

				return hash;
			}
		}
	}
}
