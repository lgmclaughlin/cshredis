using System.Text;

namespace CShredis.RESP
{
	public abstract record RESPObject
	{
		public abstract RESPType Type { get; }

		public abstract ReadOnlyMemory<byte> Encode();
	}

	public abstract record RESPError : RESPObject;
}
