# Work Item: Batch invoice download to Folder A

## Spec

Business Goal:
Allow operators to complete the downloader-side flow by generating customer invoice files after successful import and MOF authentication.

User Story:
As an operator, I need to run batch invoice download for imported customers and produce raw Excel files in Folder A, so the AI Processor can consume them in the next stage.

MVP:
Invoice Downloader executes a batch download action for imported customers, writes one raw invoice Excel per customer into Folder A using configured naming rules, and produces execution logs.

Scope:
- In scope: batch download action, session precondition checks, Folder A path resolution, raw invoice Excel generation, execution log output, and unit tests.
- Out of scope: production MOF API integration details, AI processing, Folder B enhanced export.

Acceptance Criteria:
- Downloader requires successful MOF authentication before batch download.
- System processes imported customer queue and generates raw invoice Excel files.
- Output files are written to Folder A using configured naming rules.
- Batch execution produces a log file containing success/failure details.
- Build, tests, and formatting checks pass.

## Tasks

- [x] Create feature branch for roadmap item 3.
- [x] Implement batch invoice downloader service in shared core.
- [x] Generate per-customer raw invoice Excel files using naming rules.
- [x] Add Folder A batch log generation with success/failure details.
- [x] Wire downloader UI to trigger batch download after authentication.
- [x] Add unit tests for success path, session validation, and filename collision handling.
- [x] Run build/test/format verification gates.
