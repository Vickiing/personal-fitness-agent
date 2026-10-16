# ADR-0001: Offline-first app and deterministic rule engine

- Status: Accepted for MVP
- Context: User logs infrequently, home server is not always on, and predictable low-cost recommendations are preferred.
- Decision: Persist input locally; domain parser/analytics/restrictions work without LLM or cloud. Voice STT is an optional machine-learned vendor service, not a business-logic dependency.
- Alternatives: always-online cloud backend; locally hosted Qwen with server-dependent UI; full agent autonomy.
- Consequences: manual input always works; fewer hallucination and billing risks; natural-language interpretation is narrower and ambiguity must be reviewed.
- Validation: offline kill/restart; deterministic outputs; no API calls from core unit tests.
