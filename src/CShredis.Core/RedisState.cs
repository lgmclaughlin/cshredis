using System;

namespace CShredis.Core
{
	public class RedisState
	{
		private int _selectedDb = 0;

		public RedisDb[] Databases { get; }

		public RedisDb CurrentDb => Databases[_selectedDb];

		public RedisState(int dbCount = 1)
		{
			Databases = new RedisDb[dbCount];
			for (int i = 0; i < dbCount; i++)
				Databases[i] = new RedisDb();
		}

		public void SwitchToDb(int db)
		{
			if (_selectedDb < Databases.Length)
				_selectedDb = db;
		}
	}
}
