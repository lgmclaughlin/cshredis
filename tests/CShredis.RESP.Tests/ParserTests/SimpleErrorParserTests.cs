using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.ParserTestUtilities;

namespace CShredis.RESP.Tests
{
	public class SimpleErrorParserTests : IDisposable
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
			ReadOnlyMemory<byte> expectedData = Utils.StringToMemoryBytes("unknown command 'a'");
			var expectedBytesConsumed = data.Length;
			var expectedStatus = ParseStatus.Complete;

			ParseResult parsedResult = _parser.Parse(data);

			var parsedSimpleError = Assert.IsType<RESPSimpleError>(parsedResult.ParsedObject);
			Assert.True(parsedSimpleError.Value.Span.SequenceEqual(expectedData.Span));
			Assert.Equal(expectedBytesConsumed, parsedResult.BytesConsumed);
			Assert.Equal(expectedStatus, parsedResult.Status);
		}

		public void Dispose()
		{
			_parser = null;
		}
	}
}
