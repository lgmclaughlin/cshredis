using System;
using CShredis.RESP;
using CShredis.Core;
using Utils = CShredis.Commands.CommandUtilities;

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
			
			var result = _state.CurrentDb.Get(commandEnvelope.ByteArguments[0]);

			if (!Utils.ValidateDbResult(result.Result, out var commandResultError))
				return commandResultError;

			if (result.Value is null)
				return new CommandResult(RESPObject.NullBulkString());

			var respObject = Utils.RESPObjectFrom(result.Value);

			return new CommandResult(respObject);
		}
	}
}
