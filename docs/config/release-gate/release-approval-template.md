# Release Approval Template (Q-022)

## Request Metadata

- requestId:
- requestedBy:
- requestedAtUtc:
- targetEnvironment:
- releaseCandidateId:

## Model and Dataset Identity

- modelVersion:
- trainingDatasetVersion:
- governancePolicyVersion:
- releaseGatePolicyVersion:

## Evaluator Summary

- evaluatorVersion:
- gateResult: PASS | WARN | FAIL | BLOCK
- blockedRules:
- breachedRules:
- warningRules:
- summaryReason:
- evaluatedAtUtc:

## Evaluation Window and Coverage

- evaluationWindowStart:
- evaluationWindowEnd:
- totalSamples:
- coveredSamples:
- coverageRate:

## Core Quality Metrics

- deductibleStatusAccuracy:
- accountingCategoryMacroF1:
- deductionReasonQualityScore:
- confidenceCalibrationError:

## Stability and Operational Metrics

- driftDeltaVsBaseline:
- unresolvedRate:
- manualReviewRate:
- evaluationDurationMs:
- inferenceLatencyP95Ms:
- nonDeductibleFalsePositiveRate:

## Shadow-Run Evidence

- shadowRunStartDate:
- shadowRunEndDate:
- shadowBusinessDays:
- baselineVersionCompared:
- comparisonSummaryLink:

## Rollback Readiness

- rollbackPlanVersion:
- previousApprovedModelVersion:
- rollbackExecutionOwner:
- rollbackNotificationChannel:
- rollbackDryRunEvidenceLink:

## Approval Checkpoints

- qaOwnerApproval: Approved | Rejected
- architectureOwnerApproval: Approved | Rejected
- operationsOwnerApproval: Approved | Rejected
- productOwnerApproval: Approved | Rejected

## Final Decision

- finalDecision: Approved | Rejected | Deferred
- finalDecisionAtUtc:
- finalDecisionBy:
- decisionNotes:
