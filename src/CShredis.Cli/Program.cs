using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using CShredis.RESP;
using Encoder = CShredis.RESP.Encoder;

namespace CShredis.Network
{
	internal class Program
	{
		private static readonly ParseDispatcher _parseDispatcher = new();

		static async Task Main()
		{
			try
			{
				var client = new TcpClient();
				await client.ConnectAsync("127.0.0.1", 6379);
				using var stream = client.GetStream();

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
						.Select(m => m.Value)
						.ToArray(); 

					if (tokens.Length == 0)
					{
						Console.WriteLine();
						continue;
					}

					var tokenElements = new List<RESPObject>(tokens.Length);
					foreach (var token in tokens)
						tokenElements.Add(new RESPObject(RESPType.BulkString, token));

					var tokenRespArray = new RESPArray(tokenElements);
					var encodedTokenRespArray = Encoder.Encode(tokenRespArray);

					await stream.WriteAsync(encodedTokenRespArray);
					await stream.FlushAsync();

					var buffer = new byte[1024];
					int bytesRead = await stream.ReadAsync(buffer);
					var responseRespObject = _parseDispatcher.Parse(buffer[..bytesRead]).ParsedObject;

					Console.WriteLine(Printer.Print(responseRespObject!));
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Server crashed: {ex}");
			}
		}
	}
}
