# Acceptance Criteria

## Criteria Catalog

- **AC-001 (FR-001, UF-001)**  
  Given launcher starts with packaged modules, when readiness check runs, then each module shows ready/missing status based on executable/config presence.

- **AC-002 (FR-002, UF-001)**  
  Given downloader and processor are ready, when user starts full flow, then launcher waits for downloader exit and prompts before processor start.

- **AC-003 (FR-003, UF-001)**  
  Given module binaries exist, when user clicks individual launch buttons, then selected module starts without blocking other module controls.

- **AC-004 (FR-004, UF-002)**  
  Given settings form input, when required values are missing or threshold is outside 0..1, then save is blocked with validation warning.

- **AC-005 (FR-005, UF-002)**  
  Given valid settings are submitted, when save completes, then downloader/processor INI files are written and MOF password is persisted in encrypted form.

- **AC-006 (FR-006, UF-003)**  
  Given a customer list workbook with recognized header aliases, when import executes, then records are parsed into tax ID/company/date range fields.

- **AC-007 (FR-007, UF-003)**  
  Given malformed customer rows, when import executes, then importer fails with explicit row-level validation message.

- **AC-008 (FR-008, UF-003)**  
  Given successful import, when UI refreshes, then customer grid shows loaded records and downstream actions can be enabled by state.

- **AC-009 (FR-009, UF-004)**  
  Given configured account/app key and valid password, when authenticate executes, then a session token and expiry are produced.

- **AC-010 (FR-010, UF-004/UF-005)**  
  Given missing prerequisites (config/customers/session/downloader service), when batch start is requested, then downloader blocks execution with reason.

- **AC-011 (FR-011, UF-005)**  
  Given official scope selection is enabled, when official batch runs, then queries execute with retry on transient errors and merged output is deduplicated.

- **AC-012 (FR-012, UF-005)**  
  Given one customer download fails in batch, when batch continues, then remaining customers are still processed and final summary includes mixed outcomes.

- **AC-013 (FR-013, UF-005)**  
  Given official batch records, when exporter runs, then Folder A workbook includes summary/detail sheets and allowance sheet when allowance rows exist.

- **AC-014 (FR-014, UF-005)**  
  Given batch completes, when log is written, then log contains counts and per-customer status/message/output paths.

- **AC-015 (FR-015, UF-006)**  
  Given Folder A contains supported `.xlsx`, when processor reads files, then records are parsed using legacy or official schema reader.

- **AC-016 (FR-016, UF-006)**  
  Given a record matches deterministic rule, when pipeline executes, then result is emitted from Rule Engine without cache/RAG/LLM fallback.

- **AC-017 (FR-017, UF-006)**  
  Given rule miss and cache key hit, when pipeline executes, then decision comes from case cache without RAG/LLM.

- **AC-018 (FR-018, UF-006)**  
  Given unresolved record and available knowledge cases, when retrieval runs, then top scored references are returned by local vector retriever.

- **AC-019 (FR-019, UF-006)**  
  Given unresolved record, when local analyzer runs, then decision includes deductible status, reason, category, and confidence score in `[0,1]`.

- **AC-020 (FR-020, UF-006)**  
  Given batch of records, when pipeline completes, then summary counters report total/resolved-by-rule/resolved-by-cache/resolved-by-llm.

- **AC-021 (FR-021, UF-007)**  
  Given one low-confidence record in a source group, when partitioning runs, then entire group is routed to Folder C.

- **AC-022 (FR-022, UF-007)**  
  Given decision results and naming rule, when export runs, then output files are generated with configured naming and unique-path collision handling.

- **AC-023 (FR-023, UF-007)**  
  Given invoice/allowance outputs, when workbook is generated, then worksheets follow Table-3/Table-4 compatible column layouts.

- **AC-024 (FR-024, UF-006/UF-007)**  
  Given processing in progress, when UI updates, then operator sees progress %, processed counts, and Folder B/C summary counts.

- **AC-025 (FR-025, UF-008)**  
  Given feedback list with selected rows, when user clicks update knowledge, then only approved rows are submitted for persistence.

- **AC-026 (FR-026, UF-008)**  
  Given approved rows include duplicates, when repository writes, then duplicates are skipped and response includes added/skipped counts.

- **AC-027 (FR-027, UF-009)**  
  Given governance manifest missing/invalid/unapproved, when training service initializes, then initialization fails with explicit policy message.

- **AC-028 (FR-028, UF-009)**  
  Given dataset with aliases and governed vocabulary, when validation runs, then canonical fields are mapped and non-canonical labels are reported.

- **AC-029 (FR-029, UF-010)**  
  Given duplicate/conflicting dataset rows, when preprocessing runs, then deterministic dedup/conflict resolution is applied and split leakage guard holds.

- **AC-030 (FR-030, UF-010)**  
  Given validation run completes, when reports are emitted, then summary/reject/conflict JSON files are written to quality-gate folder.

- **AC-031 (FR-031, UF-009)**  
  Given valid dataset and user actions, when train/evaluate/publish execute, then staged model, evaluation report, published model, and publish metadata are generated.

- **AC-032 (FR-032, UF-009)**  
  Given release metrics and thresholds, when gate evaluator runs, then decision is deterministic with precedence `Block > Fail > Warn > Pass` and rollback evaluation support.

---

## Traceability Completeness Check

| FR Coverage Rule | Status |
|---|---|
| Every FR in `20-requirements-functional.md` maps to at least one UF | PASS |
| Every FR maps to at least one AC in this document | PASS |
| AC IDs are unique and stable | PASS |
