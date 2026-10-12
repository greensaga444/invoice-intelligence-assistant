# Two-Model TDD Flow (Tests by Model A, Code by Model B)

Use this when you want one model to generate tests and another to implement code.

## Goal

- Keep responsibilities separate.
- Prevent implementation model from rewriting test intent.
- Ensure traceable Red-Green-Refactor loop.

## Roles

- Model A (Test Generator): writes test cases and test code only.
- Model B (Implementer): writes production code only.
- Human Reviewer (you): approves handoff artifacts and gate outcomes.

## Artifacts Per Slice

1. Behavior Card
- User flow ID
- Acceptance criterion ID/text
- One-sentence behavior statement

2. Test Design Pack (from Model A)
- Scenario list (happy, validation, boundary, idempotency when relevant)
- Given/When/Then per scenario
- Expected assertions

3. Failing Evidence
- Test run output showing expected failures before implementation

4. Implementation Brief (input for Model B)
- What behavior to satisfy
- Which tests are authoritative
- Constraints: do not edit tests unless defect approved

5. Passing Evidence
- Test run output after implementation

## Working Agreement (Critical)

- Tests are the executable contract.
- Model B cannot weaken assertions to pass.
- If tests look wrong, Model B must raise a test-defect note instead of changing behavior silently.
- Any test change after handoff requires explicit approval and rationale.

## End-to-End Flow

1. Slice Selection
- Pick one acceptance criterion only.
- Create a Behavior Card.

2. Test Generation (Model A)
- Generate test scenarios first.
- Generate failing test code.
- Include a traceability comment or metadata tying each test to criterion ID.

3. Red Gate
- Run tests.
- Confirm new tests fail for expected reasons.
- Save failing evidence.

4. Freeze Gate
- Mark test files as locked for this slice.
- Create Implementation Brief for Model B.

5. Implementation (Model B)
- Implement minimum code to satisfy locked tests.
- Do not refactor beyond touched behavior.

6. Green Gate
- Run targeted suite then broader related suite.
- Confirm all locked tests pass.

7. Refactor Gate
- Optional cleanup with no behavior changes.
- Re-run same suites.

8. Close Slice
- Record result in status/workitem tracking.
- Move to next criterion.

## Prompt Pack

### Prompt for Model A (Tests Only)

"You are Test Generator Model A. From this acceptance criterion, produce tests only. Do not write production code.

Inputs:
- User flow: <UF-ID and description>
- Criterion: <AC-ID and exact text>
- Language/framework: <stack>

Output requirements:
1) Scenario table with: ID, type (happy/validation/boundary/idempotency), Given/When/Then.
2) Test file content with clear names, one behavior per test, observable assertions only.
3) Traceability: each test references <AC-ID>.
4) Include expected failure reason summary before implementation.

Constraints:
- Black-box behavior focus.
- No implementation details.
- Deterministic test data."

### Prompt for Model B (Implementation Only)

"You are Implementer Model B. Implement production code to satisfy locked tests.

Inputs:
- Locked test files: <list>
- Behavior Card: <content>
- Constraints: tests are authoritative; do not modify tests unless test defect is explicitly approved.

Output requirements:
1) Minimal production code changes only.
2) Short mapping of code change to each failing test fixed.
3) Risk notes for any ambiguity.

Do not:
- Edit test assertions.
- Introduce unrelated refactors."

## Gate Checklist

### Red Gate (before Model B)
- [ ] Tests exist for happy, validation, and boundary.
- [ ] Tests fail for expected reason.
- [ ] Failures are reproducible.

### Green Gate (after Model B)
- [ ] All locked tests pass.
- [ ] No test weakening.
- [ ] Acceptance criterion satisfied.

### Refactor Gate
- [ ] No behavior delta.
- [ ] Related suites still pass.

## Triage Rules When Flow Breaks

1. Failing tests are unclear
- Action: send back to Model A for clearer assertions and scenario wording.

2. Passing tests but behavior is wrong
- Action: add or tighten black-box assertions in Model A test pack.

3. Model B edits tests
- Action: reject change and rerun with locked-test constraint.

4. Frequent ambiguity loops
- Action: improve Behavior Card quality and add explicit examples/non-examples.

## Minimum Quality Bar Per Slice

- At least 3 tests: happy + validation + boundary.
- Deterministic inputs and expected outputs.
- One acceptance criterion per slice.
- Evidence captured for both Red and Green.
