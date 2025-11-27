using System;
using CShredis.RESP;
using CShredis.Core;

namespace CShredis.Commands
{
	public class SetCommand : ICommand
	{
		private readonly RedisState _state;
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
				return CommandResult.SimpleError("ERR wrong number of arguments for command");

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

					var result = optionParser.Parser(setOptions, arg);
					if (!result.Success)
					{
						error = true;
						errorMessage = result.ErrorMessage;
						break;
					}
				}
			}

			if (error)
			{
				if (errorMessage is null)
					errorMessage = "syntax error";

				return CommandResult.SimpleError(errorMessage);
			}

			var redisObjectToStore = new RedisObject(RedisType.String, commandEnvelope.ByteArguments[1]);
			
			RedisObject? previousValue  = _state.CurrentDb.Set(commandEnvelope.ByteArguments[0], redisObjectToStore, setOptions);

			var commandResult = CommandResult.SimpleString("OK");
			if (previousValue is not null)
				commandResult = (previousValue.Type != RedisType.Null)
					? CommandResult.BulkString(previousValue.Value)
					: CommandResult.NullBulkString();

			return commandResult;
		}

		private static (bool Success, string? ErrorMessage) ParseEx(SetOptions options, string? ex)
		{
			if (ex is null || options.ExpiryMs.HasValue)
				return (false, null);

			if (!long.TryParse(ex, out long exLong))
				return (false, "value is not an integer or out of range");

			if (exLong < 1)
				return (false, "invalid expire time in 'set' command");

			options.ExpiryMs = exLong * 1000; // sec to ms
			return (true, null);
		}

		private static (bool Success, string? ErrorMessage) ParsePx(SetOptions options, string? px)
		{
			if (px is null || options.ExpiryMs.HasValue)
				return (false, null);

			if (!long.TryParse(px, out long pxLong))
				return (false, "value is not an integer or out of range");

			if (pxLong < 1)
				return (false, "invalid expire time in 'set' command");

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
