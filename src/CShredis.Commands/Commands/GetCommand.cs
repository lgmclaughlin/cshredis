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
			if (commandEnvelope.Arguments.Count != 1)
				return new CommandResult(new RESPObject(RESPType.SimpleError, "ERR wrong number of arguments for command"));
			
			var redisObject = _state.CurrentDb.Get(commandEnvelope.Arguments[0]);

			if (redisObject == null)
				return new CommandResult(RESPObject.NullBulkString());

			return new CommandResult(RESPObject.BulkString(redisObject.Value));
		}
	}
}
