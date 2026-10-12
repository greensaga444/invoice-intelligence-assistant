# PROJECT OVERVIEW

> Use this file for a **whole new codebase**.
> Fill it in once at project start (typically from a PRD via `init`) to define the architecture, final target, roadmap, and milestones.
> Each roadmap item is later implemented on its own branch using `status.md`.

## Init Status

Status: TEMPLATE

> TEMPLATE = not yet defined; DEFINED = initialization complete.
> Set to DEFINED only after Final Target, Architecture, and Roadmap are filled in.
> This marker is the single source of truth for "is project.md defined?".

---

## Project

Project Name: TBD

Repository: TBD  (optional — leave empty for local-only; if using GitHub, create an empty repo and `git clone` before copying this template in)

Owner: TBD

Project Type: TBD  (e.g. Web App / Desktop App (Windows/macOS/Linux) / Mobile App (Android/iOS) / CLI Tool / Library/SDK / Backend Service / Embedded)

Target Platforms: TBD

Tech Stack:

- Language(s): TBD
- Framework / UI: TBD
- Data / Storage: TBD
- Backend / Services: TBD
- Build / Packaging: TBD
- Infrastructure / Distribution: TBD  (optional — CI/CD only if publishing to GitHub or similar)

---

## Delivery Policy

> Decided once, after this file is defined. Governs how work branches land.

- Main Branch Protected: TBD  (yes / no)
- Pull Request Required: same as Main Branch Protected
- CI Required: same as Pull Request Required (must pass before merge)
- Merge Method: TBD  (e.g. squash / merge / rebase)
- One PR per Branch: yes  (if a PR already exists for the branch, update it)

If Main Branch Protected is **no** (therefore Pull Request Required is **no**) (local-only), delivery may push directly to `main` and CI is optional.

---

## Branch Naming

Pattern: `<type>/<short-name>`  (base branch: main or master)

Types:

- feature/ — new functionality
- fix/ — bug fix
- chore/ — tooling, deps, refactor, config
- docs/ — documentation only

Each branch carries one `status.md` and one work item from the Roadmap, regardless of type.

---

## Repository Layout

Template repository layout (this repo):

```
README.md
docs/
  project.md
  status.md
  prd/
  style/
  workitems/<name>.md
```

Recommended layout inside a target project:

```
docs/
  project.md            (this file — whole-project overview, read once)
  status.md             (per-task workflow, one copy per branch)
  prd/                  (product requirements source files)
  style/                (coding style baseline + language appendices)
  workitems/<name>.md   (FULL track: PLAN output — Spec + Tasks — in one file)
<source>/               (application/source code)
```

Build output convention: write all generated build artifacts to `build/`.

---

## Final Target

Vision:

TBD

Problem Statement:

TBD

Success Definition (Done means):

- TBD

Out of Scope:

- TBD

---

## Architecture

High-Level Overview:

TBD

Components:

| Component | Responsibility | Tech |
| --- | --- | --- |
| TBD | TBD | TBD |

Data Flow:

TBD

Key Decisions:

- TBD

Constraints / Non-Functional Requirements:

- Performance: TBD
- Security: TBD
- Scalability: TBD

---

## Roadmap / Todo List

> Each item becomes a work branch. On start, copy the status template into your workflow directory (`<workflow-dir>/status.md`, usually `docs/status.md`),
> set the work item, choose a Track (FULL for risky/unknown items, LIGHT for
> small/clear ones), reset items to TBD, and begin at the INIT stage.
>
> Slicing rules to avoid redundant work:
> - Define each item by a distinct, user-visible outcome, captured in its one-line Definition of Done.
> - Before starting an item, check overlap: if an earlier item already forces this work, fold them together or keep the earlier one deliberately minimal.
> - If two rows share Definition-of-Done language, merge or re-scope them.
> - Whenever any roadmap row is added or edited, re-run the overlap check and merge/re-scope duplicates immediately.
> - For vertical slices, mark any pulled-in work in the roadmap immediately, or defer it with an explicit stub — never leave silent overlap.

| # | Work Item | Definition of Done | Priority | Track | Depends On | Status |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | TBD | TBD | TBD | FULL | - | TODO |
| 2 | TBD | TBD | TBD | LIGHT | 1 | TODO |
| 3 | TBD | TBD | TBD | LIGHT | - | TODO |

Definition of Done: one line describing a distinct user-visible outcome; no two rows should share it.
Track values: FULL (full pipeline) / LIGHT (skip PLAN)
Status values: TODO / IN PROGRESS / DONE / BLOCKED

---

## Milestones

> Defined here on `init` (from the PRD/Final Target). Each finished milestone triggers a README refresh.

- [ ] M1: TBD
- [ ] M2: TBD
- [ ] M3: TBD

---

## AI Instructions

Read this file **once** to initialize the project (architecture, final target, roadmap).
After initialization, do **not** read or update it again during work on a branch — use `status.md` on the work branch instead.
Re-read this file only when: (a) the user explicitly asks to modify it, or (b) the user says `report` while on the `main` (or `master`) branch.

Detecting whether the project is defined:

- Resolve `project.md` location first: prefer `docs/project.md`; for large existing repos, `.workflow/project.md` is also valid; use root `project.md` only for legacy layouts.
- If the resolved file is missing → not defined; initialize first.
- If it exists but `Init Status` is `TEMPLATE` → not defined; finish initialization.
- If `Init Status` is `DEFINED` → already defined; do not re-initialize.

Initializing (`init`):

- If `Init Status` is already `DEFINED`, do nothing unless the user asks to modify.
- If the user provides a PRD (product requirements doc), read it and (re)define this whole file from it: Final Target, Architecture, Roadmap, and Milestones. The PRD is the source of truth for scope and goals; treat `init` as a full re-definition of project.md.
- For an existing codebase: scan the repo (languages, frameworks, structure, build files) and draft the empty fields — Project Type, Target Platforms, Tech Stack, Architecture summary.
- For an empty project: interview the user (or read the PRD) to fill the same fields.
- Always present the drafted values for confirmation; set `Status: DEFINED` only after the user approves.
- Without a PRD, never invent Final Target or Roadmap from a scan — those come from the user.
- After `Status: DEFINED`, write/update the project README so it describes the whole project's intended final state as fully as possible (from the PRD/Final Target); it is then refreshed as each milestone finishes.

Rules:

1. Define architecture and final target before writing any code.
2. Keep the roadmap as the single source of truth for what to build next; define the Roadmap and Milestones here in project.md (from the PRD on `init`), not elsewhere.
3. Prefer MVP solutions and avoid over-engineering.
4. Suggest architecture changes only when absolutely necessary; record them under Key Decisions.
5. Give every roadmap item a one-line Definition of Done describing a distinct, user-visible outcome; no two items should share it.
6. Before starting a work item, run an overlap check against earlier/related items — if an earlier item already forces this work, fold them together or keep the earlier one deliberately minimal instead of duplicating.
7. When a vertical slice pulls in adjacent work, record it in the roadmap immediately or defer it with an explicit stub; never leave silent overlap. Re-run overlap checks whenever a roadmap row is added or edited.
8. Do not start a work item until it exists in the Roadmap and `Init Status` is `DEFINED`.
9. When starting a work item, hand off to `status.md` (per-task workflow) on a new branch.
10. During work branches, ignore this file; it is only revisited when the user asks to modify it.
11. On `report`, check the current branch first: if branch is `main` or `master`, summarize progress from this file; otherwise do not use this file and report from `status.md`.
12. Keep the project README aligned with the Final Target: on `init` write it toward the whole project's intended final state, and update it whenever a milestone finishes.
13. Before Delivery, complete IMPLEMENTATION and VERIFICATION in `status.md`.
14. Delivery is post-verification only: update Roadmap/Milestones and relevant docs/README, then commit.
15. If Main Branch Protected = yes (therefore Pull Request Required = yes), open or update the branch PR (never create a second PR for the same branch).
16. If Main Branch Protected = no (therefore Pull Request Required = no), skip PR creation and push the delivery commit directly to `main`.
17. On `finish`, clean up the completed branch; if PR was skipped, ensure delivery docs update + commit + push are complete first.
18. On `run auto`, execute branch flow non-interactively from the current stage in `status.md` through delivery and landing to `main` by policy (merge via PR when required, direct push when not required), then run cleanup.

---

## Commands

init   (read a PRD if provided — or scan the codebase / interview — to (re)define Final Target, Architecture, Roadmap, and Milestones; confirm, set Init Status = DEFINED, then write the project README toward the final target)

show final target

show architecture

show roadmap

add roadmap item   (add a row with a one-line Definition of Done; check it doesn't overlap an existing item)

start <type>/<name> [full|light]   (copy the status template to `<workflow-dir>/status.md` (usually `docs/status.md`), reset to INIT stage, set the Track; type = feature/fix/chore/docs; Track defaults to the Roadmap row, else LIGHT)

run auto   (non-interactive full work-item runner after `start`: drive remaining stages in `status.md`, run verification gates, perform delivery, land changes to `main` by policy, then execute `finish` cleanup)

report   (check branch first: on main/master, re-read this file and summarize roadmap progress; on other branches, report from `status.md`)

finish   (after a work item is delivered: clean up the completed branch; if PR was skipped, ensure Roadmap/Milestones + docs/README update, commit, and push are done before cleanup)
