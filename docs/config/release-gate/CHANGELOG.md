# Release Gate Policy Changelog

## 1.0.0 - 2026-10-22

- Added initial Q-022 release-gate policy artifact.
- Defined metrics schema and field-level constraints for offline evaluation.
- Defined deterministic evaluator outputs and precedence (BLOCK > FAIL > WARN > PASS).
- Defined threshold profile with warning/fail bands.
- Defined shadow-run observation window and comparison scope.
- Defined rollback trigger thresholds and incident flow requirements.
- Added release approval template and required audit fields.

## Versioning Rules

- MAJOR: incompatible schema or gate semantics.
- MINOR: backward-compatible policy additions.
- PATCH: clarifications or typo fixes without semantic impact.
