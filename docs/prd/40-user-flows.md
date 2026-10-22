# User Flows

## UF-001 Launcher Full Flow Coordination

**Preconditions**

- Packaged module executables are available.
- `config/downloader.ini` and `config/processor.ini` are present.

**Main flow**

1. User opens launcher.
2. Launcher validates module readiness and displays statuses.
3. User clicks "執行完整流程".
4. Launcher starts Downloader and waits for exit.
5. Launcher prompts user whether to continue to Processor.
6. If confirmed, launcher starts Processor and waits for exit.
7. Launcher writes execution logs in the UI.

**Alternate flows**

- Missing executable/config: launcher blocks operation and shows warning dialog.
- User declines Processor continuation: full flow ends after Downloader.

**Postconditions**

- Downloader has completed.
- Processor has either completed or been explicitly skipped by user.

---

## UF-002 Settings Update from Launcher

**Preconditions**

- Launcher is running.

**Main flow**

1. User opens settings dialog.
2. User updates folder paths, naming rules, suffixes, review threshold, MOF fields, and knowledge path.
3. System validates required fields and threshold format.
4. System encrypts MOF password and writes `downloader.ini`.
5. System writes processor-linked settings into `processor.ini`.
6. System confirms save and instructs restart of already running modules.

**Alternate flow**

- Validation failure: save is rejected with field-level message.

**Postconditions**

- Both INI files are synchronized with the submitted settings.

---

## UF-003 Customer List Import

**Preconditions**

- Downloader configuration is loaded.
- User has an `.xlsx` customer list file.

**Main flow**

1. User selects customer list file.
2. Downloader validates required columns by aliases (`Tax ID/統編`, `Company Name/公司名稱`, `Date Range/日期區間`).
3. Downloader validates row values and parses date ranges.
4. Valid records are loaded into grid and queued for batch flow.

**Alternate flow**

- Missing/invalid schema or row data: import fails with explicit message.

**Postconditions**

- In-memory customer list is available for authentication/download steps.

---

## UF-004 Downloader Authentication and Session Gate

**Preconditions**

- Customer list is already imported.
- MOF credentials are configured.

**Main flow**

1. User enters password (or uses decrypted configured password).
2. Downloader validates credentials via local authenticator.
3. On success, downloader stores session token + expiry and enables batch download.

**Alternate flow**

- Invalid account/key/password: authentication fails and batch button remains disabled.

**Postconditions**

- Active session state is available for batch download within validity window.

---

## UF-005 Official Batch Download to Folder A

**Preconditions**

- Active session exists.
- At least one customer is loaded.

**Main flow**

1. Downloader builds per-customer official query requests.
2. Batch downloader executes enabled invoice/allowance scopes with retry handling.
3. Records are deduplicated and exported as official workbook files.
4. One log file summarizes customer-level success/failure and output paths.

**Postconditions**

- Folder A contains generated workbook files and batch log.

---

## UF-006 Processor Decision Pipeline

**Preconditions**

- Folder A has supported workbook files.

**Main flow**

1. Processor reads records from Folder A (legacy or official schema).
2. For each record, processor attempts resolution in order:
   1. Rule engine
   2. Case cache
   3. Vector retriever + local analyzer
3. Processor records decision source and confidence.
4. Processor reports progress and summary counts in UI.

**Postconditions**

- A complete decision set is produced for all parsed records.

---

## UF-007 Review Partition and Folder B/C Export

**Preconditions**

- Decision results are available.

**Main flow**

1. Processor groups results by source-file base name (with invoice/allowance suffix normalization).
2. If any record in a group is below confidence threshold, entire group routes to Folder C.
3. Otherwise group routes to Folder B.
4. Exporter writes typed Table-3/Table-4 workbooks with configured naming and collision-safe output paths.

**Postconditions**

- Folder B contains ready files; Folder C contains manual-review groups.

---

## UF-008 Manual Knowledge Feedback

**Preconditions**

- Trainer has decision rows loaded.
- Knowledge repository service is initialized.

**Main flow**

1. User checks approved rows in "手動知識回饋" tab.
2. User clicks "更新知識庫".
3. Service writes non-duplicate approved decisions into local JSON repository.
4. UI reports added/skipped counts and repository path.

**Postconditions**

- Approved knowledge becomes available for future retrieval.

---

## UF-009 Non-CLI Training Lifecycle

**Preconditions**

- Governance policy manifest is approved and valid.
- User has a training dataset `.xlsx`.

**Main flow**

1. User selects dataset file.
2. User runs validation to check schema, vocabulary, and row quality.
3. User runs training to write staged model.
4. User runs evaluation to write evaluation metrics report.
5. User runs publish to write published model and publish metadata.

**Postconditions**

- Training artifacts exist under `build/training/`.

---

## UF-010 Governance Quality-Gate Reporting

**Preconditions**

- Dataset validation has executed.

**Main flow**

1. Service computes duplicate/conflict and split metrics.
2. Service writes:
   - `validation-summary.json`
   - `reject-report.json`
   - `conflict-report.json`
3. UI surfaces validation summary and issue preview.

**Postconditions**

- Governance evidence artifacts are available for audit/review.
