using System;
using System.Text;
using System.Net.Sockets;
using CShredis.RESP;

namespace CShredis.Network
{
	internal class Program
	{
		private static readonly ParseDispatcher _parseDispatcher = new();

		static async Task Main()
		{
			try
			{
				Console.WriteLine("Connecting to server...");
				
				var client = new TcpClient();
				await client.ConnectAsync("127.0.0.1", 6379);
				using var stream = client.GetStream();

				Console.WriteLine("Connected on Port 6379.");
				Console.WriteLine("\r******** CShredis CLI ********\r\n");

				while (true)
				{
					Console.Write("cshredis> ");
					var input = Console.ReadLine();

					if (input == null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
					{
						Console.WriteLine("Shutting down.");
						break;
					}
					
					var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length == 0)
					{
						Console.WriteLine();
						continue;
					}

					var tokenElements = new List<RESPObject>(tokens.Length);
					foreach (var t in tokens)
						tokenElements.Add(new RESPBulkString(t));

					var tokenRespArray = new RESPArray(tokenElements);
					var encodedTokenRespArray = tokenRespArray.Encode();

					await stream.WriteAsync(encodedTokenRespArray);
					await stream.FlushAsync();

					var buffer = new byte[1024];
					int bytesRead = await stream.ReadAsync(buffer);
					var responseRespObject = _parseDispatcher.Parse(buffer[..bytesRead]).ParsedObject;

					Console.WriteLine(responseRespObject!.Print());
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Server crashed: {ex}");
			}
		}
	}
}
