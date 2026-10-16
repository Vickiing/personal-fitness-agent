# Voice pipeline design and benchmark

## Goal
Hands-free-style short logging for Portuguese exercise, food, sleep and activity; not an always-listening assistant. STT is ML as a vendor capability but domain/business logic requires no LLM/ML.

## Contracts
```csharp
public interface ISpeechToTextProvider {
    Task<TranscriptResult> TranscribeAsync(AudioInput audio, CancellationToken ct);
}
public record TranscriptResult(string Text, string Provider, string Language,
    TimeSpan Latency, IReadOnlyList<TranscriptionWarning> Warnings);
public interface IUtteranceParser {
    ParseResult Parse(string transcript, IReadOnlyList<ExerciseAlias> vocabulary);
}
```
Provider adapter never exposes API secrets to the client; gateway performs authentication, server-side quota enforcement and request size checks. ParseResult contains original transcript, entity spans, numeric spans, unrecognized spans, ambiguity status and proposed structured commands. Numeric ambiguity must be user-confirmed.

## Example
'Fiz remada, três séries de doze, trinta quilos. Dormi cinco horas.'
-> exercise aliases lookup; 3 sets, 12 reps each, 30 kg; sleep 5h. If named exercise matches multiple types, ask which one. For 'comi muitos doces' store qualitative meal note, kcal = null.

## Providers to evaluate
Azure AI Speech with Portuguese language models/phrase hints versus GPT-4o Transcribe or another provider if available in the user's Azure subscription. Do not assume parity with ChatGPT proprietary voice pipeline. Benchmark *rather than selecting by marketing*.

## Benchmark plan
- 30–50 consented (preferably synthetic) recordings across quiet/traffic/gym environments and cheap handset microphones.
- Ground truth manual transcription with exact load/rep/series values and food names.
- Report WER, exact match of critical numeric fields, exercise name accuracy, ambiguity rate, p50/p95 latency, and cost per minute.
- Never train on private raw health recordings without explicit opt-in.
- Select provider via recorded ADR after benchmark.

## Failure handling
No Internet/consent/quota -> manual record. STT returns partial/noisy transcript -> review/edit. Never auto-save a guessed set. Raw audio ephemeral by default and deleted after confirm or timeout; explicit opt-in required for retained/retry audio.

## Quotas
Configurable max length (e.g. initial 120 s), max bytes, daily/monthly requests, budget envelope, manual fallback. Provider credentials rotated and stored server-side.
