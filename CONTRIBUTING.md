# Contributing

## Development environment

- Windows 10 2004 or later, or Windows 11
- Visual Studio 2022 with .NET desktop development and Windows App SDK tooling
- .NET 10 SDK
- PowerShell 7 recommended

## Workflow

1. Create a branch from `main`.
2. Run `./scripts/Build.ps1`.
3. Keep system actions explicit and avoid hidden privilege escalation.
4. Open a pull request.
5. Never commit certificates, generated packages, or local SQLite files.

## Architecture rules

- UI code calls services instead of calling Win32 or SQLite directly.
- Prefer documented URI, CLI, WinRT, COM, or Win32 APIs before simulated input.
- Persist user data under `%LocalAppData%\NexaDesk`.
- New actions must declare an action kind and user-visible description.
