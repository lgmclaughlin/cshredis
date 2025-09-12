using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);

try {
	server.Start();
	Socket clientSocket = server.AcceptSocket(); 

	using (NetworkStream stream = new NetworkStream(clientSocket)) {
		byte[] buffer = new byte[1024];
		int bytesRead = stream.Read(buffer, 0, buffer.Length);
		string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
		Console.WriteLine($"Received: {message}");

		var response = "+PONG\r\n";
		byte[] responseBytes = Encoding.UTF8.GetBytes(response);
		stream.Write(responseBytes, 0, responseBytes.Length);
	}
}
finally
{
	server.Stop();
}
