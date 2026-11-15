# Development setup

The repository targets .NET 10. Application code is grouped by client platform.

## Projects

- `mobile/src/PersonalFitnessAgent.Core`: platform-independent C# core. It must not depend on MAUI, SQLite, cloud SDKs, or network access.
- `mobile/tests/PersonalFitnessAgent.Core.Tests`: automated tests for the core.
- `mobile/src/PersonalFitnessAgent.Infrastructure.Sqlite`: SQLite migrations, local records, audit events, idempotent outbox, and JSON backup/restore.
- `mobile/src/PersonalFitnessAgent.App`: Android-only MAUI interface; the database is stored in the app's private files and Android backup is disabled.
- `mobile/PersonalFitnessAgent.slnx`: mobile solution entry point.
- `web/`: reserved for the future web client. No web project or Azure integration has been introduced yet.

Build with `dotnet build mobile/PersonalFitnessAgent.slnx` and run automated tests with `dotnet test mobile/PersonalFitnessAgent.slnx`. Android builds need a JDK and Android SDK configured for .NET MAUI. The command-line tools can be installed using the [Android SDK instructions](https://developer.android.com/tools); the Android SDK license prompts must be handled by the developer.
