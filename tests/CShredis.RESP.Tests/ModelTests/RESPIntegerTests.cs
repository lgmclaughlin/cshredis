using System;
using System.Text;
using Xunit;
using CShredis.RESP;

namespace CShredis.RESP.Tests
{
	public class RESPIntegerTests
	{
		public RESPIntegerTests() { }

		[Fact]
		public void RESPInteger_EncodesCorrectly()
		{
			var expectedEncoding = ":6\r\n";

			var encoding = new RESPInteger(6).EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}
		
	}
}
