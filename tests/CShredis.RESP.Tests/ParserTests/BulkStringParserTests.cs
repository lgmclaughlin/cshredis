using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.RESPTestUtilities;

namespace CShredis.RESP.Tests
{
	public class BulkStringParserTests
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
			var expectedType = RESPType.BulkString;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			var parseResultBulkString = (ParseResultBulkString)_parser.Parse(data);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void ValidDoubleDigitLength_ReturnsDoubleDigitLengthBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$17\r\ndoubledigitlength\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("doubledigitlength");
			var expectedType = RESPType.BulkString;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			var parseResultBulkString = (ParseResultBulkString)_parser.Parse(data);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void ValidHeyCRLFThere_ReturnsHeyCRLFThereBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$10\r\nhey\r\nthere\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("hey\r\nthere");
			var expectedType = RESPType.BulkString;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			var parseResultBulkString = (ParseResultBulkString)_parser.Parse(data);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void ValidEmpty_ReturnsEmptyBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$0\r\n\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("");
			var expectedType = RESPType.BulkString;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			var parseResultBulkString = (ParseResultBulkString)_parser.Parse(data);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void ValidNullBulkString_ReturnsNullBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$-1\r\n");
			var expectedType = RESPType.NullBulkString;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parseResult = _parser.Parse(data);
			var parsedNullBulkString = parseResult.ParsedObject!;

			Assert.Equal(expectedType, parsedNullBulkString.Type);
			Assert.Equal(expectedBytesConsumed, parseResult.BytesConsumed);
			Assert.Equal(expectedStatus, parseResult.Status);
		}

		[Theory]
		[InlineData("$3\r\nhey\r", 8, 1)]
		[InlineData("$3\r\nhe", 6, 3)]
		public void PartialBulkString_ReturnsPartialBulkString
			(string input, int expectedBytesConsumed, int expectedBytesMissing)
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			var expectedType = RESPType.BulkString;
			var expectedStatus = ParseStatus.Partial;

			var parseResultBulkString = (ParseResultBulkString)_parser.Parse(data);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.False(parseResultBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void PartialBulkStringWithCompletingMessage_ReturnsCompleteBulkString()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("$3\r\nhe");
			var expectedType = RESPType.BulkString;
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedBytesMissing = 3;

			var partialParseResultBulkString = (ParseResultBulkString)_parser.Parse(dataPartial);
			var parsedPartialBulkString = partialParseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedPartialBulkString.Type);
			Assert.False(partialParseResultBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, partialParseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, partialParseResultBulkString.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("y\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("hey");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedBytesMissing = 0;

			var parseResultBulkString = (ParseResultBulkString)_parser.ContinueParse(dataFinal, partialParseResultBulkString);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.True(parseResultBulkString.IsComplete);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void PartialBulkStringWithMultipleCompletingMessages_ReturnsCompleteBulkString()
		{
			ReadOnlyMemory<byte> dataPartial = Utils.StringToMemoryBytes("$8\r\nhe");
			var expectedType = RESPType.BulkString;
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = dataPartial.Length;
			var expectedBytesMissing = 8;

			var partialParseResultBulkString1 = (ParseResultBulkString)_parser.Parse(dataPartial);
			var parsedPartialBulkString1 = partialParseResultBulkString1.ParsedObject!;

			Assert.Equal(expectedType, parsedPartialBulkString1.Type);
			Assert.False(partialParseResultBulkString1.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultBulkString1.BytesConsumed);
			Assert.Equal(expectedBytesMissing, partialParseResultBulkString1.BytesMissing);
			Assert.Equal(expectedStatus, partialParseResultBulkString1.Status);

			dataPartial = Utils.StringToMemoryBytes("ada");
			expectedStatus = ParseStatus.Partial;
			expectedBytesConsumed = dataPartial.Length;
			expectedBytesMissing = 5;

			var partialParseResultBulkString2 = (ParseResultBulkString)_parser.ContinueParse(dataPartial, partialParseResultBulkString1);
			var parsedPartialBulkString2 = partialParseResultBulkString2.ParsedObject!;

			Assert.Equal(expectedType, parsedPartialBulkString2.Type);
			Assert.False(partialParseResultBulkString2.IsComplete);
			Assert.Equal(expectedBytesConsumed, partialParseResultBulkString2.BytesConsumed);
			Assert.Equal(expectedBytesMissing, partialParseResultBulkString2.BytesMissing);
			Assert.Equal(expectedStatus, partialParseResultBulkString2.Status);

			ReadOnlyMemory<byte> dataFinal = Utils.StringToMemoryBytes("che\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("headache");
			expectedStatus = ParseStatus.Complete;
			expectedBytesConsumed = dataFinal.Length;
			expectedBytesMissing = 0;

			var parseResultBulkString = (ParseResultBulkString)_parser.ContinueParse(dataFinal, partialParseResultBulkString2);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.True(parseResultBulkString.IsComplete);
			Assert.True(parsedBulkString.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
		}

		[Fact]
		public void MismatchedLength_ReturnsPartialBulkString()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("$5\r\nhey\r\n");
			var expectedType = RESPType.BulkString;
			var expectedStatus = ParseStatus.Partial;
			var expectedBytesConsumed = 9;
			var expectedBytesMissing = 2;

			var parseResultBulkString = (ParseResultBulkString)_parser.Parse(data);
			var parsedBulkString = parseResultBulkString.ParsedObject!;

			Assert.Equal(expectedType, parsedBulkString.Type);
			Assert.False(parseResultBulkString.IsComplete);
			Assert.Equal(expectedBytesConsumed, parseResultBulkString.BytesConsumed);
			Assert.Equal(expectedBytesMissing, parseResultBulkString.BytesMissing);
			Assert.Equal(expectedStatus, parseResultBulkString.Status);
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
	}
}
