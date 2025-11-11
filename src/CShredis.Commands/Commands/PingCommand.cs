using CShredis.RESP;

namespace CShredis.Commands
{
	public class PingCommand : ICommand
	{
		public PingCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Count > 1)
				return new CommandResult(new RESPSimpleError("wrong number of arguments for command"));

			if (commandEnvelope.Arguments.Count == 0)
				return new CommandResult(new RESPSimpleString("PONG"));

			return new CommandResult(new RESPBulkString(commandEnvelope.Arguments[0]));
		}
	}
}
