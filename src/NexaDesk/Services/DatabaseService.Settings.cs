using Microsoft.Data.Sqlite;
using NexaDesk.Models;

namespace NexaDesk;

public sealed partial class DatabaseService
{
    public async Task<string?> GetSettingAsync(string key)
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM settings WHERE key = $key;";
        command.Parameters.AddWithValue("$key", key);
        return await command.ExecuteScalarAsync() as string;
    }

    public Task SetSettingAsync(string key, string value) =>
        ExecuteWriteAsync(
            """
            INSERT INTO settings(key, value, updated_at)
            VALUES($key, $value, $now)
            ON CONFLICT(key) DO UPDATE SET
                value = excluded.value,
                updated_at = excluded.updated_at;
            """,
            ("$key", key),
            ("$value", value),
            ("$now", DateTimeOffset.UtcNow.ToString("O")));


    private async Task ExecuteWriteAsync(
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await _writeGate.WaitAsync();
        try
        {
            await using SqliteConnection connection = await OpenConnectionAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            foreach ((string name, object value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _writeGate.Release();
        }
    }

}
