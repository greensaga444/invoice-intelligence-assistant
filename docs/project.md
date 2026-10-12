# PROJECT OVERVIEW

> Use this file for a **whole new codebase**.
> Fill it in once at project start (typically from a PRD via `init`) to define the architecture, final target, roadmap, and milestones.
> Each roadmap item is later implemented on its own branch using `status.md`.

## Init Status

Status: DEFINED

> TEMPLATE = not yet defined; DEFINED = initialization complete.
> Set to DEFINED only after Final Target, Architecture, and Roadmap are filled in.
> This marker is the single source of truth for "is project.md defined?".

---

## Project

Project Name: Invoice Intelligence Assistant

Repository: https://github.com/greensaga444/invoice-intelligence-assistant.git

Owner: Davis Chen

Project Type: Windows Desktop Application

Target Platforms: Windows 10 and Windows 11

Tech Stack:

- Language(s): C# (.NET 8)
- Framework / UI: WPF desktop application
- Data / Storage: Excel files, INI configuration, local file system, Qdrant vector store
- Backend / Services: Local invoice download integration, local AI pipeline via Ollama
- Build / Packaging: .NET SDK 8, solution-based build pipeline
- Infrastructure / Distribution: Local deployment within customer environment (offline-first)

---

## Delivery Policy

> Decided once, after this file is defined. Governs how work branches land.

- Main Branch Protected: no  (yes / no)
- Pull Request Required: same as Main Branch Protected
- CI Required: same as Pull Request Required (must pass before merge)
- Merge Method: squash  (e.g. squash / merge / rebase)
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

Build a local-first invoice intelligence desktop system that reduces accounting teams' manual workload by automating invoice download, AI-assisted analysis, and standardized Excel output.

Problem Statement:

Accounting teams handling 80 to 150 customer companies process high invoice volumes with manual, inconsistent decisions, creating bottlenecks and quality variance. The project addresses this by combining deterministic rules, reusable historical decisions, retrieval-based context, and local LLM reasoning.

Success Definition (Done means):

- End-to-end batch flow completes from customer import to Folder B export.
- System supports 150 customer companies and 15,000 to 30,000 invoice records per run.
- File generation success rate is above 99% and failures are logged.
- All AI inference runs locally with no external AI calls.
- User-facing interface and generated business outputs are in Traditional Chinese.

Out of Scope:

- ERP integration
- Mobile applications
- Public cloud deployment
- Automatic learning without user approval
- Multi-tenant SaaS workflow

---

## Architecture

High-Level Overview:

The product is composed of two independent executables: Invoice Downloader and Invoice AI Processor. Downloader imports customer metadata and fetches invoice files into Folder A. AI Processor reads Folder A and applies a staged decision pipeline (Rule Engine -> Case Cache -> RAG -> LLM) before exporting enhanced Excel files to Folder B and accepting approved knowledge feedback.

Components:

| Component | Responsibility | Tech |
| --- | --- | --- |
| Invoice Downloader | Import customer list, authenticate, batch download invoices, output raw files to Folder A | C#, .NET 8, WPF, MOF integration |
| Invoice AI Processor | Read Folder A invoices, apply staged AI decision pipeline, generate enhanced outputs to Folder B | C#, .NET 8, WPF |
| Rule Engine | Resolve deterministic tax/accounting rules before AI calls | Configurable local rule set |
| Case Cache | Reuse prior exact decisions to reduce repeated analysis | Local cache storage |
| RAG Engine | Retrieve similar historical records for model context | BGE-M3 embeddings, Qdrant |
| LLM Runtime | Generate deductible status, reasons, category, confidence | Ollama, Qwen3 8B |
| Knowledge Feedback Module | Accept user-approved corrections and update searchable knowledge | Manual approval flow + vector index update |
| Configuration Module | Manage MOF, AI, folder, and naming rule settings | INI configuration |

Data Flow:

1. Customer list import provides Tax ID, company name, and date range.
2. Invoice Downloader authenticates and fetches customer invoices.
3. Raw invoice files are exported to Folder A.
4. AI Processor loads records and executes Rule Engine first.
5. Unresolved records go through Case Cache, then RAG retrieval, then LLM reasoning.
6. Enhanced files preserve source columns and append AI columns.
7. Processed files are exported to Folder B.
8. Approved manual feedback updates knowledge storage and retrieval index.

Key Decisions:

- Use a two-module executable architecture to separate download and analysis responsibilities.
- Enforce local-only AI execution for security and compliance.
- Use staged decision routing (Rule Engine -> Cache -> RAG -> LLM) to optimize consistency and cost.
- Keep configuration externalized in INI files for maintainability and operations.
- Preserve source data immutability and emit enriched outputs as new files.

Constraints / Non-Functional Requirements:

- Performance: startup under 5 seconds; support 15,000 to 30,000 records and 150 companies per batch.
- Security: all data and AI inference stay local; credentials stored securely; no external AI APIs.
- Scalability: handle growing historical knowledge while preserving export reliability above 99%.

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
| 1 | Project skeleton and configuration foundation | A buildable .NET 8 solution with separate Downloader and AI Processor apps plus INI configuration loading is available. | P0 | FULL | - | DONE |
| 2 | Customer import and MOF authentication flow | Users can import customer Excel data and complete MOF authentication with clear validation and error handling. | P0 | FULL | 1 | DONE |
| 3 | Batch invoice download to Folder A | System downloads invoices in batch for imported customers and writes raw Excel outputs into Folder A with logs. | P0 | FULL | 2 | TODO |
| 4 | AI decision pipeline core | Processor executes Rule Engine, Case Cache, RAG, and LLM in order and produces structured AI decisions per unresolved record. | P0 | FULL | 1 | TODO |
| 5 | Enhanced Excel export to Folder B | Processor preserves source columns, appends all required AI columns in Traditional Chinese, and exports files to Folder B using naming rules. | P0 | FULL | 4 | TODO |
| 6 | Manual knowledge feedback workflow | User-approved records can be written back to knowledge storage and become retrievable by RAG without automatic learning. | P1 | FULL | 4 | TODO |
| 7 | Localization and quality gates | Traditional Chinese UI/output, offline-only AI checks, performance targets, and acceptance test suite are all validated. | P1 | FULL | 3,5,6 | TODO |

Definition of Done: one line describing a distinct user-visible outcome; no two rows should share it.
Track values: FULL (full pipeline) / LIGHT (skip PLAN)
Status values: TODO / IN PROGRESS / DONE / BLOCKED

---

## Milestones

> Defined here on `init` (from the PRD/Final Target). Each finished milestone triggers a README refresh.

- [ ] M1: Downloader MVP complete (customer import, authentication, batch download, Folder A export)
- [ ] M2: AI Processor MVP complete (decision pipeline and Folder B enhanced export)
- [ ] M3: Production readiness complete (knowledge feedback, localization, performance/security validation)

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
