using System.Runtime.InteropServices;
using NexaDesk.Models;

namespace NexaDesk;

public sealed class WindowService
{
    private nint _lastTargetWindow;

    public void CaptureForegroundWindow()
    {
        nint hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd != 0)
        {
            _lastTargetWindow = hwnd;
        }
    }

    public ExecutionResult ToggleForegroundTopMost()
    {
        nint hwnd = GetTargetWindow();
        if (hwnd == 0)
        {
            return ExecutionResult.Fail("没有可操作的前台窗口。");
        }

        bool isTopMost = IsWindowTopMost(hwnd);
        bool changed = NativeMethods.SetWindowPos(
            hwnd,
            isTopMost ? NativeMethods.HwndNoTopMost : NativeMethods.HwndTopMost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove |
            NativeMethods.SwpNoSize |
            NativeMethods.SwpNoActivate);

        return changed
            ? ExecutionResult.Ok(isTopMost ? "已取消窗口置顶。" : "窗口已置顶。")
            : ExecutionResult.Fail("无法修改窗口置顶状态。");
    }

    public ExecutionResult CenterForegroundWindow()
    {
        nint hwnd = GetTargetWindow();
        if (hwnd == 0 ||
            !NativeMethods.GetWindowRect(hwnd, out NativeMethods.Rect windowRect))
        {
            return ExecutionResult.Fail("无法读取前台窗口。");
        }

        nint monitor = NativeMethods.MonitorFromWindow(
            hwnd,
            NativeMethods.MonitorDefaultToNearest);

        NativeMethods.MonitorInfo info = new()
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return ExecutionResult.Fail("无法读取显示器工作区。");
        }

        int width = windowRect.Right - windowRect.Left;
        int height = windowRect.Bottom - windowRect.Top;
        int x = info.Work.Left + Math.Max(0, (info.Work.Right - info.Work.Left - width) / 2);
        int y = info.Work.Top + Math.Max(0, (info.Work.Bottom - info.Work.Top - height) / 2);

        bool moved = NativeMethods.SetWindowPos(
            hwnd,
            0,
            x,
            y,
            width,
            height,
            NativeMethods.SwpNoActivate);

        return moved
            ? ExecutionResult.Ok("窗口已居中。")
            : ExecutionResult.Fail("无法移动窗口。");
    }

    private nint GetTargetWindow() =>
        _lastTargetWindow != 0
            ? _lastTargetWindow
            : NativeMethods.GetForegroundWindow();

    private static bool IsWindowTopMost(nint hwnd)
    {
        const int GwlExStyle = -20;
        const long WsExTopMost = 0x00000008;
        nint style = GetWindowLongPtr(hwnd, GwlExStyle);
        return (style.ToInt64() & WsExTopMost) != 0;
    }

    private static nint GetWindowLongPtr(nint hwnd, int index) =>
        Environment.Is64BitProcess
            ? GetWindowLongPtr64(hwnd, index)
            : new nint(GetWindowLong32(hwnd, index));

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern nint GetWindowLongPtr64(nint hwnd, int index);
}
