using System;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// The time and difference strings at each Time Format choice, as in output-files.md's Formatting. Values are truncated.
	/// </summary>
	public class TimeFormatterTests {

		[Theory]
		[InlineData(5.234, FormatAccuracy.Flex, "0:05")]
		[InlineData(5.234, FormatAccuracy.Seconds, "0:05")]
		[InlineData(5.234, FormatAccuracy.Tenths, "0:05.2")]
		[InlineData(5.234, FormatAccuracy.Hundredths, "0:05.23")]
		[InlineData(5.234, FormatAccuracy.Milliseconds, "0:05.234")]
		[InlineData(0.4, FormatAccuracy.Flex, "0:00.4")]
		[InlineData(0.05, FormatAccuracy.Flex, "0:00.05")]
		[InlineData(65.2, FormatAccuracy.Seconds, "1:05")]
		[InlineData(3725, FormatAccuracy.Seconds, "1:02:05")]
		[InlineData(-5.234, FormatAccuracy.Seconds, "−0:05")]
		public void Duration(double seconds, FormatAccuracy accuracy, string expected) {
			Assert.Equal(expected, TimeFormatter.DurationString(TimeSpan.FromSeconds(seconds), accuracy));
		}

		[Theory]
		[InlineData(5.234, FormatAccuracy.Flex, "+5.2")]
		[InlineData(5.234, FormatAccuracy.Seconds, "+5")]
		[InlineData(5.234, FormatAccuracy.Tenths, "+5.2")]
		[InlineData(5.234, FormatAccuracy.Hundredths, "+5.23")]
		[InlineData(5.234, FormatAccuracy.Milliseconds, "+5.234")]
		[InlineData(0.052, FormatAccuracy.Flex, "+0.05")]
		[InlineData(0.052, FormatAccuracy.Seconds, "+0")]
		[InlineData(0.052, FormatAccuracy.Tenths, "+0.0")]
		[InlineData(0.052, FormatAccuracy.Hundredths, "+0.05")]
		[InlineData(0.052, FormatAccuracy.Milliseconds, "+0.052")]
		[InlineData(-5.234, FormatAccuracy.Flex, "−5.2")]
		[InlineData(-0.052, FormatAccuracy.Flex, "−0.05")]
		[InlineData(65.2, FormatAccuracy.Flex, "+1:05")]
		[InlineData(-65.2, FormatAccuracy.Flex, "−1:05")]
		[InlineData(0, FormatAccuracy.Flex, "0.00")]
		public void Delta(double seconds, FormatAccuracy accuracy, string expected) {
			Assert.Equal(expected, TimeFormatter.DeltaString(TimeSpan.FromSeconds(seconds), accuracy));
		}

		[Theory]
		[InlineData(FormatAccuracy.Flex)]
		[InlineData(FormatAccuracy.Milliseconds)]
		public void NoValueIsADash(FormatAccuracy accuracy) {
			Assert.Equal("-", TimeFormatter.DurationString(null, accuracy));
			Assert.Equal("-", TimeFormatter.DeltaString(null, accuracy));
		}
	}
}
