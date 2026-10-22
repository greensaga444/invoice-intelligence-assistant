# Work Item: GOV-IMP-05 quality-gate validator and reporting

## Spec

Business Goal:
Provide executable governance-quality evidence through machine-readable validation reports for training datasets.

User Story:
As QA and governance reviewers, we need validation summary, reject details, and conflict details emitted consistently so dataset quality decisions are auditable.

MVP:
Add a quality-gate reporter that:
- builds validation summary metrics,
- parses reject issues into row/field/reason entries,
- emits conflict decision entries,
- writes deterministic JSON report artifacts to local output.

Scope:
- In scope: reporter component, workflow integration, report-path surfacing, and test coverage.
- Out of scope: UI report viewer and external storage integration.

Acceptance Criteria:
- Summary report JSON is emitted with totals, duplicates, conflicts, and warnings.
- Reject report JSON is emitted with row/field/reason details and warnings.
- Conflict report JSON is emitted with conflict key, competing labels, and selected winner.
- `TrainingWorkflowService` returns report paths after validation/train/evaluate parsing steps.
- Tests cover reporter output and service-level report-path surfacing.
- Build/tests/format checks pass.

## Tasks

- [x] Add quality-gate reporter component and report schema records.
- [x] Integrate report generation into `TrainingWorkflowService` parse consumers.
- [x] Surface report paths in `TrainingDatasetValidationResult`.
- [x] Add tests for reporter JSON generation and workflow report-path output.
- [x] Run verification gates (`dotnet test`, `dotnet format --verify-no-changes`).
- [x] Update roadmap/workitem/open-question/testing/README/status docs for delivery traceability.
