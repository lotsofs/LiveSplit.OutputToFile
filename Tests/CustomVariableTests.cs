using LiveSplit.Model;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// CustomVariables\: LiveSplit's custom variables, which auto splitters and the server set with no event
	/// </summary>
	public class CustomVariableTests {

		const string FOLDER = @"CustomVariables\";

		[Fact]
		public void CustomVariableGetsAFile() {
			using (var run = TestRun.Base()) {
				RunMetadata metadata = run.State.Run.Metadata;
				metadata.SetCustomVariable("Deaths", "3");
				metadata.SetCustomVariable("Speed {0}", "fast");
				metadata.SetCustomVariable("Area/Zone", "1-2");
				run.Tick();
				Assert.Equal("3", run.Read(FOLDER + "Deaths.txt"));
				Assert.Equal("fast", run.Read(FOLDER + "Speed {0}.txt"));
				Assert.Equal("1-2", run.Read(FOLDER + "Area_Zone.txt"));
				metadata.SetCustomVariable("Deaths", "4");
				run.Tick();
				Assert.Equal("4", run.Read(FOLDER + "Deaths.txt"));
			}
		}

		[Fact]
		public void CustomVariableWithoutValueIsADash() {
			using (var run = TestRun.Base()) {
				// Reading a variable that isn't set adds it, with no value, as LiveSplit's Text component does
				run.State.Run.Metadata.CustomVariableValue("Deaths");
				run.Tick();
				Assert.Equal("-", run.Read(FOLDER + "Deaths.txt"));
				run.State.Run.Metadata.SetCustomVariable("Deaths", "");
				run.Tick();
				Assert.Equal("", run.Read(FOLDER + "Deaths.txt"));
			}
		}

		[Fact]
		public void CustomVariablesFollowTheSetting() {
			using (var run = TestRun.Base()) {
				run.State.Run.Metadata.SetCustomVariable("Deaths", "3");
				run.Tick();
				run.Settings.OutputCustomVariables = false;
				run.Tick();
				Assert.Equal("", run.Read(FOLDER + "Deaths.txt"));
				run.Settings.OutputCustomVariables = true;
				run.Tick();
				Assert.Equal("3", run.Read(FOLDER + "Deaths.txt"));
			}
		}

		[Fact]
		public void CustomVariableValueChangeRewritesNothingElse() {
			using (var run = TestRun.Base()) {
				// A new variable's file comes with a full rewrite
				run.State.Run.Metadata.SetCustomVariable("Deaths", "3");
				run.Tick();
				// Every full rewrite copies the split names into a new array
				FieldInfo splitNames = typeof(OutputToFileComponent).GetField("_splitNames", BindingFlags.NonPublic | BindingFlags.Instance);
				object before = splitNames.GetValue(run.Component);
				run.State.Run.Metadata.SetCustomVariable("Deaths", "4");
				run.Tick();
				Assert.Equal("4", run.Read(FOLDER + "Deaths.txt"));
				Assert.Same(before, splitNames.GetValue(run.Component));
			}
		}

		[Fact]
		public void CustomVariablesSurviveAFullRewrite() {
			using (var run = TestRun.Base()) {
				run.State.Run.Metadata.SetCustomVariable("Deaths", "3");
				run.Tick();
				// A rewrite empties the files it doesn't write
				run.Start();
				Assert.Equal("3", run.Read(FOLDER + "Deaths.txt"));
			}
		}

		[Fact]
		public void CustomVariablesOfReplacedSplitsAreEmptied() {
			using (var run = TestRun.Base()) {
				run.State.Run.Metadata.SetCustomVariable("Deaths", "3");
				run.Tick();
				run.SetRun(TestRun.MakeRun(TestRun.BaseSplits));
				Assert.Equal("", run.Read(FOLDER + "Deaths.txt"));
			}
		}

		[Fact]
		public void CustomVariablesSetFromAnotherThread() {
			using (var run = TestRun.Base()) {
				RunMetadata metadata = run.State.Run.Metadata;
				// As LiveSplit's server does, on its connection's thread: a new variable every 5 ms, up to 50, and new values all the time
				var server = new Thread(() => {
					var clock = Stopwatch.StartNew();
					for (int i = 0; clock.ElapsedMilliseconds < 300; i++) {
						int count = (int)System.Math.Min(50, clock.ElapsedMilliseconds / 5 + 1);
						metadata.SetCustomVariable("Var" + (i % count), i.ToString());
					}
				});
				server.Start();
				// Frames as fast as they come, without waiting for the files
				while (server.IsAlive) {
					run.Component.Update(null, run.State, 0, 0, LayoutMode.Vertical);
				}
				run.Tick();
				Assert.Equal(50, metadata.CustomVariables.Count);
				foreach (var variable in metadata.CustomVariables) {
					Assert.Equal(variable.Value.Value, run.Read(FOLDER + variable.Key + ".txt"));
				}
			}
		}
	}
}
