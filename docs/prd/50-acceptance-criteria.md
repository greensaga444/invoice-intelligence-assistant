# Acceptance Criteria

## AC-001 Customer List Import

**Given**

A valid customer list Excel file exists.

**When**

The user imports the file.

**Then**

The system shall successfully read:

- Tax ID
- Company Name
- Date Range

---

## AC-002 MOF Authentication

**Given**

Valid MOF credentials have been configured.

**When**

The system performs authentication.

**Then**

The system shall successfully establish a valid session.

---

## AC-003 Invoice Download

**Given**

Authentication is successful.

**When**

Invoice download is executed.

**Then**

The system shall download invoice data and generate invoice files.

---

## AC-004 Folder A Output

**Given**

Invoice download has completed.

**When**

Export is executed.

**Then**

Raw invoice Excel files shall be generated in Folder A.

---

## AC-005 Rule Engine Processing

**Given**

A record matches a predefined business rule.

**When**

Rule evaluation occurs.

**Then**

The system shall return a classification result without invoking the LLM.

---

## AC-006 Case Cache Lookup

**Given**

A matching historical case exists in Case Cache.

**When**

The same invoice pattern is processed.

**Then**

The cached decision shall be reused.

---

## AC-007 RAG Retrieval

**Given**

Historical knowledge exists.

**When**

The system performs retrieval.

**Then**

Relevant examples shall be returned from the vector database.

---

## AC-008 LLM Analysis

**Given**

An invoice record requires AI analysis.

**When**

The LLM is executed.

**Then**

The system shall generate:

- Deductible Status
- Deduction Reason
- Accounting Category
- Confidence Score

---

## AC-009 AI Output Columns

**Given**

Invoice processing is completed.

**When**

An output file is generated.

**Then**

The output file shall include:

- AI_可抵稅
- AI_判定原因
- AI_建議科目
- AI_信心度
- AI_參考案例

---

## AC-010 Naming Rule Execution

**Given**

A valid naming pattern has been configured.

**When**

The output file is generated.

**Then**

The filename shall follow the configured pattern.

Example:

```text
<統編>_<公司名稱>_<日期>.xlsx
```

---

## AC-011 Placeholder Validation

**Given**

The configured naming pattern contains placeholders.

**When**

Validation is performed.

**Then**

All placeholders must exist in the first row of the Customer List file.

---

## AC-012 Folder B Output

**Given**

AI processing is completed.

**When**

The export process finishes.

**Then**

Enhanced Excel files shall be generated in Folder B.

---

## AC-013 Original Data Preservation

**Given**

The system generates new output files.

**When**

Export is completed.

**Then**

The original invoice data shall remain unchanged.

---

## AC-014 Manual Knowledge Feedback

**Given**

A user reviews AI-generated results.

**When**

The user explicitly approves selected records.

**Then**

The selected records shall be added to the knowledge base.

---

## AC-015 No Automatic Learning

**Given**

AI processing completes.

**When**

No user approval is provided.

**Then**

The knowledge base shall not be updated.

---

## AC-016 Traditional Chinese User Interface

**Given**

The application is running.

**When**

A user views any screen.

**Then**

All user-facing content shall be displayed in Traditional Chinese.

---

## AC-017 Traditional Chinese AI Results

**Given**

AI analysis is completed.

**When**

Results are generated.

**Then**

All business outputs shall be generated in Traditional Chinese.

---

## AC-018 Offline AI Execution

**Given**

The application is running.

**When**

AI processing is executed.

**Then**

All inference shall be performed locally.

No external AI service shall be invoked.
