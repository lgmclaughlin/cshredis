using CShredis.RESP;

namespace CShredis.Commands
{
	public sealed record CommandEnvelope(string Name, List<string> Arguments);
}
