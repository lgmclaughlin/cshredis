using System;
using System.Text;

namespace CShredis.RESP.Tests
{
	internal static class RESPTestUtilities 
	{
		public static ReadOnlyMemory<byte> StringToMemoryBytes(string s)
			=> new(Encoding.UTF8.GetBytes(s));
	}
}
