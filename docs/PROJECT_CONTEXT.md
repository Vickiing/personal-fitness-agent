# Project context and product brief

## Problem
People with inconsistent availability need to record exercise, food, sleep, activity and symptoms with minimal interaction, monitor progress over time and receive conservative, explainable suggestions. Traditional trackers often require excessive typing and rigid programming.

## Target and boundaries
Personal, Android-first project; initial one-user implementation but configurable restrictions and profiles. 3 weekly workouts is an illustrative schedule, **not** hardcoded. Support A/B plans and varied user-configured schedules. Essential logs offline. Natural spoken Portuguese as primary interaction; manual input always available. Server at home may be powered off; it cannot be a prerequisite for tracking.

## MVP outcomes
- User creates/edits exercise catalog, plan A/B, sets and session history.
- Voice-to-text on demand with preflight consent and editable result; deterministic extraction of common phrases and numbers; ambiguous phrases trigger confirmation.
- Check-ins: sleep, subjective energy, food, symptoms and body measurements.
- Explainable deterministic progress, weekly summaries, confidence/coverage for energy intake/expenditure estimates.
- Generic user-configured restriction/exception engine with delayed symptom logging.
- Consent, privacy controls, export/backup, cloud limits and offline resilience.

## Out of scope
Autonomous medical or dietary prescribing, LLM agent, clinical inference, passive microphone, wearables, smart calorie measurement, image-based meal estimation, social features, public accounts, paid subscriptions and 24/7 server dependencies.

## Decisions pending
Speech provider choice via benchmark; serverless billing/region/credit validation; precise exercise progression policies reviewed with qualified professional; whether sync beyond local backups is needed for v0.1.

## Success metrics
At least 95% of normal short records persisted when offline; all restrictions block disallowed automatic load increases; every numerical voice field is visible before confirmation; records/export recover after device restart; 30–50 sample transcript benchmark with numeric-field accuracy and latency; weekly insights show data coverage and provenance.
