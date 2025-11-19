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

			if (!RESPUtilities.TryParseType(span, RESPType.Array.Qualifier(), RESPType.Array.Name()))
				return ParseResult.Incomplete;

			if (!RESPUtilities.TryParseLength(span, out int length, out int lengthBytesConsumed))
				return ParseResult.Incomplete;

			if (length == -1)
			{
				var respObject = new RESPObject(RESPType.NullArray);
				return new ParseResult(RESPObject.NullArray(), lengthBytesConsumed);
			}

			var offset = lengthBytesConsumed;
			var parseResultArray = new ParseResultArray(length);

			return ParseElements(data, parseResultArray, offset);
		}

		public ParseResult ContinueParse(ReadOnlyMemory<byte> data, ParseResult partial)
			=> ParseElements(data, partial);

		private ParseResultArray ParseElements(ReadOnlyMemory<byte> data, ParseResult parseResult, int offset = 0)
		{
			var parseResultArray = (ParseResultArray)parseResult;
			int elementsToParse = parseResultArray.DeclaredLength - parseResultArray.Count;
			
			for (int i = 0; i < elementsToParse; i++)
			{
				if (offset >= data.Length)
					break;

				var slice = data.Slice(offset);
				ParseResult elParseResult;
				if (parseResultArray.Partial != null)
				{
					elParseResult = _dispatcher.ContinueParse(slice, parseResultArray.Partial);
					parseResultArray.SetPartial(null);
				}
				else
				{
					elParseResult = _dispatcher.Parse(slice);
				}

				if (elParseResult.Status != ParseStatus.Incomplete)
					offset += elParseResult.BytesConsumed;

				if (elParseResult.Status != ParseStatus.Complete)
				{
					parseResultArray.SetPartial(elParseResult);
					break;
				}

				parseResultArray.Add(elParseResult.ParsedObject!);
			}

			parseResultArray.BytesConsumed = offset;

			return parseResultArray;
		}
	}
}
