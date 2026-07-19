[CmdletBinding()]
param(
    [string]$Publisher = "CN=NexaDesk Development",
    [string]$Password = "NexaDesk-Development-Only"
)

$ErrorActionPreference = "Stop"
$outDir = Join-Path (Split-Path -Parent $PSScriptRoot) ".local\certificates"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$certificate = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Publisher `
    -KeyUsage DigitalSignature `
    -FriendlyName "NexaDesk Development Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3") `
    -NotAfter (Get-Date).AddYears(2)

$securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText
$pfxPath = Join-Path $outDir "NexaDesk-Development.pfx"
$cerPath = Join-Path $outDir "NexaDesk-Development.cer"

Export-PfxCertificate -Cert $certificate -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $certificate -FilePath $cerPath | Out-Null

Write-Host "Created:"
Write-Host "  PFX: $pfxPath"
Write-Host "  CER: $cerPath"
Write-Host "Publisher: $Publisher"
Write-Warning "Development only. Never commit the PFX."
