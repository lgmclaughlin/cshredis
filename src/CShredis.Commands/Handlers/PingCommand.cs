using CShredis.RESP;

namespace CShredis.Commands
{
	public class PingCommand : ICommand
	{
		public PingCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			return new CommandResult(new RESPSimpleString("PONG"));
		}
	}
}
