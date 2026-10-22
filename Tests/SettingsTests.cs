using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using Xunit;

namespace LiveSplit.UI.Components.OutputToFile.Tests {
	/// <summary>
	/// Saving and loading the settings, and the controls' bindings. No window is shown.
	/// </summary>
	public class SettingsTests {

		[Fact]
		public void EverySettingSurvivesASaveAndLoad() {
			using (var saved = new OutputToFileSettings {
				FolderPath = @"C:\Stream",
				SplitsBefore = 3,
				SplitsAfter = 4,
				OutputTimer = true,
				OutputSplitList = false,
				OutputSubsplits = false,
				OutputCompletedSplits = true,
				AllCustomComparisons = true,
				TimeAccuracy = FormatAccuracy.Milliseconds,
				DeltaAccuracy = FormatAccuracy.Tenths,
			})
			using (var loaded = new OutputToFileSettings()) {
				saved.SetComparisonTicked("Target", true);
				saved.SetComparisonTicked("Best Segments", true);
				loaded.SetSettings(saved.GetSettings(new XmlDocument()));

				Assert.Equal(@"C:\Stream", loaded.FolderPath);
				Assert.Equal(3, loaded.SplitsBefore);
				Assert.Equal(4, loaded.SplitsAfter);
				Assert.True(loaded.OutputTimer);
				Assert.False(loaded.OutputSplitList);
				Assert.False(loaded.OutputSubsplits);
				Assert.True(loaded.OutputCompletedSplits);
				Assert.True(loaded.AllCustomComparisons);
				Assert.Equal(FormatAccuracy.Milliseconds, loaded.TimeAccuracy);
				Assert.Equal(FormatAccuracy.Tenths, loaded.DeltaAccuracy);
				Assert.Equal(new[] { "Best Segments", "Target" }, loaded.TickedComparisons.OrderBy(name => name, StringComparer.Ordinal));
			}
		}

		[Fact]
		public void LayoutWithoutTheSettingsLoadsTheDefaults() {
			using (var settings = new OutputToFileSettings {
				FolderPath = @"C:\Stream",
				SplitsBefore = 3,
				OutputCompletedSplits = true,
				TimeAccuracy = FormatAccuracy.Milliseconds,
			}) {
				settings.SetComparisonTicked("Target", true);
				settings.SetSettings(Xml("<Settings/>"));

				Assert.Equal("", settings.FolderPath);
				Assert.Equal(10, settings.SplitsBefore);
				Assert.Equal(10, settings.SplitsAfter);
				Assert.False(settings.OutputTimer);
				Assert.True(settings.OutputSplitList);
				Assert.True(settings.OutputSubsplits);
				Assert.False(settings.OutputCompletedSplits);
				Assert.False(settings.AllCustomComparisons);
				Assert.Equal(FormatAccuracy.Seconds, settings.TimeAccuracy);
				Assert.Equal(FormatAccuracy.Flex, settings.DeltaAccuracy);
				Assert.Empty(settings.TickedComparisons);
			}
		}

		[Fact]
		public void NewSettingsHaveTheDefaults() {
			using (var settings = new OutputToFileSettings())
			using (var loaded = new OutputToFileSettings()) {
				loaded.SetSettings(Xml("<Settings/>"));
				Assert.Equal(loaded.GetSettings(new XmlDocument()).OuterXml, settings.GetSettings(new XmlDocument()).OuterXml);
			}
		}

		[Fact]
		public void UnknownAccuracyLoadsTheDefault() {
			using (var settings = new OutputToFileSettings()) {
				settings.SetSettings(Xml("<Settings><TimeAccuracy>Nothing</TimeAccuracy><DeltaAccuracy>42</DeltaAccuracy></Settings>"));
				Assert.Equal(FormatAccuracy.Seconds, settings.TimeAccuracy);
				Assert.Equal(FormatAccuracy.Flex, settings.DeltaAccuracy);
			}
		}

		[Fact]
		public void HashFollowsTheSettings() {
			using (var settings = new OutputToFileSettings()) {
				int hash = settings.GetSettingsHashCode();
				settings.OutputCompletedSplits = true;
				Assert.NotEqual(hash, settings.GetSettingsHashCode());
				hash = settings.GetSettingsHashCode();
				settings.SetComparisonTicked("Target", true);
				Assert.NotEqual(hash, settings.GetSettingsHashCode());
			}
		}

		[Fact]
		public void ControlsSetTheirSettings() {
			using (var form = new Form())
			using (var settings = new OutputToFileSettings()) {
				form.Controls.Add(settings);
				// Creates the handles without showing the form: the bindings only work with them
				typeof(Control).GetMethod("CreateControl", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool) }, null)
					.Invoke(form, new object[] { true });

				Control<CheckBox>(settings, "checkBoxOutputCompletedSplits").Checked = true;
				Assert.True(settings.OutputCompletedSplits);
				Control<NumericUpDown>(settings, "numericUpDownSplitListBefore").Value = 3;
				Assert.Equal(3, settings.SplitsBefore);
				Control<ComboBox>(settings, "comboBoxTimeAccuracy").SelectedIndex = (int)FormatAccuracy.Milliseconds;
				Assert.Equal(FormatAccuracy.Milliseconds, settings.TimeAccuracy);
				Control<CheckBox>(settings, "checkBoxOutputSplitList").Checked = false;
				Assert.False(settings.OutputSplitList);
				Assert.False(Control<GroupBox>(settings, "groupBoxSplitList").Enabled);

				// Removed first, or disposing the form would dispose it twice
				form.Controls.Remove(settings);
			}
		}

		static T Control<T>(OutputToFileSettings settings, string name) where T : Control {
			return (T)typeof(OutputToFileSettings).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(settings);
		}

		static XmlNode Xml(string xml) {
			var document = new XmlDocument();
			document.LoadXml(xml);
			return document.DocumentElement;
		}
	}
}
