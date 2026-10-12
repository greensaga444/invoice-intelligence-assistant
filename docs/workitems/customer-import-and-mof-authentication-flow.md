# Work Item: Customer import and MOF authentication flow

## Spec

Business Goal:
Enable accounting users to load customer batch inputs and establish a validated MOF session before download execution.

User Story:
As an operator, I need to import customer Excel data and run MOF authentication with clear validation feedback so I can safely prepare invoice downloads.

MVP:
Invoice Downloader can import required customer columns from .xlsx, validate key data, and run MOF authentication based on configured account/API key plus runtime password input.

Scope:
- In scope: customer Excel file selection/import, required-column checks, row-level data validation, MOF authentication flow, clear UI messages, and unit tests.
- Out of scope: actual batch invoice download API execution and Folder A export.

Acceptance Criteria:
- Downloader imports .xlsx customer list and reads Tax ID, Company Name, and Date Range.
- Import fails with clear validation error when required columns are missing or row data is invalid.
- MOF authentication succeeds when valid credentials are provided.
- MOF authentication fails with actionable messages for placeholder/missing credentials.
- Build, tests, and formatting checks pass.

## Tasks

- [x] Create feature branch for roadmap item 2.
- [x] Implement customer list importer service for .xlsx input.
- [x] Add required-column and row-level validations (Tax ID, Company Name, Date Range).
- [x] Implement MOF authenticator service with deterministic success/failure outcomes.
- [x] Wire downloader UI for file selection, import action, and authentication action.
- [x] Add tests for importer success/failure and authenticator success/failure.
- [x] Run build/test/format verification gates.
