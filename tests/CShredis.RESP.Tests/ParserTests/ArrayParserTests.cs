using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.RESPTestUtilities;

namespace CShredis.RESP.Tests
{
	public class ArrayParserTests
	{
		private ParseDispatcher _dispatcher;
		private ArrayParser _parser;

		public ArrayParserTests()
		{
			_dispatcher = new();
			_parser = new(_dispatcher);
		}

		[Theory]
		[InlineData("*1\r\n$3\r\nhey\r\n", 1)]
		[InlineData("*2\r\n$3\r\nhey\r\n$5\r\nthere\r\n", 2)]
		[InlineData("*6\r\n$1\r\na\r\n$1\r\nb\r\n$1\r\nc\r\n$1\r\nd\r\n$1\r\ne\r\n$1\r\nf\r\n", 6)]
		public void ValidArray_ReturnsArray(string input, int expectedCount)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;
			
			var parseResultArray = (ParseResultArray)_parser.Parse(data);

			var parsedArray = Assert.IsType<RESPArray>(parseResultArray.ParsedObject);
			Assert.Equal(expectedBytesConsumed, parseResultArray.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parseResultArray.Status);
		}

		[Fact]
		public void ValidNullArray_ReturnsNullArray()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("*-1\r\n");
			var expectedType = RESPType.NullArray;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parseResult = _parser.Parse(data);
			var parsedNullArray = parseResult.ParsedObject!;

			Assert.Equal(expectedType, parsedNullArray.Type);
			Assert.Equal(expectedBytesConsumed, parseResult.BytesConsumed);
			Assert.Equal(expectedStatus, parseResult.Status);
		}

		[Fact]
		public void PartialArrayWithCompletingMessage_ReturnsCompleteArray()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("*3\r\n$3\r\nhey\r\n$5\r\nth");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedCount = 1;

			var partialParseResultArray = (ParseResultArray)_parser.Parse(dataPartial);

			var parsedPartialArray = Assert.IsType<RESPArray>(partialParseResultArray.ParsedObject);
			Assert.False(partialParseResultArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultArray.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray.Count);
			Assert.Equal(expectedStatus, partialParseResultArray.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("ere\r\n$4\r\nlove\r\n");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedCount = 3;

			var parseResultArray = (ParseResultArray)_parser.ContinueParse(dataFinal, partialParseResultArray);

			var parsedArray = Assert.IsType<RESPArray>(parseResultArray.ParsedObject);
			Assert.True(parseResultArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parseResultArray.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parseResultArray.Status);
		}

		[Fact]
		public void PartialArrayWithMultipleCompletingMessages_ReturnsCompleteArray()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("*3\r\n$3\r\nhey\r");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedCount = 0;

			var partialParseResultArray1 = (ParseResultArray)_parser.Parse(dataPartial);

			var parsedPartialArray1 = Assert.IsType<RESPArray>(partialParseResultArray1.ParsedObject);
			Assert.False(partialParseResultArray1.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultArray1.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray1.Count);
			Assert.Equal(expectedStatus, partialParseResultArray1.Status);

			dataPartial = Utils.StringToMemoryBytes("\n$5\r\nthere\r\n");
			expectedStatus = ParseStatus.Partial;
			expectedBytesConsumed = dataPartial.Length;
			expectedCount = 2;

			var partialParseResultArray2 = (ParseResultArray)_parser.ContinueParse(dataPartial, partialParseResultArray1);

			var parsedPartialArray2 = Assert.IsType<RESPArray>(partialParseResultArray2.ParsedObject);
			Assert.False(partialParseResultArray2.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultArray2.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray2.Count);
			Assert.Equal(expectedStatus, partialParseResultArray2.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("$4\r\nlove\r\n");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedCount = 3;

			var parseResultArray = (ParseResultArray)_parser.ContinueParse(dataFinal, partialParseResultArray2);

			var parsedArray = Assert.IsType<RESPArray>(parseResultArray.ParsedObject);
			Assert.True(parseResultArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parseResultArray.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parseResultArray.Status);
		}

		[Theory]
		[InlineData("*1\r\n$3", 4, 0)]
		[InlineData("*1\r\n$3\r\nh", 9, 0)]
		[InlineData("*1\r\n$3\r\nhey\r", 12, 0)]
		[InlineData("*2\r\n$3\r\nhey\r\n$5\r\nthe", 20, 1)]
		[InlineData("*3\r\n$1\r\na\r\n$1\r\nb\r\n$1\r\nc\r", 24, 2)]
		public void PartialArray_ReturnsPartialArray
			(string input, int expectedBytesConsumed, int expectedCount)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			var expectedStatus = ParseStatus.Partial;
			
			var parseResultArray = (ParseResultArray)_parser.Parse(data);

			var parsedArray = Assert.IsType<RESPArray>(parseResultArray.ParsedObject);
			Assert.False(parseResultArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parseResultArray.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parseResultArray.Status);
		}
	}
}
