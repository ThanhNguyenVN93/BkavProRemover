<#
.SYNOPSIS
    Builds Bkav Pro / BluPro Remover for both x86 and x64 and collects each
    into its own folder under dist\, ready to hand to end users.
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$sln  = Join-Path $root 'BkavRemover.sln'
$msbuild = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe'
if (-not (Test-Path $msbuild)) {
    $msbuild = (Get-Command msbuild.exe -EA SilentlyContinue).Source
}
if (-not $msbuild) { throw 'MSBuild.exe not found.' }

foreach ($platform in @('x86', 'x64')) {
    Write-Host "=== Building $platform ($Configuration) ===" -ForegroundColor Cyan
    & $msbuild $sln "/p:Configuration=$Configuration" "/p:Platform=$platform" /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $platform" }

    $src = Join-Path $root "src\BkavRemover.UI\bin\$platform\$Configuration"
    $dst = Join-Path $root "dist\$platform"
    if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
    New-Item -ItemType Directory -Path $dst -Force | Out-Null

    Copy-Item (Join-Path $src 'BkavProRemover.exe') $dst
    Write-Host "-> $dst" -ForegroundColor Green
}

Write-Host "Done. Output in $(Join-Path $root 'dist')" -ForegroundColor Cyan
