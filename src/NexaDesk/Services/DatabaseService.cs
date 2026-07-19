using Microsoft.Data.Sqlite;
using NexaDesk.Models;

namespace NexaDesk;

public sealed partial class DatabaseService : IDisposable
{
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    private string ConnectionString =>
        $"Data Source={AppPaths.DatabasePath};Mode=ReadWriteCreate;Cache=Shared;Pooling=True";

    public async Task InitializeAsync()
    {
        AppPaths.EnsureCreated();
        await using SqliteConnection connection = await OpenConnectionAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;

            CREATE TABLE IF NOT EXISTS actions (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT NOT NULL DEFAULT '',
                kind INTEGER NOT NULL,
                target TEXT NOT NULL DEFAULT '',
                arguments TEXT NOT NULL DEFAULT '',
                category TEXT NOT NULL DEFAULT '其他',
                icon_glyph TEXT NOT NULL DEFAULT '',
                is_favorite INTEGER NOT NULL DEFAULT 0,
                usage_count INTEGER NOT NULL DEFAULT 0,
                last_used_at TEXT NULL,
                is_indexed INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_actions_name ON actions(name);
            CREATE INDEX IF NOT EXISTS idx_actions_category ON actions(category);
            CREATE INDEX IF NOT EXISTS idx_actions_usage
            ON actions(is_favorite DESC, usage_count DESC);

            CREATE TABLE IF NOT EXISTS execution_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                action_id TEXT NOT NULL,
                action_name TEXT NOT NULL,
                success INTEGER NOT NULL,
                message TEXT NOT NULL DEFAULT '',
                executed_at TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_history_executed_at
            ON execution_history(executed_at DESC);

            CREATE TABLE IF NOT EXISTS workflows (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT NOT NULL DEFAULT '',
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS workflow_steps (
                id TEXT PRIMARY KEY,
                workflow_id TEXT NOT NULL,
                action_id TEXT NOT NULL,
                position INTEGER NOT NULL,
                delay_after_ms INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(workflow_id) REFERENCES workflows(id) ON DELETE CASCADE,
                FOREIGN KEY(action_id) REFERENCES actions(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_workflow_steps
            ON workflow_steps(workflow_id, position);

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();
        await SeedAsync();
    }


    private async Task<SqliteConnection> OpenConnectionAsync()
    {
        SqliteConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync();
        return connection;
    }


    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        _writeGate.Dispose();
    }
}
