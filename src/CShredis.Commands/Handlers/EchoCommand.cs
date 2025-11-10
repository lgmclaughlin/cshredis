using CShredis.RESP;

namespace CShredis.Commands
{
	public class EchoCommand : ICommand
	{
		public EchoCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Count != 1)
				return new CommandResult(new RESPSimpleError("wrong number of arguments for command"));

			return new CommandResult(new RESPBulkString(commandEnvelope.Arguments[0]));
		}
	}
}
