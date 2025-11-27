using CShredis.RESP;

namespace CShredis.Commands
{
	public sealed record CommandResult(RESPObject Result)
	{
		public bool IsError => Result.IsError;

		public static CommandResult BulkString(string message)
			=> new CommandResult(RESPObject.BulkString(message));

		public static CommandResult BulkString(ReadOnlyMemory<byte> message)
			=> new CommandResult(RESPObject.BulkString(message));

		public static CommandResult NullBulkString()
			=> new CommandResult(RESPObject.NullBulkString());

		public static CommandResult SimpleError(string message)
			=> new CommandResult(RESPObject.SimpleError(message));

		public static CommandResult SimpleString(string message)
			=> new CommandResult(RESPObject.SimpleString(message));
	}
}
