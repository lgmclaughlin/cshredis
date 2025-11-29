using System;
using System.Linq;
using CShredis.RESP;
using CShredis.Core;

namespace CShredis.Commands
{
	public static class CommandUtilities
	{
		public static RESPObject RESPObjectFrom(RedisObject redisObject)
		{
			switch (redisObject.Type)
			{
				case RedisType.String:
					return RESPObject.BulkString(redisObject.AsString());

				case RedisType.List:
					var redisObjects = redisObject.AsList();
					var respObjects = redisObjects.Select(r => RESPObjectFrom(r)).ToList();
					return RESPObject.Array(respObjects);

				default:
					throw new InvalidOperationException(
							$"Unknown RedisType found while converting RedisObject to RESPObject: {redisObject.Type}");
			}
		}

		public static bool ValidateDbResult(DbResult result, out CommandResult? commandResultError)
		{
			commandResultError = null;

			switch (result)
			{
				case DbResult.WrongType:
					commandResultError = CommandResult.SimpleError(ResponseMessages.WrongType_KeyOperationTypeMismatch);
					return false;

				case DbResult.Success:
					return true;

				default:
					throw new InvalidOperationException($"Invalid DbResult found while validating: {result}");
			}
		}
	}
}
