using System;
using System.Text;

namespace CShredis.Tests
{
	internal static class TestUtilities
	{
		public static ReadOnlyMemory<byte> StringToMemoryBytes(string s)
			=> new(Encoding.UTF8.GetBytes(s));
	}
}
