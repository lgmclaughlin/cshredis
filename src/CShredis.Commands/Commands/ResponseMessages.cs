using System;

namespace CShredis.Commands
{
	public static class ResponseMessages
	{
		public static string Success_OK = "OK";

		public static string Error_InvalidInteger = "value is not an integer or out of range";
		public static string Error_InvalidExpireTimeIn(string commandName) => $"invalid expire time in '{commandName.ToLower()}' command";
		public static string Error_Syntax = "syntax error";
		public static string Error_WrongNumberOfArguments = "ERR wrong number of arguments for command";

		public static string WrongType_KeyOperationTypeMismatch = "WRONGTYPE Operation against a key holding the wrong kind of value";
	}
}
