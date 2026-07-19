[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,
    [Parameter(Mandatory)]
    [string]$Publisher,
    [Parameter(Mandatory)]
    [string]$OutputPath,
    [string]$Repository = "weepwood/NexaDesk"
)

$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "App Installer version must contain four numeric components."
}

$baseUri = "https://github.com/$Repository/releases/latest/download"
$content = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
  xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"
  Version="$Version"
  Uri="$baseUri/NexaDesk.appinstaller">
  <MainPackage
    Name="weepwood.NexaDesk"
    Publisher="$Publisher"
    Version="$Version"
    ProcessorArchitecture="x64"
    Uri="$baseUri/NexaDesk-x64.msix" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="4" ShowPrompt="true" UpdateBlocksActivation="false" />
    <AutomaticBackgroundTask />
    <ForceUpdateFromAnyVersion>false</ForceUpdateFromAnyVersion>
  </UpdateSettings>
</AppInstaller>
"@

$directory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $directory -Force | Out-Null
[IO.File]::WriteAllText($OutputPath, $content, (New-Object Text.UTF8Encoding($false)))
Write-Host "Created $OutputPath"
