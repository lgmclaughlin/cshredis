using CShredis.RESP;

namespace CShredis.Commands
{
	public class PingCommand : ICommand
	{
		public PingCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Length > 1)
				return new CommandResult(new RESPObject(RESPType.SimpleError, "wrong number of arguments for command"));

			if (commandEnvelope.Arguments.Length == 0)
				return new CommandResult(new RESPObject(RESPType.SimpleString, "PONG"));

			return new CommandResult(new RESPObject(RESPType.BulkString, commandEnvelope.Arguments[0]));
		}
	}
}
