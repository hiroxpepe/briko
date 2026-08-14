# CLAUDE

> How the agent works in this repository. These are rules for the agent
> (a language model) that helps build this project. They are kept short and
> plain, by the writing standard.

## Set up once, before any other work

+ Run `git config core.hooksPath .githooks` once for each clone. This
  makes `git commit` run the shared checks in `.githooks/pre-commit`
  and `.githooks/commit-msg` on every commit, before the commit is
  let through. **Without this one command, neither hook runs at all
  — a broken commit message, or a markdown file still holding an
  error, would pass straight through, with nothing to stop it.**
  Check it is truly set, with `git config core.hooksPath`, which must
  answer `.githooks`. Running a hook by hand, on a file, is **not**
  proof the hook itself is live; only a true `git commit` proves that.

## Three files, three jobs

+ `CLAUDE.md` (this file) — the rules and the word given: how the
  agent works here, checked every time, not tied to any one act of
  work.
+ `TASKLIST.md` — the full list of open work, with a plan for when.
  A short checkbox line up top for each item, a full write-up below
  it.
+ `HANDOFF.md` — the hand-off to the next chat: where things stand
  right now, and the next move. Kept short; the full list lives in
  `TASKLIST.md`.

## Documents

+ Every document follows the writing standard in `docs/standard/`.
  The words are kept simple, so a reader whose first language is not
  English can take in the sense. See
  `docs/standard/writing_standard.md`.
+ Every hard word used in a document must be in the word list first.
  If a word is not there, add it to `docs/standard/tech_terms.md`
  before you use it.
+ Badges follow `docs/standard/badge_convention.md`.

## Markdown check

+ Before you commit any markdown file, run the check and get no
  errors at all. Do not commit a markdown file that still has errors.
+ The rules are set in `.markdownlint.json` at the root of the
  repository. Use that file, not your own idea of the rules.
+ The list mark is the plus sign. Use `+` for every list line, not
  `-` or `*`.

Run the check like this:

```bash
npx --yes markdownlint-cli -c .markdownlint.json <file>
```

## Commits

+ The commit message is one line, with no body under it.
+ The form is `type: Verb subject`. The verb is one of Add, Update,
  or Delete. The type is one of `feat`, `fix`, `refactor`, `docs`,
  `chore`, or `test`.
+ Keep the first line between 57 and 60 letters long.
+ Do not put square marks or forward lines in the message; keep it
  plain.

## History

+ Keep the history one straight line. Do not make a commit that
  joins two lines back into one.
+ If a push is turned down because the copy on the server is ahead,
  put your work back on top of it first, then push. Do not join the
  two lines back into one.

## Tools that keep the naming rules in order

+ `Tests~/ConventionTests/tools/*.cs.txt` hold two tools, ready to
  use again, for the section-head rule in
  `docs/standard/coding_standard.md`. The `.txt` end keeps them out
  of the built test project on purpose — they are kept copies, not
  tests run on every build.
+ `Tool_InsertMissingSectionHeaders.cs.txt` adds a line and a fixed
  label wherever a run of members of the same kind, right, and
  static state has none yet.
+ `Tool_FixSectionHeaders.cs.txt` puts a line already in the file
  straight, to column 103, and makes an existing label match the
  one fixed spelling — but only where every member under one label
  shares the same kind, right, and static state; a mixed part is left
  for a person to split by hand.
+ To run either one: copy the whole method into
  `ConventionRulesTests.cs` (put it in anywhere inside the class),
  add these `using` lines at the top of that file if they are not
  there yet —
  `System.Collections.Generic`, `System.Text.RegularExpressions`,
  `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.CSharp.Syntax` —
  then run:

  ```bash
  dotnet test --no-restore -p:UseSdkRoslyn=true --filter "Tool_InsertMissingSectionHeaders"
  ```

  (put in `Tool_FixSectionHeaders` for the other one). Read the
  printed `PROBE` line for a count of change, take the pasted method
  out again, diff the real changes, then run the full test set
  before you commit.
+ Order is important: run the add tool first, then the fix tool.
  Adding first splits any old, joined part (say, open and closed
  fields once under one label) into two groups, each with its right
  own range; the fix tool then makes each group's own label match,
  with no work by hand needed in between.
+ Both tools write real files to disk. Diff and read the changed
  files again for no writing errors at all, before you trust the
  result, the same way any other big, machine-made fix in this
  project is checked.
