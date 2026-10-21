using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.OutputToFile.UI.Components.OutputToFile {
	static class TimeFormatter {

		public enum TimeFormats {
			X = 0,			//          .0
			XX = 1,			//          .05
			S = 2,			//         0
			SX = 3,			//         0.0
			SXX = 4,		//         0.05
			SS = 5,			//        00
			SSX = 6,		//        00.0
			SSXX = 7,		//        00.05
			MSS = 8,		//      0:00
			MSSX = 9,		//      0:00.0
			MSSXX = 10,		//      0:00.05
			MMSS = 11,		//     00:00
			MMSSX = 12,		//     00:00.0
			MMSSXX = 13,	//     00:00.05
			HMMSS = 14,		//   0:00:00
			HMMSSX = 15,    //   0:00:00.0
			HMMSSXX = 16,   //   0:00:00.05
			HHMMSS = 17,    //  00:00:00
			HHMMSSX = 18,   //  00:00:00.0
			HHMMSSXX = 19,  //  00:00:00.05
		};

		// TODO: Make this a configurable setting
		public static TimeFormats DetermineTimeFormat(TimeSpan t) {
			long ms = (long)t.TotalMilliseconds;
			if (ms < 100) return TimeFormats.XX;
			if (ms < 60000) return TimeFormats.SX;
			return TimeFormats.MSS;
		}

		public static string DurationString(TimeSpan? t) {
			if (t == null) return "-";
			if (t.Value < TimeSpan.Zero) {
				return "-" + FormatTime(t.Value.Negate(), TimeFormats.MSS);
			}
			else {
				return FormatTime(t.Value, TimeFormats.MSS);
			}
		}
		
		public static string DeltaString(TimeSpan? t) {
			if (t == null) return "-";
			if (t.Value < TimeSpan.Zero) {
				return "-" + FormatTime(t.Value.Negate(), DetermineTimeFormat(t.Value.Negate()));
			}
			else {
				return "+" + FormatTime(t.Value, DetermineTimeFormat(t.Value)); 
			}
		}

		static string FormatTime(TimeSpan t, TimeFormats format) {
			long ms = (long)t.TotalMilliseconds;
			long hours = ms / 3600000;
			ms -= hours * 3600000;
			long minutes = ms / 60000;
			ms -= minutes * 60000;
			long seconds = ms / 1000;
			ms -= seconds * 1000;

			string stamp = "";
			// hours
			if (hours > 9 || (int)format >= 17) {
				stamp += hours.ToString("D2") + ":";
			}
			else if (hours > 0 || (int)format >= 14) {
				stamp += hours.ToString("D1") + ":";
			}
			// minutes
			if (minutes > 9 || (int)format >= 11 || hours > 0) {
				stamp += minutes.ToString("D2") + ":";
			}
			else if (minutes > 0 || (int)format >= 8) {
				stamp += minutes.ToString("D1") + ":";
			}
			// seconds
			if (seconds > 9 || (int)format >= 5 || minutes > 0 || hours > 0) {
				stamp += seconds.ToString("D2");
			}
			else if (seconds > 0 || (int)format >= 2) {
				stamp += seconds.ToString("D1");
			}
			// deciseconds
			if ((int)format % 3 != 2) {
				stamp += "." + (ms / 100).ToString("D1");
			}
			// centiseconds
			if ((int)format % 3 == 1) {
				stamp += ((ms % 100) / 10).ToString("D1");
			}
			return stamp;
		}

	}
}
