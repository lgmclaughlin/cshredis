using CShredis.RESP;

namespace CShredis.Commands
{
	public class PingCommandHandler : ICommandHandler
	{
		private SimpleStringEncodeHandler _simpleStringEncoder = new();

		public PingCommandHandler() { }

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			return new CommandResult(_simpleStringEncoder.Encode("PONG"));
		}
	}
}
