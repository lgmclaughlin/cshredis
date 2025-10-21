namespace CShredis.RESP
{
	public record struct ParseResult(RESPObject? ParsedObject, int BytesConsumed, ParseStatus Status)
	{
		public static ParseResult Incomplete = new(null, 0, ParseStatus.Incomplete);

		public static ParseResult Partial(RESPObject? parsedObject, int bytesConsumed)
			=> new(parsedObject, bytesConsumed, ParseStatus.Partial);

		public static ParseResult Complete(RESPObject? parsedObject, int bytesConsumed)
			=> new(parsedObject, bytesConsumed, ParseStatus.Complete);
	}
}
