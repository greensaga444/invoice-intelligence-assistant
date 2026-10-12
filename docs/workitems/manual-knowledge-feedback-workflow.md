# Work Item: Manual knowledge feedback workflow

## Spec

Business Goal:
Enable controlled knowledge accumulation so approved accounting judgments can improve future retrieval quality while preventing unreviewed auto-learning.

User Story:
As an accounting reviewer, I need to explicitly approve selected AI decision records and update the local knowledge base, so future RAG retrieval can reuse validated decisions.

MVP:
Invoice AI Processor provides a manual selection UI for approved records and writes only approved records into local knowledge storage that is loaded by RAG on subsequent processing.

Scope:
- In scope: persistent knowledge repository, manual approval action, startup loading of approved knowledge into RAG retrieval, INI config support for knowledge repository path, and unit tests.
- Out of scope: automatic learning without approval, cloud/vector service integration, role-based approval workflow.

Acceptance Criteria:
- User can select reviewed records and explicitly trigger knowledge update.
- Knowledge base is updated only for explicitly approved records.
- With no approval action, knowledge base remains unchanged.
- Newly approved records are persisted locally and retrievable by RAG matching.
- Build, tests, and formatting checks pass.

## Tasks

- [x] Create feature branch for roadmap item 6.
- [x] Add local JSON-based knowledge repository and manual feedback service in shared core.
- [x] Load approved knowledge entries into processor RAG retrieval flow.
- [x] Add processor UI for manual record approval and explicit knowledge update action.
- [x] Extend processor configuration with knowledge repository file path.
- [x] Add unit tests for approved write-back, duplicate handling, no-selection no-op, and retrieval reuse.
- [x] Run build/test/format verification gates.
