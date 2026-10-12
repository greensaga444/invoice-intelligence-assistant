# Work Item: Localization and quality gates

## Spec

Business Goal:
Ship a production-ready local-first release by enforcing Traditional Chinese user-facing outputs and executable quality gates for offline AI and performance baselines.

User Story:
As an accounting operations user, I need all screens and generated business outputs in Traditional Chinese with verified local-only AI execution and repeatable quality checks, so the system can be used safely in production.

MVP:
Traditional Chinese user-facing text is completed for current modules, offline-only AI checks are covered by tests, and acceptance/performance quality gates run in the standard test workflow.

Scope:
- In scope: user-facing text localization cleanup, offline-only AI execution validation, performance gate tests for pipeline target scale, and workflow/documentation updates.
- Out of scope: cloud AI integration, external vector database deployment, new business features beyond quality/localization readiness.

Acceptance Criteria:
- All user-facing content in current app flows is Traditional Chinese (AC-016).
- AI decision output fields and business output columns remain Traditional Chinese (AC-017).
- AI execution path is validated as local-only with no external AI service invocation patterns (AC-018).
- Performance quality gates validate target-scale processing for 15,000 to 30,000 records and startup/processing baseline assumptions (NFR-003, NFR-005).
- Build, tests, and format verification are green.

## Tasks

- [x] Create feature branch for roadmap item 7 and reset status tracker.
- [x] Complete PLAN artifacts for localization and quality gates.
- [x] Localize remaining user-facing English text in downloader/processor flows.
- [x] Add acceptance tests for Traditional Chinese outputs and offline-only AI checks.
- [x] Add performance gate tests for target-scale pipeline processing.
- [x] Run build/test/format verification and fix failures.
- [x] Update roadmap/milestone/docs for delivery.
