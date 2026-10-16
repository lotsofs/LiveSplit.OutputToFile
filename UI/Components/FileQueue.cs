using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes the output files on a background thread, so LiveSplit's UI thread only queues them.
	/// Keeps only the latest contents per file, and skips contents that are already on disk.
	/// Empties the output files that a full rewrite no longer writes (BeginSweep, EndSweep).
	/// One instance is shared by the writers. Every field is guarded by _lock.
	/// </summary>
	class FileQueue {

		readonly object _lock = new object();

		// Full path → (path relative to the folder, contents) waiting to be written
		Dictionary<string, (string FileName, string Contents)> _pending = new Dictionary<string, (string, string)>();
		// Full path → the contents last handed to the disk. Files whose contents haven't changed are skipped.
		readonly Dictionary<string, string> _written = new Dictionary<string, string>();

		// Full paths queued since BeginSweep, or null outside a sweep. Like the dictionaries above, they are exactly as MakeFile makes them.
		HashSet<string> _sweep;
		// The files the last full rewrite wrote, or null before the first. Never changed once stored, so the drain can read it.
		HashSet<string> _swept;
		// A search for files to empty, run once nothing is pending, or null
		(string Folder, HashSet<string> Keep, string[] Subfolders, string RootFiles)? _scan;

		bool _draining;
		Task _drain;

		// The latest failed write, for the settings panel
		string _errorFile;
		string _error;
		bool _errorChanged;

		/// <summary>
		/// Queue a file to be written, unless it already has, or will have, these contents.
		/// </summary>
		/// <param name="path">Full path</param>
		/// <param name="fileName">Path relative to the folder, for error messages</param>
		public void Write(string path, string fileName, string contents) {
			lock (_lock) {
				// Before the skips: a file whose contents haven't changed is still being written
				_sweep?.Add(path);
				if (_pending.TryGetValue(path, out var pending)) {
					if (pending.Contents == contents) return;
				}
				else if (_written.TryGetValue(path, out string written) && written == contents) {
					return;
				}
				_pending[path] = (fileName, contents);
				StartDrain();
			}
		}

		// Call under _lock
		void StartDrain() {
			if (_draining) return;
			_draining = true;
			_drain = Task.Run(() => Drain());
		}

		/// <summary>
		/// Start a full rewrite: record every file queued until EndSweep, including those skipped as unchanged.
		/// </summary>
		public void BeginSweep() {
			lock (_lock) {
				_sweep = new HashSet<string>();
			}
		}

		/// <summary>
		/// End a full rewrite. If it wrote a different set of files than the last one, empty every file in the subfolders,
		/// and every file in the folder itself that matches rootFiles, that it didn't write: outputs turned off, levels that are gone,
		/// and leftovers from earlier sessions. The first sweep always searches. Files that are empty already are left alone.
		/// </summary>
		/// <param name="folder">The output folder, as the writers combine it with each file's path</param>
		/// <param name="subfolders">The folders in the folder that belong to the writers</param>
		/// <param name="rootFiles">A search pattern for the writers' files in the folder itself that aren't always written</param>
		public void EndSweep(string folder, string[] subfolders, string rootFiles) {
			lock (_lock) {
				var written = _sweep;
				_sweep = null;
				if (written == null || (_swept != null && _swept.SetEquals(written))) return;
				_swept = written;
				// Files no longer written: drop their queued contents, which the search would otherwise race,
				// and forget what they hold, so they're written in full if they come back
				foreach (string path in _pending.Keys.Where(p => !written.Contains(p)).ToList()) {
					_pending.Remove(path);
				}
				foreach (string path in _written.Keys.Where(p => !written.Contains(p)).ToList()) {
					_written.Remove(path);
				}
				if (string.IsNullOrEmpty(folder)) return;
				_scan = (folder, written, subfolders, rootFiles);
				StartDrain();
			}
		}

		// Writes batches until nothing is pending, then runs the search for files to empty, if one is due
		void Drain() {
			while (true) {
				Dictionary<string, (string FileName, string Contents)> batch = null;
				(string Folder, HashSet<string> Keep, string[] Subfolders, string RootFiles)? scan = null;
				lock (_lock) {
					if (_pending.Count > 0) {
						batch = _pending;
						_pending = new Dictionary<string, (string, string)>();
						// Recorded now, so a file queued again while this batch is written compares against what's in flight
						foreach (var file in batch) {
							_written[file.Key] = file.Value.Contents;
						}
					}
					else if (_scan != null) {
						scan = _scan;
						_scan = null;
					}
					else {
						_draining = false;
						return;
					}
				}
				if (batch != null) {
					WriteBatch(batch);
				}
				else {
					EmptyOldFiles(scan.Value.Folder, scan.Value.Keep, scan.Value.Subfolders, scan.Value.RootFiles);
				}
			}
		}

		// Catches everything, per file: an exception here would end the drain with _draining still set, and nothing would be written again
		void WriteBatch(Dictionary<string, (string FileName, string Contents)> batch) {
			foreach (var file in batch) {
				try {
					WriteFile(file.Key, file.Value.Contents);
				}
				catch (Exception ex) {
					// Locked file, missing drive, no permission, ...
					lock (_lock) {
						// Forgetting the file makes the next write retry it, even with the same contents
						if (_written.TryGetValue(file.Key, out string written) && written == file.Value.Contents) {
							_written.Remove(file.Key);
						}
						SetError(file.Value.FileName, ex);
					}
					continue;
				}
				ClearError(file.Value.FileName);
			}
		}

		// Empties every file in the subfolders, and every file in the folder itself that matches rootFiles,
		// that isn't in keep and isn't empty already. Catches everything, like WriteBatch.
		void EmptyOldFiles(string folder, HashSet<string> keep, string[] subfolders, string rootFiles) {
			DirectoryInfo root;
			try {
				root = new DirectoryInfo(folder);
				if (!root.Exists) return;
			}
			catch (Exception) {
				// Invalid characters in the folder setting. MakeFile has reported them.
				return;
			}
			// The files' FullNames start with the folder's, normalized the same way
			int rootLength = root.FullName.TrimEnd(Path.DirectorySeparatorChar).Length + 1;
			// A folder on disk may differ in case from the writers' paths. Windows paths ignore case.
			keep = new HashSet<string>(keep, StringComparer.OrdinalIgnoreCase);

			void Search(string name, Func<IEnumerable<FileInfo>> files) {
				try {
					foreach (FileInfo file in files()) {
						string fileName = file.FullName.Substring(rootLength);
						if (file.Length == 0) continue;
						// The path as the writers make it, which is how keep has it
						string path = Path.Combine(folder, fileName);
						if (keep.Contains(path)) continue;
						try {
							File.WriteAllText(path, string.Empty);
						}
						catch (Exception ex) {
							lock (_lock) {
								SetError(fileName, ex);
							}
							continue;
						}
						ClearError(fileName);
					}
				}
				catch (Exception ex) {
					// Listing a folder failed, such as for lack of permission
					lock (_lock) {
						SetError(name, ex, "search");
					}
				}
			}

			Search(folder, () => root.EnumerateFiles(rootFiles));
			foreach (string subfolder in subfolders) {
				var directory = new DirectoryInfo(Path.Combine(root.FullName, subfolder));
				if (directory.Exists) {
					Search(subfolder, () => directory.EnumerateFiles("*", SearchOption.AllDirectories));
				}
			}
		}

		static void WriteFile(string path, string contents) {
			try {
				File.WriteAllText(path, contents);
			}
			catch (DirectoryNotFoundException) {
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllText(path, contents);
			}
		}

		/// <summary>
		/// Report a file that couldn't be queued, such as one whose path has invalid characters.
		/// </summary>
		public void ReportError(string fileName, Exception ex) {
			lock (_lock) {
				SetError(fileName, ex);
			}
		}

		// Call under _lock
		void SetError(string fileName, Exception ex, string action = "write") {
			_errorFile = fileName;
			_error = $"Couldn't {action} {fileName} ({DateTime.Now:T}): {ex.Message}";
			_errorChanged = true;
		}

		// A file was written: hide the error if it was about that file
		void ClearError(string fileName) {
			lock (_lock) {
				if (string.Equals(fileName, _errorFile, StringComparison.OrdinalIgnoreCase)) {
					_errorFile = null;
					_error = null;
					_errorChanged = true;
				}
			}
		}

		/// <summary>
		/// Whether the error to show has changed since the last call.
		/// </summary>
		/// <param name="error">The latest failed write, or null once that file has been written</param>
		public bool TakeErrorChange(out string error) {
			lock (_lock) {
				error = _error;
				if (!_errorChanged) return false;
				_errorChanged = false;
				return true;
			}
		}

		/// <summary>
		/// Makes the next write of every file go to disk, even if its contents haven't changed.
		/// Brings back files deleted or edited outside LiveSplit.
		/// </summary>
		public void ForgetWrittenFiles() {
			lock (_lock) {
				_written.Clear();
			}
		}

		/// <summary>
		/// Blocks until every file queued so far is written, or the timeout passes.
		/// </summary>
		/// <returns>False if the timeout passed first</returns>
		public bool WaitForWrites(int millisecondsTimeout) {
			Task drain;
			lock (_lock) {
				if (!_draining) return true;
				drain = _drain;
			}
			return drain.Wait(millisecondsTimeout);
		}
	}
}
