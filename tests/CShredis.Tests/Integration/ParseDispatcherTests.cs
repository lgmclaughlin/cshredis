using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.Tests.TestUtilities;

namespace CShredis.Tests
{
	public class ParseDispatcherTests
	{
		private ParseDispatcher _dispatcher = new();

		[Theory]
		[InlineData("*1\r\n$3\r\nhey\r\n", 1)]
		[InlineData("*2\r\n$3\r\nhey\r\n$5\r\nthere\r\n", 2)]
		public void ValidArray_ReturnsParsedArray(string input, int expectedCount)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;
			
			ParseResult parsedResult = _dispatcher.Parse(data);

			var parsedArray = Assert.IsType<RESPArray>(parsedResult.ParsedObject);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void PartialArrayWithMultipleCompletingMessages_ReturnsCompleteArray()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("*3\r\n$3\r\nhey\r");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedCount = 0;

			ParseResult parsedPartialResult1 = _dispatcher.Parse(dataPartial);

			var parsedPartialResultArray1 = Assert.IsType<ParseResultArray>(parsedPartialResult1);
			var parsedPartialArray1 = Assert.IsType<RESPArray>(parsedPartialResult1.ParsedObject);
			Assert.False(parsedPartialResultArray1.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult1.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray1.Count);
			Assert.Equal(expectedStatus, parsedPartialResult1.Status);

			dataPartial = Utils.StringToMemoryBytes("\n$5\r\nthere\r\n");
			expectedStatus = ParseStatus.Partial;
			expectedBytesConsumed = dataPartial.Length;
			expectedCount = 2;

			ParseResult parsedPartialResult2 = _dispatcher.ContinueParse(dataPartial, parsedPartialResult1);

			var parsedPartialResultArray2 = Assert.IsType<ParseResultArray>(parsedPartialResult2);
			var parsedPartialArray2 = Assert.IsType<RESPArray>(parsedPartialResult2.ParsedObject);
			Assert.False(parsedPartialResultArray2.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult2.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray2.Count);
			Assert.Equal(expectedStatus, parsedPartialResult2.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("$4\r\nlove\r\n");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedCount = 3;

			ParseResult parsedResult = _dispatcher.ContinueParse(dataFinal, parsedPartialResult2);

			var parsedResultArray = Assert.IsType<ParseResultArray>(parsedResult);
			var parsedArray = Assert.IsType<RESPArray>(parsedResult.ParsedObject);
			Assert.True(parsedResultArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Theory]
		[InlineData("$3\r\nhey\r\n", "hey")]
		[InlineData("$17\r\ndoubledigitlength\r\n", "doubledigitlength")]
		public void ValidBulkString_ReturnsParsedBulkString(string input, string expectedValue)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes(expectedValue);
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _dispatcher.Parse(data);

			var parsedBulkString = parsedResult.ParsedObject;
			Assert.Equal(parsedBulkString.Type, RESPType.BulkString);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void ValidBulkStringSplitIntoTwoParses_ReturnsCompleteBulkString()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("$3\r\nhe");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedBytesMissing = 3;

			ParseResult partialParseResult = _dispatcher.Parse(dataPartial);

			var partialParseResultBulkString = Assert.IsType<ParseResultBulkString>(partialParseResult);
			Assert.False(partialParseResultBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, partialParseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, partialParseResultBulkString.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("y\r\n");

			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("hey");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedBytesMissing = 0;

			ParseResult parseResult = _dispatcher.ContinueParse(dataFinal, partialParseResult);

			var parseResultBulkString = Assert.IsType<ParseResultBulkString>(parseResult);
			var parsedBulkString = parseResult.ParsedObject;
			Assert.True(parseResultBulkString.IsComplete);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}
	}
}
