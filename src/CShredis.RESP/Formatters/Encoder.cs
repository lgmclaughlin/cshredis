using System;
using System.Text;

namespace CShredis.RESP
{
	public static class Encoder
	{
		private static readonly Dictionary<RESPType, Func<RESPObject, string>> _encoders = new()
			{
				{ RESPType.Array, EncodeArray },
				{ RESPType.BulkError, EncodeBulk },
				{ RESPType.BulkString, EncodeBulk },
				{ RESPType.Integer, EncodeDefault },
				{ RESPType.NullArray, EncodeNull },
				{ RESPType.NullBulkString, EncodeNull },
				{ RESPType.SimpleError, EncodeDefault },
				{ RESPType.SimpleString, EncodeDefault }
			};

		public static string EncodeString(RESPObject respObject)
		{
			if (!_encoders.TryGetValue(respObject.Type, out var encoder))
				throw new InvalidOperationException($"No encoder found for RESP type'{respObject.Type.QualifierString()}'");

			return encoder(respObject);
		}

		public static ReadOnlyMemory<byte> Encode(RESPObject respObject)
			=> Encoding.UTF8.GetBytes(EncodeString(respObject)).AsMemory();

		public static string EncodeDefault(RESPObject respObject)
			=> $"{respObject.Type.QualifierString()}{respObject.ValueString}\r\n";

		public static string EncodeNull(RESPObject respObject)
			=> $"{respObject.Type.QualifierString()}-1\r\n";

		public static string EncodeArray(RESPObject respObject)
		{
			if (respObject.Elements is null)
				throw new InvalidOperationException("Cannot encode RESP array from null elements.");

			var encodedPrefix = $"{respObject.Type.QualifierString()}{respObject.Count.ToString()}\r\n";
			
			var encodedValue = new StringBuilder();
			encodedValue.Append(encodedPrefix);

			foreach (var el in respObject.Elements)
				encodedValue.Append(EncodeString(el));

			return encodedValue.ToString();
		}

		public static string EncodeBulk(RESPObject respObject)
			=> $"{respObject.Type.QualifierString()}{respObject.Length.ToString()}\r\n{respObject.ValueString}\r\n";
	}
}
