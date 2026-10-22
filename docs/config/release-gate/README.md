# Release Gate Policy Artifacts

This directory stores versioned release-gate contracts for Q-022 model switch decisions.

## Files

- model-release-gate-policy.v1.json: metrics schema, threshold profile, evaluator states, shadow-run policy, and rollback triggers.
- release-approval-template.md: required approval and audit fields for runtime switch requests.
- CHANGELOG.md: semantic history of release-gate policy changes.

## Consumption Guidance

- Use this policy as the source of truth before switching processor runtime to a trained model.
- Release-gate evaluator output must be attached to every release approval request.
- Rollback readiness must be confirmed before production cutover.
- Every policy update must append an entry in CHANGELOG.md.
