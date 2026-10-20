# Work Item: Q-017 true vector RAG migration

## Spec

Business Goal:
Improve retrieval quality for unresolved invoice records by moving from exact keyword matching to local vector similarity matching.

User Story:
As an accounting reviewer, I need the RAG stage to retrieve semantically similar reference cases, so AI decisions can use more relevant local context.

MVP:
Processor RAG retrieval uses local vector embeddings and cosine similarity to rank knowledge cases without external network dependencies.

Scope:
- In scope: local vector retriever implementation, startup wiring, unit tests, and verification gates.
- Out of scope: external vector database integration, cloud embedding APIs, model-serving orchestration.

Acceptance Criteria:
- Processor pipeline uses vector-based RAG retriever in startup orchestration.
- Retrieval remains local-only with no external HTTP AI invocation.
- Existing pipeline ordering and output contract remain unchanged.
- Unit tests cover vector retriever validation and ranking behavior.
- Build, tests, and formatting checks pass.

## Tasks

- [x] Create feature branch for Q-017.
- [x] Implement local vector-based RAG retriever with deterministic similarity scoring.
- [x] Wire processor startup orchestration to use vector retriever.
- [x] Add unit tests for vector retriever constructor/validation/ranking behavior.
- [x] Update retrieval integration tests to validate vector retriever compatibility.
- [x] Run build/test/format verification gates.
- [x] Update PRD open-question status for Q-017.
