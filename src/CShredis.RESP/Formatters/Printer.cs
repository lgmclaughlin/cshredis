using System;
using System.Text;

namespace CShredis.RESP
{
	public static class Printer
	{
		private static readonly Dictionary<RESPType, Func<RESPObject, string>> _printers = new()
			{
				{ RESPType.Array, PrintArray },
				{ RESPType.BulkError, PrintDefault },
				{ RESPType.BulkString, PrintDefault },
				{ RESPType.Integer, PrintInteger },
				{ RESPType.NullArray, PrintNull },
				{ RESPType.NullBulkString, PrintNull },
				{ RESPType.SimpleError, PrintSimpleError },
				{ RESPType.SimpleString, PrintDefault }
			};

		public static string Print(RESPObject respObject)
		{
			if (!_printers.TryGetValue(respObject.Type, out var printer))
				throw new InvalidOperationException($"No printer found for RESP type '{respObject.Type.QualifierString()}'");

			return printer(respObject);
		}

		public static string PrintDefault(RESPObject respObject) => $"\"{respObject.ValueString}\"";

		public static string PrintNull(RESPObject respObject) => "(nil)";

		public static string PrintArray(RESPObject respObject)
		{
			if ((respObject.Count ?? 0) == 0) return "(empty array)";
			
			var count = respObject.Count;

			var printedArray = new StringBuilder();
			for (int i = 0; i < respObject.Count - 1; i++)
				printedArray.AppendLine($"{i + 1}) {Print(respObject.Elements[i])}");

			printedArray.Append($"{count}) {Print(respObject.Elements[^1])}");

			return printedArray.ToString();
		}

		public static string PrintInteger(RESPObject respObject)
			=> $"(integer) {respObject.ValueString}";

		public static string PrintSimpleError(RESPObject respObject)
			=> $"(error) {respObject.ValueString}";
	}
}
