# Requirements Traceability Matrix (RTM)

## Purpose

This document provides traceability between:

- Functional Requirements (FR)
- Non-Functional Requirements (NFR)
- User Flows (UF)
- Acceptance Criteria (AC)

This matrix ensures that every requirement can be validated and that every acceptance criterion has a corresponding business requirement.

---

# Functional Requirements ↔ Acceptance Criteria Mapping

| Requirement ID | Requirement Summary | Acceptance Criteria |
|----------|----------|----------|
| FR-001 | Import Customer List Excel | AC-001 |
| FR-002 | Read Tax ID | AC-001 |
| FR-003 | Read Company Name | AC-001 |
| FR-004 | Read Date Range | AC-001 |
| FR-005 | Configure MOF Account | AC-002 |
| FR-006 | Configure API Credentials | AC-002 |
| FR-007 | Download Invoice Data | AC-003 |
| FR-008 | Batch Invoice Download | AC-003 |
| FR-009 | Export Raw Invoice Files to Folder A | AC-004 |
| FR-010 | Support INI Configuration | AC-010 |
| FR-011 | Support Naming Rules | AC-010 |
| FR-012 | Validate Naming Placeholders | AC-011 |
| FR-013 | Read Invoice Files From Folder A | AC-012 |
| FR-014 | Execute Rule Engine | AC-005 |
| FR-015 | Execute Case Cache Lookup | AC-006 |
| FR-016 | Execute RAG Retrieval | AC-007 |
| FR-017 | Execute LLM Analysis | AC-008 |
| FR-018 | Generate Deductible Status | AC-008 |
| FR-019 | Generate Deduction Reason | AC-008 |
| FR-020 | Generate Accounting Category Recommendation | AC-008 |
| FR-021 | Generate Confidence Score | AC-008 |
| FR-022 | Generate Enhanced Excel Files | AC-012 |
| FR-023 | Preserve Original Data | AC-013 |
| FR-024 | Append AI Generated Columns | AC-009 |
| FR-025 | Export Files To Folder B | AC-012 |
| FR-026 | Manual Knowledge Feedback | AC-014 |
| FR-027 | Update Knowledge Base After Approval | AC-014 |
| FR-028 | Traditional Chinese User Interface | AC-016 |
| FR-029 | Traditional Chinese AI Output | AC-017 |
| FR-030 | Traditional Chinese Excel Columns | AC-009 |

---

# Non-Functional Requirements ↔ Validation Mapping

| NFR ID | Requirement | Validation Method |
|----------|----------|----------|
| NFR-001 | Windows 10 Support | Installation Test |
| NFR-002 | Windows 11 Support | Installation Test |
| NFR-003 | Support 15,000–30,000 Invoice Records | Performance Test |
| NFR-004 | Support 150 Customer Companies Per Batch | Performance Test |
| NFR-005 | Application Startup Time < 5 Seconds | Benchmark Test |
| NFR-006 | Data Remains Within Local Environment | Security Audit |
| NFR-007 | No External AI Service Used | Architecture Review |
| NFR-008 | Secure Credential Storage | Security Audit |
| NFR-009 | Configurable Business Rules | Functional Validation |
| NFR-010 | Configurable Naming Rules | Functional Validation |
| NFR-011 | Configurable AI Settings | Functional Validation |
| NFR-012 | Success Rate > 99% | User Acceptance Test |
| NFR-013 | Error Logging Available | Functional Validation |
| NFR-014 | Preserve Source Data Integrity | Integration Test |
| NFR-015 | Traditional Chinese User Interface | Localization Validation |
| NFR-016 | Traditional Chinese AI Output | Localization Validation |
| NFR-017 | Traditional Chinese Excel Columns | Localization Validation |
| NFR-018 | Traditional Chinese User Documentation | Documentation Review |

---

# User Flow ↔ Functional Requirement Mapping

| User Flow | Description | Functional Requirements |
|----------|----------|----------|
| UF-001 | Customer List Import | FR-001, FR-002, FR-003, FR-004 |
| UF-002 | MOF Authentication | FR-005, FR-006 |
| UF-003 | Batch Invoice Download | FR-007, FR-008, FR-009 |
| UF-004 | Invoice Loading | FR-013 |
| UF-005 | Rule Engine Processing | FR-014 |
| UF-006 | Case Cache Lookup | FR-015 |
| UF-007 | RAG Retrieval | FR-016 |
| UF-008 | LLM Analysis | FR-017, FR-018, FR-019, FR-020, FR-021 |
| UF-009 | Processed Excel Export | FR-022, FR-023, FR-024, FR-025 |
| UF-010 | Manual Knowledge Feedback | FR-026, FR-027 |

---

# End-to-End Business Capability Coverage

| Business Capability | Acceptance Criteria |
|----------|----------|
| Customer Import | AC-001 |
| MOF Authentication | AC-002 |
| Invoice Download | AC-003 |
| Folder A Generation | AC-004 |
| Rule Engine Execution | AC-005 |
| Case Cache Reuse | AC-006 |
| RAG Retrieval | AC-007 |
| LLM Analysis | AC-008 |
| AI Output Columns | AC-009 |
| Naming Rule Execution | AC-010 |
| Placeholder Validation | AC-011 |
| Folder B Generation | AC-012 |
| Original Data Preservation | AC-013 |
| Manual Knowledge Feedback | AC-014 |
| No Automatic Learning | AC-015 |
| Traditional Chinese User Interface | AC-016 |
| Traditional Chinese AI Output | AC-017 |
| Offline Local AI Execution | AC-018 |

---

# Governance Specification Traceability (Q-016 / Q-021)

| Governance Requirement | Implementation Slice | Verification Evidence |
|----------|----------|----------|
| Canonical Table3/Table4 mapping and normalization | GOV-IMP-02 | `Table3Table4CanonicalMapperTests` |
| Deterministic knowledge dedup/conflict precedence | GOV-IMP-03 | `KnowledgeBootstrapGovernanceEngineTests` |
| Deterministic training dedup/conflict preprocessing | GOV-IMP-04 | `TrainingDatasetGovernancePreprocessorTests` |
| Quality-gate summary/reject/conflict reporting | GOV-IMP-05 | `TrainingGovernanceQualityGateReporterTests` |
| Train/validation/test split-policy runtime alignment + cohort leakage guard | GOV-IMP-06 | `TrainingWorkflowServiceTests` split-policy cases |
| Governance policy approval metadata and version enforcement | GOV-IMP-07 | `TrainingWorkflowServiceTests` policy-approval rejection cases |
| End-to-end governance verification and document traceability | GOV-IMP-08 | `GovernanceEndToEndVerificationTests` + PRD/open-question/RTM updates |

---

# MVP Exit Criteria

The MVP release is considered complete when all items below are satisfied.

## Functional Requirements

- [ ] Customer List Import Completed
- [ ] MOF Authentication Completed
- [ ] Invoice Download Completed
- [ ] Folder A Export Completed
- [ ] Rule Engine Completed
- [ ] Case Cache Completed
- [ ] RAG Retrieval Completed
- [ ] LLM Analysis Completed
- [ ] Folder B Export Completed
- [ ] Naming Rule Engine Completed
- [ ] Manual Knowledge Feedback Completed

---

## AI Components

- [ ] Ollama Installed
- [ ] Qwen3 8B Operational
- [ ] BGE-M3 Operational
- [ ] Qdrant Operational
- [ ] Local AI Processing Verified

---

## Localization

- [ ] Traditional Chinese User Interface
- [ ] Traditional Chinese AI Outputs
- [ ] Traditional Chinese Excel Columns
- [ ] Traditional Chinese User Documentation

---

## Security

- [ ] No External AI Calls
- [ ] Credentials Stored Securely
- [ ] Customer Data Remains Local

---

## Performance

- [ ] Support 150 Customer Companies
- [ ] Support 15,000–30,000 Invoice Records
- [ ] Startup Time Less Than 5 Seconds
- [ ] Success Rate Greater Than 99%

---

## Acceptance Criteria

All acceptance criteria must pass:

- [ ] AC-001
- [ ] AC-002
- [ ] AC-003
- [ ] AC-004
- [ ] AC-005
- [ ] AC-006
- [ ] AC-007
- [ ] AC-008
- [ ] AC-009
- [ ] AC-010
- [ ] AC-011
- [ ] AC-012
- [ ] AC-013
- [ ] AC-014
- [ ] AC-015
- [ ] AC-016
- [ ] AC-017
- [ ] AC-018

---

# Release Recommendation

The product may proceed to User Acceptance Testing (UAT) only after:

1. All High Priority Functional Requirements are implemented.
2. All Acceptance Criteria pass.
3. Localization validation passes.
4. Local AI execution is verified.
5. Security review is completed.
6. Customer data remains fully local.
