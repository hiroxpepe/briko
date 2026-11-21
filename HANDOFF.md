# HANDOFF

> New chat starts here. Read this file first, before anything else.

## Read this first, in every new chat

**Read `CLAUDE.md`, in this repository, before any other work.** It
holds the one command that must be run once, for each clone
(`git config core.hooksPath .githooks`), and every rule the agent
holds to, here. **Every other repository in this family holds its own
`CLAUDE.md` too — read that one, in that repository, before any work
begins there.**

## Where things stand

State on 2026-11-07, checked against the code.

| # | Part | State |
| --- | --- | --- |
| 1 | The tool | `Editor/` holds the Scene-to-JSON round trip: `Layout.cs`, `Exporter.cs`, `Importer.cs`, `HierarchySorter.cs`, `ObjectVisibilityPanel.cs`, and the tools of `Internal/` |
| 2 | The tests that run | 99 in `Tests~/ConventionTests`, all pass |
| 3 | The tests that do not run | The 99 tests of `Tests~/IntegrationTests` (7 files) run in no project: the `IntegrationTests.csproj` that `README.md` names is not in the repository |
| 4 | A trial run | In a copy in another folder (the repository not changed): `RoundTripTests`, `GridSnapperTests`, and `PrefabNameParserTests` (14 tests) ran, and 13 passed. `Root_RoundTrip_IsDeepEqual` failed: `Fixtures/sample_level_minimal.json` holds the key `target_duration_sec`, and `Layout.cs` now holds `target_duration_second` |
| 5 | Tests behind the code | Done 2026-11-15: the names in `LayoutTests` and `FloorDetectorTests` now match the code. `target_duration_second` is the name of record (the design document, the level data and the test data were changed to it). The 30 tests ran and passed in a temporary project outside the repository. `HierarchySorterTests` (34 tests) and `ObjectVisibilityPanelTests` (21 tests) were not tried: the classes they test use Unity |
| 6 | Phases | `PHASE-01` is in progress (the test set runs since 2026-11-15: `IntegrationTests` 99 and `ConventionTests` 99 pass); `PHASE-02` (the check by hand, in a real Unity) is in progress and needs the master |
| 7 | Open work | `TASK-001` to `TASK-006` (the docs into Basic English, one file to a task), `TASK-007` (two lines of `README.md`), `TASK-008` (the draft words, on the word of the master) |

## Next move

`PHASE-02` needs the master's own hand in a real Unity (see `docs/briko_roadmap.md` §6). `TASK-001` and `TASK-002`: the Basic English part is done
(2026-11-15); three lines still point at the old file name `docs/development_plan_v1_detail_JP.md` (`docs/briko_spec.md` lines 1025 and 1555,
`.github/copilot-instructions.md` line 299).
