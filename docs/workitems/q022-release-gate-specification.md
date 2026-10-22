# Work Item: Q-022 release-gate specification and decision flow

## Spec

Business Goal:
Define a deterministic model-release gate before switching processor runtime from baseline logic to a trained LLM model.

User Story:
As architecture, QA, and operations owners, we need a unified release-gate contract with measurable thresholds, pass/fail decision logic, and rollback triggers so runtime cutover is controlled and reversible.

MVP:
Deliver a release-gate work item that specifies:
- evaluation metrics schema,
- deterministic pass/fail evaluator logic,
- rollback trigger flow,
- release approval ownership and sign-off checkpoints.

Scope:
- In scope: metric definitions, threshold fields, evaluator algorithm, decision outputs, rollback trigger conditions, rollback steps, and sign-off flow.
- Out of scope: immediate runtime implementation of model switching, online service deployment automation, and UI redesign.

Acceptance Criteria:
- A metrics schema is defined with required fields for offline evaluation (for example status accuracy, category F1, reason quality score, coverage rate, sample size, evaluation window, model version).
- Pass/fail evaluator rules are defined with deterministic gate outputs (PASS, WARN, FAIL, BLOCK) and tie-break precedence.
- Rollback trigger flow is defined for post-release regression signals, including trigger thresholds, owner notification, and rollback execution steps.
- Decision artifact schema is defined with evaluator inputs, computed result, trigger reasons, and approval metadata.
- Q-022 progress is linked to this work item in open-question tracking.

## Metrics Schema Draft (v0.1)

Required dimensions:
- Model identity: modelVersion, trainingDatasetVersion, governancePolicyVersion, releaseCandidateId
- Evaluation scope: datasetType, evaluationWindowStart, evaluationWindowEnd, totalSamples, coveredSamples
- Core quality metrics: deductibleStatusAccuracy, accountingCategoryMacroF1, deductionReasonQualityScore, confidenceCalibrationError
- Stability metrics: driftDeltaVsBaseline, unresolvedRate, manualReviewRate
- Operational metrics: evaluationDurationMs, inferenceLatencyP95Ms

Field-level constraints:
- All rate metrics are decimal values in [0, 1].
- totalSamples must be > 0.
- coveredSamples must be <= totalSamples.
- Version fields are non-empty and immutable for one evaluation run.

## Pass/Fail Evaluator Draft (v0.1)

Gate outputs:
- PASS: all required thresholds met and no blocker flags.
- WARN: no blocker threshold broken, but one or more warning thresholds crossed.
- FAIL: one or more fail thresholds crossed.
- BLOCK: required metadata missing or evaluation sample size below minimum gate.

Deterministic precedence:
1. BLOCK dominates all other outcomes.
2. FAIL dominates WARN and PASS.
3. WARN dominates PASS.
4. PASS only when none of the above conditions are true.

Evaluator inputs:
- metrics payload
- threshold profile
- baseline reference snapshot
- evaluator version

Evaluator outputs:
- gateResult
- breachedRules
- warningRules
- summaryReason
- evaluatedAtUtc

## Rollback Trigger Flow Draft (v0.1)

Trigger categories:
- Quality regression trigger: key production metrics regress beyond rollback threshold versus baseline.
- Safety trigger: critical business misclassification spike (for example non-deductible false positive spike).
- Stability trigger: sustained runtime/inference degradation crossing operational threshold.

Rollback sequence:
1. Trigger detected and incident record created.
2. On-call owner validates signal and confirms rollback condition.
3. Runtime switches to previous approved model or baseline pipeline.
4. Incident note captures trigger metrics, rollback timestamp, approver, and affected version.
5. Re-release requires a new evaluation run and fresh gate approval.

Ownership checkpoints:
- QA owner confirms metric validity.
- Architecture owner confirms threshold profile and evaluator version.
- Operations owner confirms rollback readiness and execution path.
- Product owner grants release approval for runtime switch.

## Tasks

- [x] Create Q-022 release-gate work item skeleton.
- [x] Draft metrics schema (required fields and constraints).
- [x] Draft deterministic pass/fail evaluator states and precedence.
- [x] Draft rollback trigger categories and rollback sequence.
- [ ] Define concrete numeric threshold profile with stakeholders.
- [ ] Define production observation window and shadow-run duration.
- [ ] Define release approval form and audit artifact template.
- [ ] Link final decision values back into Q-022 and RTM once approved.
