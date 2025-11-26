using System;
using System.Text;
using System.Threading;
using Xunit;
using System.Net.Sockets;
using CShredis.Network;

namespace CShredis.Tests
{
	public class ClientCommandTests : IDisposable
	{
		private const int CLIENT_PORT = 6379;
		private const string CLIENT_HOSTNAME = "localhost";
		private Server _server;
		private Thread _serverThread;

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
			var stream = GetNewClientStream();

			var request = "*1\r\n$4\r\nPING\r\n";
			
			var response = SendRequestAndGetResponse(request, stream);
			Assert.Equal("+PONG\r\n", response);
		}

		[Fact]
		public void PingPingCommand_ReturnsPongPong()
		{
			var stream = GetNewClientStream();

			var request = "*1\r\n$4\r\nPING\r\n";
			
			var response1 = SendRequestAndGetResponse(request, stream);
			Assert.Equal("+PONG\r\n", response1);
			
			var response2 = SendRequestAndGetResponse(request, stream);
			Assert.Equal("+PONG\r\n", response2);
		}

		[Fact]
		public void MultipleClients_PingCommand_ReturnsPong()
		{
			var stream1 = GetNewClientStream();
			var stream2 = GetNewClientStream();

			var request = "*1\r\n$4\r\nPING\r\n";

			SendRequest(request, stream1);
			SendRequest(request, stream2);
			
			var response1 = GetResponse(stream1);
			Assert.Equal("+PONG\r\n", response1);

			var response2 = GetResponse(stream2);
			Assert.Equal("+PONG\r\n", response2);
		}

		[Fact]
		public void SetCommandAndGetCommand_ReturnOKAndValue()
		{
			var stream = GetNewClientStream();

			var request1 = "*3\r\n$3\r\nSET\r\n$4\r\nblue\r\n$3\r\njam\r\n";

			SendRequest(request1, stream);

			var response1 = GetResponse(stream);
			Assert.Equal("+OK\r\n", response1);

			var request2 = "*2\r\n$3\r\nGET\r\n$4\r\nblue\r\n";

			SendRequest(request2, stream);

			var response2 = GetResponse(stream);
			Assert.Equal("$3\r\njam\r\n", response2);
		}

		[Fact]
		public void GetCommand_ReturnsNull()
		{
			var stream = GetNewClientStream();

			var request = "*2\r\n$3\r\nGET\r\n$4\r\nblue\r\n";

			SendRequest(request, stream);

			var response = GetResponse(stream);
			Assert.Equal("$-1\r\n", response);
		}

		private NetworkStream GetNewClientStream()
		{
			var newClient = new TcpClient(CLIENT_HOSTNAME, CLIENT_PORT);

			return newClient.GetStream();
		}

		private string SendRequestAndGetResponse(string request, NetworkStream stream)
		{
			SendRequest(request, stream);
			return GetResponse(stream);
		}

		private void SendRequest(string request, NetworkStream stream)
		{
			var requestBytes = Encoding.UTF8.GetBytes(request);
			stream.Write(requestBytes);
		}

		private string GetResponse(NetworkStream stream)
		{
			var buffer = new byte[1024];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			
			return Encoding.UTF8.GetString(buffer, 0, bytesRead);
		}
	}
}
