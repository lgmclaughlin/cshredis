using System;

namespace CShredis.Core
{
	public class SetOptions
	{
		public long? ExpiryMs { get; set; } = null;
		public bool OnlyIfExists { get; set; } = false;
		public bool OnlyIfNotExists { get; set; } = false;
		public bool GetPrevious { get; set; } = false;

		public SetOptions() { }
	}
}
