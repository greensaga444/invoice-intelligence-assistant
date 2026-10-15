# Open Questions

## Q-001 Invoice Download Mechanism

Which invoice download mechanism will be used in production?

Options:

- MOF Business API
- Turnkey
- Both

Status:

- To Be Confirmed

---

## Q-002 Invoice File Format

What is the final invoice file format provided by the download process?

Required:

- Sample invoice files
- Column definitions

Status:

- Pending sample files

---

## Q-003 AI Input Columns

Which invoice columns should be used as AI input?

Potential candidates:

- Item Description
- Vendor Name
- Vendor Tax ID
- Invoice Type
- Tax Category
- Amount
- Remarks

Status:

- To Be Confirmed

---

## Q-004 AI Output Columns

Which business fields should AI generate?

Current proposal:

- AI_可抵稅
- AI_判定原因
- AI_建議科目
- AI_信心度
- AI_參考案例

Status:

- Awaiting customer confirmation

---

## Q-005 Historical Dataset Size

How many historical records are available?

Estimated ranges:

- 10,000+
- 50,000+
- 100,000+

Status:

- To Be Confirmed

---

## Q-006 Knowledge Base Structure

Which fields should be stored in the knowledge repository?

Candidate fields:

- Customer
- Vendor
- Invoice Description
- Deductible Status
- Reason
- Accounting Category
- Reviewer

Status:

- To Be Confirmed

---

## Q-007 Rule Engine Definition

Which business rules should be handled before AI execution?

Examples:

- Fixed tax rules
- Customer-specific rules
- Vendor-specific rules

Status:

- To Be Confirmed

---

## Q-008 Confidence Threshold

What confidence threshold should trigger manual review?

Suggested:

```text
>= 90%      Auto Accept
80% ~ 89%   Review Recommended
< 80%       Manual Review Required
```

Status:

- Awaiting customer decision

---

## Q-009 Naming Rule Governance

Who will maintain the naming rules?

Options:

- System Administrator
- Operations Team
- Accounting Team

Status:

- To Be Confirmed

---

## Q-010 Knowledge Feedback Approval

Who is authorized to approve knowledge-base updates?

Options:

- Accounting Staff
- Accounting Supervisor
- Project Administrator

Current Recommendation:

- Supervisor Approval

Status:

- To Be Confirmed

---

## Q-011 Scheduling Requirement

Should future versions support scheduled execution?

Examples:

- Daily download
- Weekly processing
- Monthly processing

Status:

- Future Enhancement

---

## Q-012 ERP Integration

Will future versions integrate with ERP systems?

Potential systems:

- SAP
- Oracle
- 鼎新
- In-house ERP

Status:

- Future Enhancement

---

## Q-013 Processing Volume Validation

Actual invoice volume has not been verified.

Current estimate:

- 150 customers
- 15,000 ~ 30,000 invoices per execution

Status:

- Requires production validation

---

## Q-014 AI Accuracy Target

What is the minimum acceptable AI accuracy rate?

Suggested target:

```text
90%+
```

Status:

- Awaiting customer confirmation

---

## Q-015 Industry Classification Source

For deductible decision logic, which source is authoritative for customer industry classification?

Options:

- Firm-provided industry code
- Public disclosure data lookup by Tax ID

Status:

- Awaiting customer decision

Policy reference:

- docs/policy/excel-policy-structured.md (Section 1)

---

## Q-016 Expense Category Credit Rule for Amount >= 10,000

For expense-category entries, what is the credit account rule when amount is greater than or equal to 10,000?

Known rules from policy:

- Cost category: amount > 10,000 -> 2130 (Accounts Payable)
- Expense category: rule for amount >= 10,000 not explicitly defined

Status:

- Awaiting customer decision

Policy reference:

- docs/policy/excel-policy-structured.md (Section 4)

---

## Q-017 Highest-Amount Item Selection Method

When selecting the highest-amount item for fallback judgment, how should negative lines (e.g., discounts) be handled?

Options:

- Compare by signed value (default numeric order)
- Compare by absolute value
- Compare only positive values

Status:

- Awaiting customer decision

Policy reference:

- docs/policy/excel-policy-structured.md (Section 3)

---

## Q-018 Tax Allocation Rounding Precision

Tax allocation formula is specified, but rounding precision is not fully explicit.

To confirm:

- Round to integer NTD
- Round to 1 decimal place
- Round to 2 decimal places

Status:

- Awaiting customer decision

Policy reference:

- docs/policy/excel-policy-structured.md (Section 5)

---

## Q-019 Item Name Noise-Removal Dictionary

Should item-name cleanup be governed by a fixed dictionary/rule set?

Examples:

- Symbol removal (e.g., star marks)
- Marketing phrase removal
- Prefix/suffix cleanup

Status:

- Awaiting customer decision

Policy reference:

- docs/policy/excel-policy-structured.md (Section 5)
