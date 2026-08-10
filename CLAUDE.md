# CLAUDE

> How the agent works in this repository. These are rules for the agent
> (a language model) that helps build this project. They are kept short and
> plain, by the writing standard.

## Set up once, before any other work

+ Run `git config core.hooksPath .githooks` once per clone. This makes
  `git commit` run the shared checks in `.githooks/pre-commit` on every
  markdown file staged for the commit, before the commit is let through.

## Three files, three jobs

+ `CLAUDE.md` (this file) — the rules and the promise: how the agent works
  here, checked every time, not tied to any one piece of work.
+ `TASKLIST.md` — the full list of open work, with a schedule. A short
  checkbox line up top for each item, a full write-up below it.
+ `HANDOFF.md` — the hand-off to the next chat: where things stand right
  now, and the next move. Kept short; the full list lives in `TASKLIST.md`.

## Documents

+ Every document follows the writing standard in `docs/standard/`. The words
  are kept simple, so a reader whose first language is not English can take in
  the sense. See `docs/standard/writing_standard.md`.
+ Every technical term used in a document must be in the term list first. If a
  term is not there, add it to `docs/standard/tech_terms.md` before you use it.
+ Badges follow `docs/standard/badge_convention.md`.

## Markdown lint

+ Before you commit any markdown file, run the lint check and get zero errors.
  Do not commit a markdown file that still has lint errors.
+ The rules are set in `.markdownlint.json` at the repository root. Use that
  file, not your own idea of the rules.
+ The list marker is the plus sign. Use `+` for every list item, not `-` or
  `*`.

Run the check like this:

```bash
npx --yes markdownlint-cli -c .markdownlint.json <file>
```

## Commits

+ The commit message is one line, with no body.
+ The form is `type: Verb subject`. The verb is one of Add, Update, or Delete.
  The type is one of feat, fix, refactor, docs, chore, or test.
+ Keep the first line between 57 and 60 characters.
+ Do not put brackets or slashes in the message; keep it plain.

## History

+ Keep the history a single straight line. Do not make merge commits.
+ If a push is refused because the remote is ahead, rebase your work on top of
  the remote, then push. Do not merge.

## Convention maintenance tools

+ `Tests~/ConventionTests/tools/*.cs.txt` hold two reusable fixer tools for
  the section-header rule in `docs/standard/coding_standard.md`. The `.txt`
  extension keeps them out of the compiled test project on purpose — they
  are reference copies, not tests that run on every build.
+ `Tool_InsertMissingSectionHeaders.cs.txt` adds a divider and a canonical
  label wherever a run of same-kind/access/static members has none yet.
+ `Tool_FixSectionHeaders.cs.txt` straightens dividers already in the file to
  column 103 and normalizes existing labels to the canonical spelling — but
  only where every member under one label shares the same kind, access, and
  static-ness; a mixed section is left alone for a human to split.
+ To run either one: copy the whole method into `ConventionRulesTests.cs`
  (paste anywhere inside the class), add these usings at the top of that
  file if they are not already there —
  `System.Collections.Generic`, `System.Text.RegularExpressions`,
  `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.CSharp.Syntax` —
  then run:

  ```bash
  dotnet test --no-restore -p:UseSdkRoslyn=true --filter "Tool_InsertMissingSectionHeaders"
  ```

  (swap in `Tool_FixSectionHeaders` for the other one). Read the printed
  `PROBE` line for a change count, remove the pasted method again, diff the
  real changes, then run the full test suite before committing.
+ Order matters: run the insert tool first, then the fix tool. Inserting
  first splits any old merged section (e.g. public and private fields once
  sharing one label) into two correctly-scoped groups; the fix tool then
  normalizes each group's own label on its own, with no manual edit needed
  in between.
+ Both tools write real files on disk. Diff and re-parse the changed files
  for zero syntax errors before trusting the result, the same way any other
  mass mechanical fix in this project is checked.
