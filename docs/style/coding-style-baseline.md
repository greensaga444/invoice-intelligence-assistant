# Coding Style Baseline (Layer 1)

These rules apply to all languages.

## Tooling

- Auto-format on save is required.
- Linting runs locally and in CI.
- Static analysis or type checks run in CI.

## Quality Gates

- Lint errors block merge.
- Build and tests must pass before merge.
- New logic requires tests.

## Naming and Structure

- Use descriptive names over abbreviations.
- Keep functions small and single-purpose.
- Keep modules focused by domain responsibility.

## API and Documentation

- Public interfaces require clear docs.
- Breaking changes require migration notes.
- Add examples for non-obvious APIs.

## Safety and Maintainability

- No dead code or commented-out code blocks.
- No broad lint/type suppressions without rationale.
- Handle errors explicitly; no silent failures.
