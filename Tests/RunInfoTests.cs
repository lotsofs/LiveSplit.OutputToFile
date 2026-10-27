using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// The root files from the run's metadata, and Variables\
	/// </summary>
	public class RunInfoTests {

		[Fact]
		public void RegionAndEmulatorAreWritten() {
			using (var run = TestRun.Base()) {
				Assert.Equal("", run.Read("RegionName.txt"));
				Assert.Equal("", run.Read("UsesEmulator.txt"));
				run.Edit(r => {
					r.Metadata.RegionName = "USA / NTSC";
					r.Metadata.UsesEmulator = true;
				});
				Assert.Equal("USA / NTSC", run.Read("RegionName.txt"));
				Assert.Equal("Emulator", run.Read("UsesEmulator.txt"));
				run.Edit(r => r.Metadata.UsesEmulator = false);
				Assert.Equal("", run.Read("UsesEmulator.txt"));
			}
		}

		[Fact]
		public void EachVariableHasAFileAndAllShareALine() {
			using (var run = TestRun.Base()) {
				Assert.Equal("", run.Read("Variables.txt"));
				run.Edit(r => {
					r.Metadata.VariableValueNames["Variant"] = "Unrestricted";
					r.Metadata.VariableValueNames["Glitches?"] = "No";
					r.Metadata.VariableValueNames["Turbo?"] = "Yes";
				});
				Assert.Equal("Unrestricted, No Glitches, Turbo", run.Read("Variables.txt"));
				Assert.Equal("Unrestricted", run.Read(@"Variables\Variant.txt"));
				// The file has the value as stored; only the name loses its question mark
				Assert.Equal("No", run.Read(@"Variables\Glitches.txt"));
				Assert.Equal("Yes", run.Read(@"Variables\Turbo.txt"));
			}
		}

		[Fact]
		public void VariableNamesBecomeFileNames() {
			using (var run = TestRun.Base()) {
				run.Edit(r => {
					r.Metadata.VariableValueNames["Speed {0}"] = "2x";
					r.Metadata.VariableValueNames["Any/All: Mode"] = "Hard";
					r.Metadata.VariableValueNames["Version. "] = "1.0";
				});
				Assert.Equal("2x", run.Read(@"Variables\Speed {0}.txt"));
				Assert.Equal("Hard", run.Read(@"Variables\Any_All_ Mode.txt"));
				Assert.Equal("1.0", run.Read(@"Variables\Version.txt"));
			}
		}

		[Fact]
		public void RemovedVariableHasItsFileEmptied() {
			using (var run = TestRun.Base()) {
				run.Edit(r => {
					r.Metadata.VariableValueNames["Variant"] = "Unrestricted";
					r.Metadata.VariableValueNames["Character"] = "Sonic";
				});
				// Edit Splits removes a variable set back to nothing
				run.Edit(r => r.Metadata.VariableValueNames.Remove("Character"));
				Assert.Equal("", run.Read(@"Variables\Character.txt"));
				Assert.Equal("Unrestricted", run.Read(@"Variables\Variant.txt"));
				Assert.Equal("Unrestricted", run.Read("Variables.txt"));
			}
		}
	}
}
