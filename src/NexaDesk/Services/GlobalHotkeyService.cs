using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NexaDesk;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x4E58;
    private const nuint SubclassId = 0x4E58484B;

    private readonly NativeMethods.SubclassProc _subclassProc;
    private nint _hwnd;
    private bool _registered;

    public event EventHandler? Triggered;

    public GlobalHotkeyService()
    {
        _subclassProc = WindowProcedure;
    }

    public void Initialize(nint hwnd)
    {
        _hwnd = hwnd;

        if (!NativeMethods.SetWindowSubclass(hwnd, _subclassProc, SubclassId, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _registered = NativeMethods.RegisterHotKey(
            hwnd,
            HotkeyId,
            NativeMethods.ModControl | NativeMethods.ModAlt,
            NativeMethods.VkSpace);

        if (!_registered)
        {
            NativeMethods.RemoveWindowSubclass(hwnd, _subclassProc, SubclassId);
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "无法注册 Ctrl + Alt + Space，可能已被其他程序占用。");
        }
    }

    private nint WindowProcedure(
        nint hwnd,
        uint message,
        nuint wParam,
        nint lParam,
        nuint subclassId,
        nuint referenceData)
    {
        if (message == NativeMethods.WmHotkey && (int)wParam == HotkeyId)
        {
            Triggered?.Invoke(this, EventArgs.Empty);
            return 0;
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hwnd == 0)
        {
            return;
        }

        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_hwnd, HotkeyId);
        }

        NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, SubclassId);
        _hwnd = 0;
    }
}
