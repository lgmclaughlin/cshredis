using CShredis.RESP;

namespace CShredis.Commands
{
	public class EchoCommand : ICommand
	{
		public EchoCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Length != 1)
				return CommandResult.SimpleError("ERR wrong number of arguments for command");

			return CommandResult.BulkString(commandEnvelope.Arguments[0]);
		}
	}
}
