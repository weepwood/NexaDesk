using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;

namespace NexaDesk;

public sealed class TrayIconService : IDisposable
{
    private const nuint SubclassId = 0x4E585452;
    private const uint TrayMessage = NativeMethods.WmApp + 37;
    private const uint TrayId = 1;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly NativeMethods.SubclassProc _subclassProc;
    private nint _hwnd;
    private nint _icon;
    private NativeMethods.NotifyIconData _data;
    private bool _initialized;

    public event EventHandler? ShowRequested;
    public event EventHandler? PaletteRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
        _subclassProc = WindowProcedure;
    }

    public void Initialize(nint hwnd)
    {
        _hwnd = hwnd;
        NativeMethods.SetWindowSubclass(hwnd, _subclassProc, SubclassId, 0);

        _icon = NativeMethods.LoadIcon(0, NativeMethods.IdiApplication);

        if (_icon == 0)
        {
            return;
        }

        _data = new NativeMethods.NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
            WindowHandle = hwnd,
            Id = TrayId,
            Flags = NativeMethods.NifMessage | NativeMethods.NifIcon | NativeMethods.NifTip,
            CallbackMessage = TrayMessage,
            Icon = _icon,
            ToolTip = "NexaDesk",
            Info = string.Empty,
            InfoTitle = string.Empty,
            TimeoutOrVersion = NativeMethods.NotifyIconVersion4
        };

        _initialized = NativeMethods.Shell_NotifyIcon(NativeMethods.NimAdd, ref _data);
        if (_initialized)
        {
            NativeMethods.Shell_NotifyIcon(NativeMethods.NimSetVersion, ref _data);
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
        if (message == TrayMessage)
        {
            uint mouseMessage = unchecked((uint)(lParam.ToInt64() & 0xFFFF));
            if (mouseMessage == NativeMethods.WmLButtonUp)
            {
                _dispatcherQueue.TryEnqueue(
                    () => ShowRequested?.Invoke(this, EventArgs.Empty));
                return 0;
            }

            if (mouseMessage == NativeMethods.WmRButtonUp)
            {
                ShowMenu();
                return 0;
            }
        }

        return NativeMethods.DefSubclassProc(hwnd, message, wParam, lParam);
    }

    private void ShowMenu()
    {
        nint menu = NativeMethods.CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, 1, "打开 NexaDesk");
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, 2, "打开命令面板");
            NativeMethods.AppendMenu(menu, NativeMethods.MfSeparator, 0, null);
            NativeMethods.AppendMenu(menu, NativeMethods.MfString, 3, "退出");

            NativeMethods.GetCursorPos(out NativeMethods.Point point);
            NativeMethods.SetForegroundWindow(_hwnd);

            uint command = NativeMethods.TrackPopupMenu(
                menu,
                NativeMethods.TpmRightButton | NativeMethods.TpmReturnCmd,
                point.X,
                point.Y,
                0,
                _hwnd,
                0);

            _dispatcherQueue.TryEnqueue(() =>
            {
                switch (command)
                {
                    case 1:
                        ShowRequested?.Invoke(this, EventArgs.Empty);
                        break;
                    case 2:
                        PaletteRequested?.Invoke(this, EventArgs.Empty);
                        break;
                    case 3:
                        ExitRequested?.Invoke(this, EventArgs.Empty);
                        break;
                }
            });
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        if (_initialized)
        {
            NativeMethods.Shell_NotifyIcon(NativeMethods.NimDelete, ref _data);
        }

        if (_hwnd != 0)
        {
            NativeMethods.RemoveWindowSubclass(_hwnd, _subclassProc, SubclassId);
        }

        if (_icon != 0)
        {
            NativeMethods.DestroyIcon(_icon);
        }
    }
}
