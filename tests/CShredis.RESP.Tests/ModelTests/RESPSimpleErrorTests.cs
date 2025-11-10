using System;
using System.Text;
using Xunit;
using CShredis.RESP;

namespace CShredis.RESP.Tests
{
	public class RESPSimpleErrorTests
	{
		public RESPSimpleErrorTests() { }

		[Fact]
		public void RESPSimpleError_EncodesCorrectly()
		{
			var expectedEncoding = "-ERR invalid arguments\r\n";

			var encoding = new RESPSimpleError("invalid arguments").EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}
		
	}
}
