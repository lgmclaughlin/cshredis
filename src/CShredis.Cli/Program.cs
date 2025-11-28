using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using CShredis.RESP;
using Encoder = CShredis.RESP.Encoder;

namespace CShredis.Cli
{
	internal class Program
	{
		private static readonly ParseDispatcher _parseDispatcher = new();

		static async Task Main()
		{
			var client = new TcpClient();
			NetworkStream? stream = null;

			try
			{
				await client.ConnectAsync("127.0.0.1", 6379);
				stream = client.GetStream();
			}
			catch (SocketException)
			{
				Console.Error.WriteLine("Could not connect to server on 127.0.0.1:6379. Exiting.");
				return;
			}

			while (true)
			{
				Console.Write("cshredis> ");
				var input = Console.ReadLine();

				if (input == null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Shutting down.");
					break;
				}

				string[] tokens = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+")
					.Select(m => m.Value.Replace("\"", ""))
					.ToArray();

				if (tokens.Length == 0)
				{
					Console.WriteLine();
					continue;
				}

				var tokenElements = new List<RESPObject>(tokens.Length);
				foreach (var token in tokens)
					tokenElements.Add(RESPObject.BulkString(token));

				var tokenRespArray = RESPObject.Array(tokenElements);
				var encodedTokenRespArray = Encoder.Encode(tokenRespArray);

				try
				{
					await stream!.WriteAsync(encodedTokenRespArray);
					await stream.FlushAsync();

					var buffer = new byte[1024];
					int bytesRead = await stream.ReadAsync(buffer);

					if (bytesRead == 0)
					{
						Console.WriteLine("\nServer closed the connection gracefully. Exiting.");
						break;
					}

					var responseRespObject = _parseDispatcher.Parse(buffer[..bytesRead]).ParsedObject;
					Console.WriteLine(Printer.Print(responseRespObject!));
				}
				catch (IOException ioEx) when (ioEx.InnerException is SocketException se)
				{
					Console.Error.WriteLine($"\n[ERROR] Connection lost ({se.SocketErrorCode}). Exiting.");
					break;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"\n[ERROR] An unexpected error occurred during I/O: {ex.Message}. Exiting.");
					break;
				}
			}

			stream?.Dispose();
			client.Close();
		}
	}
}
