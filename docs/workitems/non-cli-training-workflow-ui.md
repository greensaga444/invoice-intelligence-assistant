# Work Item: Q-023 non-CLI training workflow UI

## Spec

Business Goal:
Enable operations and accounting teams to execute model-training workflow in-product without command-line operations.

User Story:
As an operations user who does not use CLI tools, I need a guided UI flow to validate dataset quality, run training, generate evaluation metrics, and publish a model with approver identity.

MVP:
Invoice AI Processor provides a dedicated non-CLI training tab with four explicit steps: dataset selection, validation, training, evaluation, and publish.

Scope:
- In scope: training workflow UI in processor app, local training/evaluation/publish service, staged/published model artifacts, and unit tests for validation/training/publish.
- Out of scope: remote training service, role-based auth integration, runtime inference switching to trained model.

Acceptance Criteria:
- User can select a Table3/Table4-style `.xlsx` dataset from UI.
- Validation reports total/valid/invalid rows and row-level quality issues.
- Training writes a staged local model artifact and reports train/test split counts.
- Evaluation generates local metrics/report file without CLI.
- Publish requires approver value and produces a published model artifact.
- Build and targeted tests pass.

## Tasks

- [x] Add roadmap entry for Q-023 and initialize branch status.
- [x] Create `TrainingWorkflowService` for dataset validation, deterministic split, training, evaluation, and publish.
- [x] Add processor UI tab for non-CLI training flow with explicit step actions and status logs.
- [x] Wire startup orchestration to initialize training workflow service.
- [x] Add unit tests for validation summary, training artifact generation, publish flow, and precondition errors.
- [x] Run build/test verification gates.
