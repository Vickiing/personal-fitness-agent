using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace PersonalFitnessAgent.Infrastructure.Sqlite;

public sealed record StoredRecord(string Kind, Guid Id, int SchemaVersion, long Revision, DateTimeOffset UpdatedAtUtc,
    int OriginalOffsetMinutes, JsonElement Payload);
public sealed record OutboxRecord(Guid EventId, long Revision, DateTimeOffset CreatedAtUtc, int OffsetMinutes, string Status,
    DateTimeOffset? AcknowledgedAtUtc, JsonElement Payload);
public sealed record AuditStoredRecord(Guid Id, string Action, string Actor, DateTimeOffset OccurredAtUtc,
    int OffsetMinutes, JsonElement SourceIds, JsonElement Detail);
public sealed record AuditEnvelope(Guid Id, string Action, string Actor, DateTimeOffset OccurredAtUtc,
    int OffsetMinutes, IReadOnlyList<Guid> SourceIds, JsonElement Detail);
public sealed record BackupPreview(int FormatVersion, int Records, int AuditEvents, int OutboxEvents, bool IsValid, string? Error);

/// <summary>SQLite local store. Payloads are versioned JSON DTOs owned by application contracts.</summary>
public sealed class SqliteLocalStore : IDisposable
{
    private const int CurrentSchemaVersion = 2;
    private readonly string _connectionString;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public SqliteLocalStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required.", nameof(databasePath));
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
        Migrate();
    }

    public long Save(string kind, Guid id, int schemaVersion, JsonElement payload, DateTimeOffset updatedAtUtc,
        int originalOffsetMinutes, long expectedRevision, AuditEnvelope? audit = null)
    {
        ValidateIdentity(kind, id, schemaVersion);
        if (updatedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Update timestamp must be UTC.", nameof(updatedAtUtc));
        if (originalOffsetMinutes is < -840 or > 840) throw new ArgumentOutOfRangeException(nameof(originalOffsetMinutes));
        if (expectedRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var nextRevision = expectedRevision + 1;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = expectedRevision == 0
            ? "INSERT INTO records(kind,id,schema_version,revision,updated_utc,offset_minutes,payload) VALUES($kind,$id,$schema,$revision,$updated,$offset,$payload)"
            : "UPDATE records SET schema_version=$schema,revision=$revision,updated_utc=$updated,offset_minutes=$offset,payload=$payload WHERE kind=$kind AND id=$id AND revision=$expected";
        command.Parameters.AddWithValue("$kind", kind.Trim());
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$schema", schemaVersion);
        command.Parameters.AddWithValue("$revision", nextRevision);
        command.Parameters.AddWithValue("$updated", updatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$offset", originalOffsetMinutes);
        command.Parameters.AddWithValue("$payload", payload.GetRawText());
        if (expectedRevision > 0) command.Parameters.AddWithValue("$expected", expectedRevision);
        try
        {
            var changed = command.ExecuteNonQuery();
            if (changed != 1) throw new InvalidOperationException("Record revision conflict or missing record.");
            if (audit is not null) InsertAudit(connection, transaction, audit);
            transaction.Commit();
            return nextRevision;
        }
        catch (SqliteException error) when (expectedRevision == 0 && error.SqliteErrorCode == 19)
        {
            transaction.Rollback();
            throw new InvalidOperationException("Record already exists; expected revision was zero.", error);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public IReadOnlyList<StoredRecord> ReadAll(string kind)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT kind,id,schema_version,revision,updated_utc,offset_minutes,payload FROM records WHERE kind=$kind ORDER BY updated_utc,id";
        command.Parameters.AddWithValue("$kind", kind);
        using var reader = command.ExecuteReader();
        var result = new List<StoredRecord>();
        while (reader.Read())
        {
            using var document = JsonDocument.Parse(reader.GetString(6));
            result.Add(new StoredRecord(reader.GetString(0), Guid.Parse(reader.GetString(1)), reader.GetInt32(2),
                reader.GetInt64(3), DateTimeOffset.Parse(reader.GetString(4)).ToUniversalTime(), reader.GetInt32(5), document.RootElement.Clone()));
        }
        return result;
    }

    public string ExportBackup()
    {
        var records = ReadAllKinds();
        var audit = ReadAudit();
        var outbox = ReadAllOutbox();
        return JsonSerializer.Serialize(new BackupDocument("personal-fitness-agent", 1, DateTimeOffset.UtcNow,
            records, audit, outbox), _jsonOptions);
    }

    public void DeleteAllRecords()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        Execute(connection, transaction, "DELETE FROM records; DELETE FROM audit_events; DELETE FROM sync_outbox;");
        transaction.Commit();
    }

    public BackupPreview PreviewBackup(string json)
    {
        try
        {
            var backup = DeserializeBackup(json);
            if (backup.Product != "personal-fitness-agent") return new(backup.FormatVersion, 0, 0, 0, false, "Arquivo de backup incompatível.");
            if (backup.FormatVersion != 1) return new(backup.FormatVersion, 0, 0, 0, false, "Versão de backup não suportada.");
            if (backup.Records is null || backup.AuditEvents is null || backup.Outbox is null)
                return new(backup.FormatVersion, 0, 0, 0, false, "O backup está incompleto.");
            if (backup.CreatedAtUtc.Offset != TimeSpan.Zero)
                return new(backup.FormatVersion, 0, 0, 0, false, "O horário de criação do backup deve estar em UTC.");
            if (backup.Records.Any(record => record is null || string.IsNullOrWhiteSpace(record.Kind) ||
                    record.Id == Guid.Empty || record.SchemaVersion < 1 || record.Revision < 1 ||
                    record.UpdatedAtUtc.Offset != TimeSpan.Zero || record.OriginalOffsetMinutes is < -840 or > 840) ||
                backup.AuditEvents.Any(item => item is null || item.Id == Guid.Empty ||
                    item.OccurredAtUtc.Offset != TimeSpan.Zero || item.OffsetMinutes is < -840 or > 840) ||
                backup.Outbox.Any(item => item is null || item.EventId == Guid.Empty || item.Revision < 1 ||
                    item.CreatedAtUtc.Offset != TimeSpan.Zero ||
                    item.AcknowledgedAtUtc is { Offset: var acknowledgedOffset } && acknowledgedOffset != TimeSpan.Zero ||
                    item.OffsetMinutes is < -840 or > 840))
                return new(backup.FormatVersion, 0, 0, 0, false, "Há registros com horário, fuso ou identidade inválidos.");
            var duplicateRecords = backup.Records.GroupBy(record => (record.Kind, record.Id)).Any(group => group.Count() > 1);
            var duplicateAudit = backup.AuditEvents.GroupBy(item => item.Id).Any(group => group.Count() > 1);
            var duplicateOutbox = backup.Outbox.GroupBy(item => item.EventId).Any(group => group.Count() > 1);
            if (duplicateRecords || duplicateAudit || duplicateOutbox) return new(backup.FormatVersion, 0, 0, 0, false, "O backup contém identificadores duplicados.");
            return new(backup.FormatVersion, backup.Records.Count, backup.AuditEvents.Count, backup.Outbox.Count, true, null);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or FormatException)
        {
            return new(0, 0, 0, 0, false, "Não foi possível ler o arquivo de backup.");
        }
    }

    public void RestoreBackup(string json)
    {
        var preview = PreviewBackup(json);
        if (!preview.IsValid) throw new InvalidDataException(preview.Error);
        var backup = DeserializeBackup(json);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        Execute(connection, transaction, "DELETE FROM records; DELETE FROM audit_events; DELETE FROM sync_outbox;");
        foreach (var item in backup.Records)
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO records(kind,id,schema_version,revision,updated_utc,offset_minutes,payload) VALUES($kind,$id,$schema,$revision,$updated,$offset,$payload)";
            command.Parameters.AddWithValue("$kind", item.Kind); command.Parameters.AddWithValue("$id", item.Id.ToString("D"));
            command.Parameters.AddWithValue("$schema", item.SchemaVersion); command.Parameters.AddWithValue("$revision", item.Revision);
            command.Parameters.AddWithValue("$updated", item.UpdatedAtUtc.ToString("O")); command.Parameters.AddWithValue("$offset", item.OriginalOffsetMinutes);
            command.Parameters.AddWithValue("$payload", item.Payload.GetRawText()); command.ExecuteNonQuery();
        }
        foreach (var item in backup.AuditEvents)
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO audit_events(id,action,actor,occurred_utc,offset_minutes,source_ids,detail) VALUES($id,$action,$actor,$occurred,$offset,$sources,$detail)";
            command.Parameters.AddWithValue("$id", item.Id.ToString("D")); command.Parameters.AddWithValue("$action", item.Action);
            command.Parameters.AddWithValue("$actor", item.Actor); command.Parameters.AddWithValue("$occurred", item.OccurredAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$offset", item.OffsetMinutes);
            command.Parameters.AddWithValue("$sources", item.SourceIds.GetRawText()); command.Parameters.AddWithValue("$detail", item.Detail.GetRawText());
            command.ExecuteNonQuery();
        }
        foreach (var item in backup.Outbox)
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO sync_outbox(event_id,revision,created_utc,offset_minutes,status,acknowledged_utc,payload) VALUES($id,$revision,$created,$offset,$status,$ack,$payload)";
            command.Parameters.AddWithValue("$id", item.EventId.ToString("D")); command.Parameters.AddWithValue("$revision", item.Revision);
            command.Parameters.AddWithValue("$created", item.CreatedAtUtc.ToString("O")); command.Parameters.AddWithValue("$status", item.Status);
            command.Parameters.AddWithValue("$offset", item.OffsetMinutes);
            command.Parameters.AddWithValue("$ack", (object?)item.AcknowledgedAtUtc?.ToString("O") ?? DBNull.Value);
            command.Parameters.AddWithValue("$payload", item.Payload.GetRawText()); command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void AppendAudit(Guid id, string action, string actor, DateTimeOffset occurredAtUtc,
        IEnumerable<Guid> sourceIds, JsonElement detail, int originalOffsetMinutes = 0)
    {
        if (id == Guid.Empty) throw new ArgumentException("Audit ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Audit action and actor are required.");
        if (occurredAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Audit timestamp must be UTC.", nameof(occurredAtUtc));
        if (originalOffsetMinutes is < -840 or > 840) throw new ArgumentOutOfRangeException(nameof(originalOffsetMinutes));
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO audit_events(id,action,actor,occurred_utc,offset_minutes,source_ids,detail) VALUES($id,$action,$actor,$occurred,$offset,$sources,$detail)";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$actor", actor);
        command.Parameters.AddWithValue("$occurred", occurredAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$offset", originalOffsetMinutes);
        command.Parameters.AddWithValue("$sources", JsonSerializer.Serialize(sourceIds.Select(x => x.ToString("D")), _jsonOptions));
        command.Parameters.AddWithValue("$detail", detail.GetRawText());
        command.ExecuteNonQuery();
    }

    private void InsertAudit(SqliteConnection connection, SqliteTransaction transaction, AuditEnvelope audit)
    {
        if (audit.Id == Guid.Empty || string.IsNullOrWhiteSpace(audit.Action) || string.IsNullOrWhiteSpace(audit.Actor))
            throw new ArgumentException("Audit ID, action and actor are required.", nameof(audit));
        if (audit.OccurredAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Audit timestamp must be UTC.", nameof(audit));
        if (audit.OffsetMinutes is < -840 or > 840) throw new ArgumentOutOfRangeException(nameof(audit));
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO audit_events(id,action,actor,occurred_utc,offset_minutes,source_ids,detail) VALUES($id,$action,$actor,$occurred,$offset,$sources,$detail)";
        command.Parameters.AddWithValue("$id", audit.Id.ToString("D")); command.Parameters.AddWithValue("$action", audit.Action);
        command.Parameters.AddWithValue("$actor", audit.Actor); command.Parameters.AddWithValue("$occurred", audit.OccurredAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$offset", audit.OffsetMinutes);
        command.Parameters.AddWithValue("$sources", JsonSerializer.Serialize(audit.SourceIds.Select(x => x.ToString("D")), _jsonOptions));
        command.Parameters.AddWithValue("$detail", audit.Detail.GetRawText());
        command.ExecuteNonQuery();
    }

    public bool Enqueue(Guid eventId, long revision, DateTimeOffset createdAtUtc, JsonElement payload, int originalOffsetMinutes = 0)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event ID is required.", nameof(eventId));
        if (revision < 1) throw new ArgumentOutOfRangeException(nameof(revision));
        if (createdAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Creation timestamp must be UTC.", nameof(createdAtUtc));
        if (originalOffsetMinutes is < -840 or > 840) throw new ArgumentOutOfRangeException(nameof(originalOffsetMinutes));
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT OR IGNORE INTO sync_outbox(event_id,revision,created_utc,offset_minutes,status,payload) VALUES($id,$revision,$created,$offset,'pending',$payload)";
        insert.Parameters.AddWithValue("$id", eventId.ToString("D"));
        insert.Parameters.AddWithValue("$revision", revision);
        insert.Parameters.AddWithValue("$created", createdAtUtc.ToString("O"));
        insert.Parameters.AddWithValue("$offset", originalOffsetMinutes);
        insert.Parameters.AddWithValue("$payload", payload.GetRawText());
        var inserted = insert.ExecuteNonQuery() == 1;
        using var verify = connection.CreateCommand();
        verify.Transaction = transaction;
        verify.CommandText = "SELECT revision,payload FROM sync_outbox WHERE event_id=$id";
        verify.Parameters.AddWithValue("$id", eventId.ToString("D"));
        using var reader = verify.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(0) != revision || reader.GetString(1) != payload.GetRawText())
            throw new InvalidOperationException("Event ID was reused with different content.");
        reader.Close();
        transaction.Commit();
        return inserted;
    }

    public IReadOnlyList<OutboxRecord> ReadPendingOutbox(int limit = 100)
    {
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT event_id,revision,created_utc,offset_minutes,status,acknowledged_utc,payload FROM sync_outbox WHERE status='pending' ORDER BY created_utc LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        var result = new List<OutboxRecord>();
        while (reader.Read())
        {
            using var document = JsonDocument.Parse(reader.GetString(6));
            result.Add(new OutboxRecord(Guid.Parse(reader.GetString(0)), reader.GetInt64(1), DateTimeOffset.Parse(reader.GetString(2)).ToUniversalTime(),
                reader.GetInt32(3), reader.GetString(4), reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5)).ToUniversalTime(), document.RootElement.Clone()));
        }
        return result;
    }

    public void Acknowledge(Guid eventId, DateTimeOffset acknowledgedAtUtc)
    {
        if (acknowledgedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Acknowledgment time must be UTC.", nameof(acknowledgedAtUtc));
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE sync_outbox SET status='acknowledged',acknowledged_utc=COALESCE(acknowledged_utc,$at) WHERE event_id=$id";
        command.Parameters.AddWithValue("$at", acknowledgedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$id", eventId.ToString("D"));
        if (command.ExecuteNonQuery() == 0) throw new KeyNotFoundException("Outbox event was not found.");
    }

    private IReadOnlyList<StoredRecord> ReadAllKinds()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT kind,id,schema_version,revision,updated_utc,offset_minutes,payload FROM records ORDER BY kind,updated_utc,id";
        using var reader = command.ExecuteReader();
        var result = new List<StoredRecord>();
        while (reader.Read())
        {
            using var document = JsonDocument.Parse(reader.GetString(6));
            result.Add(new StoredRecord(reader.GetString(0), Guid.Parse(reader.GetString(1)), reader.GetInt32(2), reader.GetInt64(3),
                DateTimeOffset.Parse(reader.GetString(4)).ToUniversalTime(), reader.GetInt32(5), document.RootElement.Clone()));
        }
        return result;
    }

    private IReadOnlyList<AuditStoredRecord> ReadAudit()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,action,actor,occurred_utc,offset_minutes,source_ids,detail FROM audit_events ORDER BY occurred_utc,id";
        using var reader = command.ExecuteReader();
        var result = new List<AuditStoredRecord>();
        while (reader.Read())
        {
            using var sources = JsonDocument.Parse(reader.GetString(5)); using var detail = JsonDocument.Parse(reader.GetString(6));
            result.Add(new AuditStoredRecord(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3)).ToUniversalTime(), reader.GetInt32(4), sources.RootElement.Clone(), detail.RootElement.Clone()));
        }
        return result;
    }

    private IReadOnlyList<OutboxRecord> ReadAllOutbox()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT event_id,revision,created_utc,offset_minutes,status,acknowledged_utc,payload FROM sync_outbox ORDER BY created_utc,event_id";
        using var reader = command.ExecuteReader();
        var result = new List<OutboxRecord>();
        while (reader.Read())
        {
            using var payload = JsonDocument.Parse(reader.GetString(6));
            result.Add(new OutboxRecord(Guid.Parse(reader.GetString(0)), reader.GetInt64(1), DateTimeOffset.Parse(reader.GetString(2)).ToUniversalTime(),
                reader.GetInt32(3), reader.GetString(4), reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5)).ToUniversalTime(), payload.RootElement.Clone()));
        }
        return result;
    }

    private BackupDocument DeserializeBackup(string json) => JsonSerializer.Deserialize<BackupDocument>(json, _jsonOptions)
        ?? throw new JsonException("Backup is empty.");

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = sql; command.ExecuteNonQuery();
    }

    private sealed record BackupDocument(string Product, int FormatVersion, DateTimeOffset CreatedAtUtc,
        IReadOnlyList<StoredRecord> Records, IReadOnlyList<AuditStoredRecord> AuditEvents, IReadOnlyList<OutboxRecord> Outbox);

    private void Migrate()
    {
        using var connection = Open();
        var version = connection.QueryScalarInt("PRAGMA user_version;");
        if (version > CurrentSchemaVersion) throw new InvalidOperationException("Database schema is newer than this application.");
        if (version == 0)
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                CREATE TABLE records(kind TEXT NOT NULL,id TEXT NOT NULL,schema_version INTEGER NOT NULL,revision INTEGER NOT NULL,
                  updated_utc TEXT NOT NULL,offset_minutes INTEGER NOT NULL,payload TEXT NOT NULL,PRIMARY KEY(kind,id));
                CREATE TABLE audit_events(id TEXT PRIMARY KEY,action TEXT NOT NULL,actor TEXT NOT NULL,occurred_utc TEXT NOT NULL,
                  source_ids TEXT NOT NULL,detail TEXT NOT NULL);
                CREATE TABLE sync_outbox(event_id TEXT PRIMARY KEY,revision INTEGER NOT NULL,created_utc TEXT NOT NULL,
                  status TEXT NOT NULL,acknowledged_utc TEXT NULL,payload TEXT NOT NULL);
                CREATE INDEX ix_records_kind_updated ON records(kind,updated_utc);
                CREATE INDEX ix_outbox_status_created ON sync_outbox(status,created_utc);
                PRAGMA user_version=1;
                """;
            command.ExecuteNonQuery();
            transaction.Commit();
            version = 1;
        }
        if (version == 1)
        {
            using var transaction = connection.BeginTransaction();
            if (!HasColumn(connection, transaction, "audit_events", "offset_minutes"))
                Execute(connection, transaction, "ALTER TABLE audit_events ADD COLUMN offset_minutes INTEGER NOT NULL DEFAULT 0;");
            if (!HasColumn(connection, transaction, "sync_outbox", "offset_minutes"))
                Execute(connection, transaction, "ALTER TABLE sync_outbox ADD COLUMN offset_minutes INTEGER NOT NULL DEFAULT 0;");
            Execute(connection, transaction, "PRAGMA user_version=2;");
            transaction.Commit();
        }
    }

    private static bool HasColumn(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"PRAGMA table_info({table});";
        using var reader = command.ExecuteReader();
        while (reader.Read()) if (StringComparer.OrdinalIgnoreCase.Equals(reader.GetString(1), column)) return true;
        return false;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static void ValidateIdentity(string kind, Guid id, int schemaVersion)
    {
        if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("Record kind is required.", nameof(kind));
        if (id == Guid.Empty) throw new ArgumentException("Record ID is required.", nameof(id));
        if (schemaVersion < 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
    }

    public void Dispose() => SqliteConnection.ClearAllPools();
}

internal static class SqliteExtensions
{
    public static int QueryScalarInt(this SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
