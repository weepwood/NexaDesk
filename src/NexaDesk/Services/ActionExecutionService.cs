using System.Diagnostics;
using NexaDesk.Models;

namespace NexaDesk;

public sealed class ActionExecutionService(
    DatabaseService database,
    WindowService windows)
{
    public async Task<ExecutionResult> ExecuteAsync(
        ActionDefinition action,
        CancellationToken cancellationToken = default)
    {
        ExecutionResult result;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            result = action.Kind switch
            {
                ActionKind.LaunchFile => Launch(action.Target, action.Arguments),
                ActionKind.LaunchUri => Launch(action.Target, action.Arguments),
                ActionKind.OpenFolder => Launch(action.Target, action.Arguments),
                ActionKind.WindowTopMost => windows.ToggleForegroundTopMost(),
                ActionKind.WindowCenter => windows.CenterForegroundWindow(),
                ActionKind.LockWorkstation => LockWorkstation(),
                ActionKind.RunCommand => Launch(action.Target, action.Arguments),
                _ => ExecutionResult.Fail("不支持的动作类型。")
            };
        }
        catch (OperationCanceledException)
        {
            result = ExecutionResult.Fail("操作已取消。");
        }
        catch (Exception ex)
        {
            result = ExecutionResult.Fail(ex.Message);
        }

        await database.RecordExecutionAsync(action, result);
        return result;
    }

    private static ExecutionResult Launch(string target, string arguments)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ExecutionResult.Fail("动作目标为空。");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            Arguments = arguments,
            UseShellExecute = true
        });

        return ExecutionResult.Ok();
    }

    private static ExecutionResult LockWorkstation() =>
        NativeMethods.LockWorkStation()
            ? ExecutionResult.Ok("电脑已锁定。")
            : ExecutionResult.Fail("Windows 拒绝了锁定请求。");
}
