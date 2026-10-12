# Work Item: AI decision pipeline core

## Spec

Business Goal:
Enable the processor module to classify invoice records through a staged AI flow that prioritizes deterministic and reusable knowledge before local LLM reasoning.

User Story:
As an accounting operator, I need the processor to generate structured AI decisions for each unresolved invoice record, so I can get consistent deductible guidance with traceable decision sources.

MVP:
Invoice AI Processor reads Folder A records, executes Rule Engine, Case Cache, RAG retrieval, and local LLM in order, and outputs structured decision results with stage-level summary.

Scope:
- In scope: processor-side invoice record model, staged decision pipeline orchestration, local heuristic implementations for Rule/Cache/RAG/LLM, Folder A reader, startup integration, and unit tests.
- Out of scope: enhanced Folder B Excel export columns, manual feedback write-back, external AI service integration.

Acceptance Criteria:
- Rule Engine can resolve matching records without invoking LLM.
- Case Cache reuses historical decisions when exact pattern is found.
- RAG retrieval returns relevant reference cases for unresolved records.
- Local LLM stage generates deductible status, deduction reason, accounting category, and confidence score.
- Processor executes stages in order and reports structured decision results.
- Build, tests, and formatting checks pass.

## Tasks

- [x] Create feature branch for roadmap item 4.
- [x] Add processor core domain models for invoice analysis input and structured AI decisions.
- [x] Implement staged decision pipeline orchestration (Rule Engine -> Case Cache -> RAG -> LLM).
- [x] Implement local Rule Engine, Case Cache, RAG retriever, and LLM analyzer placeholders.
- [x] Add Folder A invoice reader to transform raw Excel rows into analysis records.
- [x] Wire Invoice AI Processor startup to execute pipeline and show summary output.
- [x] Add unit tests for stage ordering, stage short-circuit logic, and Folder A reader validation.
- [x] Run build/test/format verification gates.