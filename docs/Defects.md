# Defects

Wrong output, crashes, and output that goes stale. The index is [TODO.md](TODO.md).

"Reproduced" means seen on the date given by driving the built DLL headless against the real `LiveSplit.Core.dll` (see [testing.md](testing.md)) and reading back the files it wrote. "Read" means found by reading the code only. An item that a test can show gets a test that asserts the correct output, skipped with the item's code until it's fixed ([testing.md](testing.md#adding-a-test)). One that can't be a test gets a row, marked with its code, in testing.md's [manual regression checks](testing.md#manual-regression-checks).
