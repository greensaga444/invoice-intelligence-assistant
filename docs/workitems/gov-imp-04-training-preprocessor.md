# Work Item: GOV-IMP-04 training dataset governance preprocessor

## Spec

Business Goal:
Ensure training inputs are governance-clean before model build by applying deterministic deduplication and conflict resolution on canonicalized rows.

User Story:
As a QA/data-governance owner, I need training rows to be deduplicated and conflict-resolved consistently so training outcomes are reproducible and auditable.

MVP:
Implement a training dataset governance preprocessor that:
- normalizes row values,
- removes exact duplicates via governance key,
- resolves label conflicts per deterministic precedence,
- exposes duplicate/conflict summary counts to workflow validation output.

Scope:
- In scope: preprocessor component, `TrainingWorkflowService` integration, governance summary counts, and unit tests.
- Out of scope: split-policy partition redesign, report-file writer pipeline, and policy-approval enforcement flow.

Acceptance Criteria:
- Training row exact duplicates are removed deterministically.
- Training label conflicts are resolved deterministically and conflict count is produced.
- `TrainingWorkflowService` parsing path consumes governance preprocessor output.
- Validation result exposes governance duplicate/conflict counts.
- Unit tests cover duplicate handling, conflict majority/tie-break, and workflow summary count surfacing.
- Build/tests/format checks pass.

## Tasks

- [x] Add training governance preprocessor component.
- [x] Integrate preprocessor into `TrainingWorkflowService` dataset parsing path.
- [x] Expose governance duplicate/conflict counts in dataset validation result.
- [x] Add tests for duplicate handling, conflict resolution, and workflow summary output.
- [x] Run verification gates (`dotnet test`, `dotnet format --verify-no-changes`).
- [x] Update roadmap/workitem/open-question/testing/README/status docs for delivery traceability.
