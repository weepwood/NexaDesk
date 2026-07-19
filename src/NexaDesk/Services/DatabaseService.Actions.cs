using Microsoft.Data.Sqlite;
using NexaDesk.Models;

namespace NexaDesk;

public sealed partial class DatabaseService
{
    public async Task<IReadOnlyList<ActionDefinition>> SearchActionsAsync(
        string? query,
        int limit = 60)
    {
        string normalized = query?.Trim() ?? string.Empty;
        string like = $"%{normalized}%";

        await using SqliteConnection connection = await OpenConnectionAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, name, description, kind, target, arguments, category,
                   icon_glyph, is_favorite, usage_count, last_used_at
            FROM actions
            WHERE $query = ''
               OR name LIKE $like
               OR description LIKE $like
               OR category LIKE $like
            ORDER BY
                is_favorite DESC,
                CASE WHEN name = $query THEN 0
                     WHEN name LIKE $prefix THEN 1
                     ELSE 2 END,
                usage_count DESC,
                COALESCE(last_used_at, '') DESC,
                name COLLATE NOCASE
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$query", normalized);
        command.Parameters.AddWithValue("$like", like);
        command.Parameters.AddWithValue("$prefix", normalized + "%");
        command.Parameters.AddWithValue("$limit", limit);

        List<ActionDefinition> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadAction(reader));
        }

        return items;
    }

    public async Task<IReadOnlyList<ActionDefinition>> GetFavoriteActionsAsync(int limit = 8)
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, name, description, kind, target, arguments, category,
                   icon_glyph, is_favorite, usage_count, last_used_at
            FROM actions
            WHERE is_favorite = 1
            ORDER BY usage_count DESC, name COLLATE NOCASE
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        List<ActionDefinition> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadAction(reader));
        }

        return items;
    }

    public async Task<ActionDefinition?> GetActionAsync(string id)
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, name, description, kind, target, arguments, category,
                   icon_glyph, is_favorite, usage_count, last_used_at
            FROM actions
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadAction(reader) : null;
    }

    public Task SetFavoriteAsync(string id, bool isFavorite) =>
        ExecuteWriteAsync(
            """
            UPDATE actions
            SET is_favorite = $favorite, updated_at = $now
            WHERE id = $id;
            """,
            ("$favorite", isFavorite ? 1 : 0),
            ("$now", DateTimeOffset.UtcNow.ToString("O")),
            ("$id", id));


    public async Task UpsertIndexedActionsAsync(IEnumerable<ActionDefinition> actions)
    {
        ActionDefinition[] snapshot = actions.ToArray();
        if (snapshot.Length == 0)
        {
            return;
        }

        await UpsertActionsAsync(snapshot, isIndexed: true);
    }

    private async Task SeedAsync()
    {
        ActionDefinition[] actions =
        [
            BuiltIn("system.settings", "打开 Windows 设置", "打开系统设置主页。", ActionKind.LaunchUri, "ms-settings:", "系统", "\uE713"),
            BuiltIn("system.display", "打开显示设置", "调整缩放、分辨率与显示器。", ActionKind.LaunchUri, "ms-settings:display", "系统", "\uE7F4"),
            BuiltIn("system.explorer", "打开文件资源管理器", "打开资源管理器主页。", ActionKind.LaunchFile, "explorer.exe", "系统", "\uED25"),
            BuiltIn("system.terminal", "打开 Windows Terminal", "启动 Windows Terminal。", ActionKind.LaunchFile, "wt.exe", "开发", "\uE756"),
            BuiltIn("system.calculator", "打开计算器", "启动 Windows 计算器。", ActionKind.LaunchFile, "calc.exe", "系统", "\uE8EF"),
            BuiltIn("system.snipping", "开始屏幕截图", "打开 Windows 截图界面。", ActionKind.LaunchUri, "ms-screenclip:", "系统", "\uE722"),
            BuiltIn("system.taskmanager", "打开任务管理器", "查看进程和系统资源。", ActionKind.LaunchFile, "taskmgr.exe", "系统", "\uE9D9"),
            BuiltIn("window.topmost", "切换当前窗口置顶", "将前台窗口设为置顶或取消置顶。", ActionKind.WindowTopMost, "", "窗口", "\uE718"),
            BuiltIn("window.center", "将当前窗口居中", "把前台窗口移到当前显示器中央。", ActionKind.WindowCenter, "", "窗口", "\uE8A7"),
            BuiltIn("system.lock", "锁定电脑", "立即锁定当前 Windows 会话。", ActionKind.LockWorkstation, "", "系统", "\uE72E"),
            BuiltIn("web.github.nexadesk", "打开 NexaDesk 仓库", "在默认浏览器中打开项目仓库。", ActionKind.LaunchUri, "https://github.com/weepwood/NexaDesk", "网页", "\uE774")
        ];

        await UpsertActionsAsync(actions, isIndexed: false);
        await SeedWorkflowAsync();
    }

    private async Task UpsertActionsAsync(
        IReadOnlyList<ActionDefinition> actions,
        bool isIndexed)
    {
        await _writeGate.WaitAsync();
        try
        {
            await using SqliteConnection connection = await OpenConnectionAsync();
            await using SqliteTransaction transaction = connection.BeginTransaction();
            string now = DateTimeOffset.UtcNow.ToString("O");

            foreach (ActionDefinition action in actions)
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO actions
                        (id, name, description, kind, target, arguments, category,
                         icon_glyph, is_favorite, usage_count, last_used_at,
                         is_indexed, created_at, updated_at)
                    VALUES
                        ($id, $name, $description, $kind, $target, $arguments, $category,
                         $icon, 0, 0, NULL, $indexed, $now, $now)
                    ON CONFLICT(id) DO UPDATE SET
                        name = excluded.name,
                        description = excluded.description,
                        kind = excluded.kind,
                        target = excluded.target,
                        arguments = excluded.arguments,
                        category = excluded.category,
                        icon_glyph = excluded.icon_glyph,
                        is_indexed = excluded.is_indexed,
                        updated_at = excluded.updated_at;
                    """;
                command.Parameters.AddWithValue("$id", action.Id);
                command.Parameters.AddWithValue("$name", action.Name);
                command.Parameters.AddWithValue("$description", action.Description);
                command.Parameters.AddWithValue("$kind", (int)action.Kind);
                command.Parameters.AddWithValue("$target", action.Target);
                command.Parameters.AddWithValue("$arguments", action.Arguments);
                command.Parameters.AddWithValue("$category", action.Category);
                command.Parameters.AddWithValue("$icon", action.IconGlyph);
                command.Parameters.AddWithValue("$indexed", isIndexed ? 1 : 0);
                command.Parameters.AddWithValue("$now", now);
                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        finally
        {
            _writeGate.Release();
        }
    }


    private static ActionDefinition ReadAction(SqliteDataReader reader) =>
        new()
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Description = reader.GetString(2),
            Kind = (ActionKind)reader.GetInt32(3),
            Target = reader.GetString(4),
            Arguments = reader.GetString(5),
            Category = reader.GetString(6),
            IconGlyph = reader.GetString(7),
            IsFavorite = reader.GetInt64(8) == 1,
            UsageCount = reader.GetInt64(9),
            LastUsedAt = reader.IsDBNull(10)
                ? null
                : DateTimeOffset.Parse(reader.GetString(10))
        };

    private static ActionDefinition BuiltIn(
        string id,
        string name,
        string description,
        ActionKind kind,
        string target,
        string category,
        string icon) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            Kind = kind,
            Target = target,
            Category = category,
            IconGlyph = icon
        };

}
