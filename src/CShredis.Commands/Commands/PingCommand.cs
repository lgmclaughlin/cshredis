using CShredis.RESP;

namespace CShredis.Commands
{
	public class PingCommand : ICommand
	{
		public PingCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Length > 1)
				return CommandResult.SimpleError("wrong number of arguments for 'ping' command");

			if (commandEnvelope.Arguments.Length == 0)
				return CommandResult.SimpleString("PONG");

			return CommandResult.BulkString(commandEnvelope.Arguments[0]);
		}
	}
}
