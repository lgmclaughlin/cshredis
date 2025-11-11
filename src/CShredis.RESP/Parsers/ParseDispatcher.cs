using System;
using System.Text;

namespace CShredis.RESP
{
	public class ParseDispatcher
	{
		private readonly Dictionary<byte, IParser> _parsers; 

		private RESPObject? _partial;

		public ParseDispatcher()
		{
			_parsers = new Dictionary<byte, IParser> ()
			{
				{ RESPType.Array.Qualifier(), new ArrayParser(this) },
				//{ RESPType.BulkError.Qualifier(), new BulkErrorParser() },
				{ RESPType.SimpleString.Qualifier(), new SimpleStringParser() },
				{ RESPType.BulkString.Qualifier(), new BulkStringParser() }
				//{ RESPType.Integer.Qualifier(), new IntegerParser() },
				//{ RESPType.NullArray.Qualifier(), new NullArrayParser() },
				//{ RESPType.NullBulkString.Qualifier(), new NullBulkStringParser() },
				//{ RESPType.SimpleError.Qualifier(), new SimpleErrorParser() },
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

			if (!_parsers.TryGetValue(type, out var parser))
				throw new InvalidOperationException($"Unknown RESP type '{(char)type}'");

			var parseResult = parser.Parse(data);

			if (parseResult.Status == ParseStatus.Partial)
				_partial = parseResult.ParsedObject;
			
			return parseResult;
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, RESPObject partial)
		{
			_partial = null;

			var parser = (IPartialParser)_parsers[partial.Type.Qualifier()];

			var parseResult = parser.ContinueParse(data, partial);

			if (parseResult.Status == ParseStatus.Partial)
				_partial = parseResult.ParsedObject;

			return parseResult;
		}
	}
}
