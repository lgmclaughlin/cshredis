using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Xunit;
using System.Net.Sockets;
using CShredis.Network;
using CShredis.Commands;
using CShredis.RESP;
using Encoder = CShredis.RESP.Encoder;

namespace CShredis.Tests
{
	public class ClientCommandTests : IDisposable
	{
		private const int CLIENT_PORT = 6379;
		private const string CLIENT_HOSTNAME = "localhost";
		private Server _server;
		private Thread _serverThread;
		private static readonly ParseDispatcher _parseDispatcher = new();

		public ClientCommandTests()
		{
			_server = new Server();
			_serverThread = new Thread(_server.Start);
			_serverThread.Start();

			Thread.Sleep(200);
		}

		public void Dispose()
		{
			_server.Stop();
			_serverThread.Join();
		}

		#region ***** PING *****

		[Fact]
		public void PingCommand_ReturnsPong()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "PING", @"""PONG""");
		}

		[Fact]
		public void PingPingCommand_ReturnsPongPong()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "PING", @"""PONG""");
			MakeCommandAndExpect(stream, "PING", @"""PONG""");
		}

		[Fact]
		public void MultipleClients_PingCommand_ReturnsPong()
		{
			using var stream1 = GetNewClientStream();
			using var stream2 = GetNewClientStream();
			MakeCommandAndExpect(stream1, "PING", @"""PONG""");
			MakeCommandAndExpect(stream2, "PING", @"""PONG""");
		}

		#endregion

		#region ***** SET / GET *****

		[Fact]
		public void SetCommandAndGetCommand_ReturnOKAndValue()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam", @"""OK""");
			MakeCommandAndExpect(stream, "GET blue", @"""jam""");
		}

		[Fact]
		public void GetCommand_ReturnsNull()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "GET blue", "(nil)");
		}

		[Fact]
		public void SetWithExGetWaitAndGet_ReturnsValueThenNull()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam EX 1", @"""OK""");
			MakeCommandAndExpect(stream, "GET blue", @"""jam""");
			Thread.Sleep(1001);
			MakeCommandAndExpect(stream, "GET blue", "(nil)");
		}

		[Fact]
		public void SetWithPxGetWaitAndGet_ReturnsValueThenNull()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam PX 1000", @"""OK""");
			MakeCommandAndExpect(stream, "GET blue", @"""jam""");
			Thread.Sleep(1001);
			MakeCommandAndExpect(stream, "GET blue", "(nil)");
		}

		[Fact]
		public void SetThenSetWithGet_ReturnsValue()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam GET", "(nil)");
			MakeCommandAndExpect(stream, "SET blue velvet GET", @"""jam""");
		}

		[Fact]
		public void SetOnList_OverwritesList()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "SET blue jam", @"""OK""");
		}

		[Fact]
		public void SetOnListWithGet_ReturnsWrongType()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "SET blue jam GET", "(error) " + ResponseMessages.WrongType_KeyOperationTypeMismatch);
		}

		#endregion

		#region ***** LPUSH / RPUSH *****

		[Fact]
		public void LPushWithTwoElementsThenLRange_ReturnsTwoAndReverseOrder()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "LPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "LRANGE blue 0 1", "1) \"two\"\n2) \"one\"");
		}

		[Fact]
		public void RPushWithTwoElements_ReturnsTwo()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
		}

		[Fact]
		public void RPushWithTwoElementsTwice_ReturnsTwoAndFour()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "RPUSH blue three four", "(integer) 4");
		}

		[Fact]
		public void RPushOnSetString_ReturnsWrongType()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam", @"""OK""");
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(error) " + ResponseMessages.WrongType_KeyOperationTypeMismatch);
		}

		#endregion

		#region ***** LLEN *****

		[Fact]
		public void LLenOnEmpty_ReturnsZero()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "LLEN blue", "(integer) 0");
		}

		[Fact]
		public void LLenWithTwoElementsThenFour_ReturnsTwoAndFour()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "LLEN blue", "(integer) 2");
			MakeCommandAndExpect(stream, "RPUSH blue three four", "(integer) 4");
			MakeCommandAndExpect(stream, "LLEN blue", "(integer) 4");
		}

		[Fact]
		public void LLenOnSetString_ReturnsWrongType()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam", @"""OK""");
			MakeCommandAndExpect(stream, "LLEN blue", "(error) " + ResponseMessages.WrongType_KeyOperationTypeMismatch);
		}

		#endregion

		#region ***** LRANGE *****

		[Fact]
		public void LRangeWithValidRange_ReturnsRange()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "LRANGE blue 0 1", "1) \"one\"\n2) \"two\"");
			MakeCommandAndExpect(stream, "LRANGE blue 1 5", @"1) ""two""");
			MakeCommandAndExpect(stream, "LRANGE blue -6 0", @"1) ""one""");
		}

		[Fact]
		public void LRangeWithValidRangeNegativeIndexes_ReturnsRange()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "LRANGE blue -2 -1", "1) \"one\"\n2) \"two\"");
			MakeCommandAndExpect(stream, "LRANGE blue -1 -1", @"1) ""two""");
		}

		[Fact]
		public void LRangeWithEmptyRange_ReturnsEmpty()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "LRANGE blue 150 200", "(empty array)");
			MakeCommandAndExpect(stream, "LRANGE blue 0 1", "(empty array)");
		}

		[Fact]
		public void LRangeOnSetString_ReturnsWrongType()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam", @"""OK""");
			MakeCommandAndExpect(stream, "LRANGE blue 0 1", "(error) " + ResponseMessages.WrongType_KeyOperationTypeMismatch);
		}

		#endregion

		#region ***** LPOP / RPOP *****

		[Fact]
		public void LPop_ReturnsElementAndRemovesFromList()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "LPOP blue", @"""one""");
			MakeCommandAndExpect(stream, "LRANGE blue 0 0", @"1) ""two""");
		}

		[Fact]
		public void LPopTwoElements_ReturnsTwoAndRemovesFromList()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two three", "(integer) 3");
			MakeCommandAndExpect(stream, "LPOP blue 2", "1) \"one\"\n2) \"two\"");
			MakeCommandAndExpect(stream, "LRANGE blue 0 0", @"1) ""three""");
		}

		[Fact]
		public void RPop_ReturnsElementAndRemovesFromList()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two", "(integer) 2");
			MakeCommandAndExpect(stream, "RPOP blue", @"""two""");
			MakeCommandAndExpect(stream, "LRANGE blue 0 0", @"1) ""one""");
		}

		[Fact]
		public void RPopTwoElements_ReturnsTwoReverseOrderAndRemovesFromList()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "RPUSH blue one two three", "(integer) 3");
			MakeCommandAndExpect(stream, "RPOP blue 2", "1) \"three\"\n2) \"two\"");
			MakeCommandAndExpect(stream, "LRANGE blue 0 0", @"1) ""one""");
		}

		[Fact]
		public void LPopAndRPopOnSetString_ReturnWrongType()
		{
			using var stream = GetNewClientStream();
			MakeCommandAndExpect(stream, "SET blue jam", @"""OK""");
			MakeCommandAndExpect(stream, "LPOP blue", "(error) " + ResponseMessages.WrongType_KeyOperationTypeMismatch);
			MakeCommandAndExpect(stream, "RPOP blue", "(error) " + ResponseMessages.WrongType_KeyOperationTypeMismatch);
		}

		#endregion

		#region ***** Helpers *****

		private void MakeCommandAndExpect(NetworkStream stream, string command, string expect)
		{
			var request = EncodeInput(command);
			var response = SendRequestAndGetResponse(request, stream);
			Assert.Equal(response, expect);
		}

		private NetworkStream GetNewClientStream()
		{
			var newClient = new TcpClient(CLIENT_HOSTNAME, CLIENT_PORT);

			return newClient.GetStream();
		}

		private string SendRequestAndGetResponse(ReadOnlyMemory<byte> request, NetworkStream stream)
		{
			SendRequest(request, stream);
			var responseBytes = GetResponse(stream);
			
			return PrintResponse(responseBytes);
		}

		private void SendRequest(ReadOnlyMemory<byte> request, NetworkStream stream)
		{
			stream.Write(request.Span);
		}

		private ReadOnlyMemory<byte> GetResponse(NetworkStream stream)
		{
			var buffer = new byte[1024];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			
			return buffer[..bytesRead].AsMemory();
		}

		private ReadOnlyMemory<byte> EncodeInput(string input)
		{
			string[] tokens = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")
				.Select(m => m.Value.Replace("\"", ""))
				.ToArray();

			var tokenElements = new List<RESPObject>(tokens.Length);
			foreach (var token in tokens)
				tokenElements.Add(RESPObject.BulkString(token));

			var tokenRespArray = RESPObject.Array(tokenElements);

			return Encoder.Encode(tokenRespArray);
		}

		private string PrintResponse(ReadOnlyMemory<byte> response)
		{
			var responseRespObject = _parseDispatcher.Parse(response).ParsedObject;
			return Printer.Print(responseRespObject!);
		}

		#endregion
	}
}
