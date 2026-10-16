# Privacy, security and cost guardrails

## Sensitive data
Exercise and symptom history, food/sleep data and audio may contain sensitive personal health information. Public repository must contain only synthetic fixtures. Do not log transcripts, keys or identifiable health records to CI or public bug reports.

## Architecture controls
- Device-first SQLite with Android-backed key management, secure credentials and optional encrypted local DB.
- TLS for any remote communication; Azure Function gateway protected by auth, abuse limits and payload caps; no Speech keys in APK.
- Voice consent per recording, explicit cloud provider disclosure, retention/deletion setting, export and user-invoked wipe.
- Store only necessary metadata. Set audio retention default to ephemeral; metadata/transcripts retention user-configurable.
- Avoid relying on home server availability. Backups encrypted with a tested restore procedure.
- Revoke/rotate leaked tokens; do not commit example real secrets, audio or health screenshots.
- If telemetry enabled, aggregate anonymous operational counts only; off by default.

## Budget
- Validate Azure credit applicability, regional service availability, service prices and current subscription quota before deployment.
- App-enforced hard limits: audio duration/bytes, per-user requests per day/month, monthly spending envelope.
- Azure budget alerts are notification mechanisms, **not guaranteed automatic hard caps**.
- Disable or degrade to manual when allowance exhausted; no accidental auto-resume of uploads.
- No always-on VMs in MVP; Azure Function/Speech on demand. Add speech vendor comparison ADR after experiment.

## Threats
Credential leakage from mobile, replayed sync, leaked recordings, weak server API, accidental public fixture, unexpected cloud costs and unsafe recommendation. Add tests for unauthorized request, request replay, rate limit, expired token and attempted policy bypass.
