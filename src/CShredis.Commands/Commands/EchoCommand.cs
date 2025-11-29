using CShredis.RESP;

namespace CShredis.Commands
{
	public class EchoCommand : ICommand
	{
		public EchoCommand() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Length != 1)
				return CommandResult.SimpleError(ResponseMessages.Error_WrongNumberOfArguments);

			return CommandResult.BulkString(commandEnvelope.Arguments[0]);
		}
	}
}
