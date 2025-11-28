using System;

namespace CShredis.Core
{
	public enum DbResult
	{
		Success,
		KeyTypeNotFound,
		WrongType,
		InvalidParameter,
		NotSet
	}
}
