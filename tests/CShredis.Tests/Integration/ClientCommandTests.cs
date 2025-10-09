using System;
using System.Text;
using System.Threading;
using Xunit;
using System.Net.Sockets;
using CShredis.Core;

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
