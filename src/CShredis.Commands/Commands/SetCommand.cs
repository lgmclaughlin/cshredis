using System;
using CShredis.RESP;
using CShredis.Core;

namespace CShredis.Commands
{
	public class SetCommand : ICommand
	{
		private readonly RedisState _state;

		public SetCommand(RedisState state)
		{
			_state = state;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			if (commandEnvelope.Arguments.Count != 2)
				return new CommandResult(new RESPObject(RESPType.SimpleError, "ERR wrong number of arguments for command"));

			var redisObjectToStore = new RedisObject(RedisType.String, commandEnvelope.Arguments[1]);
			
			_ = _state.CurrentDb.Set(commandEnvelope.Arguments[0], redisObjectToStore);

			return new CommandResult(new RESPObject(RESPType.SimpleString, "OK"));
		}
	}
}
