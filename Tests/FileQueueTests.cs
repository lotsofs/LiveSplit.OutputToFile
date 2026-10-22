using System.IO;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// Writing the files in the background: skipping unchanged contents, and reporting errors
	/// </summary>
	public class FileQueueTests {

		[Fact]
		public void LockedFileIsReportedAndWrittenOnceFree() {
			using (var run = TestRun.Base()) {
				const string NAME = @"PreviousSplit\AllSplits\Name.txt";
				run.Start();
				using (new FileStream(Path.Combine(run.Folder, NAME), FileMode.Open, FileAccess.Read, FileShare.None)) {
					run.Split(9.5);
					// The error happens on the background thread, after the frame that queued the file. The next frame shows it.
					run.Tick();
					Assert.StartsWith(@"Couldn't write " + NAME, run.Settings.WriteError);
				}
				run.Split(21);
				run.Tick();
				Assert.Equal("A2", run.Read(NAME));
				Assert.Null(run.Settings.WriteError);
			}
		}

		[Fact]
		public void UnchangedContentsAreSkippedUntilForgotten() {
			using (var folder = new TempFolder()) {
				var queue = new FileQueue();
				string path = Path.Combine(folder.Path, "File.txt");
				queue.Write(path, "File.txt", "one");
				queue.WaitForWrites(-1);
				File.WriteAllText(path, "edited");

				queue.Write(path, "File.txt", "one");
				queue.WaitForWrites(-1);
				Assert.Equal("edited", File.ReadAllText(path));

				queue.ForgetWrittenFiles();
				queue.Write(path, "File.txt", "one");
				queue.WaitForWrites(-1);
				Assert.Equal("one", File.ReadAllText(path));
			}
		}
	}
}
