# Deterministic analytics and quality of evidence

## Workout
- Progression based on repeatable movements, range attainment, symptoms, technique self-report and recovery flags.
- 'Same exercise' comparability requires equipment, form/ROM and plan version agreement; avoid false equivalence between machines.
- Prefer double progression as an optional configurable policy; no mandatory load increase.
- Adherence = completed prescribed sessions / available planned sessions over rolling 4 weeks. Missed days do not trigger punitive resets.

## Energy expenditure
- Estimated resting metabolic rate through a documented equation based on available profile fields; store assumptions and equation version.
- Activity multiplier is a **range**, never precise daily measured calories; optional self-reported activity categories.
- Trend-based calibration only when food intake and body weight series have enough coverage. Lack of data => hold estimate and say so.
- A scale-weight change includes water and other noise. Do not attribute daily fluctuations to calorie balance.
- Never fabricate calorie quantity for qualitative comments. Foods use portion assumptions and a documented, locale-appropriate composition source when available.

## Statistics
- 7-day rolling *median* weight and 2–4 week trend if measurement coverage adequate; display individual measurements.
- Workout trends by exercise (repetitions at same load, estimated volume proxy); no strength inference without comparability.
- Recovery trend from self-reported sleep/difficulty and adherence (association, not causal claim).
- Label outputs with Provenance: measured, self-reported, derived or heuristic; show coverage and estimated bounds.
- If sample count below threshold, abstain or say insufficient data rather than pretending precision.

## Recommendations
```text
Candidate -> restriction engine -> data quality gate -> pending proposal -> explicit user decision
```
Store rule id, revision, arguments, inputs, bounds, confidence label, rationale and approval status. Alert on symptoms rather than optimizing through pain.

## Testing
Synthetic weekly logs with sparse measurements, outliers, conflicting reports, missing foods, delayed pain, plateau, canceled sessions and consecutive rest days.
