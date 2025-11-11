using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CShredis.RESP;

namespace CShredis.Commands
{
	public class CommandDispatcher
	{
		private readonly Dictionary<string, ICommand> _handlers;

		public CommandDispatcher()
		{
			_handlers = new Dictionary<string, ICommand> (StringComparer.OrdinalIgnoreCase)
			{
				{ "PING", new PingCommand() },
				{ "ECHO", new EchoCommand() }
			};
		}

		public CommandResult Execute(RESPObject command)
		{
			if (command.Type != RESPType.Array)
				throw new ArgumentException("Invalid command type. Array expected.", nameof(command));

			var commandArray = (RESPArray)command;

			if (commandArray.Count == 0)
				return new CommandResult(new RESPSimpleError("no command to execute"));

			if (commandArray.Elements.Any(e => e.Type != RESPType.BulkString))
				throw new ArgumentException("Invalid command types. Bulk strings expected.", nameof(command));

			var commandBulkStrings = commandArray.Elements.Cast<RESPBulkString>().ToList();
			var commandStrings = commandBulkStrings.Select(cbs => Encoding.UTF8.GetString(cbs.Value.Span)).ToList();
			var commandName = commandStrings[0];

			if (!_handlers.TryGetValue(commandName, out var handler))
				return new CommandResult(new RESPSimpleError($"unknown command '{commandName}'"));

			var commandArgs = (commandStrings.Count > 1)
				? commandStrings.GetRange(1, commandStrings.Count - 1)
				: new List<string>();

			var commandEnvelope = new CommandEnvelope(commandName.ToUpper(), commandArgs);

			return handler.Execute(commandEnvelope);
		}
	}
}
