# Invoice Intelligence Assistant

## PRD Index

### Document Information

| Item | Value |
|----------|----------|
| Product Name | Invoice Intelligence Assistant |
| Version | 1.0 |
| Status | Draft |
| Owner | Davis Chen |
| Target Platform | Windows Desktop Application |
| Last Updated | 2026-10-12 |

---

## Product Summary

Invoice Intelligence Assistant is a Windows desktop application designed for accounting firms and bookkeeping service providers.

The system automates:

- Customer list import
- Invoice download from Ministry of Finance services
- AI-assisted invoice analysis
- Tax deduction determination
- Accounting category recommendation
- Excel generation
- Manual knowledge feedback

The solution uses local AI technologies and keeps all customer data inside the company environment.

---

## Core Architecture

Customer List

↓

Invoice Downloader

↓

Folder A (Raw Invoice Files)

↓

Invoice AI Processor

- Rule Engine
- Case Cache
- RAG Engine
- LLM

↓

Folder B (Processed Results)

↓

Manual Knowledge Feedback

↓

Knowledge Base Update

---

## Technology Stack

### Desktop Application

- C#
- .NET 8
- WPF

### AI Components

- Ollama
- Qwen3 8B
- BGE-M3
- Qdrant

### Configuration

- INI Files

---

## Documentation Structure

- 01-master-prd.md
- 10-scope-and-goals.md
- 20-requirements-functional.md
- 30-requirements-nonfunctional.md
- 40-user-flows.md
- 50-acceptance-criteria.md
- 90-open-questions.md

---

## Language Policy

### Development Documents

PRD and technical documents shall be maintained in English.

### End User Deliverables

The following must be Traditional Chinese:

- User Interface
- Error Messages
- AI Outputs
- User Manuals
- Operation Manuals
- Customer Documents

---

## Requirement ID Scheme

Functional Requirements:

- FR-001 and above

Non-Functional Requirements:

- NFR-001 and above

User Flows:

- UF-001 and above

Acceptance Criteria:

- AC-001 and above

Open Questions:

- Q-001 and above
