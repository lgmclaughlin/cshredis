namespace CShredis.RESP
{
	public record ParseResult
	{
		public RESPObject? ParsedObject { get; set; }
		public int BytesConsumed { get; set; }
		public ParseStatus Status { get; set; }

		public static readonly ParseResult Incomplete = new(null, 0, ParseStatus.Incomplete);

		public ParseResult(
				RESPObject? parsedObject,
				int bytesConsumed,
				ParseStatus status = ParseStatus.Complete)
		{
			ParsedObject = parsedObject;
			BytesConsumed = bytesConsumed;
			Status = status;
		}
	}
}
