using System.Text;

namespace CShredis.RESP
{
	public sealed class ParseResultArray : ParseResult
	{
		public ParseResult? Partial { get; private set; }
		public int DeclaredLength { get; private set; }
		
		public int Count => ParsedObject.Count ?? 0;
		public bool IsComplete => DeclaredLength == Count; 

		public ParseResultArray(RESPObject parsedArray)
			: this(parsedArray, parsedArray.Elements!.Count!) { }

		public ParseResultArray(int declaredLength)
			: this(RESPObject.Array(declaredLength), declaredLength) { }

		public ParseResultArray(
				RESPObject parsedArray,
				int declaredLength, 
				int bytesConsumed = 0)
			: base(parsedArray, bytesConsumed, ParseStatus.Incomplete)
		{
			DeclaredLength = declaredLength;
			UpdateStatus();
		}

		public void Add(RESPObject element)
		{
			ParsedObject.Add(element);
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
