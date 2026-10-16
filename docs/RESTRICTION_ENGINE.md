# Generic restrictions and exceptions engine

## Purpose
Configurable movement-level policies independent of a specific diagnosis, injury or user. Do not encode an actual patient's clinical details in this public project.

## Model
RestrictionPolicy {
 id, profile_id, label, category, effective_from, review_on?,
 trigger: { movement_pattern?, joint_region?, equipment?, external_load?, body_position?, range_of_motion?, symptom_timing? },
 action: ALLOW_WITH_REVIEW | WARN | BLOCK,
 source: USER_REPORT | CLINICIAN | OTHER,
 status: ACTIVE | PENDING_REVIEW | RETIRED,
 reason, version
}

## Evaluation sequence
1. Resolve exercise metadata to movement patterns, load direction, positions and equipment tags.
2. Evaluate all active predicates; most restrictive action wins. Unknown metadata or conflict => review required (fail closed).
3. Consider current symptoms and post-exercise symptom history; delay-based reactions need retrospective linkage.
4. Apply contraindication checks to the **proposed** change, not just the current exercise.
5. Record decision trace: policy IDs, version, input event IDs, conflicts and timestamp.
6. User can approve ordinary non-blocked recommendations. A BLOCK cannot be bypassed through a normal recommendation approval; require explicit policy review workflow.

## Examples (synthetic—not individual medical advice)
- User reports no external axial loading -> block loaded back squat, review overhead carries.
- User reports leg-press pain -> block leg press pattern.
- Shoulder injury with overhead movement restriction -> block overhead press but allow candidate alternatives only after review.
- Symptom onset 12h after exercise -> mark related progression pending review.

## Safety invariants and tests
- Negative conditions must not be treated as positive clearance.
- Missing clinician approval must not be inferred.
- No change by STT/parser or LLM can modify a restriction policy.
- Restrictions apply to reminders, UI substitutions, workout recommendations and plan edits.
- Audit overrides and retain prior versions.
- Movement tolerability is individual; application does not generate diagnoses or substitute evaluation.
