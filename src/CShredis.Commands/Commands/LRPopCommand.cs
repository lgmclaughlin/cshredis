using System;
using CShredis.RESP;
using CShredis.Core;
using Utils = CShredis.Commands.CommandUtilities;

namespace CShredis.Commands
{
	public class LRPopCommand : ICommand
	{
		private readonly RedisState _state;
		private bool _rPop;

		public LRPopCommand(RedisState state, bool rPop)
		{
			_state = state;
			_rPop = rPop;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			var byteArguments = commandEnvelope.ByteArguments;
			var arguments = commandEnvelope.Arguments;
			var argCount = byteArguments.Length;
			if (argCount < 1 || argCount > 2)
				return CommandResult.SimpleError(ResponseMessages.Error_WrongNumberOfArguments);

			long howMany = 1;
			if (argCount == 2 && !long.TryParse(arguments[1], out howMany))
				return CommandResult.SimpleError(ResponseMessages.Error_InvalidInteger);

			(DbResult Result, RedisObject? RemovedValues) result =
				_state.CurrentDb.LRPop(byteArguments[0], howMany, _rPop);

			if (!Utils.ValidateDbResult(result.Result, out var commandResultError))
				return commandResultError;

			if (result.RemovedValues is null)
				return CommandResult.NullArray();

			var respObject = (howMany > 1)
				? Utils.RESPObjectFrom(result.RemovedValues!)
				: Utils.RESPObjectFrom(result.RemovedValues!.AsList().First());

			return new CommandResult(respObject);
		}
	}
}
