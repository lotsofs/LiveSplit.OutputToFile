# Features and limitations

Things the component doesn't do yet. The index is [TODO.md](TODO.md).

TST makes the Verify steps in the other detail docs repeatable, so it pays to do it early. Its test project can take LiveSplit's folder from the `LiveSplitDir` property, like the component (see CLAUDE.md "Build / deploy"). It has to reference LiveSplit's DLLs itself, as the harness does, because the component's build doesn't copy them.

## TST — No automated tests

There is no test project. Changes are checked by hand, in LiveSplit or with a headless harness that lives outside the repo.

**Evidence.** [testing.md](testing.md) has the harness as a copy-paste program and a table of regression checks; both worked on 2026-10-14.

**Plan.** Turn the harness into a net481 test project in the repo, and the regression checks into tests that assert today's correct values, marking the ones for open [Defects.md](Defects.md) items as expected failures.

**Verify.** The tests pass, and a reintroduced bug makes one fail.

**Docs.** CLAUDE.md "Build / deploy", which says there are no tests. [testing.md](testing.md).
