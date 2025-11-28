using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CShredis.RESP;
using CShredis.Core;

namespace CShredis.Commands
{
	public class CommandDispatcher
	{
		private readonly RedisState _state;
		private readonly Dictionary<string, ICommand> _handlers;

		public CommandDispatcher(RedisState state)
		{
			_state = state;
			_handlers = new Dictionary<string, ICommand> (StringComparer.OrdinalIgnoreCase)
			{
				{ "PING",  new PingCommand()        },
				{ "ECHO",  new EchoCommand()        },
				{ "GET",   new GetCommand(_state)   },
				{ "SET",   new SetCommand(_state)   },
				{ "RPUSH", new RPushCommand(_state) }
			};
		}

		public CommandResult Execute(RESPObject command)
		{
			if (command.Type != RESPType.Array || command.Elements is null)
				throw new ArgumentException("Invalid command type. Array expected.", nameof(command));

			int count = command.Elements.Count;
			if (count == 0)
				return CommandResult.SimpleError("no command to execute");

			if (command.Elements.Any(e => e.Type != RESPType.BulkString))
				throw new ArgumentException("Invalid command types. Bulk strings expected.", nameof(command));

			var commandName = Encoding.ASCII.GetString(command.Elements[0].Value.Span);

			var argStrings = new string[count - 1];
			var argBytes = new ReadOnlyMemory<byte>[count - 1];
			for (int i = 0; i < count - 1; i++)
			{
				argStrings[i] = command.Elements[i + 1].ValueString;
				argBytes[i] = command.Elements[i + 1].Value;
			}

			if (!_handlers.TryGetValue(commandName, out var handler))
				return CommandResult.SimpleError($"unknown command '{commandName}'");

			var commandEnvelope = new CommandEnvelope(commandName.ToUpper(), argStrings, argBytes);

			return handler.Execute(commandEnvelope);
		}
	}
}
