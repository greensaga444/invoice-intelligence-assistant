# Work Item: Processor column validation and generation rules

## Spec

Business Goal:
Replace prototype hardcoded behavior with customer-approved, auditable rules for input column validation and AI output generation in the processor module.

User Story:
As an accounting supervisor, I need explicit and configurable processor rules for Excel column checks and AI result generation, so outputs are consistent, reviewable, and aligned with accounting policy.

MVP:
Define the canonical Folder A input schema and Folder B AI output rules, then implement processor-side validation and generation behavior from an explicit rule contract instead of scattered hardcoded literals.

Scope:
- In scope: Folder A column schema (required/optional, aliases, type/format), row-level validation rules, AI output field/value policy, confidence threshold policy, processor refactor to consume rule definitions, and test coverage.
- Out of scope: external vector database integration, external LLM provider integration, downloader-side source format redesign, and role-based approval workflow redesign.

Acceptance Criteria:
- A canonical processor schema definition exists for Folder A input columns and Folder B AI output fields.
- Required-column, alias, and type/format validation behavior is deterministic and documented.
- Validation failures report file name, row number, field, and failure reason.
- AI output generation follows approved deductible status/category/confidence rules and manual-review threshold policy.
- Rule changes can be updated through configuration/spec artifacts without requiring broad source rewrites.
- Unit/integration tests cover success path, alias mapping, missing required columns, invalid field values, and threshold routing behavior.
- Build, tests, and formatting checks pass.

## Tasks

- [ ] Confirm customer-approved Folder A column definitions (required/optional/aliases) with sample files.
- [ ] Finalize AI input field list and transformation rules from raw invoice rows.
- [ ] Finalize AI output field constraints (allowed values, formats, confidence precision, fallback behavior).
- [ ] Define confidence threshold policy for auto-accept vs review-required routing.
- [ ] Add a processor rule definition artifact (config/spec) for schema and policy governance.
- [ ] Refactor Folder A reader to validate and map using the approved schema definition.
- [ ] Refactor AI decision generation path to apply approved policy values instead of hardcoded literals.
- [ ] Add processor tests for schema validation errors and generation policy compliance.
- [ ] Update PRD open questions and traceability links after rule decisions are confirmed.
- [ ] Run build/test/format verification gates.