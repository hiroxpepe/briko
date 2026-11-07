# TASKLIST

Work items still open for this repository. Any person may put in a new
item; the person who does the work marks it done (`+ [x]`) and puts the
change in as a commit.

<!-- format: v1 | fields: status, id, title, phase -->

+ [ ] TASK-001 [P-XX]: Put the rest of the docs into Basic English

## Detail

### TASK-001

**State 2026-11-07, by the Basic English check.** These fail it (the lines with a word not allowed, of the lines of the file): `docs/briko_spec.md` (157 of 1,580),
`.github/copilot-instructions.md` (77 of 308), `docs/hierarchy_sorter_spec.md` (53 of 522), `docs/hierarchy_sorter_tdd.md` (51 of 490), `docs/object_visibility_spec.md`
(30 of 420), `docs/HANDOFF_coding_standard_rollout.md` (17 of 86), and `docs/briko_roadmap.md` (5 of 204). `README.md` passes.

**Small, still-open things, each inside a file that must come into Basic English first**: these lines still point at the old file name
`docs/development_plan_v1_detail_JP.md`, which no longer holds that document; the real one sits at `docs/briko_roadmap.md`. They are `README.md` lines 344 and 513,
`.github/copilot-instructions.md` line 299, and `docs/briko_spec.md` lines 1025 and 1555. Fixing any one of these lines stages the whole file it sits in for the same
check that blocks a commit on any of its words not yet in Basic English. Bring each whole file into Basic English first, then fix its own old-name line as part of that
same pass.

Also still open: words put into `draft_words.md` in a hurry, to get
`coding_standard.md` and `tech_terms.md` to pass. Some of these are real
technical words that should move to `tech_terms.md`, each with its own
short sense, and not sit in `draft_words.md` with no sense given at all.
This move needs the master's own GO first, word by word.
