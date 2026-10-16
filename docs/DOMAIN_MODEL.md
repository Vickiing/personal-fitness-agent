# Domain model

## Aggregates
- Profile (local user key, preferences, consent, locale, units; **no hardcoded personal conditions**).
- WorkoutPlan (version, templates, exercise prescriptions and schedule rules).
- WorkoutSession (immutable plan snapshot, set events, started/completed timestamps).
- HealthContext (restrictions, symptom observations, review status and evidence source).
- DailyLog (food, sleep, activity, body measurements; incomplete entries permitted).
- Recommendation (rule ID/version, source references, assumptions, confidence/quality, status pending/approved/rejected).
- VoiceRecord (audio optional, transcript, parsed fields, uncertainties, transcription provider/latency; retention policy).
- SyncEnvelope (event ID, source revision, delivery status, remote acknowledgment).

## Values and invariants
- WeightKg >= 0; Repetitions >= 0; time >= 0. Invalid input is rejected, not silently coerced.
- Numeric quantity has Unit, SourceType = measured|self_reported|estimated, optional Range and ConfidenceDescriptor.
- Absence of a symptom record does NOT imply absence of symptoms.
- ExerciseSet points to recorded exercise/version; future catalog edits must not rewrite history.
- RestrictionPolicy has scope, predicate, action, priority, source and optional expiration/review date.
- Recommendation never writes source records and must be explicitly approved before plan changes.
- TimeOffset is persisted alongside UTC for sessions and meals crossing midnight.

## Proposed records
```text
Exercise(Id, Name, MovementPatterns[], Equipment[], ContraindicationTags[])
WorkoutPlan(Id, Version, Label, Exercises[])
WorkoutSession(Id, PlanId, PlanVersion, StartedUtc, Offset, Status)
ExerciseSet(Id, SessionId, ExerciseId, SetIndex, LoadKg?, Reps?, Rir?, Notes)
DailyLog(Id, DateLocal, SleepHours?, ActivityLevel?, FoodEntries[], Symptoms[])
SymptomEvent(Id, RecordedUtc, OnsetUtc?, Region, Severity?, Description, RelatedSessionId?)
RestrictionPolicy(Id, ProfileId, Scope, Predicate, Action, Source, ReviewStatus, Version)
VoiceRecord(Id, CapturedUtc, Transcript, ParsedJson, ConfirmedJson?, Provider, ReviewStatus)
Recommendation(Id, RuleVersion, SourceEventIds[], Rationale, Quality, ProposedChange, Decision)
SyncEvent(Id, Revision, PayloadSchema, CreatedUtc, AckUtc?, Status)
```
