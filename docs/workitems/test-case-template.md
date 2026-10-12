# Test Case Template (Behavior First)

Copy this block for each criterion before implementation.

```md
## Test Case: <short behavior title>

### Traceability
- PRD user flow: <link or reference>
- Acceptance criterion: <exact criterion text or ID>

### Behavior Contract
- Trigger:
- Input:
- Preconditions:
- Expected result:
- Error result:

### Test Scenarios (Given/When/Then)

1. Happy path
- Given:
- When:
- Then:

2. Validation failure
- Given:
- When:
- Then:

3. Boundary case
- Given:
- When:
- Then:

4. Idempotency or duplicate action (if relevant)
- Given:
- When:
- Then:

### Data Setup
- Test data:
- Mocks/stubs (only if necessary):

### Assertions
- Observable output assertions:
- Observable state assertions:
- Error assertions:

### Red-Green-Refactor Log
- Red: failing reason observed:
- Green: minimal implementation done:
- Refactor: structural cleanup done:

### Completion Checklist
- [ ] Tests created before implementation
- [ ] Initial test failure observed
- [ ] Tests pass after minimal code
- [ ] Refactor done with tests still passing
```

## Optional File Naming Pattern

- Unit test files: `<feature>.spec.<ext>`
- Contract/API tests: `<feature>.contract.spec.<ext>`
- End-to-end tests: `<flow>.e2e.spec.<ext>`

Keep naming consistent with your language appendix in docs/style/language-appendices.md.
