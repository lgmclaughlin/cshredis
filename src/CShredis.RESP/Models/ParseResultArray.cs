using System.Text;

namespace CShredis.RESP
{
	public sealed record ParseResultArray : ParseResult
	{
		public ParseResult? Partial { get; private set; }
		public int DeclaredLength { get; private set; }
		
		public int Count => ParsedArray.Count;
		public bool IsComplete => DeclaredLength == Count; 
		public RESPArray ParsedArray => (RESPArray)ParsedObject!;

		public ParseResultArray(RESPArray parsedArray)
			: this(parsedArray, parsedArray.Elements.Count) { }

		public ParseResultArray(int declaredLength)
			: this(new RESPArray(declaredLength), declaredLength) { }

		public ParseResultArray(
				RESPArray parsedArray,
				int declaredLength, 
				int bytesConsumed = 0)
			: base(parsedArray, bytesConsumed, ParseStatus.Incomplete)
		{
			DeclaredLength = declaredLength;
			UpdateStatus();
		}

		public void Add(RESPObject element)
		{
			ParsedArray.Add(element);
			UpdateStatus();
		}

		public void SetPartial(ParseResult? partial)
		{
			Partial = partial;
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			if (IsComplete)
				Status = ParseStatus.Complete;
			else
				Status = ParseStatus.Partial;
		}
	}
}
