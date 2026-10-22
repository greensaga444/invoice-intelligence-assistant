# Work Item: GOV-IMP-03 knowledge bootstrap dedup/conflict engine

## Spec

Business Goal:
Provide deterministic governance handling for knowledge bootstrap rows so duplicate and conflicting labels are resolved consistently before retrieval ingestion.

User Story:
As a data-governance owner, I need the system to deduplicate and resolve conflicting bootstrap candidates using explicit precedence, with conflict evidence retained for audit.

MVP:
Add a dedicated knowledge bootstrap governance engine that:
- normalizes bootstrap candidates,
- deduplicates by configured knowledge fingerprint,
- resolves conflicts by source/date/label precedence,
- emits structured conflict audit entries.

Scope:
- In scope: processor-side governance engine, deterministic precedence logic, conflict audit model, and unit tests.
- Out of scope: UI import flow, physical report-file export pipeline, and final knowledge repository write-back integration.

Acceptance Criteria:
- Duplicate candidate fingerprints are detected and counted.
- Conflict identity groups resolve deterministically by governance precedence.
- Conflict audit output includes conflict key, selected label, and competing labels.
- Unit tests cover duplicate handling, source-priority precedence, date precedence, and conflict-audit generation.
- Build/tests/format checks pass.

## Tasks

- [x] Add processor-side knowledge bootstrap governance engine component.
- [x] Implement dedup fingerprint and conflict identity grouping logic.
- [x] Implement deterministic precedence (source, date, majority/lexical tie-break).
- [x] Add unit tests for duplicate handling and conflict resolution behavior.
- [x] Run verification gates (`dotnet test`, `dotnet format --verify-no-changes`).
- [x] Update roadmap/workitem/open-question/testing/README/status docs for delivery traceability.
