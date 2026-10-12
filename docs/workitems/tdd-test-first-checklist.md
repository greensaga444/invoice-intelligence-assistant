# TDD Test-First Checklist

Use this checklist before writing implementation code.

## 1) Pick the Smallest Behavior Slice

- [ ] Select one user flow step from PRD.
- [ ] Select one acceptance criterion for this cycle.
- [ ] Write one sentence for expected observable behavior.

Template:

- Behavior: "When [trigger], the system should [observable result]."

## 2) Define the Contract First

- [ ] List inputs.
- [ ] List outputs.
- [ ] List error conditions.
- [ ] List boundary values.

Template:

- Inputs:
- Outputs:
- Errors:
- Boundaries:

## 3) Write Failing Tests First (Red)

- [ ] Add one happy-path test.
- [ ] Add validation failure tests.
- [ ] Add at least one boundary test.
- [ ] Add one idempotency or duplicate-action test if applicable.
- [ ] Run tests and confirm they fail for the expected reason.

Quality gate for test names:

- [ ] Name describes behavior, not implementation.
- [ ] One behavior per test.
- [ ] Assertions target observable outputs/state only.

## 4) Implement the Minimum (Green)

- [ ] Write only code required to pass current failing tests.
- [ ] Avoid refactoring during this step.
- [ ] Run tests and confirm all new tests pass.

## 5) Refactor Safely (Refactor)

- [ ] Improve structure and readability.
- [ ] Remove duplication.
- [ ] Keep behavior unchanged.
- [ ] Re-run full related test suite.

## 6) Commit Discipline

- [ ] Commit includes tests + minimal implementation for one behavior slice.
- [ ] Commit message follows: "test: add failing tests for [behavior]" then "feat: implement [behavior]" if split.
- [ ] No unrelated refactors in the same change.

## 7) Definition of Done for Each Slice

- [ ] Tests were written before implementation.
- [ ] Test failure was observed before coding.
- [ ] All tests pass locally.
- [ ] Acceptance criterion is satisfied.
- [ ] Notes added to docs/status.md if required by team workflow.

## AI Prompt Snippets for Test-First Work

For split-model execution (tests by one model, implementation by another), use:

- docs/workitems/two-model-test-impl-flow.md

Generate tests only:

"From this acceptance criterion, generate failing tests only. Do not implement production code. Include happy path, validation errors, boundary cases, and one idempotency case when relevant."

Review generated tests:

"Review these tests for coupling to implementation details. Rewrite them to assert only observable behavior and keep one behavior per test."
