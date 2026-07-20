# NexaDesk

NexaDesk is a native, local-first Windows action launcher and workflow center.
Press `Ctrl + Alt + Space`, search for an application or Windows action, and
execute it without opening multiple menus.

## Download

For a normal Windows installation, download:

```text
NexaDesk-windows-x64.zip
```

Extract the entire archive into a normal folder, and then run `NexaDesk.exe`.
Do not run it directly from the ZIP preview.

The package includes the required .NET and Windows App SDK runtime files. The
`NexaDesk-portable-x64-framework.zip` package is intended only for machines that
already have the matching .NET and Windows App Runtime installed.

## Implemented

- WinUI 3 Fluent interface
- global command palette
- system tray and close-to-tray behavior
- Windows and Start menu action indexing
- local SQLite persistence
- favorites, usage ranking, and execution history
- window topmost and centering actions
- local multi-step workflows
- packaged startup task
- MSIX/App Installer automatic-update channel
- framework-dependent and self-contained portable packages

No application data is sent to a backend. Runtime data is stored under:

```text
%LocalAppData%\NexaDesk
```

## Startup troubleshooting

NexaDesk 0.1.2 fixes a missing WinUI control-resource dictionary that prevented
the main window from being created. It also uses a unified unpackaged,
self-contained startup configuration.

If startup still fails, inspect:

```text
%LocalAppData%\NexaDesk\logs\bootstrap.log
%LocalAppData%\NexaDesk\logs\nexadesk.log
```

If a visible window is not detected within 12 seconds, NexaDesk displays a
native Windows error dialog with the log locations.

## Technology

- C# and .NET 10
- WinUI 3 / Windows App SDK 2.2
- Win32 and WinRT
- Microsoft.Data.Sqlite
- CommunityToolkit.Mvvm
- MSIX and App Installer

## Build

Requirements:

- Windows 10 2004 or later, or Windows 11
- Visual Studio 2022 with .NET desktop development and Windows App SDK tooling
- .NET 10 SDK

```powershell
git clone https://github.com/weepwood/NexaDesk.git
cd NexaDesk
./scripts/Build.ps1
```

Open `NexaDesk.sln` for normal development.

## Release and automatic updates

The release workflow builds a signed self-contained MSIX when signing secrets
are configured, plus portable Windows packages and SHA-256 checksums.

Users who install `NexaDesk.appinstaller` are associated with its update
source. Windows checks for updates when the app starts and through a background
update task.

See [docs/release.md](docs/release.md) for signing configuration.

## Keyboard

| Shortcut | Action |
|---|---|
| `Ctrl + Alt + Space` | Open command palette |
| `Enter` | Execute selected action |
| `Esc` | Hide command palette |

## License

MIT
