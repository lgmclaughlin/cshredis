using System;
using CShredis.RESP;
using CShredis.Core;
using Utils = CShredis.Commands.CommandUtilities;

namespace CShredis.Commands
{
	public class SetCommand : ICommand
	{
		private readonly RedisState _state;
		private static readonly string _commandName = "SET";
		private readonly Dictionary<string, (bool NeedsInput, ParserDelegate Parser)> _optionParsers =
			new(StringComparer.OrdinalIgnoreCase)
			{
				{ "EX",  (true,  (ParserDelegate)ParseEx) },
				{ "PX",  (true,  (ParserDelegate)ParsePx) },
				{ "GET", (false, (ParserDelegate)ParseGet) }
			};

		private delegate (bool Success, string? ErrorMessage) ParserDelegate(SetOptions options, string? arg);

		public SetCommand(RedisState state)
		{
			_state = state;
		}

		public CommandResult Execute(CommandEnvelope commandEnvelope)
		{
			var arguments = commandEnvelope.Arguments;
			var argCount = arguments.Length;
			if (argCount < 2)
				return CommandResult.SimpleError(ResponseMessages.Error_WrongNumberOfArguments);

			var setOptions = new SetOptions();
			bool error = false;
			string? errorMessage = null;
			if (argCount > 2)
			{
				for (int i = 2; i < argCount; i++)
				{
					var option = arguments[i];
					if (!_optionParsers.TryGetValue(option, out var optionParser))
					{
						error = true;
						break;
					}

					string? arg = null;
					if (optionParser.NeedsInput)
					{
						if (++i == argCount)
						{
							error = true;
							break;
						}
						arg = arguments[i];
					}

					var argParseResult = optionParser.Parser(setOptions, arg);
					if (!argParseResult.Success)
					{
						error = true;
						errorMessage = argParseResult.ErrorMessage;
						break;
					}
				}
			}

			if (error)
			{
				if (errorMessage is null)
					errorMessage = ResponseMessages.Error_Syntax;

				return CommandResult.SimpleError(errorMessage);
			}

			var redisObjectToStore = new RedisObject(RedisType.String, commandEnvelope.ByteArguments[1]);
			
			var result = _state.CurrentDb.Set(commandEnvelope.ByteArguments[0], redisObjectToStore, setOptions);

			if (!Utils.ValidateDbResult(result.Result, out var commandResultError))
				return commandResultError;

			var commandResult = CommandResult.SimpleString(ResponseMessages.Success_OK);
			var previousValue = result.PreviousValue;
			if (previousValue is not null)
				commandResult = (previousValue.Type != RedisType.Null)
					? CommandResult.BulkString(previousValue.AsString())
					: CommandResult.NullBulkString();

			return commandResult;
		}

		private static (bool Success, string? ErrorMessage) ParseEx(SetOptions options, string? ex)
		{
			if (ex is null || options.ExpiryMs.HasValue)
				return (false, null);

			if (!long.TryParse(ex, out long exLong))
				return (false, ResponseMessages.Error_InvalidInteger);

			if (exLong < 1)
				return (false, ResponseMessages.Error_InvalidExpireTimeIn(_commandName));

			options.ExpiryMs = exLong * 1000; // sec to ms
			return (true, null);
		}

		private static (bool Success, string? ErrorMessage) ParsePx(SetOptions options, string? px)
		{
			if (px is null || options.ExpiryMs.HasValue)
				return (false, null);

			if (!long.TryParse(px, out long pxLong))
				return (false, ResponseMessages.Error_InvalidInteger);

			if (pxLong < 1)
				return (false, ResponseMessages.Error_InvalidExpireTimeIn(_commandName));

			options.ExpiryMs = pxLong;
			return (true, null);
		}

		private static (bool Success, string? ErrorMessage) ParseGet(SetOptions options, string? _)
		{
		    options.GetPrevious = true;
		    return (true, null);
		}
	}
}
