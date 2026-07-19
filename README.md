# NexaDesk

NexaDesk is a native, local-first Windows action launcher and workflow center.
Press `Ctrl + Alt + Space`, search for an application or Windows action, and
execute it without opening multiple menus.

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

The release workflow builds a signed self-contained MSIX, two portable
variants, an App Installer manifest, and SHA-256 checksums.

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
