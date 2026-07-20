# Changelog

## [Unreleased]

## [0.1.1] - 2026-07-20

### Fixed

- Show the main window before local services finish initializing.
- Display an in-app startup failure page instead of silently leaving a background process.
- Fall back when Mica, the tray icon, or the global hotkey is unavailable.
- Reposition the initial window into the visible display work area.
- Log startup, AppDomain, XAML, and unobserved task exceptions.
- Add a Windows CI smoke test that verifies a visible top-level window is created.

## [0.1.0] - 2026-07-19

### Added

- Native WinUI 3 shell with Fluent navigation.
- Global `Ctrl + Alt + Space` command palette.
- Local SQLite actions, workflows, settings, and history.
- Start menu application indexing.
- Built-in Windows and window-management actions.
- System tray and close-to-tray behavior.
- Packaged startup task support.
- MSIX/App Installer release pipeline and portable builds.
- Packaged and portable update checks.
