using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CShredis.RESP;

namespace CShredis.Commands
{
	public class CommandDispatcher
	{
		private readonly Dictionary<string, ICommandHandler> _handlers;

		public CommandDispatcher()
		{
			_handlers = new Dictionary<string, ICommandHandler> (StringComparer.OrdinalIgnoreCase)
			{
				{ "PING", new PingCommandHandler() }
			};
		}

		public CommandResult Execute(RESPArray command)
		{
			if (command.Count == 0)
				throw new ArgumentException("No commands to execute.", nameof(command));

			if (command.Elements.Any(e => e.Type != RESPType.BulkString))
				throw new ArgumentException("Invalid command types. Bulk strings expected.", nameof(command));

			var commandBulkStrings = command.Elements.Cast<RESPBulkString>().ToList();
			var commandStrings = commandBulkStrings.Select(cbs => Encoding.UTF8.GetString(cbs.Value.Span)).ToList();
			var commandName = commandStrings[0].ToUpper();

			if (!_handlers.TryGetValue(commandName, out var handler))
				return new CommandResult(new RESPSimpleError($"ERR unknown command '{commandName}'"));

			var commandArgs = (commandStrings.Count > 1)
				? commandStrings.GetRange(1, commandStrings.Count - 1)
				: new List<string>();

			var commandEnvelope = new CommandEnvelope(commandName, commandArgs);

			return handler.Execute(commandEnvelope);
		}
	}
}
