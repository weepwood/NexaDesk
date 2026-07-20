# Changelog

## [Unreleased]

## [0.1.2]

### Fixed

- Added the required WinUI `XamlControlsResources` dictionary. The missing dictionary caused `MainWindow.InitializeComponent()` to fail with `XamlParseException` because the `TabViewButtonBackground` resource could not be resolved.
- Unified portable deployment as unpackaged and Windows App SDK self-contained at the project level.
- Added an explicit WinUI entry point, bootstrap logging, and a startup watchdog.
- Disabled trimming and ReadyToRun for the portable release path to prioritize startup reliability.

## [0.1.1]

### Fixed

- Show the main window before local services are initialized.
- Surface startup failures in the UI instead of leaving an inaccessible process.
- Treat Mica, tray icon, and global hotkey integration as optional capabilities.
- Reposition the initial window into the primary display work area.
- Add startup-stage diagnostics and portable launch smoke coverage.
- Exit normally when close-to-tray is unavailable.

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
