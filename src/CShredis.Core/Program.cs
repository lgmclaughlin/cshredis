using System;
using System.Threading;

namespace CShredis.Core
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
				serverThread.Start();

				Console.WriteLine("Server started. Press ESC to stop...");

				while (true)
				{
					var key = Console.ReadKey(intercept: true);
					if (key.Key == ConsoleKey.Escape)
						break;
				}

				Console.WriteLine("Stopping server...");
				
				server.Stop();
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
