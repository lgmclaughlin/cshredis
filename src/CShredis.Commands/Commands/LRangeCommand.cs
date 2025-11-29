using System;
using CShredis.RESP;
using CShredis.Core;
using Utils = CShredis.Commands.CommandUtilities;

namespace CShredis.Commands
{
	public class LRangeCommand : ICommand
	{
		private readonly RedisState _state;

		public LRangeCommand(RedisState state)
		{
			_state = state;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			var arguments = commandEnvelope.Arguments;
			var byteArguments = commandEnvelope.ByteArguments;
			var argCount = byteArguments.Length;
			if (argCount < 3)
				return CommandResult.SimpleError(ResponseMessages.Error_WrongNumberOfArguments);
			
			long left = -1, right = -1;
			if (!long.TryParse(arguments[1], out left) ||
				!long.TryParse(arguments[2], out right))
				CommandResult.SimpleError(ResponseMessages.Error_InvalidInteger);

			(DbResult Result, RedisObject? Value) result =
				_state.CurrentDb.LRange(commandEnvelope.ByteArguments[0], left, right);

			if (!Utils.ValidateDbResult(result.Result, out var commandResultError))
				return commandResultError;

			return CommandResult.Array(Utils.RESPObjectFrom(result.Value!).Elements);
		}
	}
}
