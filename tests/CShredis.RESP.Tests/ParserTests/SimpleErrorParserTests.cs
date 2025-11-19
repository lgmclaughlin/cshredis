using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.RESPTestUtilities;

namespace CShredis.RESP.Tests
{
	public class SimpleErrorParserTests
	{
		private SimpleErrorParser _parser;

		public SimpleErrorParserTests()
		{
			_parser = new();
		}

		[Fact]
		public void ValidError_ReturnsSimpleError()
		{
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes("-ERR unknown command 'a'\r\n");
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("ERR unknown command 'a'");
			var expectedType = RESPType.SimpleError;
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parseResult = _parser.Parse(data);
			var parsedSimpleError = parseResult.ParsedObject!;

			Assert.Equal(expectedType, parsedSimpleError.Type);
			Assert.True(parsedSimpleError.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parseResult.BytesConsumed);
			Assert.Equal(expectedStatus, parseResult.Status);
		}
	}
}
