using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.RESPTestUtilities;

namespace CShredis.RESP.Test
{
	public class ParseDispatcherTests
	{
		private ParseDispatcher _dispatcher;

		public ParseDispatcherTests()
		{
			_dispatcher = new();
		}

		[Fact]
		public void ParseStreamValidInput_ReturnsValidRESPObjects()
		{
			string input = "*2\r\n$4\r\nECHO\r\n$5\r\nhello\r\n*1\r\n$4\r\nPING\r\n";
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			ReadOnlyMemory<byte> expectedData1 = Utils.StringToMemoryBytes("ECHO");
			ReadOnlyMemory<byte> expectedData2 = Utils.StringToMemoryBytes("hello");
			ReadOnlyMemory<byte> expectedData3 = Utils.StringToMemoryBytes("PING");
			var expectedCountAll = 2;
			var expectedCountFirstArray = 2;
			var expectedCountSecondArray = 1;
			var expectedTypeBulkString = RESPType.BulkString;

			var parsedObjects = _dispatcher.ParseStream(data);

			Assert.Equal(expectedCountAll, parsedObjects.Count);
			var parsedArray1 = Assert.IsType<RESPArray>(parsedObjects[0]);
			var parsedArray2 = Assert.IsType<RESPArray>(parsedObjects[1]);
			Assert.Equal(expectedCountFirstArray, parsedArray1.Count);
			Assert.Equal(expectedCountSecondArray, parsedArray2.Count);
			Assert.Equal(expectedTypeBulkString, parsedArray1.Elements[0].Type);
			Assert.Equal(expectedTypeBulkString, parsedArray1.Elements[1].Type);
			Assert.Equal(expectedTypeBulkString, parsedArray2.Elements[0].Type);
			Assert.True(parsedArray1.Elements[0].Value.Span.SequenceEqual(expectedData1.Span));
			Assert.True(parsedArray1.Elements[1].Value.Span.SequenceEqual(expectedData2.Span));
			Assert.True(parsedArray2.Elements[0].Value.Span.SequenceEqual(expectedData3.Span));
		}

		[Fact]
		public void ParseStreamPartialInput_ReturnsOnlyCompleteRESPObjects()
		{
			string input = "*2\r\n$4\r\nECHO\r\n$5\r\nhello\r\n*1\r\n$4\r\nPIN";
			ReadOnlyMemory<byte> data = Utils.StringToMemoryBytes(input);
			ReadOnlyMemory<byte> expectedData1 = Utils.StringToMemoryBytes("ECHO");
			ReadOnlyMemory<byte> expectedData2 = Utils.StringToMemoryBytes("hello");
			var expectedCountAll = 1;
			var expectedCountFirstArray = 2;
			var expectedTypeBulkString = RESPType.BulkString;

			var parsedObjects = _dispatcher.ParseStream(data);

			Assert.Equal(expectedCountAll, parsedObjects.Count);
			var parsedArray1 = Assert.IsType<RESPArray>(parsedObjects[0]);
			Assert.Equal(expectedCountFirstArray, parsedArray1.Count);
			Assert.Equal(expectedTypeBulkString, parsedArray1.Elements[0].Type);
			Assert.Equal(expectedTypeBulkString, parsedArray1.Elements[1].Type);
			Assert.True(parsedArray1.Elements[0].Value.Span.SequenceEqual(expectedData1.Span));
			Assert.True(parsedArray1.Elements[1].Value.Span.SequenceEqual(expectedData2.Span));
		}

		[Fact]
		public void ParseStreamPartialInputWithCompleteingMessage_ReturnsValidRESPObjects()
		{
			string input1 = "*2\r\n$4\r\nECHO\r\n$5\r\nhello\r\n*1\r\n$4\r\nPIN";
			ReadOnlyMemory<byte> data1 = Utils.StringToMemoryBytes(input1);
			ReadOnlyMemory<byte> expectedData1 = Utils.StringToMemoryBytes("ECHO");
			ReadOnlyMemory<byte> expectedData2 = Utils.StringToMemoryBytes("hello");
			var expectedCountAll1 = 1;
			var expectedCountFirstArray = 2;
			var expectedTypeBulkString = RESPType.BulkString;

			var parsedObjects1 = _dispatcher.ParseStream(data1);

			Assert.Equal(expectedCountAll1, parsedObjects1.Count);
			var parsedArray1 = Assert.IsType<RESPArray>(parsedObjects1[0]);
			Assert.Equal(expectedCountFirstArray, parsedArray1.Count);
			Assert.Equal(expectedTypeBulkString, parsedArray1.Elements[0].Type);
			Assert.Equal(expectedTypeBulkString, parsedArray1.Elements[1].Type);
			Assert.True(parsedArray1.Elements[0].Value.Span.SequenceEqual(expectedData1.Span));
			
			string input2 = "G\r\n";
			ReadOnlyMemory<byte> data2 = Utils.StringToMemoryBytes(input2);
			ReadOnlyMemory<byte> expectedData3 = Utils.StringToMemoryBytes("PING");
			var expectedCountAll2 = 1;
			var expectedCountSecondArray = 1;

			var parsedObjects2 = _dispatcher.ParseStream(data2);
			
			Assert.Equal(expectedCountAll2, parsedObjects2.Count);
			var parsedArray2 = Assert.IsType<RESPArray>(parsedObjects2[0]);
			Assert.Equal(expectedCountSecondArray, parsedArray2.Count);
			Assert.Equal(expectedTypeBulkString, parsedArray2.Elements[0].Type);
			Assert.True(parsedArray2.Elements[0].Value.Span.SequenceEqual(expectedData3.Span));
		}
	}
}
