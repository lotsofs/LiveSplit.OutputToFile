using LiveSplit.TimeFormatters;
using System;

namespace LiveSplit.UI.Components.OutputToFile {
	/// <summary>
	/// The choices of the Times and Differences settings. Flex adds decimals only where a value would otherwise read as zero.
	/// </summary>
	public enum FormatAccuracy {
		Flex,
		Seconds,
		Tenths,
		Hundredths,
		Milliseconds,
	}

	/// <summary>
	/// Time and difference strings, made by LiveSplit's own formatters so they read as in LiveSplit's components.
	/// Values are truncated, not rounded. Negatives get the minus sign '−' (U+2212), and null shows as "-".
	/// </summary>
	static class TimeFormatter {

		static readonly TimeSpan TENTH = TimeSpan.FromSeconds(0.1);
		static readonly TimeSpan SECOND = TimeSpan.FromSeconds(1);

		// One formatter per LiveSplit accuracy, indexed by it. Used on the UI thread only.
		// Times show minutes, as 0:05. LiveSplit's own writes a null time as 0, so set it to "-".
		static readonly GeneralTimeFormatter[] _durations = Formatters(accuracy => new RegularTimeFormatter(accuracy) { NullFormat = NullFormat.Dash });
		// Differences have a sign, and no decimals from a minute on
		static readonly GeneralTimeFormatter[] _deltas = Formatters(accuracy => new DeltaTimeFormatter { Accuracy = accuracy });

		static GeneralTimeFormatter[] Formatters(Func<TimeAccuracy, GeneralTimeFormatter> create) {
			var accuracies = (TimeAccuracy[])Enum.GetValues(typeof(TimeAccuracy));
			var formatters = new GeneralTimeFormatter[accuracies.Length];
			foreach (TimeAccuracy accuracy in accuracies) {
				formatters[(int)accuracy] = create(accuracy);
			}
			return formatters;
		}

		/// <summary>
		/// A time: 0:05, 1:05, 1:02:05, with the accuracy's decimals. Flex: whole seconds, but tenths under a second and hundredths under a tenth.
		/// </summary>
		public static string DurationString(TimeSpan? t, FormatAccuracy accuracy) {
			TimeAccuracy timeAccuracy;
			if (accuracy == FormatAccuracy.Flex) {
				TimeSpan size = t?.Duration() ?? TimeSpan.Zero;
				timeAccuracy = size < TENTH ? TimeAccuracy.Hundredths : size < SECOND ? TimeAccuracy.Tenths : TimeAccuracy.Seconds;
			}
			else {
				timeAccuracy = Fixed(accuracy);
			}
			return _durations[(int)timeAccuracy].Format(t);
		}

		/// <summary>
		/// A difference: +5.2, −5.2, +1:05, with the accuracy's decimals, and none from a minute on. No sign for zero.
		/// Flex: tenths, but hundredths under a tenth.
		/// </summary>
		public static string DeltaString(TimeSpan? t, FormatAccuracy accuracy) {
			TimeAccuracy timeAccuracy;
			if (accuracy == FormatAccuracy.Flex) {
				timeAccuracy = t?.Duration() < TENTH ? TimeAccuracy.Hundredths : TimeAccuracy.Tenths;
			}
			else {
				timeAccuracy = Fixed(accuracy);
			}
			return _deltas[(int)timeAccuracy].Format(t);
		}

		// LiveSplit's accuracy for a choice other than Flex
		static TimeAccuracy Fixed(FormatAccuracy accuracy) {
			switch (accuracy) {
				case FormatAccuracy.Seconds: return TimeAccuracy.Seconds;
				case FormatAccuracy.Tenths: return TimeAccuracy.Tenths;
				case FormatAccuracy.Hundredths: return TimeAccuracy.Hundredths;
				case FormatAccuracy.Milliseconds: return TimeAccuracy.Milliseconds;
				default: throw new ArgumentOutOfRangeException(nameof(accuracy), accuracy, null);
			}
		}
	}
}
