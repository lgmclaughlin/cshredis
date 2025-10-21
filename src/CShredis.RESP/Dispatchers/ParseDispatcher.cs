using System;
using System.Text;

namespace CShredis.RESP
{
	public class ParseDispatcher
	{
		private readonly Dictionary<byte, IParseHandler> _handlers; 

		private RESPObject? _partial;

		public ParseDispatcher()
		{
			_handlers = new Dictionary<byte, IParseHandler> ()
			{
				{ RESPType.Array.Qualifier(), new ArrayParseHandler(this) },
				{ RESPType.BulkString.Qualifier(), new BulkStringParseHandler() }
			};
		}

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			if (data.IsEmpty)
				throw new ArgumentException("Data cannot be null or empty.", nameof(data));

			if (_partial != null)
			{
				return ContinueParse(data, _partial);
			}
			else
			{
				return ParseNew(data);
			}
		}

		private ParseResult ParseNew(ReadOnlyMemory<byte> data)
		{
			byte type = data.Span[0];

			if (!_handlers.TryGetValue(type, out var handler))
				throw new InvalidOperationException($"Unknown RESP type '{(char)type}'");

			var parseResult = handler.Parse(data);

			if (parseResult.Status == ParseStatus.Partial)
				_partial = parseResult.ParsedObject;
			
			return parseResult;
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, RESPObject partial)
		{
			_partial = null;

			var handler = (IPartialParseHandler)_handlers[partial.Type.Qualifier()];

			var parseResult = handler.ContinueParse(data, partial);

			if (parseResult.Status == ParseStatus.Partial)
				_partial = parseResult.ParsedObject;

			return parseResult;
		}
	}
}
