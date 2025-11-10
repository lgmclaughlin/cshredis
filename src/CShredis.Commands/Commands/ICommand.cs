using CShredis.RESP;

namespace CShredis.Commands
{
	public interface ICommand
	{
		public CommandResult Execute(CommandEnvelope commandEnvelope);
	}
}
