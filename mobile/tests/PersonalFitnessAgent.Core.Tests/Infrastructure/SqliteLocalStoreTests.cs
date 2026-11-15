using System.Text.Json;
using System.Text.Json.Nodes;
using PersonalFitnessAgent.Infrastructure.Sqlite;

namespace PersonalFitnessAgent.Core.Tests.Infrastructure;

public sealed class SqliteLocalStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pfa-tests", Guid.NewGuid().ToString("N"));
    private readonly SqliteLocalStore _store;

    public SqliteLocalStoreTests()
    {
        Directory.CreateDirectory(_directory);
        _store = new SqliteLocalStore(Path.Combine(_directory, "synthetic.db"));
    }

    [Fact]
    public void SavesAndReadsVersionedRecordWithOptimisticRevision()
    {
        using var payload = JsonDocument.Parse("{\"name\":\"synthetic exercise\"}");
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(1, _store.Save("exercise", id, 1, payload.RootElement, now, -180, 0));
        Assert.Throws<InvalidOperationException>(() => _store.Save("exercise", id, 1, payload.RootElement, now, -180, 0));
        Assert.Equal(id, Assert.Single(_store.ReadAll("exercise")).Id);
    }

    [Fact]
    public void OutboxReplayIsIdempotentAndAcknowledgmentIsStable()
    {
        using var payload = JsonDocument.Parse("{\"synthetic\":true}");
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        Assert.True(_store.Enqueue(id, 1, now, payload.RootElement));
        Assert.False(_store.Enqueue(id, 1, now, payload.RootElement));
        Assert.Single(_store.ReadPendingOutbox());
        _store.Acknowledge(id, now);
        _store.Acknowledge(id, now.AddMinutes(1));
        Assert.Empty(_store.ReadPendingOutbox());
    }

    [Fact]
    public void RecordAndAuditCommitAtomically()
    {
        using var payload = JsonDocument.Parse("{\"synthetic\":true}");
        var auditId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _store.AppendAudit(auditId, "synthetic.existing", "test", now, [], payload.RootElement);
        var audit = new AuditEnvelope(auditId, "synthetic.duplicate", "test", now, -180, [], payload.RootElement);

        Assert.Throws<InvalidOperationException>(() =>
            _store.Save("exercise", Guid.NewGuid(), 1, payload.RootElement, now, -180, 0, audit));
        Assert.Empty(_store.ReadAll("exercise"));
    }

    [Fact]
    public void BackupPreviewAndRestoreKeepRecordIdsAndAuditHistory()
    {
        using var payload = JsonDocument.Parse("{\"synthetic\":\"backup\"}");
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _store.Save("exercise", id, 1, payload.RootElement, now, -180, 0);
        _store.AppendAudit(Guid.NewGuid(), "synthetic.created", "test", now, [id], payload.RootElement);
        var json = _store.ExportBackup();
        var preview = _store.PreviewBackup(json);

        Assert.True(preview.IsValid);
        Assert.Equal(1, preview.Records);
        _store.DeleteAllRecords();
        _store.RestoreBackup(json);
        Assert.Equal(id, Assert.Single(_store.ReadAll("exercise")).Id);
        Assert.Contains("synthetic.created", _store.ExportBackup());
    }

    [Fact]
    public void BackupPreviewRejectsInvalidRecordOffsetAndNullRecords()
    {
        using var payload = JsonDocument.Parse("""{"synthetic":true}""");
        _store.Save("exercise", Guid.NewGuid(), 1, payload.RootElement, DateTimeOffset.UtcNow, 0, 0);
        var exported = _store.ExportBackup();

        var wrongOffset = JsonNode.Parse(exported)!;
        wrongOffset["records"]![0]!["originalOffsetMinutes"] = 900;
        Assert.False(_store.PreviewBackup(wrongOffset.ToJsonString()).IsValid);

        var nullRecord = JsonNode.Parse(exported)!;
        nullRecord["records"]![0] = null;
        Assert.False(_store.PreviewBackup(nullRecord.ToJsonString()).IsValid);
    }

    [Fact]
    public void EncryptedBackupCanBeOpenedOnlyWithTheCorrectPassphrase()
    {
        const string payload = "synthetic backup data";
        var encrypted = BackupEncryption.Encrypt(payload, "synthetic test passphrase");

        Assert.True(BackupEncryption.IsEncrypted(encrypted));
        Assert.Equal(payload, BackupEncryption.Decrypt(encrypted, "synthetic test passphrase"));
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            BackupEncryption.Decrypt(encrypted, "incorrect synthetic passphrase"));
    }

    [Fact]
    public void MigratesVersionOneSchemaAndPreservesPendingEvents()
    {
        var path = Path.Combine(_directory, "legacy-v1.db");
        var eventId = Guid.NewGuid();
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE records(kind TEXT NOT NULL,id TEXT NOT NULL,schema_version INTEGER NOT NULL,revision INTEGER NOT NULL,updated_utc TEXT NOT NULL,offset_minutes INTEGER NOT NULL,payload TEXT NOT NULL,PRIMARY KEY(kind,id));
                CREATE TABLE audit_events(id TEXT PRIMARY KEY,action TEXT NOT NULL,actor TEXT NOT NULL,occurred_utc TEXT NOT NULL,source_ids TEXT NOT NULL,detail TEXT NOT NULL);
                CREATE TABLE sync_outbox(event_id TEXT PRIMARY KEY,revision INTEGER NOT NULL,created_utc TEXT NOT NULL,status TEXT NOT NULL,acknowledged_utc TEXT NULL,payload TEXT NOT NULL);
                INSERT INTO sync_outbox(event_id,revision,created_utc,status,payload) VALUES($id,1,'2026-11-14T12:00:00.0000000+00:00','pending','{"synthetic":true}');
                PRAGMA user_version=1;
                """;
            command.Parameters.AddWithValue("$id", eventId.ToString("D"));
            command.ExecuteNonQuery();
        }

        using var store = new SqliteLocalStore(path);

        var pending = Assert.Single(store.ReadPendingOutbox());
        Assert.Equal(eventId, pending.EventId);
        Assert.Equal(0, pending.OffsetMinutes);
        Assert.True(store.PreviewBackup(store.ExportBackup()).IsValid);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
