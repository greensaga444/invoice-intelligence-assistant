# PROJECT STATUS

> Use this file for a **single work item** (feature, fix, chore, docs) on its own branch.
> Place it in your workflow directory (`<workflow-dir>/status.md`, usually `docs/status.md`), fill in the Current Work Item, reset the checklist to unchecked, and start at the INIT stage.
> For whole-codebase architecture, target, and roadmap, see the resolved project file (prefer `docs/project.md`; use `.workflow/project.md` for large repos; root `project.md` is a legacy fallback).

## Project

See the resolved project file for Project Name, Repository, and Tech Stack.
(For a standalone task with no resolved project file, fill those in here instead.)

---

## Current Work Item

Work Item Name: TBD

Description: TBD

---

## Track

LIGHT

Tracks:

- LIGHT — INIT → IMPLEMENTATION → VERIFICATION (default).
- FULL — INIT → PLAN → IMPLEMENTATION → VERIFICATION (use only when planning is needed).

Simple rule: start with LIGHT. Switch to FULL only when requirements/design are unclear or risk is high.

Optional check (only if unsure): use FULL when any of these is true:

- Unresolved design uncertainty
- Migration risk (data/schema/external API)
- More than one major component touched

---

## Current Stage

INIT

Available Stages (depends on Track):

- INIT
- PLAN            (FULL track only)
- IMPLEMENTATION
- VERIFICATION

---

## Stage Definitions

### INIT

Goal:

Set up this task's context before any requirements work.

Output:

Filled header of this file (Project + Current Work Item).

Allowed:

- Confirm Branch
- Set Work Item Name / Description
- Inherit Tech Stack from project.md
- Agree Task Boundary / Scope Line
- Confirm the item's one-line Definition of Done and check it doesn't overlap an earlier item

Not Allowed:

- Requirements / MVP Definition
- Task Planning
- Source Code

Exit Criteria:

- Branch created
- Work Item Name + Description filled
- Tech Stack confirmed
- Scope boundary agreed
- Definition of Done confirmed and overlap with earlier items checked
- If `project.md` exists, `Init Status` is `DEFINED` (do not proceed while `TEMPLATE`)

---

### PLAN

Goal:

Define requirements/MVP and break them into implementable tasks.  (FULL track only — skipped on LIGHT.)

Output:

<workflow-dir>/workitems/<name>.md — "Spec" and "Tasks" sections (`<workflow-dir>` is usually `docs/`; in this template repo it is also `docs/`)

Allowed:

- Business Goal
- User Story
- MVP
- Scope Definition
- Acceptance Criteria
- Task Breakdown
- Dependencies
- Priorities
- Development Order

Not Allowed:

- Source Code
- Implementation Discussion

Exit Criteria:

- MVP is clearly defined
- Scope is agreed
- Success criteria exist
- Tasks are small, actionable, and can be completed independently

---

### IMPLEMENTATION

Goal:

Build the work item.

Output:

Source Code

Allowed:

- Data / Storage Changes
- Core / Logic Code
- UI / Interface Code
- Tests

Not Allowed:

- New Scope Changes

Exit Criteria:

- All tasks completed
- Tests added

---

### VERIFICATION

Goal:

Confirm implementation matches specification.

Output:

Verification Result

Allowed:

- Testing
- Code Review
- Spec Coverage Check
- Bug Fixes
- Check formatting / linting if the project defines it (formatter, linter, or style config)
- Run CI locally if a CI config exists (see "Local CI" below)

Exit Criteria:

- Implementation matches Spec
- Tests pass
- Formatting/lint clean if the project defines it (run the formatter/linter; fix or auto-format any issues)
- CI passes — if a CI config exists, run it locally and make it green before delivery (required if Delivery Policy in project.md sets CI Required = yes)
- No critical issues remain

#### Local CI

During VERIFICATION, detect and run CI locally before delivery:

1. Detect a CI config (first match wins): `.github/workflows/*.yml`, `.gitlab-ci.yml`, `.circleci/config.yml`, `azure-pipelines.yml`, `Jenkinsfile`, or a documented CI script.
2. If none exists, skip local CI — fall back to the project's own build + test commands and note "no CI config" in the checklist.
3. If one exists, reproduce its jobs locally (lint, build, test) — prefer a runner like `act` for GitHub Actions when available, otherwise run the equivalent commands the workflow invokes.
4. Make it green locally before `open pr`. Fix failures during VERIFICATION; do not defer them to remote CI.

---

## Delivery (after VERIFICATION)

> Not a stage. Runs once VERIFICATION is done.

Precondition: IMPLEMENTATION and VERIFICATION are already complete.

Flow:

1. Update delivery documents as needed: `project.md` progress (Roadmap/Milestones), related docs, and `README.md`.
2. Commit implementation changes and delivery updates together.
3. Apply branch policy:
   - Main Branch Protected = yes (therefore Pull Request Required = yes): run `open pr` (confirm, then push and open/update PR to main). One PR per branch only.
   - Main Branch Protected = no (therefore Pull Request Required = no): skip `open pr`, push the delivery commit directly to `main`.
4. Merge/land per policy.
5. `finish` — switch to main and delete the branch.

Delivery done definition: delivery updates are committed and landed per policy (merged or pushed to `main`), roadmap/milestones are updated, and branch cleanup is complete.

---

## Current Checklist

> On LIGHT, skip PLAN.

### INIT

- [ ] Branch Created
- [ ] Work Item Name + Description
- [ ] Tech Stack Confirmed
- [ ] Scope + Definition of Done Confirmed

### PLAN

- [ ] Goal + MVP Defined
- [ ] Scope + Acceptance Criteria
- [ ] Task Breakdown Ready

### IMPLEMENTATION

- [ ] Code Implemented
- [ ] Tests Added/Updated

### VERIFICATION

- [ ] Work Item Tested
- [ ] Definition of Done Verified
- [ ] Format / Lint Checked (if the project defines a formatter or linter)
- [ ] CI Passed — run locally if a CI config exists (required if project.md Delivery Policy sets CI Required = yes)

### DELIVERY

- [ ] project.md progress updated (Roadmap/Milestones)
- [ ] Docs/README updated
- [ ] Delivery commit + push completed
- [ ] PR opened/updated (if required)

---

## Current Task

TBD

> Keep this to 3-4 lines. Detail belongs in `<workflow-dir>/workitems/<name>.md` (`docs/` in target projects and this template repo), not here.

---

## Next Action

TBD

---

## AI Instructions

Always read this file first.

Rules:

1. Follow Current Stage; default to LIGHT track.
2. Use FULL only when planning is necessary (unknowns/risk).
3. Prefer `goto next stage` as the normal progression command.
4. Keep Current Task to 3-4 lines; put details in `<workflow-dir>/workitems/<name>.md`.
5. On `goto init`, reset fields to TBD and uncheck checklist items.
6. During VERIFICATION, run tests, format/lint (if defined), and local CI when a CI config exists.
7. On `open pr` (when required), ensure delivery docs are updated before push/PR.
8. On `report`, use `project.md` only on main/master; otherwise report from this file.
9. If you need manual rework navigation, `goto plan|implementation|verification` is allowed; record a one-line reason in Next Action. These specific-stage commands change stage only (no auto-start).
10. On `finish`, complete branch cleanup only after delivery updates are landed.
11. On `run auto`, do not ask follow-up questions: use deterministic defaults, execute all remaining stages and delivery, and stop only on hard blockers with a concise failure report.

Efficiency (keep credit/token spend low):

- Reuse the shell environment: configure PATH/toolchain once per terminal, or call the project's build script — don't re-emit long environment setup on every command.
- Batch build + test into one command instead of running configure → build → test as separate calls.
- Don't re-run builds or tests that already passed unless the code changed.
- Prefer fewer, larger file reads over many small ranged reads of the same file.
- Persist build/run/test commands and workflow conventions to repo memory once confirmed, so they aren't re-derived each session.

---

## Commands

report   (check branch first: on main/master, report from project.md; on other branches, summarize this work item's progress from status.md only)

set track full | light   (optional — default is LIGHT)

goto init   (reset this file to init state: track=LIGHT, stage=INIT, header fields=TBD, all checkboxes unchecked)

goto next stage   (advances along the current Track only when current-stage Exit Criteria are met; otherwise stay in place and report missing checklist items; if advanced, immediately begin the new stage's work; on LIGHT, INIT → IMPLEMENTATION)

run ci   (VERIFICATION: detect a CI config and run it locally until green; if none exists, run the project's build + test instead)

run auto   (non-interactive end-to-end runner for the current branch: execute all remaining stages from the current stage through DELIVERY using defaults, run tests/format/lint/local CI as required, update delivery docs, commit, push, and open/update PR only when required by policy; stop on first blocker and report exact failure + next manual action)

open pr   (after VERIFICATION done: for Main Branch Protected = yes, ensure delivery document updates + commit are complete, then confirm PR title/description, push branch, and open/update the branch PR to main; one PR per branch; requires CI green if project.md sets CI Required = yes)

finish   (switch to main and delete the current branch; if PR is skipped, first ensure delivery document updates + commit + push are complete)

Advanced (optional):

goto plan | goto implementation | goto verification   (manual stage navigation for rework; no auto-start; record one-line reason in Next Action)

---

## Command Examples

1. `goto next stage` from INIT on LIGHT
   Result: moves to IMPLEMENTATION.

2. `goto next stage` from PLAN on FULL, with missing PLAN checks
   Result: stays on PLAN and lists missing checklist items.

3. `run auto` from any non-final stage
   Result: runs the remaining pipeline without prompts and either lands the item or stops with a blocker report.

3. `goto verification` from IMPLEMENTATION (manual re-check)
   Result: stage changes to VERIFICATION; Next Action records the reason.
