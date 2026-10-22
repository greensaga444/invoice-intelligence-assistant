# Master Product Requirements Document

## Product Name

Invoice Intelligence Assistant

---

## Executive Summary

The implemented product is a local-first Windows desktop suite for accounting teams that need to process invoice batches with consistent, auditable decisions. It combines:

1. customer-list driven official MOF retrieval,
2. multi-stage AI-assisted decisioning, and
3. human-in-the-loop feedback + training governance.

The current codebase emphasizes deterministic operations, explicit validations, and local artifact generation over remote orchestration.

---

## Current Product Modules

### 1) InvoiceLauncher (control center)

- Detects module runtime readiness (`Downloader`, `Processor`, `Trainer`) and config presence.
- Supports both full flow execution (`Downloader` then optional `Processor`) and independent module launch.
- Provides unified settings dialog that writes both `config/downloader.ini` and `config/processor.ini`.

### 2) InvoiceDownloader

- Imports customer list from `.xlsx`.
- Performs credential checks and local session issuance before batch execution.
- Queries official MOF endpoint through configured app/account/password parameters.
- Exports official-format Folder A workbook(s) and writes batch log files per run.

### 3) InvoiceAiProcessor

- Reads Folder A `.xlsx` files in either legacy schema or official exporter schema.
- Executes staged decision pipeline:
  - keyword rule engine,
  - in-memory case cache,
  - local vector-style retrieval,
  - local heuristic analyzer.
- Splits output into Folder B (ready) and Folder C (manual review) by confidence threshold at source-group level.

### 4) InvoiceTrainer

- Allows explicit approval-based feedback writes into JSON knowledge repository.
- Provides non-CLI training workflow:
  - dataset selection,
  - validation,
  - train,
  - evaluate,
  - publish.
- Enforces approved governance policy metadata before training service initialization.
- Emits quality-gate and model artifacts under `build/training/`.

---

## Operating Model

### Deployment model

- Offline-capable desktop operation for the AI decision pipeline and training flow.
- Configuration-driven behavior via INI files.
- Local filesystem as source of truth for runtime inputs/outputs.

### Dataflow model

1. Customer list input (`config/company-list.xlsx` by default).
2. Downloader writes raw/official workbooks to Folder A.
3. Processor creates structured decision outputs in Folder B and review set in Folder C.
4. Trainer writes approved knowledge cases to JSON repository and training/evaluation/publish artifacts.

---

## Product Boundaries

Included in current implementation:

- launcher orchestration and settings sync,
- official-mode download path,
- deterministic AI decision pipeline with confidence-based triage,
- manual knowledge write-back with deduplication,
- governance-aware non-CLI model workflow.

Explicitly out of scope in current implementation:

- ERP integration,
- cloud deployment,
- mobile client,
- multi-tenant SaaS orchestration,
- auto-learning without manual approval.

---

## Language and Output Policy

- User-facing UI and business output field labels are Traditional Chinese.
- Product requirement documentation remains English.
