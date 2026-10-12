# User Flows

## Overview

This document describes the primary user and system workflows for Invoice Intelligence Assistant.

The product consists of two independent executable modules:

1. Invoice Downloader
2. Invoice AI Processor

Both modules can be executed independently.

---

# UF-001 Customer List Import

## Preconditions

- Customer List Excel exists.
- Required columns exist.

Required columns:

- Tax ID
- Company Name
- Date Range

## Main Flow

1. User launches Invoice Downloader.
2. User selects Customer List Excel file.
3. System validates required columns.
4. System loads customer records.
5. System prepares download queue.

## Alternate Flow

### UF-001-A1 Missing Required Columns

1. System detects missing column.
2. System displays validation error.
3. Import process stops.

## Postconditions

- Customer records loaded successfully.

---

# UF-002 MOF Authentication

## Preconditions

- Customer records loaded.
- MOF credentials configured.

## Main Flow

1. System loads MOF configuration.
2. System authenticates using:
   - Username
   - Password
   - API Key
3. Authentication succeeds.
4. Download session established.

## Alternate Flow

### UF-002-A1 Authentication Failure

1. Authentication fails.
2. Error logged.
3. User notified.

## Postconditions

- Authenticated session established.

---

# UF-003 Batch Invoice Download

## Preconditions

- Authentication successful.

## Main Flow

1. System processes customer queue.
2. System downloads invoice data.
3. System generates raw invoice Excel.
4. System saves files into Folder A.

## Output

Example:

```text
Folder A

12345678_ABC_2026-01~2026-03.xlsx
```

## Postconditions

- Invoice files available in Folder A.

---

# UF-004 AI Processing

## Preconditions

- Invoice files exist in Folder A.

## Main Flow

1. User launches Invoice AI Processor.
2. System scans Folder A.
3. System loads invoice files.
4. System prepares invoice records.

## Postconditions

- Records ready for analysis.

---

# UF-005 Rule Engine Processing

## Main Flow

1. System evaluates configured rules.
2. Matching records are classified immediately.
3. Results stored.

## Postconditions

- Matching records resolved without AI.

---

# UF-006 Case Cache Lookup

## Main Flow

1. System searches historical cache.
2. Exact matches identified.
3. Existing decisions reused.

## Postconditions

- Duplicate analysis avoided.

---

# UF-007 RAG Retrieval

## Main Flow

1. System creates embeddings.
2. System queries Qdrant.
3. Similar historical examples retrieved.
4. Retrieved examples attached to context.

## Postconditions

- Context prepared for LLM.

---

# UF-008 LLM Analysis

## Main Flow

1. System sends context to Qwen.
2. Qwen determines:
   - Deductible status
   - Reason
   - Accounting category
   - Confidence score
3. Structured result returned.

## Postconditions

- AI decision generated.

---

# UF-009 Export Processed Excel

## Main Flow

1. Original invoice data preserved.
2. AI columns appended.
3. Naming rule applied.
4. Excel exported to Folder B.

## Added Columns

- AI_可抵稅
- AI_判定原因
- AI_建議科目
- AI_信心度
- AI_參考案例

## Output

```text
Folder B

12345678_ABC_2026-01~2026-03.xlsx
```

---

# UF-010 Manual Knowledge Feedback

## Preconditions

- User has reviewed results.

## Main Flow

1. User selects approved records.
2. User clicks Update Knowledge Base.
3. Records added to knowledge repository.
4. Vector index updated.

## Important Rule

System shall never automatically learn without explicit user approval.

## Postconditions

- Knowledge base updated.
