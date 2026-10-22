# Open Questions

Only active unresolved items are listed here. Resolved items were moved into the owning PRD sections.

## Q-001 Real MOF Authentication Contract

**Question**  
Current downloader authentication uses local credential-format checks and local session token generation. Should production mode require a real upstream-auth/session contract before API query execution?

**Why still open**  
Code currently validates credentials locally (`LocalMofAuthenticator`) while official API calls rely on account/app/password payload in request body.

**Owner**  
Backend Integration Owner

**Due date**  
2026-11-15

---

## Q-002 Release-Gate Enforcement in Publish Path

**Question**  
Should model publish be blocked unless `TrainingModelReleaseGateEvaluator` result is `Pass` (or `Warn` with explicit override)?

**Why still open**  
Release-gate evaluator exists in core but is not wired into `TrainingWorkflowService.Publish`.

**Owner**  
ML Governance Owner

**Due date**  
2026-11-22

---

## Q-003 Production Dataset Certification Procedure

**Question**  
What is the mandatory certification workflow for customer-provided production datasets beyond synthetic test evidence?

**Why still open**  
Repository has synthetic and production-like validation evidence (`Q-013`) but no codified approval process for customer-specific dataset sign-off.

**Owner**  
QA Lead

**Due date**  
2026-11-29

---

## Q-004 Local LLM Runtime Implementation Decision

**Question**  
For the processor stage `Rule Engine -> Case Cache -> RAG -> Local LLM`, should "Local LLM" remain the current heuristic analyzer implementation, or be replaced by an actual local model runtime (for example Ollama + Qwen or Llama)?

**Why still open**  
Current code path uses `LocalHeuristicLlmAnalyzer` (heuristic rules) and has no model/runtime configuration in `processor.ini`, while architecture docs mention Ollama + Qwen3 8B as target runtime.

**Owner**  
AI/ML Tech Lead

**Due date**  
2026-11-29

---

## Q-005 Customer Rule Template Completion Follow-ups

**Question**  
After customer completes `docs/policy/table3-table4-generation-template.md`, what mandatory implementation/validation tasks must be executed before sign-off?

**Why still open**  
Current repository has the customer template, but no locked execution checklist covering rule-gap review, PRD freeze, implementation mapping, code/test rollout, UAT evidence, and sign-off baseline.

**Required follow-up tasks (to confirm and assign)**
- Rule gap review: verify all filled items are unambiguous; list conflicts and missing decisions.
- PRD freeze: update functional requirements and close/refresh related open questions.
- Implementation mapping: build per-column mapping table for 表三/表四 (source, transform, fallback, validation).
- Code updates: implement confirmed mapping/rules in processor/export path.
- Validation rules: add required input validation and explicit error handling for invalid combinations.
- Tests: add unit tests and workbook golden-output tests; run regression.
- UAT evidence package: prepare sample input/output and rule-trace evidence for customer review.
- Sign-off and baseline: record customer approval and create a versioned baseline for change control.

**Owner**  
Product + Engineering Leads

**Due date**  
2026-12-06
