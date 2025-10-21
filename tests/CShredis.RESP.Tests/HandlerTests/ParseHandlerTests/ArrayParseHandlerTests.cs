using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.HandlerTestUtilities;

namespace CShredis.RESP.Tests
{
	public class ArrayParseHandlerTests : IDisposable
	{
		private ParseDispatcher _dispatcher;
		private ArrayParseHandler _handler;

		public ArrayParseHandlerTests()
		{
			_dispatcher = new();
			_handler = new(_dispatcher);
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
			
			ParseResult parsedResult = _handler.Parse(data);

			var parsedArray = Assert.IsType<RESPArray>(parsedResult.ParsedObject);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void ValidNullArray_ReturnsNullArray()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("*-1\r\n");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _handler.Parse(data);

			Assert.IsType<RESPNullArray>(parsedResult.ParsedObject);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void PartialArrayWithCompletingMessage_ReturnsCompleteArray()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("*3\r\n$3\r\nhey\r\n$5\r\nth");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedCount = 1;

			ParseResult parsedPartialResult = _handler.Parse(dataPartial);

			var parsedPartialArray = Assert.IsType<RESPArray>(parsedPartialResult.ParsedObject);
			Assert.False(parsedPartialArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray.Count);
			Assert.Equal(expectedStatus, parsedPartialResult.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("ere\r\n$4\r\nlove\r\n");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedCount = 3;

			ParseResult parsedResult = _handler.ContinueParse(dataFinal, parsedPartialArray);

			var parsedArray = Assert.IsType<RESPArray>(parsedResult.ParsedObject);
			Assert.True(parsedArray.IsComplete);
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

			ParseResult parsedPartialResult1 = _handler.Parse(dataPartial);

			var parsedPartialArray1 = Assert.IsType<RESPArray>(parsedPartialResult1.ParsedObject);
			Assert.False(parsedPartialArray1.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult1.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray1.Count);
			Assert.Equal(expectedStatus, parsedPartialResult1.Status);

			dataPartial = Utils.StringToMemoryBytes("\n$5\r\nthere\r\n");
			expectedStatus = ParseStatus.Partial;
			expectedBytesConsumed = dataPartial.Length;
			expectedCount = 2;

			ParseResult parsedPartialResult2 = _handler.ContinueParse(dataPartial, parsedPartialArray1);

			var parsedPartialArray2 = Assert.IsType<RESPArray>(parsedPartialResult2.ParsedObject);
			Assert.False(parsedPartialArray2.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult2.BytesConsumed);
			Assert.Equal(expectedCount, parsedPartialArray2.Count);
			Assert.Equal(expectedStatus, parsedPartialResult2.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("$4\r\nlove\r\n");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedCount = 3;

			ParseResult parsedResult = _handler.ContinueParse(dataFinal, parsedPartialArray2);

			var parsedArray = Assert.IsType<RESPArray>(parsedResult.ParsedObject);
			Assert.True(parsedArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parsedResult.Status);
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
			
			ParseResult parsedResult = _handler.Parse(data);

			var parsedArray = Assert.IsType<RESPArray>(parsedResult.ParsedObject);
			Assert.False(parsedArray.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedCount, parsedArray.Count);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		public void Dispose()
		{
			_handler = null;
		}
	}
}
