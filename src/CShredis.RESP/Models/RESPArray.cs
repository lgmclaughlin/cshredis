using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPArray : RESPObject
	{
		public List<RESPObject> Elements { get; private set; }
		public int Count => Elements.Count;

		public RESPArray(int declaredLength)
			: this(new List<RESPObject>(declaredLength)) { }

		public RESPArray(List<RESPObject> elements)
			: base(RESPType.Array)
		{
			Elements = elements;
		}

		public void Add(RESPObject element) => Elements.Add(element);
	}
}
