# Requirements Traceability Matrix (RTM)

## Purpose

This supplemental matrix mirrors the synchronized PRD set and provides quick FR/UF/AC/NFR cross-reference.

---

## Functional Requirement Coverage

| FR | Flow(s) | Acceptance |
|---|---|---|
| FR-001 | UF-001 | AC-001 |
| FR-002 | UF-001 | AC-002 |
| FR-003 | UF-001 | AC-003 |
| FR-004 | UF-002 | AC-004 |
| FR-005 | UF-002 | AC-005 |
| FR-006 | UF-003 | AC-006 |
| FR-007 | UF-003 | AC-007 |
| FR-008 | UF-003 | AC-008 |
| FR-009 | UF-004 | AC-009 |
| FR-010 | UF-004, UF-005 | AC-010 |
| FR-011 | UF-005 | AC-011 |
| FR-012 | UF-005 | AC-012 |
| FR-013 | UF-005 | AC-013 |
| FR-014 | UF-005 | AC-014 |
| FR-015 | UF-006 | AC-015 |
| FR-016 | UF-006 | AC-016 |
| FR-017 | UF-006 | AC-017 |
| FR-018 | UF-006 | AC-018 |
| FR-019 | UF-006 | AC-019 |
| FR-020 | UF-006 | AC-020 |
| FR-021 | UF-007 | AC-021 |
| FR-022 | UF-007 | AC-022 |
| FR-023 | UF-007 | AC-023 |
| FR-024 | UF-006, UF-007 | AC-024 |
| FR-025 | UF-008 | AC-025 |
| FR-026 | UF-008 | AC-026 |
| FR-027 | UF-009 | AC-027 |
| FR-028 | UF-009 | AC-028 |
| FR-029 | UF-010 | AC-029 |
| FR-030 | UF-010 | AC-030 |
| FR-031 | UF-009 | AC-031 |
| FR-032 | UF-009 | AC-032 |

---

## Non-Functional Requirement Validation Map

| NFR | Validation evidence |
|---|---|
| NFR-001 | Runtime target in app csproj files (`net8.0-windows`) |
| NFR-002 | Core/test target split in csproj files (`net8.0` / `net9.0-windows`) |
| NFR-003 | `LocalizationAndQualityGateTests` scale scenarios |
| NFR-004 | Processor UI progress + summary count behavior |
| NFR-005 | `LocalizationAndQualityGateTests` external AI invocation pattern guard |
| NFR-006 | `MofPasswordProtector` + config writer behavior + tests |
| NFR-007 | `TrainingGovernancePolicyApprovalLoader` + workflow tests |
| NFR-008 | Batch downloader orchestration tests and integration test |
| NFR-009 | `OutputFileNameHelperTests` |
| NFR-010 | Importer/config/reader failure-path tests |
| NFR-011 | Export ordering logic + exporter tests |
| NFR-012 | Governance preprocessing and bootstrap tests |
| NFR-013 | Split-policy leakage guard tests |
| NFR-014 | XAML/UI strings + localization tests |
| NFR-015 | `LocalHeuristicLlmAnalyzerTests` |
| NFR-016 | Training artifact generation tests |
