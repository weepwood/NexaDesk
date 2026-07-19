# Architecture

NexaDesk is a local-only Windows application.

```text
WinUI 3 shell
  ├─ Command palette
  ├─ Dashboard and pages
  └─ Tray and global hotkey
          │
Application services
  ├─ ActionExecutionService
  ├─ WorkflowService
  ├─ ApplicationIndexService
  ├─ UpdateService
  └─ StartupService
          │
Platform and persistence
  ├─ Win32 / WinRT
  ├─ Process and URI launch
  └─ SQLite under %LocalAppData%\NexaDesk
```

## Action execution order

1. documented software URI or API
2. command-line interface
3. WinRT, COM, or Win32
4. UI Automation
5. simulated input only as a final fallback

The first implementation intentionally excludes arbitrary scripts and
simulated pointer input.

## Database

SQLite uses WAL mode, a busy timeout, and a serialized write gate. Tables:

- `actions`
- `execution_history`
- `workflows`
- `workflow_steps`
- `settings`

Start menu entries are generated actions and can be rebuilt at any time.

## Updates

- MSIX installations use an `.appinstaller` association.
- Portable builds query GitHub Releases and open the release page.
