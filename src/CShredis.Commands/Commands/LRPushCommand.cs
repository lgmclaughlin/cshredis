using System;
using CShredis.RESP;
using CShredis.Core;
using Utils = CShredis.Commands.CommandUtilities;

namespace CShredis.Commands
{
	public class LRPushCommand : ICommand
	{
		private readonly RedisState _state;
		private bool _rPush;

		public LRPushCommand(RedisState state, bool rPush)
		{
			_state = state;
			_rPush = rPush;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			var byteArguments = commandEnvelope.ByteArguments;
			var argCount = byteArguments.Length;
			if (argCount < 2)
				return CommandResult.SimpleError(ResponseMessages.Error_WrongNumberOfArguments);

			var elements = new List<RedisObject>(argCount - 1); // ignore key
			for (int i = 1; i < argCount; i++)
				elements.Add(new RedisObject(byteArguments[i]));

			var redisObjectToStore = new RedisObject(elements);
			
			(DbResult Result, int? NewLength) result =
				_state.CurrentDb.LRPush(commandEnvelope.ByteArguments[0], redisObjectToStore, _rPush);

			if (!Utils.ValidateDbResult(result.Result, out var commandResultError))
				return commandResultError;

			return CommandResult.Integer(result.NewLength!.ToString());
		}
	}
}
