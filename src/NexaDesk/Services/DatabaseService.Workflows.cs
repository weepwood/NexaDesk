using Microsoft.Data.Sqlite;
using NexaDesk.Models;

namespace NexaDesk;

public sealed partial class DatabaseService
{
    public async Task RecordExecutionAsync(
        ActionDefinition action,
        ExecutionResult result)
    {
        await _writeGate.WaitAsync();
        try
        {
            await using SqliteConnection connection = await OpenConnectionAsync();
            await using SqliteTransaction transaction = connection.BeginTransaction();

            string now = DateTimeOffset.UtcNow.ToString("O");

            await using (SqliteCommand history = connection.CreateCommand())
            {
                history.Transaction = transaction;
                history.CommandText =
                    """
                    INSERT INTO execution_history
                        (action_id, action_name, success, message, executed_at)
                    VALUES
                        ($actionId, $actionName, $success, $message, $executedAt);
                    """;
                history.Parameters.AddWithValue("$actionId", action.Id);
                history.Parameters.AddWithValue("$actionName", action.Name);
                history.Parameters.AddWithValue("$success", result.Success ? 1 : 0);
                history.Parameters.AddWithValue("$message", result.Message);
                history.Parameters.AddWithValue("$executedAt", now);
                await history.ExecuteNonQueryAsync();
            }

            if (result.Success)
            {
                await using SqliteCommand usage = connection.CreateCommand();
                usage.Transaction = transaction;
                usage.CommandText =
                    """
                    UPDATE actions
                    SET usage_count = usage_count + 1,
                        last_used_at = $now,
                        updated_at = $now
                    WHERE id = $id;
                    """;
                usage.Parameters.AddWithValue("$now", now);
                usage.Parameters.AddWithValue("$id", action.Id);
                await usage.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<IReadOnlyList<ExecutionHistoryItem>> GetHistoryAsync(int limit = 100)
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, action_id, action_name, success, message, executed_at
            FROM execution_history
            ORDER BY executed_at DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        List<ExecutionHistoryItem> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new ExecutionHistoryItem
            {
                Id = reader.GetInt64(0),
                ActionId = reader.GetString(1),
                ActionName = reader.GetString(2),
                Success = reader.GetInt64(3) == 1,
                Message = reader.GetString(4),
                ExecutedAt = DateTimeOffset.Parse(reader.GetString(5))
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetWorkflowsAsync()
    {
        await using SqliteConnection connection = await OpenConnectionAsync();
        Dictionary<string, WorkflowBuilder> builders = new(StringComparer.Ordinal);

        await using (SqliteCommand workflowCommand = connection.CreateCommand())
        {
            workflowCommand.CommandText =
                "SELECT id, name, description FROM workflows ORDER BY name COLLATE NOCASE;";
            await using SqliteDataReader reader = await workflowCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string id = reader.GetString(0);
                builders[id] = new WorkflowBuilder(id, reader.GetString(1), reader.GetString(2));
            }
        }

        await using (SqliteCommand stepCommand = connection.CreateCommand())
        {
            stepCommand.CommandText =
                """
                SELECT s.id, s.workflow_id, s.action_id, a.name, s.position, s.delay_after_ms
                FROM workflow_steps s
                INNER JOIN actions a ON a.id = s.action_id
                ORDER BY s.workflow_id, s.position;
                """;
            await using SqliteDataReader reader = await stepCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string workflowId = reader.GetString(1);
                if (builders.TryGetValue(workflowId, out WorkflowBuilder? builder))
                {
                    builder.Steps.Add(new WorkflowStep
                    {
                        Id = reader.GetString(0),
                        WorkflowId = workflowId,
                        ActionId = reader.GetString(2),
                        ActionName = reader.GetString(3),
                        Position = reader.GetInt32(4),
                        DelayAfterMilliseconds = reader.GetInt32(5)
                    });
                }
            }
        }

        return builders.Values.Select(static builder => builder.Build()).ToArray();
    }

    public async Task CreateWorkflowAsync(
        string name,
        IReadOnlyList<ActionDefinition> actions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("工作流名称不能为空。", nameof(name));
        }

        if (actions.Count == 0)
        {
            throw new ArgumentException("工作流至少需要一个动作。", nameof(actions));
        }

        await _writeGate.WaitAsync();
        try
        {
            await using SqliteConnection connection = await OpenConnectionAsync();
            await using SqliteTransaction transaction = connection.BeginTransaction();

            string workflowId = $"workflow.{Guid.NewGuid():N}";
            string now = DateTimeOffset.UtcNow.ToString("O");

            await using (SqliteCommand workflow = connection.CreateCommand())
            {
                workflow.Transaction = transaction;
                workflow.CommandText =
                    """
                    INSERT INTO workflows(id, name, description, created_at, updated_at)
                    VALUES($id, $name, '', $now, $now);
                    """;
                workflow.Parameters.AddWithValue("$id", workflowId);
                workflow.Parameters.AddWithValue("$name", name.Trim());
                workflow.Parameters.AddWithValue("$now", now);
                await workflow.ExecuteNonQueryAsync();
            }

            for (int index = 0; index < actions.Count; index++)
            {
                await using SqliteCommand step = connection.CreateCommand();
                step.Transaction = transaction;
                step.CommandText =
                    """
                    INSERT INTO workflow_steps
                        (id, workflow_id, action_id, position, delay_after_ms)
                    VALUES
                        ($id, $workflowId, $actionId, $position, 250);
                    """;
                step.Parameters.AddWithValue("$id", $"step.{Guid.NewGuid():N}");
                step.Parameters.AddWithValue("$workflowId", workflowId);
                step.Parameters.AddWithValue("$actionId", actions[index].Id);
                step.Parameters.AddWithValue("$position", index);
                await step.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public Task DeleteWorkflowAsync(string id) =>
        ExecuteWriteAsync("DELETE FROM workflows WHERE id = $id;", ("$id", id));


    private async Task SeedWorkflowAsync()
    {
        await _writeGate.WaitAsync();
        try
        {
            await using SqliteConnection connection = await OpenConnectionAsync();
            await using SqliteTransaction transaction = connection.BeginTransaction();
            string now = DateTimeOffset.UtcNow.ToString("O");

            await using (SqliteCommand workflow = connection.CreateCommand())
            {
                workflow.Transaction = transaction;
                workflow.CommandText =
                    """
                    INSERT OR IGNORE INTO workflows
                        (id, name, description, created_at, updated_at)
                    VALUES
                        ('workflow.workspace', '打开基础工作区',
                         '依次打开资源管理器和 Windows Terminal。',
                         $now, $now);
                    """;
                workflow.Parameters.AddWithValue("$now", now);
                await workflow.ExecuteNonQueryAsync();
            }

            string[] actionIds = ["system.explorer", "system.terminal"];
            for (int index = 0; index < actionIds.Length; index++)
            {
                await using SqliteCommand step = connection.CreateCommand();
                step.Transaction = transaction;
                step.CommandText =
                    """
                    INSERT OR IGNORE INTO workflow_steps
                        (id, workflow_id, action_id, position, delay_after_ms)
                    VALUES
                        ($id, 'workflow.workspace', $actionId, $position, 350);
                    """;
                step.Parameters.AddWithValue("$id", $"workflow.workspace.{index}");
                step.Parameters.AddWithValue("$actionId", actionIds[index]);
                step.Parameters.AddWithValue("$position", index);
                await step.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        finally
        {
            _writeGate.Release();
        }
    }


    private sealed class WorkflowBuilder(
        string id,
        string name,
        string description)
    {
        public List<WorkflowStep> Steps { get; } = [];

        public WorkflowDefinition Build() =>
            new()
            {
                Id = id,
                Name = name,
                Description = description,
                Steps = Steps.ToArray()
            };
    }
}
