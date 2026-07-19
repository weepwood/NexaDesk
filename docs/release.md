# Release and signing guide

## Outputs

A tagged release builds:

- `NexaDesk-x64.msix` — signed self-contained MSIX
- `NexaDesk.appinstaller` — automatic-update association
- `NexaDesk-portable-x64-framework.zip` — smaller portable build
- `NexaDesk-portable-x64-selfcontained.zip` — standalone portable build
- `SHA256SUMS.txt`

## Required repository secrets

| Secret | Purpose |
|---|---|
| `WINDOWS_CERTIFICATE` | Base64-encoded PFX |
| `WINDOWS_CERTIFICATE_PASSWORD` | PFX password |
| `WINDOWS_CERTIFICATE_PUBLISHER` | Exact certificate subject |

The manifest publisher must exactly match the certificate subject.

## Development certificate

```powershell
./scripts/Create-DevCertificate.ps1
```

The generated PFX and CER are written below `.local/certificates`. Never
commit the PFX.

## Publishing

1. Merge release changes.
2. Push a tag such as `v0.1.0`.
3. GitHub Actions builds and signs the MSIX.
4. Fixed release asset names are uploaded.
5. Users install through `NexaDesk.appinstaller`.
6. Windows checks that update source on launch and in the background.

Use a trusted code-signing certificate for public distribution. A self-signed
certificate is only suitable for development.

MSIX maps tag `v1.2.3` to package version `1.2.3.0`.
