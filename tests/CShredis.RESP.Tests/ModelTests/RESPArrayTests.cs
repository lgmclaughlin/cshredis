using System;
using System.Text;
using Xunit;
using CShredis.RESP;

namespace CShredis.RESP.Tests
{
	public class RESPArrayTests
	{
		public RESPArrayTests() { }

		[Fact]
		public void RESPArray_SameTypesEncodesCorrectly()
		{
			var expectedEncoding = "*2\r\n$3\r\nhey\r\n$5\r\nthere\r\n";

			var respArrayElements = new List<RESPObject>
			{
				new RESPBulkString("hey"),
				new RESPBulkString("there")
			};
			var respArray = new RESPArray(respArrayElements);
			var encoding = respArray.EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}
		
		[Fact]
		public void RESPArray_VariedTypesEncodesCorrectly()
		{
			var expectedEncoding = "*3\r\n$3\r\nhey\r\n+there\r\n:4\r\n";

			var respArrayElements = new List<RESPObject>
			{
				new RESPBulkString("hey"),
				new RESPSimpleString("there"),
				new RESPInteger(4)
			};
			var respArray = new RESPArray(respArrayElements);
			var encoding = respArray.EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}

		[Fact]
		public void RESPArray_EmptyEncodesCorrectly()
		{
			var expectedEncoding = "*0\r\n";

			var respArray = new RESPArray(new List<RESPObject>());
			var encoding = respArray.EncodeString();

			Assert.Equal(expectedEncoding, encoding);
		}
	}
}
