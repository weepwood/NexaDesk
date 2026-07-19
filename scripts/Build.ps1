[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateSet("x64", "ARM64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    dotnet restore .\NexaDesk.sln -p:Platform=$Platform
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    dotnet build .\NexaDesk.sln `
        --no-restore `
        -c $Configuration `
        -p:Platform=$Platform `
        -p:WindowsPackageType=None `
        -p:AppxPackageSigningEnabled=false

    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}
finally {
    Pop-Location
}
