namespace CShredis.RESP
{
	public static class ParseUtilities
	{
		public static bool TryParseType(ReadOnlySpan<byte> span, byte expectedTypeByte, string typeName)
		{
			if (span.Length == 0)
				return false;

			if (span[0] != expectedTypeByte)
				throw new InvalidOperationException(
						$"Type mismatch: expected '{(char)expectedTypeByte}' for {typeName}, saw '{(char)span[0]}'.");

			return true;
		}

		public static bool TryParseLength(ReadOnlySpan<byte> span, out int length, out int bytesConsumed)
        {
            length = 0;
            bytesConsumed = 0;
			
			int index = 1;

            if (index >= span.Length)
                return false;

            int sign = 1;

            if (span[index] == (byte)'-')
            {
                sign = -1;
                index++;
            }

            if (index >= span.Length)
                return false;

            int result = 0;
            bool sawDigit = false;

            for (; index < span.Length; index++)
            {
                byte b = span[index];

                if (b == (byte)'\r')
                {
					if (!sawDigit)
						throw new InvalidOperationException("Invalid length. Must include integers.");

                    if (index + 1 >= span.Length)
                        return false; // incomplete CRLF

                    if (span[index + 1] != (byte)'\n')
                        throw new InvalidOperationException(
							$"Invalid length terminator: expected '\\r\\n', saw {(char)span[index]}{(char)span[index + 1]}.");

                    length = result * sign;
                    bytesConsumed = index + 2; // include CRLF

                    return true;
                }

                if (b < (byte)'0' || b > (byte)'9')
                    throw new InvalidOperationException($"Invalid character in length: {(char)b}");

                sawDigit = true;
                result = checked(result * 10 + (b - (byte)'0'));
            }

            return false;
        }
	}
}
