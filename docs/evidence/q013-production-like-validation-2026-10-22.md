# Q-013 Production-Like Validation Evidence (2026-10-22)

## Validation Scope

- Scenario: synthetic production-like pipeline run for 30,000 records across 150 customers.
- Source test: `LocalizationAndQualityGateTests.InvoiceDecisionPipeline_ProcessThirtyThousandRecordsAcrossOneHundredFiftyCustomers_CompletesWithinScaleTarget`.
- Gate command: `dotnet test InvoiceIntelligenceAssistant.slnx -v minimal --filter FullyQualifiedName~LocalizationAndQualityGateTests`.

## Machine Profile

| Field | Value |
| --- | --- |
| Run timestamp (local) | 2026-10-22 10:37:08 +08:00 |
| OS | Microsoft Windows 11 專業版 10.0.26200 (Build 26200) |
| CPU | AMD Ryzen 7 PRO 8840HS w/ Radeon 780M Graphics |
| RAM | 27.67 GB |
| Storage | WD PC SN740 SDDQNQD-1T00-1201 (SSD, 1,024,209,543,168 bytes) |

## Execution Results

### Target Quality-Gate Suite

- Command: `dotnet test InvoiceIntelligenceAssistant.slnx -v minimal --filter FullyQualifiedName~LocalizationAndQualityGateTests`
- Result: PASS (5 passed, 0 failed)
- Elapsed time: 7.303 seconds
- Exit code: 0

### Production-Like Dry Run

- Command: `dotnet test InvoiceIntelligenceAssistant.slnx -v minimal --filter FullyQualifiedName~InvoiceDecisionPipeline_ProcessThirtyThousandRecordsAcrossOneHundredFiftyCustomers_CompletesWithinScaleTarget`
- Result: PASS (1 passed, 0 failed)
- Processor elapsed time: 3.207 seconds
- Exit code: 0

## Evidence Fields Summary

| Evidence Field | Value |
| --- | --- |
| Run date/time | 2026-10-22 10:37:08 +08:00 |
| Customer count | 150 |
| Invoice count | 30,000 |
| Processor elapsed time | 3.207 seconds |
| Test gate result | PASS (LocalizationAndQualityGateTests: 5/5) |
| Error count | 0 |
| Output success rate | 100% (30,000/30,000 processed in dry-run test assertions) |

## Notes

- This evidence is based on the repository's deterministic production-like dry-run scenario used by quality-gate tests.
- For customer dataset validation runs, attach the same evidence fields in an additional report entry and keep Q-013 append-only history in this folder.
