# Invoice Intelligence Assistant

## PRD Index

### Document Information

| Item | Value |
|---|---|
| Product Name | Invoice Intelligence Assistant |
| Version | 2.0 |
| Status | Synced with implementation |
| Owner | Davis Chen |
| Target Platform | Windows desktop (.NET / WPF) |
| Last Updated | 2026-10-22 |

---

## Read Order

1. `01-master-prd.md` — executive-level product baseline
2. `10-scope-and-goals.md` — in-scope boundaries and measurable outcomes
3. `20-requirements-functional.md` — implementation-aligned FR catalog
4. `30-requirements-nonfunctional.md` — measurable quality, security, and runtime constraints
5. `40-user-flows.md` — concrete operator workflows
6. `50-acceptance-criteria.md` — verifiable AC + FR/UF mapping
7. `90-open-questions.md` — active unresolved gaps only
8. `60-requirements-traceability-matrix.md` — supplemental historical matrix

---

## Product Summary

Invoice Intelligence Assistant is a local-first Windows solution that coordinates four WPF apps:

- `InvoiceLauncher` (control center)
- `InvoiceDownloader` (customer import + official MOF fetch)
- `InvoiceAiProcessor` (rule/cache/RAG/local-heuristic decision pipeline)
- `InvoiceTrainer` (manual feedback + non-CLI training lifecycle)

The current implementation uses local processing for AI inference and local files for operational artifacts (`config/`, `data/`, `build/training/`).

---

## Architecture Snapshot

`Customer List (.xlsx)`  
→ `Downloader (MOF official API batch + Folder A export + batch log)`  
→ `Processor (Rule → Case Cache → Vector RAG → Local Heuristic Analyzer)`  
→ `Folder B (ready output) / Folder C (manual review)`  
→ `Trainer (approved feedback to knowledge repo + validate/train/evaluate/publish artifacts)`

---

## ID Scheme and Stability Rules

- Functional requirements: `FR-###`
- Non-functional requirements: `NFR-###`
- User flows: `UF-###`
- Acceptance criteria: `AC-###`
- Open questions: `Q-###`

IDs are unique and stable across PRD updates. New items append new IDs; existing IDs keep their meaning.

---

## Language Policy

- PRD and technical documents: English.
- End-user UI strings and business output fields: Traditional Chinese.
