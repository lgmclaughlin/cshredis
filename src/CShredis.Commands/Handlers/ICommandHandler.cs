using CShredis.RESP;

namespace CShredis.Commands
{
	public interface ICommandHandler
	{
		public CommandResult Execute(CommandEnvelope commandEnvelope);
	}
}
