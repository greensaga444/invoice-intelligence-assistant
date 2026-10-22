# Work Item: Q-013 processing volume validation

## Spec

Business Goal:
Validate that the processor can handle target-scale invoice volume reliably and produce auditable evidence before production rollout.

User Story:
As a delivery owner, I need repeatable scale-validation evidence for 150 customers and 15,000 to 30,000 invoice records per run, so we can make a production readiness decision with objective data.

MVP:
Provide automated baseline tests for target-scale load in CI/local runs and a production-run evidence checklist for final sign-off.

Scope:
- In scope: synthetic scale gate tests, timing evidence capture, production validation checklist, and Q-013 status updates.
- Out of scope: business-rule changes, AI model architecture changes, and customer-specific policy tuning.

Acceptance Criteria:
- Automated tests cover 30,000 records processing path.
- Automated tests explicitly cover 150-customer distribution under 30,000 records.
- Test results include elapsed-time assertion against current local baseline gate.
- A production validation checklist exists for real dataset execution and evidence capture.
- Q-013 status reflects baseline-complete and production-validation pending state.

## Tasks

- [x] Add processor quality-gate test for 30,000 records across 150 customers.
- [x] Run targeted quality-gate tests and capture elapsed-time evidence.
- [x] Add production-run checklist (input data snapshot, machine profile, elapsed time, error rate).
- [ ] Execute one production-like dry run when customer dataset is available.
- [ ] Update Q-013 status to resolved after production validation evidence is complete.
