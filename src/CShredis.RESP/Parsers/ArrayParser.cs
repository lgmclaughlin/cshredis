using System;
using System.Text;

namespace CShredis.RESP
{
	public class ArrayParser : IParser, IPartialParser
	{
		private readonly ParseDispatcher _dispatcher;

		public ArrayParser(ParseDispatcher dispatcher)
		{
			_dispatcher = dispatcher;
		}

		public ParseResult Parse(ReadOnlyMemory<byte> data)
		{
			ReadOnlySpan<byte> span = data.Span;

			if (!Utilities.TryParseType(span, RESPType.Array.Qualifier(), RESPType.Array.Name()))
				return ParseResult.Incomplete;

			if (!Utilities.TryParseLength(span, out int length, out int lengthBytesConsumed))
				return ParseResult.Incomplete;

			if (length == -1)
				return ParseResult.Complete(new RESPNullArray(), lengthBytesConsumed);

			var offset = lengthBytesConsumed;

			var respArray = new RESPArray(length);

			return ParseElements(data, respArray, offset);
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, RESPObject partial)
			=> ParseElements(data, (RESPArray)partial);

		private ParseResult ParseElements(ReadOnlyMemory<byte> data, RESPArray respArray, int offset = 0)
		{
			if (respArray.Partial != null)
			{
				var completedPartialParseResult = _dispatcher.ContinueParse(data, respArray.Partial);
				respArray.Add(completedPartialParseResult.ParsedObject);
				respArray.SetPartial(null);
				offset += completedPartialParseResult.BytesConsumed;
			}

			int elementsToParse = respArray.DeclaredLength - respArray.Count;

			for (int i = 0; i < elementsToParse; i++)
			{
				var slice = data.Slice(offset);
				var parseResult = _dispatcher.Parse(slice);

				if (parseResult.Status == ParseStatus.Incomplete)
					return ParseResult.Partial(respArray, offset);

				offset += parseResult.BytesConsumed;

				if (parseResult.Status == ParseStatus.Partial)
				{
					respArray.SetPartial(parseResult.ParsedObject);
					return ParseResult.Partial(respArray, offset);
				}

				respArray.Add(parseResult.ParsedObject);

				if (!respArray.IsComplete && offset >= data.Length)
					return ParseResult.Partial(respArray, offset);
			}

			return ParseResult.Complete(respArray, offset);
		}
	}
}
