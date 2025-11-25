using CShredis.RESP;

namespace CShredis.Commands
{
	public sealed record CommandEnvelope(string Name, string[] Arguments, ReadOnlyMemory<byte>[] ByteArguments);
}
