# ADR-0004: Modular monolith rather than microservices

- Status: Accepted for MVP
- Context: Personal project maintained by coding agents; very low event volume; intermittent server.
- Decision: Android .NET MAUI app with local SQLite, separate pure C# domain/analytics libraries; optional authenticated serverless Speech gateway and optional backup/sync API. No always-on cloud VM, message broker, distributed events, or local LLM.
- Alternatives: cloud-first microservices; Kubernetes; always-on home backend.
- Consequences: simpler deployment/tests and lower cost; future scaling would require boundaries and migrations, for which explicit module contracts are retained.
- Validation: offline build, no cloud dependencies in domain, provider adapter tests, low-cost Azure deployment only after permission/budget check.
