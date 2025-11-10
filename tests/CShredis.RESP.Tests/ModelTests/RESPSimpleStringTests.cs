using System;
using System.Text;
using Xunit;
using CShredis.RESP;

namespace CShredis.RESP.Tests
{
	public class RESPSimpleStringTests
	{
		public RESPSimpleStringTests() { }

		[Fact]
		public void RESPSimpleString_EncodesCorrectly()
		{
			var expectedEncoding = "+hello\r\n";

			var encoding = new RESPSimpleString("hello").EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}
		
	}
}
