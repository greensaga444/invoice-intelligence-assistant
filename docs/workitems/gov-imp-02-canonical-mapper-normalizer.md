# Work Item: GOV-IMP-02 canonical mapper and normalizer

## Spec

Business Goal:
Implement the runtime canonical mapper/normalizer baseline so Table3/Table4-style datasets are parsed into deterministic canonical rows aligned with governance policy.

User Story:
As a training-operations owner, I need alias-based column resolution and normalized row values so model training input is consistent across historical datasets.

MVP:
Refactor trainer dataset parsing to use a dedicated canonical mapper that:
- resolves governed aliases,
- normalizes whitespace and text fields deterministically,
- maps deductible-status variants to canonical vocabulary,
- emits deterministic row-validation issues.

Scope:
- In scope: trainer-side canonical mapper component, workflow-service integration, and mapper/unit coverage.
- Out of scope: knowledge bootstrap ingestion runtime, dedup/conflict engine runtime, and split-policy runtime changes.

Acceptance Criteria:
- A dedicated mapper/normalizer component exists in trainer core code.
- Dataset parsing path uses the mapper instead of duplicated ad-hoc mapping logic.
- Alias headers map correctly to canonical fields.
- Whitespace normalization and deductible-status variant canonicalization are deterministic.
- Unknown deductible-status values produce explicit validation issues.
- Build/tests/format checks pass.

## Tasks

- [x] Create dedicated canonical mapper/normalizer component for Table3/Table4 training dataset parsing.
- [x] Refactor `TrainingWorkflowService` parsing path to consume the mapper component.
- [x] Add focused unit tests for alias mapping, normalization, and unknown label handling.
- [x] Run verification gates (`dotnet test`, `dotnet format --verify-no-changes`).
- [x] Update roadmap/status/traceability docs for delivery.
