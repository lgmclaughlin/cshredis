using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using Xunit;
using System.Net.Sockets;
using CShredis.Network;
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

		[Fact]
		public void PingCommand_ReturnsPong()
		{
			using var stream = GetNewClientStream();

			var request = EncodeInput("PING");
			
			var response = SendRequestAndGetResponse(request, stream);
			Assert.Equal(@"""PONG""", response);
		}

		[Fact]
		public void PingPingCommand_ReturnsPongPong()
		{
			using var stream = GetNewClientStream();

			var request = EncodeInput("PING");
			
			var response1 = SendRequestAndGetResponse(request, stream);
			Assert.Equal(@"""PONG""", response1);
			
			var response2 = SendRequestAndGetResponse(request, stream);
			Assert.Equal(@"""PONG""", response2);
		}

		[Fact]
		public void MultipleClients_PingCommand_ReturnsPong()
		{
			using var stream1 = GetNewClientStream();
			using var stream2 = GetNewClientStream();

			var request = EncodeInput("PING");

			var response1 = SendRequestAndGetResponse(request, stream1);
			Assert.Equal(@"""PONG""", response1);

			var response2 = SendRequestAndGetResponse(request, stream2);
			Assert.Equal(@"""PONG""", response2);
		}

		[Fact]
		public void SetCommandAndGetCommand_ReturnOKAndValue()
		{
			using var stream = GetNewClientStream();

			var request1 = EncodeInput("SET blue jam");

			var response1 = SendRequestAndGetResponse(request1, stream);
			Assert.Equal(@"""OK""", response1);

			var request2 = EncodeInput("GET blue");

			var response2 = SendRequestAndGetResponse(request2, stream);
			Assert.Equal(@"""jam""", response2);
		}

		[Fact]
		public void GetCommand_ReturnsNull()
		{
			using var stream = GetNewClientStream();

			var request = EncodeInput("GET blue");

			var response = SendRequestAndGetResponse(request, stream);
			Assert.Equal("(nil)", response);
		}

		[Fact]
		public void SetWithExGetWaitAndGet_ReturnsValueThenNull()
		{
			using var stream = GetNewClientStream();

			var request1 = EncodeInput("SET blue jam EX 1");

			var response1 = SendRequestAndGetResponse(request1, stream);
			Assert.Equal(@"""OK""", response1);

			var request2 = EncodeInput("GET blue");

			var response2 = SendRequestAndGetResponse(request2, stream);
			Assert.Equal(@"""jam""", response2);

			Thread.Sleep(1001);

			var request3 = EncodeInput("GET blue");

			var response3 = SendRequestAndGetResponse(request3, stream);
			Assert.Equal("(nil)", response3);
		}

		[Fact]
		public void SetWithPxGetWaitAndGet_ReturnsValueThenNull()
		{
			using var stream = GetNewClientStream();

			var request1 = EncodeInput("SET blue jam PX 1000");

			var response1 = SendRequestAndGetResponse(request1, stream);
			Assert.Equal(@"""OK""", response1);

			var request2 = EncodeInput("GET blue");

			var response2 = SendRequestAndGetResponse(request2, stream);
			Assert.Equal(@"""jam""", response2);

			Thread.Sleep(1001);

			var request3 = EncodeInput("GET blue");

			var response3 = SendRequestAndGetResponse(request3, stream);
			Assert.Equal("(nil)", response3);
		}

		[Fact]
		public void SetThenSetWithGet_ReturnsValue()
		{
			using var stream = GetNewClientStream();

			var request1 = EncodeInput("SET blue jam GET");

			var response1 = SendRequestAndGetResponse(request1, stream);
			Assert.Equal("(nil)", response1);

			var request2 = EncodeInput("SET blue velvet GET");

			var response2 = SendRequestAndGetResponse(request2, stream);
			Assert.Equal(@"""jam""", response2);
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
	}
}
