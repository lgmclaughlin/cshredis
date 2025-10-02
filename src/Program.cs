using System;

namespace CShredis
{
	class Program
	{
		static void Main()
		{
			try
			{
				var server = new Server();
				server.Start();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Server crashed: {ex}");
			}
		}
	}
}
