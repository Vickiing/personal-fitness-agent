# Instructions for implementation agents

Read README.md, docs/REQUIREMENTS.md, docs/ARCHITECTURE.md, docs/DOMAIN_MODEL.md, docs/RESTRICTION_ENGINE.md and applicable ADRs before coding.

## Non-negotiable constraints
1. Do not place real user health, location, body metrics, audio, tokens or secrets in source control, issues, fixtures or logs.
2. The core must run without LLM, Internet or Azure. Voice recording and transcription are optional adapters. The user must be able to enter data manually.
3. Do not make medical diagnoses, prescribe injury rehabilitation, autonomously increase loads against user restrictions, or claim energy estimates are measurements.
4. Safety policy is fail-closed: blocking or unknown contraindications require human review; never infer 'safe' from silence. Delayed symptoms are supported.
5. Clinical clearance and user preference are distinct concepts; report conflicts and defer to clinician recommendations.
6. All recommendations are proposals with reason, input provenance, rule version, timestamps and approval state.
7. No undisclosed cloud calls. Make provider, consent, audio retention and budget clear.
8. Prefer small vertical slices with migrations, tests, docs and clear error handling. Never introduce Kubernetes, brokers, vector DB, LLM orchestration or microservices without an accepted ADR.

## Engineering conventions
- C#/.NET and MAUI mobile, domain/application layers platform-independent.
- SQLite mobile is authoritative for captured events; sync uses UUID + revision and idempotent acknowledgment.
- All times stored UTC plus recorded local offset; units SI and culture-safe parsing. Never assume decimal separators.
- Parsing must preserve transcript, structured result, unknown fields and ambiguity status.
- Split core business rules, persistence and adapters; use contracts and dependency injection.
- Every change includes automated tests: nominal, edge, ambiguity, missing data, restriction conflict and offline failure.
- Version data contracts and migrations; do not silently change meaning of existing records.
- UI accessible and low-friction, including manual correction of voice records.
- PR body: scope, impacted docs, tests run, screenshots when UI, security considerations, work item link.

## Definition of done
Build/test passing; no real PII in repository; applicable acceptance criteria met; error/offline behavior covered; docs updated; no runtime costs introduced unnoticed; reviewer approval for rule changes.
