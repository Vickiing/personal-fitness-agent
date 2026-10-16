# ADR-0002: Cloud STT via swappable provider gateway

- Status: Proposed (benchmark pending)
- Context: High-quality Brazilian Portuguese and gym terminology; server has 16GB without GPU and is not continuously available; cloud credits exist but pricing/availability require validation.
- Decision: ISpeechToTextProvider behind an authenticated quota-enforcing Azure Function. Evaluate Azure AI Speech and available GPT transcription service using 30–50 test clips. Do not claim proprietary ChatGPT pipeline equivalence.
- Alternatives: Android offline speech; Whisper CPU; Qwen (not STT).
- Consequences: Internet/consent needed for cloud transcription, extra privacy obligations and consumption costs; manual entry remains fallback.
- Approval gate: accept chosen vendor only after numeric accuracy/latency/billing/privacy benchmark and cost ceiling validated.
