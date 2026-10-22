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
