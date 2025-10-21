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

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
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

			ParseResult parsedPartialResult = _dispatcher.Parse(dataPartial);

			var parsedPartialBulkString = Assert.IsType<RESPBulkString>(parsedPartialResult.ParsedObject);
			Assert.False(parsedPartialBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedPartialBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parsedPartialResult.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("y\r\n");

			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("hey");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedBytesMissing = 0;

			ParseResult parsedResult = _dispatcher.ContinueParse(dataFinal, parsedPartialBulkString);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.IsComplete);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}
	}
}
