using CShredis.RESP;

namespace CShredis.Commands
{
	public sealed record CommandResult(RESPObject Result)
	{
		public bool IsError => Result.IsError;

		public static CommandResult BulkString(string message)
			=> new CommandResult(new RESPObject(RESPType.BulkString, message));

		public static CommandResult SimpleError(string message)
			=> new CommandResult(new RESPObject(RESPType.SimpleError, message));

		public static CommandResult SimpleString(string message)
			=> new CommandResult(new RESPObject(RESPType.SimpleString, message));
	}
}
