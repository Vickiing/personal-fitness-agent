# Roadmap and agent work packages

Issues are the canonical task tracker. Use IDs for stable dependencies.

## Milestone 0 — Foundation
PFA-001 Solution skeleton and tests -> PFA-002 SQLite schema/profile/workout CRUD -> PFA-003 manual offline UI.
PFA-004 restriction engine can be developed alongside UI after domain records exist.

## Milestone 1 — Voice capture
PFA-005 Azure Speech gateway and budget controls (after security foundations).
PFA-006 voice adapter/record UX (depends on 003 and 005).
PFA-007 deterministic Portuguese parser and confirmation workflow (depends on exercise catalog and 006).
PFA-008 STT benchmark + provider ADR (requires gateway and test audio).

## Milestone 2 — Insights
PFA-009 deterministic workout progress and recommendations (requires 002,004).
PFA-010 meals, sleep and energy analysis with uncertainty (requires 002).
PFA-011 weekly review dashboard (requires 009,010).

## Milestone 3 — Reliability
PFA-012 encrypted export/restore + optional idempotent sync (requires 003).
PFA-013 end-to-end tests, accessibility, security and release hardening (requires all relevant stories).

## Planning
Ship a usable offline manual logging vertical slice before any cloud features. Each issue requires acceptance scenarios, unit/integration tests and docs updates. No implementation should expose real personal health data.
