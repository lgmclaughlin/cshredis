using System.Text;

namespace CShredis.RESP
{
	public sealed record RESPArray(List<RESPObject> Elements, int DeclaredLength) : RESPObject
	{
		public override RESPType Type => RESPType.Array;

		private RESPObject? _partial;

		public RESPObject? Partial { get { return _partial; } }

		public int Count => Elements.Count;

		public bool IsComplete => DeclaredLength == Count;

		public RESPArray(List<RESPObject> Elements)
			: this(Elements, Elements.Count) { }

		public RESPArray(int declaredLength)
			: this(new List<RESPObject>(declaredLength), declaredLength) { }

		public override ReadOnlyMemory<byte> Encode() => ReadOnlyMemory<byte>.Empty;

		public void Add(RESPObject element) => Elements.Add(element);

		public void SetPartial(RESPObject? partial) => _partial = partial;
	}
}
