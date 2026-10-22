# Work Item: GOV-IMP-07 governance approval metadata flow

## Spec

Business Goal:
Prevent unapproved governance policies from affecting training behavior by enforcing approval metadata and version validation at runtime.

User Story:
As a governance owner, I need runtime policy loading to verify approval metadata and policy-version compatibility so training only uses approved policy contracts.

MVP:
Implement policy-loader enforcement that:
- reads governance policy manifest metadata,
- validates approval state and required fields (`approvedBy`, `approvedAt`, `effectiveFrom`),
- validates supported policy major version,
- blocks trainer workflow startup when policy metadata is invalid.

Scope:
- In scope: policy approval loader, trainer-service integration, policy artifact metadata update, and test coverage.
- Out of scope: multi-version policy negotiation and external approval workflow systems.

Acceptance Criteria:
- Runtime loader rejects non-approved policy status.
- Runtime loader rejects malformed/unsupported policy version.
- Runtime loader rejects missing approval metadata and future effective date.
- `TrainingWorkflowService` refuses initialization with unapproved/invalid policy manifest.
- Governance policy artifact carries approval metadata values.
- Build/tests/format checks pass.

## Tasks

- [x] Add governance policy approval loader with metadata and version checks.
- [x] Integrate loader into `TrainingWorkflowService` initialization.
- [x] Update governance policy artifact with approval metadata fields.
- [x] Add workflow tests for draft-policy and unsupported-version rejection.
- [x] Update governance artifact docs/changelog for metadata enforcement behavior.
- [x] Run verification gates (`dotnet test`, `dotnet format --verify-no-changes`).
- [x] Update roadmap/workitem/open-question/testing/README/status docs for delivery traceability.
