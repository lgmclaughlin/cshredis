using System;
using System.Threading;

namespace CShredis.Network
{
	class Program
	{
		static void Main()
		{
			try
			{
				var server = new Server();
				var serverThread = new Thread(server.Start)
				{
					IsBackground = true
				};

				Console.WriteLine("Starting server...");

				serverThread.Start();

				if (!Console.IsInputRedirected)
				{
					Console.WriteLine("Server started. Press ESC to stop...");

					while (true)
					{
						var key = Console.ReadKey(intercept: true);
						if (key.Key == ConsoleKey.Escape)
							break;
					}

					Console.WriteLine("Stopping server...");
					
					server.Stop();
				}
				else
				{
					Console.WriteLine("Server started. Running in non-interactive mode.");
				}

				serverThread.Join();

				Console.WriteLine("Done.");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Server crashed: {ex}");
			}
		}
	}
}
