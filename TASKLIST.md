# TASKLIST

Work items still open for this repository. Any person may put in a new
item; the person who does the work marks it done (`+ [x]`) and puts the
change in as a commit.

<!-- format: v1 | fields: status, id, title, phase -->

+ [ ] TASK-001 [P-XX]: Put the main design paper into Basic English
+ [ ] TASK-002 [P-XX]: Put the guide for the code helper into Basic English
+ [ ] TASK-003 [P-XX]: Put the paper on the order of the scene into Basic English
+ [ ] TASK-004 [P-XX]: Put the paper on what is seen into Basic English
+ [ ] TASK-005 [P-XX]: Put the notes on the coding rules into Basic English
+ [ ] TASK-006 [P-XX]: Put the long roadmap into Basic English
+ [ ] TASK-007 [P-XX]: Point two lines of the first page at the right roadmap file
+ [ ] TASK-008 [P-XX]: Move the technical words out of the draft list, on the word of the master

## Detail

### TASK-001

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `docs/briko_spec.md` has 153 words not allowed by the Basic English check (counted 2026-11-14). The rule of the master: a word that has a simple word to take its place is replaced; only a word that no simple word can take the place of goes into `draft_words.md`, and no hard word goes there. Lines 1025 and 1555 still point at the old file name `docs/development_plan_v1_detail_JP.md`; point them at `docs/briko_roadmap.md` in the same pass. **Done, once**: `tools/basic_english_cli.js` gives no error on the file, and the commit hook takes it.

### TASK-002

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `.github/copilot-instructions.md` has 77 words not allowed by the Basic English check (counted 2026-11-14). The rule of the master: a word that has a simple word to take its place is replaced; only a word that no simple word can take the place of goes into `draft_words.md`, and no hard word goes there. Line 299 still points at the old file name `docs/development_plan_v1_detail_JP.md`; point it at `docs/briko_roadmap.md` in the same pass. **Done, once**: `tools/basic_english_cli.js` gives no error on the file, and the commit hook takes it.

### TASK-003

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `docs/hierarchy_sorter_spec.md` has 52 words not allowed by the Basic English check (counted 2026-11-14). The rule of the master: a word that has a simple word to take its place is replaced; only a word that no simple word can take the place of goes into `draft_words.md`, and no hard word goes there. **Done, once**: `tools/basic_english_cli.js` gives no error on the file, and the commit hook takes it.

### TASK-004

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `docs/object_visibility_spec.md` has 28 words not allowed by the Basic English check (counted 2026-11-14). The rule of the master: a word that has a simple word to take its place is replaced; only a word that no simple word can take the place of goes into `draft_words.md`, and no hard word goes there. **Done, once**: `tools/basic_english_cli.js` gives no error on the file, and the commit hook takes it.

### TASK-005

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `docs/HANDOFF_coding_standard_rollout.md` has 16 words not allowed by the Basic English check (counted 2026-11-14). The rule of the master: a word that has a simple word to take its place is replaced; only a word that no simple word can take the place of goes into `draft_words.md`, and no hard word goes there. **Done, once**: `tools/basic_english_cli.js` gives no error on the file, and the commit hook takes it.

### TASK-006

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `docs/briko_roadmap.md` has 5 words not allowed by the Basic English check (counted 2026-11-14). The rule of the master: a word that has a simple word to take its place is replaced; only a word that no simple word can take the place of goes into `draft_words.md`, and no hard word goes there. **Done, once**: `tools/basic_english_cli.js` gives no error on the file, and the commit hook takes it.

### TASK-007

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. `README.md` passes the Basic English check, but its lines 344 and 513 still point at the old file name `docs/development_plan_v1_detail_JP.md`, which no longer holds that document; the real one is `docs/briko_roadmap.md`. **Done, once**: both lines point at `docs/briko_roadmap.md`, and the commit hook takes the file.

### TASK-008

Cut out of the old TASK-001 on 2026-11-14, one file to a task, so that each one is 8 hours or less. Some words were put into `draft_words.md` in a hurry, to get `coding_standard.md` and `tech_terms.md` to pass. Some of these are real technical words that should go to `tech_terms.md`, each with its own short sense. This move needs the GO of the master first, word by word. **Done, once**: each word the master names is in `tech_terms.md` with its sense, and no longer in `draft_words.md`.
