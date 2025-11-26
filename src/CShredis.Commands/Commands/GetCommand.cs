using System;
using CShredis.RESP;
using CShredis.Core;

namespace CShredis.Commands
{
	public class GetCommand : ICommand
	{
		private readonly RedisState _state;

		public GetCommand(RedisState state)
		{
			_state = state;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Length != 1)
				return CommandResult.SimpleError("ERR wrong number of arguments for command");
			
			var redisObject = _state.CurrentDb.Get(commandEnvelope.ByteArguments[0]);

			if (redisObject == null)
				return new CommandResult(RESPObject.NullBulkString());

			return new CommandResult(RESPObject.BulkString(redisObject.Value));
		}
	}
}
