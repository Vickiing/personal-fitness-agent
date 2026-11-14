# Development setup

The repository currently targets .NET 10 and Android MAUI.

## Projects

- `src/PersonalFitnessAgent.Core`: platform-independent C# core. It must not depend on MAUI, SQLite, cloud SDKs, or network access.
- `tests/PersonalFitnessAgent.Core.Tests`: automated tests for the core.
- `src/PersonalFitnessAgent.Infrastructure.Sqlite`: SQLite migrations, local records, audit events, idempotent outbox, and JSON backup/restore.
- `src/PersonalFitnessAgent.App`: Android-only MAUI interface; the database is stored in the app's private files and Android backup is disabled.
- `PersonalFitnessAgent.slnx`: solution entry point.

Build with `dotnet build PersonalFitnessAgent.slnx` and run automated tests with `dotnet test PersonalFitnessAgent.slnx`. Android builds need a JDK and Android SDK configured for .NET MAUI. The command-line tools can be installed using the [Android SDK instructions](https://developer.android.com/tools); the Android SDK license prompts must be handled by the developer.

The core preserves an explicitly UTC instant together with its original local offset. Implemented manual flows include exercise and plan versioning, exercise restriction metadata (external load, region, position, range of motion), workout sessions/sets, daily check-ins with date selection and editable measurement corrections, editable food and delayed symptom entries, generic restriction policies, weekly medians and a 28-day weight change with minimum coverage, configurable workout weekdays for adherence, a small offline Brazilian food catalog, and user-controlled JSON/CSV export. CSV includes versioned exercises and plans, session/set history, daily measurements, meals, symptoms, restrictions, and configured schedule weekdays; JSON backups additionally preserve audit and outbox records. JSON backups can optionally be AES-GCM encrypted with a passphrase. Missing restriction metadata remains unknown for records from older contract versions and requires review when a policy depends on it.

The app has no network permission and no cloud runtime calls. Voice capture, transcription, deterministic speech parsing, recommendation generation, and recommendation review are explicitly deferred to a future Azure version by the project owner. Manual offline entry remains the MVP path. Backup preview rejects invalid IDs, revisions, offsets and non-UTC timestamps before restore. The local SQLite file is protected by Android private app storage; SQLCipher is not configured. ADR-0002 remains proposed pending the provider benchmark and approval gate. Delayed symptoms are recorded and linked locally; recommendation suspension is not active while recommendation behavior is deferred. The Android app source compiled in a local MAUI-reference harness and XAML/XML files were parsed, but a full Android build and device validation require the developer JDK and Android SDK.

Manual decimal fields use the device culture's decimal separator and reject grouping separators. For example, in `pt-BR`, `12,5` is accepted while `12.5` is rejected instead of being silently interpreted as `125`.

Current local validation: `dotnet test tests/PersonalFitnessAgent.Core.Tests/PersonalFitnessAgent.Core.Tests.csproj -m:1 -v:minimal` (31 tests); app source harness build (0 warnings/errors). These checks do not replace a full Android build.
