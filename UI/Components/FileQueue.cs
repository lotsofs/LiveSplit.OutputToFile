using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LiveSplit.UI.Components {
	/// <summary>
	/// Writes the output files on a background thread, so LiveSplit's UI thread only queues them.
	/// Keeps only the latest contents per file, and skips contents that are already on disk.
	/// One instance is shared by the writers. Every field is guarded by _lock.
	/// </summary>
	class FileQueue {

		readonly object _lock = new object();

		// Full path → (path relative to the folder, contents) waiting to be written
		Dictionary<string, (string FileName, string Contents)> _pending = new Dictionary<string, (string, string)>();
		// Full path → the contents last handed to the disk. Files whose contents haven't changed are skipped.
		readonly Dictionary<string, string> _written = new Dictionary<string, string>();

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
				if (_pending.TryGetValue(path, out var pending)) {
					if (pending.Contents == contents) return;
				}
				else if (_written.TryGetValue(path, out string written) && written == contents) {
					return;
				}
				_pending[path] = (fileName, contents);
				if (!_draining) {
					_draining = true;
					_drain = Task.Run(() => Drain());
				}
			}
		}

		// Writes batches until nothing is pending
		void Drain() {
			while (true) {
				Dictionary<string, (string FileName, string Contents)> batch;
				lock (_lock) {
					if (_pending.Count == 0) {
						_draining = false;
						return;
					}
					batch = _pending;
					_pending = new Dictionary<string, (string, string)>();
					// Recorded now, so a file queued again while this batch is written compares against what's in flight
					foreach (var file in batch) {
						_written[file.Key] = file.Value.Contents;
					}
				}
				foreach (var file in batch) {
					try {
						WriteFile(file.Key, file.Value.Contents);
					}
					catch (Exception ex) {
						// Locked file, missing drive, no permission, ... Catch everything: an exception here
						// would end the drain with _draining still set, and nothing would be written again.
						lock (_lock) {
							// Forgetting the file makes the next write retry it, even with the same contents
							if (_written.TryGetValue(file.Key, out string written) && written == file.Value.Contents) {
								_written.Remove(file.Key);
							}
							SetError(file.Value.FileName, ex);
						}
						continue;
					}
					lock (_lock) {
						if (file.Value.FileName == _errorFile) {
							_errorFile = null;
							_error = null;
							_errorChanged = true;
						}
					}
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

		void SetError(string fileName, Exception ex) {
			_errorFile = fileName;
			_error = $"Couldn't write {fileName} ({DateTime.Now:T}): {ex.Message}";
			_errorChanged = true;
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
