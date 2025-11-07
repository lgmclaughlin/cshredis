using CShredis.RESP;

namespace CShredis.Commands
{
	public sealed record CommandResult(RESPObject Result)
	{
		public bool IsError => Result is RESPError;
	}
}
