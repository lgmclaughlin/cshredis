using System;
using System.Text;

namespace CShredis.RESP
{
	public class ParseDispatcher
	{
		private readonly Dictionary<byte, IParser> _parsers; 
		private ParseResult? _partial;

		public ParseDispatcher()
		{
			_parsers = new Dictionary<byte, IParser>()
			{
				{ RESPType.Array.Qualifier(), new ArrayParser(this) },
				//{ RESPType.BulkError.Qualifier(), new BulkErrorParser() },
				{ RESPType.BulkString.Qualifier(), new BulkStringParser() },
				{ RESPType.Integer.Qualifier(), new IntegerParser() },
				//{ RESPType.NullArray.Qualifier(), new NullArrayParser() },
				//{ RESPType.NullBulkString.Qualifier(), new NullBulkStringParser() },
				{ RESPType.SimpleError.Qualifier(), new SimpleErrorParser() },
				{ RESPType.SimpleString.Qualifier(), new SimpleStringParser() }
			};
		}

		public List<RESPObject> ParseStream(ReadOnlyMemory<byte> data)
		{
			if (data.IsEmpty)
				throw new ArgumentException("Data cannot be empty.", nameof(data));

			var offset = 0;
			var parsedObjects = new List<RESPObject>();

			while (offset < data.Length)
			{
				var slice = data.Slice(offset);
				var parseResult = (_partial is null)
					? Parse(slice)
					: ContinueParse(slice, _partial);

				if (parseResult.Status == ParseStatus.Incomplete)
					break;

				offset += parseResult.BytesConsumed;

				if (parseResult.Status == ParseStatus.Partial)
				{
					_partial = parseResult;
					break;
				}

				parsedObjects.Add(parseResult.ParsedObject!);
				_partial = null;
			}

			return parsedObjects;
		}

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			byte type = data.Span[0];

			if (!_parsers.TryGetValue(type, out var parser))
				throw new InvalidOperationException($"Unknown RESP type '{(char)type}'");

			var parseResult = parser.Parse(data);

			if (parseResult.Status == ParseStatus.Partial)
				_partial = parseResult;
			
			return parseResult;
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, ParseResult partial)
		{
			var parser = (IPartialParser)_parsers[partial.ParsedObject!.Type.Qualifier()];

			var parseResult = parser.ContinueParse(data, partial);

			_partial = (parseResult.Status == ParseStatus.Partial)
				? parseResult
				: null;

			return parseResult;
		}
	}
}
