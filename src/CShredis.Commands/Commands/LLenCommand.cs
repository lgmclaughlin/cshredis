using System;
using CShredis.RESP;
using CShredis.Core;
using Utils = CShredis.Commands.CommandUtilities;

namespace CShredis.Commands
{
	public class LLenCommand : ICommand
	{
		private readonly RedisState _state;

		public LLenCommand(RedisState state)
		{
			_state = state;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			var byteArguments = commandEnvelope.ByteArguments;
			var argCount = byteArguments.Length;
			if (argCount != 1)
				return CommandResult.SimpleError(ResponseMessages.Error_WrongNumberOfArguments);

			(DbResult Result, int? Length) result =
				_state.CurrentDb.LLen(byteArguments[0]);

			if (!Utils.ValidateDbResult(result.Result, out var commandResultError))
				return commandResultError;

			return CommandResult.Integer(result.Length!.ToString());
		}
	}
}
