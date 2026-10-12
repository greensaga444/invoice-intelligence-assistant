# Work Item: Enhanced Excel export to Folder B

## Spec

Business Goal:
Provide an operator-ready processed workbook output so accounting teams can consume AI-assisted decisions without altering raw Folder A source files.

User Story:
As an accounting operator, I need processed invoice files exported to Folder B with all required Traditional Chinese AI columns appended, so I can review and continue downstream accounting work efficiently.

MVP:
Invoice AI Processor exports enhanced Excel files to Folder B, preserving original invoice columns while appending AI decision columns and applying configured naming rules.

Scope:
- In scope: Folder B exporter, naming-rule based filename generation, source-column preservation, Traditional Chinese AI output columns, app startup integration, and unit tests.
- Out of scope: manual knowledge write-back, external AI provider integration, localization sweep across the full UI.

Acceptance Criteria:
- Output files preserve original invoice columns and values.
- Output files append AI columns: AI_可抵稅, AI_判定原因, AI_建議科目, AI_信心度, AI_參考案例.
- Output file naming follows configured placeholders and extension rules.
- Enhanced output files are generated in Folder B.
- Build, tests, and formatting checks pass.

## Tasks

- [x] Create feature branch for roadmap item 5.
- [x] Implement Folder B enhanced exporter in shared core.
- [x] Preserve original columns and append Traditional Chinese AI columns.
- [x] Apply output naming rule placeholders and unique filename handling.
- [x] Wire AI Processor startup flow to generate Folder B outputs after pipeline completion.
- [x] Add unit tests for column preservation, naming rule execution, and Folder B file generation.
- [x] Run build/test/format verification gates.