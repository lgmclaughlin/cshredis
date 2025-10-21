using System;
using System.Text;
using Xunit;
using CShredis.RESP;
using Utils = CShredis.RESP.Tests.HandlerTestUtilities;

namespace CShredis.RESP.Tests
{
	public class UtilitiesTests
	{
		[Theory]
		[InlineData("$3\r\nhey\r\n")]
		[InlineData("$3\r\nhey\r")]
		[InlineData("$3\r\n")]
		public void ValidLength_ReturnsLength(string input)
		{
			ReadOnlySpan<byte> span = Utils.StringToMemoryBytes(input).Span;
			var expectedLength = 3;
			var expectedBytesConsumed = 4;

			bool complete = Utilities.TryParseLength(span, out int length, out int bytesConsumed);

			Assert.True(complete);
			Assert.Equal(expectedLength, length);
			Assert.Equal(expectedBytesConsumed, bytesConsumed);
		}

		[Theory]
		[InlineData("$3\r\n")]
		[InlineData("*3\r\n")]
		[InlineData("%3\r\n")]
		public void ValidLengthVariableType_ReturnsLength(string input)
		{
			ReadOnlySpan<byte> span = Utils.StringToMemoryBytes(input).Span;
			var expectedLength = 3;
			var expectedBytesConsumed = 4;

			bool complete = Utilities.TryParseLength(span, out int length, out int bytesConsumed);

			Assert.True(complete);
			Assert.Equal(expectedLength, length);
			Assert.Equal(expectedBytesConsumed, bytesConsumed);
		}

		[Fact]
		public void ValidMultiDigitLength_ReturnsLength()
		{
			ReadOnlySpan<byte> span = Utils.StringToMemoryBytes("$158\r\nhey\r\n").Span;
			var expectedLength = 158;
			var expectedBytesConsumed = 6;

			bool complete = Utilities.TryParseLength(span, out int length, out int bytesConsumed);

			Assert.True(complete);
			Assert.Equal(expectedLength, length);
			Assert.Equal(expectedBytesConsumed, bytesConsumed);
		}

		[Fact]
		public void ValidNullLength_ReturnsNullLength()
		{
			ReadOnlySpan<byte> span = Utils.StringToMemoryBytes("$-1\r\n").Span;
			var expectedLength = -1;
			var expectedBytesConsumed = 5;

			bool complete = Utilities.TryParseLength(span, out int length, out int bytesConsumed);

			Assert.True(complete);
			Assert.Equal(expectedLength, length);
			Assert.Equal(expectedBytesConsumed, bytesConsumed);
		}

		[Theory]
		[InlineData("$3\r")]
		[InlineData("$3")]
		[InlineData("$")]
		[InlineData("$-1\r")]
		public void IncompleteLength_ReturnsIncomplete(string input)
		{
			ReadOnlySpan<byte> span = Utils.StringToMemoryBytes(input).Span;
			var expectedLength = 0;
			var expectedBytesConsumed = 0;

			bool complete = Utilities.TryParseLength(span, out int length, out int bytesConsumed);

			Assert.False(complete);
			Assert.Equal(expectedLength, length);
			Assert.Equal(expectedBytesConsumed, bytesConsumed);
		}
	
		[Theory]
		[InlineData("$-\r\n")]
		[InlineData("$-\r")]
		[InlineData("$\r\n")]
		public void InvalidLengthNoDigits_ThrowsException(string input)
		{
			byte[] span = Encoding.UTF8.GetBytes(input);

			Assert.Throws<InvalidOperationException>(() 
					=> Utilities.TryParseLength(span, out int length, out int bytesConsumed));
		}
	
		[Theory]
		[InlineData("$9abc\r\n")]
		[InlineData("$abc\r\n")]
		public void InvalidLengthBadChars_ThrowsException(string input)
		{
			byte[] span = Encoding.UTF8.GetBytes(input);

			Assert.Throws<InvalidOperationException>(() 
					=> Utilities.TryParseLength(span, out int length, out int bytesConsumed));
		}

		[Fact]
		public void InvalidCRLF_ThrowsException()
		{
			byte[] span = Encoding.UTF8.GetBytes("$3??");

			Assert.Throws<InvalidOperationException>(() 
					=> Utilities.TryParseLength(span, out int length, out int bytesConsumed));
		}
	}
}
