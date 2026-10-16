# Requirements and acceptance criteria

Identifiers are stable; treat these as implementation contracts.

## Functional
- FR-001 Workout templates and exercise catalog can be edited and versioned; A/B alternates by most recently completed plan, not calendar day.
- FR-002 A session stores exercises, sets, weight (kg), reps, difficulty/RIR optional, duration and notes, preserving historic snapshots.
- FR-003 User can record and edit sleep, activity, meals, weight, waist and delayed symptoms. Unknown is not zero.
- FR-004 Audio capture is opt-in per recording. Transcription result is editable; original transcript plus normalized parse and confidence/ambiguity flags are retained according to user settings.
- FR-005 On-device manual logging works offline with no cloud or home server.
- FR-006 Rule-driven interpreter handles Portuguese common workout phrases using lexicon + grammar; requests clarification when exercise or number cannot be safely assigned.
- FR-007 Restriction policies are configured per user, region, movement pattern, equipment, loading/position, duration and clinical/user source. Conflicts default to requiring review.
- FR-008 Delayed symptom event may be attached to a prior session and invalidates pending load progressions until reviewed.
- FR-009 Recommendations are proposals (never silent mutations); show reasons, source inputs, uncertainty, rule version and approve/reject.
- FR-010 Weekly trends show workout adherence, exercise-by-exercise trends, sleep, symptom flags, weight/waist and sparse-data warnings.
- FR-011 Simple food catalog supports Brazilian staples, estimated portion ranges and optional measured quantities. Qualitative statements such as 'comi muito doce' must not result in fabricated calories.
- FR-012 Export data locally (JSON/CSV), import with preview, restore from backup; configurable deletion and audio retention.
- FR-013 Cloud STT provider is replaceable, obeys opt-in, maximum audio length, per-day/month quotas and cost alerts.
- FR-014 Offline sync/event replay uses immutable event ID, revision, timestamp, status and idempotent server confirmation, when server adapter is enabled.

## Non-functional
- NFR-001 Brazilian Portuguese first, localization-ready.
- NFR-002 No cloud credentials in Android app, logs, git, issue body or test snapshots.
- NFR-003 All personally identifiable health data encrypted at rest when possible; Android secure keystore for secrets, transport TLS.
- NFR-004 Core and analytics are deterministic, covered by unit tests and runnable offline.
- NFR-005 Full audit of transformations, suggestions and approvals; never edit source transcript in place.
- NFR-006 Failed transcription never prevents logging; requeue/retry explicitly controlled.
- NFR-007 Store timestamps in UTC plus original offset; metric units and local decimal separators.
- NFR-008 Graceful CPU/network constraints and non-blocking UI.
- NFR-009 Budget policy is application-enforced; Azure budget notifications alone do not stop spending.

## User acceptance scenarios
1. Airplane mode: create workout A with two sets, kill app, relaunch, records preserved.
2. Speech 'remada três séries de doze com trinta quilos': parser identifies 3 × 12 and 30kg; user edits before saving.
3. Speech 'trinta ou treze quilos': ask for explicit confirmation; never guess.
4. Unknown food portions: show 'unquantified', not zero kcal.
5. Restriction 'no axial external load': proposed loaded squat cannot be accepted automatically.
6. Symptom reported next day: pending recommendations touching implicated patterns are suspended.
7. Home server offline: no loss of records, queue sync until availability.
8. Exceeded STT monthly cap: explain limit and allow manual entry.
9. Repeated sync event: one record, one acknowledgment; no duplicates.
10. Export/restore: record IDs and audit histories remain intact.
