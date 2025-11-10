using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.ParserTestUtilities;

namespace CShredis.RESP.Tests
{
	public class BulkStringParserTests : IDisposable
	{
		private BulkStringParser _parser;

		public BulkStringParserTests()
		{
			_parser = new();
		}

		[Fact]
		public void ValidHey_ReturnsHeyBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$3\r\nhey\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("hey");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void ValidDoubleDigitLength_ReturnsDoubleDigitLengthBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$17\r\ndoubledigitlength\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("doubledigitlength");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void ValidHeyCRLFThere_ReturnsHeyCRLFThereBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$10\r\nhey\r\nthere\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("hey\r\nthere");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void ValidEmpty_ReturnsEmptyBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$0\r\n\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void ValidNullBulkString_ReturnsNullBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$-1\r\n");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _parser.Parse(data);

			Assert.IsType<RESPNullBulkString>(parsedResult.ParsedObject);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Theory]
		[InlineData("$3\r\nhey\r", 8, 1)]
		[InlineData("$3\r\nhe", 6, 3)]
		public void PartialBulkString_ReturnsPartialBulkString
			(string input, int expectedBytesConsumed, int expectedBytesMissing)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			var expectedStatus = ParseStatus.Partial;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.False(parsedBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void PartialBulkStringWithCompletingMessage_ReturnsCompleteBulkString()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("$3\r\nhe");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedBytesMissing = 3;

			ParseResult parsedPartialResult = _parser.Parse(dataPartial);

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

			ParseResult parsedResult = _parser.ContinueParse(dataFinal, parsedPartialBulkString);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.IsComplete);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void PartialBulkStringWithMultipleCompletingMessages_ReturnsCompleteBulkString()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("$8\r\nhe");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedBytesMissing = 8;

			ParseResult parsedPartialResult1 = _parser.Parse(dataPartial);

			var parsedPartialBulkString1 = Assert.IsType<RESPBulkString>(parsedPartialResult1.ParsedObject);
			Assert.False(parsedPartialBulkString1.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult1.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedPartialBulkString1.BytesMissing);
			Assert.Equal(expectedStatus, parsedPartialResult1.Status);

			dataPartial = Utils.StringToMemoryBytes("ada");
			expectedStatus = ParseStatus.Partial;
			expectedBytesConsumed = dataPartial.Length;
			expectedBytesMissing = 5;

			ParseResult parsedPartialResult2 = _parser.ContinueParse(dataPartial, parsedPartialBulkString1);

			var parsedPartialBulkString2 = Assert.IsType<RESPBulkString>(parsedPartialResult2.ParsedObject);
			Assert.False(parsedPartialBulkString2.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedPartialResult2.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedPartialBulkString2.BytesMissing);
			Assert.Equal(expectedStatus, parsedPartialResult2.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("che\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("headache");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedBytesMissing = 0;

			ParseResult parsedResult = _parser.ContinueParse(dataFinal, parsedPartialBulkString2);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.True(parsedBulkString.IsComplete);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Fact]
		public void MismatchedLength_ReturnsPartialBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$5\r\nhey\r\n");
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = 9;
			var expectedBytesMissing = 2;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedBulkString = Assert.IsType<RESPBulkString>(parsedResult.ParsedObject);
			Assert.False(parsedBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parsedBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		[Theory]
		[InlineData("$-\r\n")]
		[InlineData("$-\r")]
		public void InvalidLength_ThrowsException(string input)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);

			Assert.Throws<InvalidOperationException>(() => _parser.Parse(data));
		}

		[Theory]
		[InlineData("$3??hey\r\n")]
		[InlineData("$3\r\nhey??")]
		public void MalformedCRLF_ThrowsException(string input)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);

			Assert.Throws<InvalidOperationException>(() => _parser.Parse(data));
		}

		public void Dispose()
		{
			_parser = null;
		}
	}
}
