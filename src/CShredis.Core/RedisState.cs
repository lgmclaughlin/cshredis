using System;

namespace CShredis.Core
{
	public class RedisState
	{
		private int _selectedDb = 0;

		public RedisDatabase[] Databases { get; }

		public RedisDatabase CurrentDb => Databases[_selectedDb];

		public RedisState(int dbCount = 1)
		{
			Databases = new RedisDatabase[dbCount];
			for (int i = 0; i < dbCount; i++)
				Databases[i] = new RedisDatabase();
		}

		public void SwitchToDb(int db)
		{
			if (_selectedDb < Databases.Length)
				_selectedDb = db;
		}
	}
}
