using System;
using System.Text;
using Xunit;
using CShredis.RESP;

namespace CShredis.RESP.Tests
{
	public class EncoderTests
	{
		public EncoderTests() { }

		[Fact]
		public void Array_SameTypesEncodesCorrectly()
		{
			var expectedEncoding = "*2\r\n$3\r\nhey\r\n$5\r\nthere\r\n";

			var respArrayElements = new List<RESPObject>
			{
				new RESPObject(RESPType.BulkString, "hey"),
				new RESPObject(RESPType.BulkString, "there")
			};
			var respArray = new RESPArray(respArrayElements);
			var encoding = Encoder.EncodeString(respArray);

			Assert.Equal(expectedEncoding, encoding);
		}
		
		[Fact]
		public void Array_VariedTypesEncodesCorrectly()
		{
			var expectedEncoding = "*3\r\n$3\r\nhey\r\n+there\r\n:4\r\n";

			var respArrayElements = new List<RESPObject>
			{
				new RESPObject(RESPType.BulkString, "hey"),
				new RESPObject(RESPType.SimpleString, "there"),
				new RESPObject(RESPType.Integer, "4")
			};
			var respArray = new RESPArray(respArrayElements);
			var encoding = Encoder.EncodeString(respArray);

			Assert.Equal(expectedEncoding, encoding);
		}

		[Fact]
		public void Array_EmptyEncodesCorrectly()
		{
			var expectedEncoding = "*0\r\n";

			var respArray = new RESPArray(new List<RESPObject>());
			var encoding = Encoder.EncodeString(respArray);

			Assert.Equal(expectedEncoding, encoding);
		}

		[Fact]
		public void BulkString_EncodesCorrectly()
		{
			var expectedEncoding = "$5\r\nhello\r\n";

			var encoding = Encoder.EncodeString(new RESPObject(RESPType.BulkString, "hello"));

			Assert.Equal(expectedEncoding, encoding);
		}

		[Fact]
		public void Integer_EncodesCorrectly()
		{
			var expectedEncoding = ":6\r\n";

			var encoding = Encoder.EncodeString(new RESPObject(RESPType.Integer, "6"));

			Assert.Equal(expectedEncoding, encoding);
		}

		[Fact]
		public void SimpleError_EncodesCorrectly()
		{
			var expectedEncoding = "-ERR invalid arguments\r\n";

			var encoding = Encoder.EncodeString(new RESPObject(RESPType.SimpleError, "ERR invalid arguments"));

			Assert.Equal(expectedEncoding, encoding);
		}

		[Fact]
		public void SimpleString_EncodesCorrectly()
		{
			var expectedEncoding = "+hello\r\n";

			var encoding = Encoder.EncodeString(new RESPObject(RESPType.SimpleString, "hello"));

			Assert.Equal(expectedEncoding, encoding);
		}
	}
}
