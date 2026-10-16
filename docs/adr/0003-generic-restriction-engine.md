# ADR-0003: Restriction/exception management is generic

- Status: Accepted for MVP
- Context: A patient's particular condition must not determine core application shape.
- Decision: Policies tied to movement patterns/load/equipment/position/body region, with provenance, priority, review status and effective dates. Most restrictive rule wins; unknown/conflicting cases require review. Delayed symptoms can suspend progression.
- Alternatives: hardcoded diagnosis-based switch statements; per-exercise boolean `is_safe`; generative safety decisions.
- Consequences: more domain modeling and audit overhead; extensibility and ability to test safe conservative behavior across varied conditions.
- Validation: rule conflict, shoulder example, axial load example, leg-press pain example, delayed symptom example, policy versioning.
