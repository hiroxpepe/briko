# HANDOFF — Coding Standard Rollout to briko

> **STATUS: IN PROGRESS. The convention test harness is in place and most of the
> repo is already clean, but the source fixes are NOT finished and NOTHING has
> been committed yet. Do not commit until the whole test suite is green.**

This note hands off the work of bringing the STUDIO MeowToon coding standard and
the whitelist convention check (already live in animo and opinio) over to briko.
Read it before touching anything, so the work is not started from zero.

## What this rollout is

briko is a Unity editor extension (pure C#, no JS) that builds levels. It had a
writing standard, a tech-terms list, and a badge convention, but no coding
standard and no automated convention check. The goal is to give briko the same
whitelist convention test the other two repos use, so naming drift is caught by
a test rather than by eye. Keep the構成 (structure) the same across repos.

## What is already done

+ The convention harness was copied from animo into
  `Tests~/ConventionTests/`: `ConventionRules.cs`, `ConventionScan.cs`, the three
  test files, and a project file `Briko.Tests.Convention.csproj`. It builds.
+ `ConventionScan.TARGET_DIRS` was set to `Editor` (briko keeps its source under
  `Editor/`, not `Scripts/`).
+ The vocabulary was ported and grown for briko's words: `plain_words.md`,
  `unit_marks.md`, `letter_words.md`, `single_letters.md`, and a briko
  `project_words.md`. `mock`, `node`, and `region` were added to
  `docs/standard/tech_terms.md` so the mock tests pass.
+ Every source header was set to the MIT license. The repo's LICENSE file is
  MIT, but 18 source files still carried a GPL v2.0 header — that mismatch was
  fixed, so all headers now match the LICENSE.
+ `Editor/Model/Layout.cs` — the four DTO classes (Root, Platform, Item, Zone)
  were marked `[Serializable]`, so their snake_case JSON-key properties are
  allowed by the standard (a Serializable type's members are external JSON keys).
+ `ImportJsonToNewScene` was renamed to `ImportJSONToNewScene` (JSON is a letter
  word, so it is all caps).
+ The standard's exception table (in animo's `coding_standard.md`) gained one
  row: a Unity message method is not checked, because Unity fixes the name. This
  is the written basis for the code change described next.

## What is NOT done yet — pick up here

1. **Unity message exception in ConventionRules.** Add a strict allow-set
   `UNITY_MESSAGES` to `Tests~/ConventionTests/ConventionRules.cs` holding ONLY
   the names Unity fixes: `Awake`, `Start`, `Update`, `LateUpdate`,
   `FixedUpdate`, `OnEnable`, `OnDisable`, `OnDestroy`, `OnGUI`, `OnValidate`,
   `Reset`. In the method-casing loop, skip a method whose name is in that set.
   Do NOT add any name briko wrote itself. In particular `OnSceneOpened` is a
   briko-authored event handler, not a Unity message — it stays checked and must
   be renamed (see step 3).

2. **Rename private methods to camelCase.** Many private methods in
   `Editor/Exporter.cs`, `Editor/HierarchySorter.cs`, `Editor/Importer.cs`, and
   `Editor/ObjectVisibilityPanel.cs` are PascalCase (`CollectItems`,
   `FindRootObject`, `ScanScene`, and so on). By the standard a private method is
   camelCase. Rename them and their call sites.

3. **Rename `OnSceneOpened` to `onSceneOpened`** in
   `Editor/ObjectVisibilityPanel.cs` (it is briko's own name, not a Unity
   message).

4. **Fix the const.** `_json_settings` in `Editor/ExportMenu.cs` and
   `Editor/ImportMenu.cs` is a `const` and must be `UPPER_SNAKE` (`JSON_SETTINGS`).

5. **Run the whole suite green, then report.** Use:
   `dotnet test Tests~/ConventionTests/Briko.Tests.Convention.csproj -p:UseSdkRoslyn=true`.
   The `-p:UseSdkRoslyn=true` flag is needed in a sandbox without the package
   feed. Only after every test is green may the work be committed — and the
   commit needs the master's explicit go-ahead.

## How to judge a violation — the rule, not an authority

The standard decides every case; do not reach for .NET, Microsoft, or Unity
habit as the reason. A name spells the way print spells it (full words, letter
words in all caps). A name takes the form of what it faces: a public method
faces other code (PascalCase), a JSON property faces a JSON file (snake_case), a
private helper faces the reader alone (camelCase). A name that an outside library
fixes — an override, an extern, a Unity message — is not ours to restyle. If a
case feels unsettled, the standard is what to read, and if the standard is silent
it is the standard that needs a line added, not a guess made.

## State of the tree

Everything above lives under `/home/claude/briko` and is uncommitted. The commit
is gated on the master's word and on a green suite.
