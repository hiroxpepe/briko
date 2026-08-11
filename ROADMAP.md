# ROADMAP

<!-- format: v1 | fields: status, phase, title -->

+ [x] PHASE-01: Build a Scene-to-JSON round trip for a level
+ [~] PHASE-02: Check the round trip on a real level, by hand
+ [~] PHASE-03: Work that does not fit the first two phases

## Detail

### PHASE-01

Build a UPM package that turns a Unity scene into JSON and back
again: a data model (`Layout.cs`), an Exporter, an Importer, and a
full test set. This is done; every file this called for was found
built and tested in the real code. See `docs/briko_roadmap.md` §5
for the full list.

### PHASE-02

Turn the real Level 1 into JSON by hand, then check that an LLM can
build a Level 2 from it, through a real Unity run — no sandbox can
check this on its own. See `TASKLIST.md` and `docs/briko_roadmap.md`
§6 for the full steps.

### PHASE-03

Work that is not part of building or checking the round trip itself
(say, putting the project's own docs into Basic English) is tracked
here instead. See `TASKLIST.md` for the open work under this phase.
