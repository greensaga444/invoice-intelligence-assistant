# Work Item: GOV-IMP-06 split-policy runtime alignment

## Spec

Business Goal:
Align runtime dataset partition behavior with governance split policy so training and evaluation operate on leakage-safe deterministic cohorts.

User Story:
As governance and QA owners, we need train/validation/test partition outputs and leakage checks to be deterministic and auditable.

MVP:
Implement split-policy runtime alignment that:
- partitions canonical training rows into train/validation/test,
- applies cohort leakage guard by company and month key,
- surfaces partition counts and ratios in validation outputs,
- emits split metrics into quality-gate summary artifacts.

Scope:
- In scope: split runtime logic, leakage guard, workflow/result shape updates, summary report enrichment, and tests.
- Out of scope: model selection strategy changes and UI-level split-configuration controls.

Acceptance Criteria:
- Runtime partition assignment follows governance buckets (0-7 train, 8 validation, 9 test).
- Cohort key (`Normalize(CompanyName)|yyyy-MM`) remains in exactly one partition.
- Validation output includes train/validation/test counts and ratios.
- Summary report JSON includes split distribution metrics and leakage-guard status.
- Trainer status text surfaces validation partition count for training runs.
- Build/tests/format checks pass.

## Tasks

- [x] Update split logic to produce train/validation/test partitions.
- [x] Add cohort leakage guard verification in split runtime.
- [x] Extend validation result schema with partition counts/ratios and leakage status.
- [x] Enrich quality-gate summary report schema with split metrics.
- [x] Update trainer training-status message with validation partition count.
- [x] Add tests for split metrics and same-cohort partition consistency.
- [x] Run verification gates (`dotnet test`, `dotnet format --verify-no-changes`).
- [x] Update roadmap/workitem/open-question/testing/README/status docs for delivery traceability.
