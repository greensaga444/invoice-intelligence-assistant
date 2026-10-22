# Scope and Goals

## Scope Baseline

This document reflects implemented behavior in the current repository (`src/`, `config/`, `tests/`).

---

## In Scope (Implemented)

### A. Launcher orchestration and settings

- Module readiness checks for executable and required config files.
- Full flow trigger: run Downloader first, then prompt to continue to Processor.
- Standalone launch controls for Downloader/Processor/Trainer.
- Unified settings editor with field validation and dual-INI persistence.

### B. Downloader workflow

- Customer-list `.xlsx` import with required-column alias support.
- Data validation for tax ID, company name, and date range.
- Session-gated batch execution flow.
- Official API query + export flow for invoice and allowance scopes.
- Per-customer failure isolation and consolidated batch log output.

### C. Processor workflow

- Folder A reader supporting both legacy and official worksheet patterns.
- Decision pipeline order: Rule Engine → Case Cache → Vector Retriever → Local Heuristic Analyzer.
- Confidence-threshold review partitioning at source-group granularity.
- Folder B / Folder C typed workbook export with configurable naming.

### D. Knowledge and training workflow

- Explicit approval-based knowledge feedback updates to local JSON repository.
- Training module UI for dataset validation, model training, evaluation, and publish.
- Governance policy approval/version gating.
- Deterministic preprocessing (dedup/conflict), split leakage guard, and quality-gate report artifacts.

---

## Out of Scope (Not Implemented)

- ERP integration and outbound system connectors.
- Cloud-hosted deployment model.
- Mobile app or web portal operator surface.
- Automatic learning without explicit human approval.
- Multi-tenant access control and role-based workflow.

---

## Measurable Goals (Code-Aligned)

| Goal ID | Goal | Current Validation Source |
|---|---|---|
| G-001 | Support batch handling up to 150 customers and 30,000 records in quality-gate tests. | `LocalizationAndQualityGateTests` + `docs/evidence/q013-production-like-validation-2026-10-22.md` |
| G-002 | Keep AI inference pipeline local (no external AI service invocation patterns). | `LocalizationAndQualityGateTests.ProcessorCodePath_DoesNotContainExternalAiInvocationPatterns` |
| G-003 | Preserve deterministic folder outputs and naming-rule enforcement. | `FolderBEnhancedInvoiceExporterTests`, `OutputFileNameHelperTests`, `NamingRuleValidatorTests` |
| G-004 | Keep governance pipeline auditable via generated JSON reports and model artifacts. | `TrainingWorkflowServiceTests`, `TrainingGovernanceQualityGateReporterTests` |
| G-005 | Keep user-facing workflows executable without CLI for training lifecycle. | `InvoiceTrainer` UI + `TrainingWorkflowService` integration |

---

## Non-Goals for This PRD Sync

- No new feature commitments beyond implemented code.
- No speculative roadmap assertions without source evidence.
