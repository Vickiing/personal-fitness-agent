# Architecture - MVP v0.1

> Scope: voice capture, transcription, speech parsing, recommendation generation, and recommendation review are deferred to a future Azure version. The data-flow and cloud modules below describe planned architecture, not features in this offline runtime.

## Context
```mermaid
flowchart TD
 U[Android user] --> M[MAUI app]
 M --> DB[(SQLite on device)]
 M --> STT[Authenticated Azure Function]
 STT --> AZ[Azure Speech provider]
 M -. optional sync .-> H[Home API when online]
 H --> S[(Server SQLite / backup)]
 M --> RULE[Pure C# deterministic core]
 RULE --> DB
```

## Logical modules
- Implemented foundation: `src/PersonalFitnessAgent.Core` is a platform-independent .NET 10 library; `tests/PersonalFitnessAgent.Core.Tests` contains automated tests. The initial core contract preserves event instants in UTC with the original local offset.
- Implemented offline vertical slice: `src/PersonalFitnessAgent.Infrastructure.Sqlite` owns versioned SQLite migrations, optimistic revisions, audit and outbox records, and JSON backup/restore; `src/PersonalFitnessAgent.App` is the Android MAUI client with local-only manual workout, check-in, restriction, progress, and read-only imported A/B workout-sheet viewer flows.
- Mobile (implemented): Today, Workout, Check-in, Restrictions, Progress, export/restore. Voice, recommendation generation/review and settings are future scope.
- Core.Domain: Exercise, Session, Set, Food, Symptom, RestrictionPolicy, Recommendation, AuditEvent.
- Core.Application (implemented subset): validation and weekly analytics. Deterministic speech parsing and progression recommendation generation are future scope.
- Analytics (implemented subset): weekly medians, sparse-data warnings, exercise trends and 28-day weight comparison. Energy estimates and progression recommendations are future scope.
- Mobile persistence (implemented): SQLite records, migrations, audit, local outbox and backup/restore. Secure storage, voice capture and remote sync adapters are future scope.
- Workout-sheet viewer (implemented): user-selected XLSX files are parsed locally, normalized, and stored as a versioned SQLite record; no workbook content is sent to a server.
- Speech.Contracts: normalized transcription envelope and provider interface.
- Cloud.SpeechProxy: authenticated Azure Function; checks consent token, size, quotas and provider credentials; returns transcript with metadata.
- Infrastructure.Server (optional): sync API, idempotent event ledger and encrypted backup. Not required for MVP launch.

## Data flow
1. User taps record and sees provider/privacy disclosure. Device captures bounded audio only after consent.
2. With Internet and remaining quota, app obtains authenticated short-lived request via proxy. Proxy delegates STT, returns text, does not keep raw audio by default.
3. If unavailable or declined, user types or saves pending audio locally with explicit retention consent. No automatic transmission later without consent.
4. Pure parser extracts entities, preservation of original transcript, ambiguity flags; user validates numbers before commit.
5. Local database commits activity event transactionally. Analytics/restriction policies can run without cloud.
6. Weekly analyzer emits recommendations in pending state, requiring user approval.

## Dependency rules
Mobile -> Application/Domain; Adapters -> Contracts; Analytics -> Domain. Domain cannot reference MAUI, Azure SDK, or SQLite. Never let a speech provider mutate workout plans.

## Persistence
Source events append-only with UUID, schema_version, recorded_at_utc, offset, captured_by (manual/voice/import), status and user override fields. Projections are replaceable; exercise plan version snapshots are immutable per workout.

## Security
Serverless proxy is authenticated and rate limited; don't embed Azure keys in APK. Avoid public API and public health logs; build testing with synthetic personas. Offline secrets use Android Keystore. Data exports should be user-controlled, with encryption option.

## Deployment
No 24/7 VM. Local Android primary. Azure Function consumption plan *subject to provider availability, billing eligibility and observed limits*. Optional domestic server can wake/sync when available.

## Performance and reliability
Use short recordings, streaming or chunked uploads as supported; degrade to manual input; implement exponential backoff only for transient server failures with explicit authorization for re-send. No energy estimation treated as precise measurement.
