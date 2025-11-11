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

		public override ReadOnlyMemory<byte> Encode() =>
			Encoding.UTF8.GetBytes(EncodeString()).AsMemory();

		public override string EncodeString()
		{
			var encodedPrefix = $"{Type.QualifierString()}{Count.ToString()}\r\n";
			
			var encodedArray = new StringBuilder();
			foreach (var el in Elements)
				encodedArray.Append(el.EncodeString());

			return encodedPrefix + encodedArray.ToString();
		}

		public override string Print()
		{
			if (Count == 0) return "(empty array)";
			
			var sb = new StringBuilder();
			for (int i = 0; i < Elements.Count; i++)
				sb.AppendLine($"{i + 1} {Elements[i].Print()}");

			return sb.ToString();
		}

		public void Add(RESPObject element) => Elements.Add(element);

		public void SetPartial(RESPObject? partial) => _partial = partial;
	}
}
