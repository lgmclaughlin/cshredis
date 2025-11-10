using System;
using System.Text;
using Xunit;
using CShredis.RESP;

namespace CShredis.RESP.Tests
{
	public class RESPBulkStringTests
	{
		public RESPBulkStringTests() { }

		[Fact]
		public void RESPBulkString_EncodesCorrectly()
		{
			var expectedEncoding = "$5\r\nhello\r\n";

			var encoding = new RESPBulkString("hello").EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}
		
	}
}
