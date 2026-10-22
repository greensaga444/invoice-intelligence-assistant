# Functional Requirements

## Launcher and Configuration

- **FR-001**: The launcher shall detect module executable/config readiness and show per-module readiness state.
- **FR-002**: The launcher shall support an end-to-end run mode that executes Downloader, then prompts the user before starting Processor.
- **FR-003**: The launcher shall allow independent startup for Downloader, Processor, and Trainer modules.
- **FR-004**: The launcher settings UI shall validate required fields and low-confidence threshold format before save.
- **FR-005**: The launcher shall persist settings into both `downloader.ini` and `processor.ini`, encrypting MOF password before writing.

## Downloader

- **FR-006**: The downloader shall import customer list data from `.xlsx` and resolve required fields by multilingual aliases.
- **FR-007**: The downloader shall reject rows with invalid tax ID, empty company name, invalid date range, or start date after end date.
- **FR-008**: The downloader shall display imported customer records and gate subsequent actions on successful import.
- **FR-009**: The downloader shall validate MOF credentials via local authentication rules and issue a time-limited session token.
- **FR-010**: The downloader shall block batch execution unless configuration, customer data, session token, and downloader service are ready.
- **FR-011**: The official batch downloader shall query enabled official scopes (sales/purchase invoices and allowances), retry transient errors, and deduplicate merged records.
- **FR-012**: Batch download shall isolate per-customer failures and continue processing remaining customers.
- **FR-013**: The downloader shall export Folder A outputs as official workbook format (summary/detail sheets and optional allowance sheet).
- **FR-014**: The downloader shall generate a human-readable batch log file with per-customer status, message, and output paths.

## Processor

- **FR-015**: The processor shall read Folder A `.xlsx` files and auto-detect supported worksheet schema (legacy or official).
- **FR-016**: The decision pipeline shall execute rule-engine resolution before cache/RAG/LLM stages.
- **FR-017**: The decision pipeline shall attempt case-cache resolution using vendor tax ID + item name + rounded net amount key.
- **FR-018**: The decision pipeline shall run local vector-style retrieval against built-in and approved knowledge cases when unresolved.
- **FR-019**: The local analyzer shall return structured decision output (`DeductibleStatus`, `DeductionReason`, `AccountingCategory`, `ConfidenceScore`).
  - See open question `Q-004` in `docs/prd/90-open-questions.md` for local heuristic vs local model runtime decision.
- **FR-020**: The pipeline shall return per-record source stage and aggregate stage summary counters.
- **FR-021**: Review partitioning shall route an entire source-group to Folder C if any record confidence is below configured threshold.
- **FR-022**: Export shall generate unique output files in Folder B/Folder C according to naming rule and output labels.
- **FR-023**: Export shall write Table-3/Table-4 compatible worksheet structures with AI decision fields mapped into business columns.
- **FR-024**: Processor UI shall display real-time progress, completion summary counts, and quick access to Folder C.

## Knowledge Feedback and Training

- **FR-025**: Manual knowledge feedback shall only write rows explicitly approved by user selection.
- **FR-026**: Knowledge repository updates shall deduplicate by decision fingerprint and persist case metadata (who/when/source fields).
- **FR-027**: Training service initialization shall require an approved governance policy manifest with supported semantic version.
- **FR-028**: Dataset validation shall canonicalize required fields through alias mapping and governance vocabulary checks.
- **FR-029**: Governance preprocessing shall apply deterministic deduplication, conflict resolution, and cohort-consistent split partitioning.
- **FR-030**: Validation shall emit quality-gate artifacts (`validation-summary.json`, `reject-report.json`, `conflict-report.json`).
- **FR-031**: Training workflow shall support staged model write, evaluation report generation, and publish metadata generation.
- **FR-032**: Release-gate evaluator shall provide deterministic `Block/Fail/Warn/Pass` decisions and rollback recommendations from threshold profiles.
