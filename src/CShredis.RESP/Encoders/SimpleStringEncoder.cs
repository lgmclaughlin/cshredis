using System;

namespace CShredis.RESP
{
	public class SimpleStringEncoder
	{
		public RESPSimpleString Encode(string value)
		{
			var encodedValue = RESPType.SimpleString.Qualifier() + value + "\r\n";

			return new RESPSimpleString(encodedValue);
		}
	}
}
