# Work Item: Q-016 and Q-021 governance-spec framework

## Spec

Business Goal:
Define a governed, auditable policy framework for converting historical Table3/Table4 datasets into reusable knowledge and training inputs.

User Story:
As a product and QA owner, I need explicit governance rules for mapping, deduplication, and train/validation/test split so bootstrap and training outputs stay consistent and reviewable.

MVP:
Deliver a single governance specification that standardizes:
- canonical field mapping from Table3/Table4 into target knowledge/training schema,
- deterministic dedup/conflict resolution policy,
- leakage-safe split policy by company and time window.

Scope:
- In scope: mapping matrix, dedup key design, conflict resolution policy, data-quality gate rules, split strategy, and ownership/approval workflow.
- Out of scope: model retraining runtime changes, UI redesign, and external service integration.

Acceptance Criteria:
- Canonical mapping table exists for required input and target output fields.
- Dedup key definition and conflict-resolution precedence are deterministic and documented.
- Data-quality gates are defined with reject/warn behavior and reporting requirements.
- Split policy defines train/validation/test assignment rules that prevent leakage by company and time window.
- Governance spec identifies owners and approval checkpoints for future rule updates.
- Q-016 and Q-021 are traceably linked to the delivered specification.

## PLAN Expansion (Draft v0.1)

### Governance Baseline

- Source scope: historical Table3/Table4 style workbooks (`.xlsx`) used for knowledge bootstrap and training inputs.
- Current implementation baseline:
	- Training service already supports deterministic split by `CompanyName + yyyy-MM` hash bucket.
	- Knowledge repository dedup currently uses normalized fingerprint of `VendorTaxId + ItemName + DeductibleStatus + AccountingCategory`.
- This work item defines governance policy first; runtime changes are follow-up implementation items.

### 1) Canonical Mapping Framework

#### 1.1 Shared normalization rules

- Trim leading/trailing spaces for all text fields.
- Collapse repeated internal whitespace to single space for key fields.
- Preserve Traditional Chinese text exactly; do not transliterate.
- Date normalization output format: `yyyy-MM-dd`.
- Empty strings after trim are treated as null.

#### 1.2 Canonical fields for training governance (Q-021)

| Canonical Field | Required | Source aliases (Table3/Table4) | Type | Reject condition |
| --- | --- | --- | --- | --- |
| CompanyName | Yes | 公司名稱, 公司, 客戶名稱, 營業人名稱 | string | Missing/blank |
| InvoiceDate | Yes | 發票日期, 日期, 憑證日期 | date | Parse failure |
| ItemName | Yes | 品名, 品項, 摘要, 內容 | string | Missing/blank |
| DeductibleStatus | Yes | 可抵稅, AI_可抵稅, 是否可抵稅 | enum | Not in canonical vocabulary |
| AccountingCategory | Yes | 建議科目, AI_建議科目, 科目 | string | Missing/blank |
| DeductionReason | Yes | 判定原因, AI_判定原因, 原因 | string | Missing/blank |

#### 1.3 Canonical fields for knowledge bootstrap (Q-016)

| Canonical Field | Required | Candidate source aliases | Type | Reject condition |
| --- | --- | --- | --- | --- |
| VendorTaxId | Yes | 賣方統編, 廠商統編, 對方統編 | string(8) | Missing/invalid length |
| ItemName | Yes | 品名, 品項, 摘要, 內容 | string | Missing/blank |
| DeductibleStatus | Yes | 可抵稅, AI_可抵稅, 是否可抵稅 | enum | Not in canonical vocabulary |
| AccountingCategory | Yes | 建議科目, AI_建議科目, 科目 | string | Missing/blank |
| DeductionReason | Recommended | 判定原因, AI_判定原因, 原因 | string | Warn only when missing |
| CompanyName | Recommended | 公司名稱, 公司, 客戶名稱 | string | Warn only when missing |
| InvoiceDate | Recommended | 發票日期, 日期, 憑證日期 | date | Warn only when parse failure |
| InvoiceNumber | Optional | 發票號碼, 憑證號碼 | string | No reject |

#### 1.4 Canonical vocabulary and label mapping

| Input variants | Canonical DeductibleStatus |
| --- | --- |
| 可抵, 可扣抵, 可抵稅 | 可抵稅 |
| 不可抵, 不可扣抵, 不可抵稅 | 不可抵稅 |
| 不判定, 免判定 | 不判定 |

### 2) Deduplication and conflict-resolution policy

#### 2.1 Dedup keys

- Knowledge dedup fingerprint (aligned with current repository behavior):
	- `Normalize(VendorTaxId)|Normalize(ItemName)|Normalize(DeductibleStatus)|Normalize(AccountingCategory)`
- Training exact-duplicate key:
	- `Normalize(CompanyName)|InvoiceDate|Normalize(ItemName)|Normalize(DeductibleStatus)|Normalize(AccountingCategory)|Normalize(DeductionReason)`

#### 2.2 Conflict key and conflict types

- Conflict identity key:
	- `Normalize(VendorTaxId)|Normalize(ItemName)` for knowledge.
	- `Normalize(CompanyName)|InvoiceDate|Normalize(ItemName)` for training.
- Conflict types:
	- Type A: same key, different `DeductibleStatus`.
	- Type B: same key, same status, different `AccountingCategory`.
	- Type C: same status/category, different `DeductionReason` text.

#### 2.3 Conflict resolution precedence

1. Source priority: manual approved feedback > governed bootstrap import > heuristic/generated label.
2. Newer business date wins (`InvoiceDate` descending) when source priority ties.
3. Majority vote over label triples when date ties.
4. Deterministic lexical tie-breaker by `(DeductibleStatus, AccountingCategory, DeductionReason)` ascending.
5. All non-winning variants are recorded in conflict audit output.

### 3) Data-quality gate policy

#### 3.1 Reject gates

- Missing required canonical fields.
- Invalid date parsing for required date fields.
- Deductible status not in canonical vocabulary.
- VendorTaxId missing or not 8 digits for knowledge bootstrap rows.

#### 3.2 Warning gates

- Missing `DeductionReason` for knowledge bootstrap.
- Short reason text (length < 4).
- High duplicate ratio (> 30% of input rows).
- Category values outside approved dictionary (still ingestible, requires review flag).

#### 3.3 Required output reports

- Validation summary: total, valid, invalid, warning rows.
- Reject report: row number + field + reason.
- Conflict report: key + competing labels + selected winner + rule path.

### 4) Split-policy framework (Q-021)

#### 4.1 Leakage prevention rule

- Records sharing the same cohort key must remain in one partition only.
- Cohort key:
	- `Normalize(CompanyName)|yyyy-MM(InvoiceDate)`.

#### 4.2 Deterministic partition assignment

- Compute `hash = SHA256(cohortKey)` and `bucket = hash[0] % 10`.
- Partition policy target:
	- `bucket 0..7` -> Train (80%)
	- `bucket 8` -> Validation (10%)
	- `bucket 9` -> Test (10%)

#### 4.3 Transitional compatibility rule

- Until runtime supports separate validation artifact outputs, validation and test buckets may be combined operationally while still reported separately in governance output.

### 5) Governance ownership and approval flow

| Role | Responsibility |
| --- | --- |
| Product Owner | Approves canonical mapping and business-label vocabulary |
| Data Governance Owner | Approves dedup/conflict and quality gates |
| QA Owner | Approves split-policy leakage checks and report completeness |
| Engineering Owner | Implements and version-controls approved policy |

Approval flow:

1. Draft policy created in work item spec.
2. Product + data governance review mapping/dedup decisions.
3. QA reviews split-policy and quality-gate verifiability.
4. Final sign-off recorded with version tag and effective date.

### 6) Deliverables from this work item

- Governance spec document with versioned sections for mapping/dedup/split.
- Explicit Q-016 and Q-021 progress updates in open questions.
- Follow-up implementation backlog items for runtime changes required by the approved policy.
- GOV-IMP-01 artifact published at `docs/config/governance/table3-table4-governance-policy.v1.json` with changelog.

## Implementation Backlog (Derived from PLAN v0.1)

Execution model:

- Each backlog item is an independently deliverable slice.
- Runtime behavior changes must be gated by tests and migration-safe defaults.

| Backlog ID | Scope | Primary outputs | Depends on | Exit criteria |
| --- | --- | --- | --- | --- |
| GOV-IMP-01 | Governance schema contract artifact | Versioned governance spec artifact (`mapping`, `labels`, `qualityGates`, `splitPolicy`, `dedupPolicy`) under docs/config | - | Artifact committed with semantic version and changelog template |
| GOV-IMP-02 | Table3/Table4 canonical mapper and normalizer | Mapping/normalization component that resolves aliases and emits canonical rows | GOV-IMP-01 | Deterministic canonical row output with unit tests for alias/normalization cases |
| GOV-IMP-03 | Knowledge bootstrap dedup/conflict engine (Q-016) | Dedup fingerprint and conflict resolver for knowledge ingestion + conflict audit report | GOV-IMP-01, GOV-IMP-02 | Duplicate/conflict handling matches governance precedence and audit output is generated |
| GOV-IMP-04 | Training dataset governance preprocessor (Q-021) | Training-row dedup/conflict handling before model build, including canonical label mapping | GOV-IMP-01, GOV-IMP-02 | Training input snapshot is deterministic and conflict decisions are traceable |
| GOV-IMP-05 | Quality-gate validator and reporting | Reject/warn validation runner + summary/reject/conflict report emitters | GOV-IMP-01, GOV-IMP-02, GOV-IMP-03, GOV-IMP-04 | Reports generated with required fields and row-level traceability |
| GOV-IMP-06 | Split-policy runtime alignment | Train/validation/test partition support with cohort leakage guard and distribution metrics | GOV-IMP-01, GOV-IMP-04 | Cohort leakage tests pass and partition ratio metrics are reported |
| GOV-IMP-07 | Governance approval metadata flow | Policy version approval manifest (`approvedBy`, `approvedAt`, `effectiveFrom`) and loader checks | GOV-IMP-01 | Runtime refuses unapproved/invalid policy version with actionable errors |
| GOV-IMP-08 | End-to-end governance verification and traceability updates | Integration tests + PRD/open-question/RTM traceability updates | GOV-IMP-03, GOV-IMP-04, GOV-IMP-05, GOV-IMP-06, GOV-IMP-07 | End-to-end tests pass and Q-016/Q-021 can move to resolved after sign-off |

### Suggested Development Order

1. GOV-IMP-01
2. GOV-IMP-02
3. GOV-IMP-03 and GOV-IMP-04 (can run in parallel after GOV-IMP-02)
4. GOV-IMP-05
5. GOV-IMP-06
6. GOV-IMP-07
7. GOV-IMP-08

### Recommended First Slice

- Start with GOV-IMP-01 to freeze schema/versioning contract before coding mappers or validators.

## Tasks

- [x] Inventory current Table3/Table4 fields and existing internal target fields.
- [x] Draft canonical mapping matrix with required/optional field flags.
- [x] Define deduplication key and duplicate handling policy.
- [x] Define conflict-resolution policy for inconsistent labels.
- [x] Define minimum data-quality gate rules before knowledge/training write-back.
- [x] Define deterministic split policy (train/validation/test) by company and time window.
- [x] Define governance ownership, approval flow, and change-log format.
- [x] Split approved PLAN sections into sequenced implementation backlog items.
- [x] Update Q-016 and Q-021 status/progress notes after governance draft review.
