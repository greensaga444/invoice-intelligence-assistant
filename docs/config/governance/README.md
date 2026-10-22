# Governance Policy Artifacts

This directory stores versioned governance contracts for Q-016 and Q-021.

## Files

- table3-table4-governance-policy.v1.json: canonical mapping, dedup/conflict, quality gates, and split policy.
- CHANGELOG.md: semantic version history for governance artifacts.

## Consumption Guidance

- Treat policy JSON as the source of truth for implementation slices GOV-IMP-02 and beyond.
- Runtime training workflow must refuse unapproved or invalid policy manifests with actionable errors.
- Every policy update must append an entry in CHANGELOG.md.
