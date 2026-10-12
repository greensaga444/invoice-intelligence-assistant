# Master Product Requirements Document

## Product Name

Invoice Intelligence Assistant

---

## Background

Accounting firms currently perform invoice downloading, invoice review, tax deduction determination, and accounting classification manually.

Main challenges:

- 80 to 150 customer companies
- Large invoice volume
- Inconsistent classification decisions
- Time-consuming manual review
- Limited scalability

The objective is to automate the process while maintaining decision quality.

---

## Business Objectives

1. Automate electronic invoice downloading.
2. Automate invoice analysis.
3. Reduce manual review effort.
4. Improve consistency.
5. Support future knowledge accumulation.
6. Operate within a local environment.

---

## Target Users

### Primary Users

- Accounting Firms
- Bookkeeping Service Providers
- Finance Departments

### Secondary Users

- Tax Consultants
- Accounting Supervisors

---

## Product Scope

### Included

- Customer list import
- MOF API configuration
- Invoice downloading
- Folder-based processing
- Rule engine
- Knowledge retrieval
- AI analysis
- Excel output
- Naming rule configuration
- Manual knowledge feedback

### Excluded

- ERP Integration
- Mobile Applications
- Public Cloud Deployment
- Automatic AI Learning
- Multi-Tenant SaaS

---

## Solution Overview

The system consists of two independent executable modules.

### Module A

Invoice Downloader

Responsibilities:

- Read customer list
- Authenticate with MOF services
- Download invoices
- Export raw invoice files to Folder A

### Module B

Invoice AI Processor

Responsibilities:

- Read invoice files from Folder A
- Apply Rule Engine
- Apply RAG search
- Execute LLM analysis
- Generate enhanced Excel files
- Export to Folder B

---

## AI Strategy

The system shall use:

Rule Engine

↓

Case Cache

↓

RAG Search

↓

LLM Reasoning

This approach minimizes unnecessary AI calls and improves consistency.

---

## Local AI Stack

### Runtime

Ollama

### LLM

Qwen3 8B

### Embedding Model

BGE-M3

### Vector Database

Qdrant

---

## User Language Requirements

Version 1 shall use Traditional Chinese as the only user-facing language.

This includes:

- Menus
- Buttons
- Dialogs
- Notifications
- Error Messages
- Processing Results
- AI Outputs

---

## Documentation Language

Technical documents:

- English

End-user documentation:

- Traditional Chinese
