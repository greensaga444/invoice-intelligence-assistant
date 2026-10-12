# Project Workflow Template

A lightweight, stage-based workflow for shipping one work item per branch.

## Core Files

| File | Scope | Purpose |
| --- | --- | --- |
| `docs/project.md` | Whole codebase | One-time project definition: target, architecture, roadmap, milestones |
| `docs/status.md` | Single branch / work item | Day-to-day execution through INIT/PLAN/IMPLEMENTATION/VERIFICATION/DELIVERY |

Works for web, desktop, mobile, CLI, libraries, backend services, and embedded projects.

## Optional Documentation Packs

| Path | Purpose |
| --- | --- |
| `docs/prd/` | Product requirements source files (single or multi-file PRDs) |
| `docs/style/` | Two-layer coding style system (baseline + language appendices) |

---

## Startup Modes

### New Project

1. Fill `docs/project.md` (Final Target, Architecture, Roadmap, Milestones).
2. Set `Status: DEFINED` only after content is reviewed and approved.

### Existing Project

1. Copy this template into the repo.
2. Run `init` to scan languages/frameworks/layout and draft fields.
3. Fill Final Target and Roadmap manually (the scan cannot infer business goals).

---

## Is Project Defined?

Use `Init Status` in the resolved project file (`TEMPLATE` or `DEFINED`):

```bash
PROJECT_FILE="docs/project.md"
[ -f "$PROJECT_FILE" ] || PROJECT_FILE=".workflow/project.md"
[ -f "$PROJECT_FILE" ] || PROJECT_FILE="project.md"

if [ -f "$PROJECT_FILE" ] && grep -q '^Status: DEFINED' "$PROJECT_FILE"; then
  echo "defined"
else
  echo "not defined"
fi
```

Branch-work prerequisite: before starting any work-item branch flow, the resolved project file (`docs/project.md`, `.workflow/project.md`, or root `project.md`) must be `Status: DEFINED`.
If status is `TEMPLATE`, run `init` and complete project definition first.

---

## Per-Branch Workflow

1. Create branch: `<type>/<short-name>` (`feature|fix|chore|docs`).
2. Prepare `docs/status.md`, then run `goto init`.
3. Choose track:
   - FULL: INIT -> PLAN -> IMPLEMENTATION -> VERIFICATION
   - LIGHT: INIT -> IMPLEMENTATION -> VERIFICATION
   - Quick rule: choose FULL if any of these are true: unresolved design unknowns, migration/external API risk, or multi-component changes; otherwise choose LIGHT.
4. Move forward with `goto next stage` (guarded by exit criteria).
  - Optional non-interactive mode: `run auto` to execute all remaining stages and delivery in one pass.
5. After IMPLEMENTATION and VERIFICATION are complete, run Delivery:
   - Update docs (`docs/project.md`, other docs, `README.md`) as needed.
   - Commit delivery updates.
   - Apply branch policy:
     - If main is protected (therefore PR is required): run `open pr` (one PR per branch; reuse existing PR).
     - If main is not protected (therefore PR is not required): push the delivery commit directly to `main`.
   - Merge (if needed), then `finish`.

---

## One-Page Flow Cheat Sheet

### 0) Initialize Once (main/master)

1. Fill `docs/project.md`.
2. Confirm Final Target, Architecture, Roadmap, Milestones.
3. Set `Status: DEFINED`.

Gate: do not start branch work while status is `TEMPLATE`.

### 1) Start One Work Item (new branch)

1. Create branch: `<type>/<short-name>`.
2. Open `docs/status.md`.
3. Run `goto init`.
4. Fill Work Item Name + Description.
5. Confirm Definition of Done and overlap check.

### 2) Pick Track During INIT

Choose FULL if any answer is yes:

1. Unresolved design uncertainty?
2. Migration/external API risk?
3. More than one major component touched?

If all are no, choose LIGHT.

### 3) Execute Stages

| Track | Stage Path |
| --- | --- |
| FULL | INIT -> PLAN -> IMPLEMENTATION -> VERIFICATION |
| LIGHT | INIT -> IMPLEMENTATION -> VERIFICATION |

Use `goto next stage` for guarded progression.
Use `goto plan|implementation|verification` only for explicit navigation/rework (must record reason in Next Action).
Use `run auto` when you want unattended execution from the current stage through delivery.

### 4) Verification Must-Haves

1. Tests pass.
2. Format/lint clean (if defined).
3. Local CI green when CI config exists; otherwise run project build + test.
4. Add verification evidence (test output, CI summary, and/or screenshots).

### 5) Delivery (after verification only)

1. Update docs: roadmap/milestones + README/related docs.
2. Commit delivery updates.
3. Land by policy: protected main uses `open pr` (one PR per branch); unprotected main pushes directly to `main`.
4. Run `finish` for branch cleanup.

Done definition: updates landed per policy, docs updated, branch cleaned.

---

## Stage Command Semantics

- `goto next stage`: guarded transition; auto-starts entered stage.
- `goto plan|implementation|verification`: explicit navigation; no guard; no auto-start.
- Manual stage change must record a one-line reason in `Next Action`.
- Only `goto init` resets checklist items.

---

## File Location

- Small/new projects: keep workflow files in `docs/`.
- Large existing repos: use `.workflow/` to avoid conflicts.

On FULL track, PLAN output goes to `<workflow-dir>/workitems/<name>.md`.

---

## Template Notes

- This repository is a template; copy or use it as a starting point.
- In target projects, keep canonical workflow files under `docs/` (or `.workflow/` for large repos).
- Keep product requirements in `docs/prd/` (indexed by `docs/prd/00-index.md`).
- Keep coding standards in `docs/style/` (Layer 1 baseline + Layer 2 language appendices).
- This README describes workflow mechanics; target-project README should describe that project's product.
- See `docs/project.md` and `docs/status.md` for complete command behavior and AI rules.
