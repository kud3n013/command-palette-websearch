<#
.SYNOPSIS
    Builds the extension's MSIX packages, optionally signing them.
.DESCRIPTION
    Command Palette only discovers extensions that have package identity, so the extension ships as an MSIX
    (an unpackaged exe registered only by CLSID is never loaded). For each platform this does a clean Release
    build, copies the MSIX to artifacts/WebSearchShortcut-<version>-<arch>.msix and writes SHA256SUMS.txt.

    An unsigned MSIX is what you upload to the Microsoft Store. To install it with winget or by double-click it
    must be signed by a certificate the machine trusts: pass -CertificateThumbprint (a code-signing certificate in
    Cert:\CurrentUser\My whose subject equals the Publisher in Package.appxmanifest).
.EXAMPLE
    .\scripts\build-msix.ps1
    .\scripts\build-msix.ps1 -Platform x64 -CertificateThumbprint 0123ABCD...
#>
param(
    [ValidateSet("x64", "arm64", "all")]
    [string]$Platform = "all",
    [string]$CertificateThumbprint
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

$global:LASTEXITCODE = 0
$version = & (Join-Path $PSScriptRoot "check-version.ps1")
if ($LASTEXITCODE -ne 0) { throw "Version check failed" }

$project = Join-Path $root "CmdPalWebSearchShortcut\WebSearchShortcut\WebSearchShortcut.csproj"
$artifacts = Join-Path $root "artifacts"
New-Item -ItemType Directory -Force $artifacts | Out-Null

$signtool = $null
if ($CertificateThumbprint) {
    if (-not (Test-Path "Cert:\CurrentUser\My\$CertificateThumbprint")) { throw "Certificate $CertificateThumbprint not found in Cert:\CurrentUser\My" }
    # The Windows SDK build tools NuGet package (restored by the build) ships signtool.
    $signtool = Get-ChildItem (Join-Path $env:USERPROFILE ".nuget\packages\microsoft.windows.sdk.buildtools") -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
}

$platforms = if ($Platform -eq "all") { @("x64", "arm64") } else { @($Platform) }
foreach ($arch in $platforms) {
    Write-Host "`n=== $arch ($version) ===" -ForegroundColor Cyan
    $msbuildPlatform = if ($arch -eq "arm64") { "ARM64" } else { "x64" }
    $packageDir = Join-Path $artifacts "msix-$arch"
    Remove-Item -Recurse -Force $packageDir -ErrorAction SilentlyContinue

    # --no-incremental: an up-to-date incremental build skips writing the MSIX.
    dotnet build $project -c Release -p:Platform=$msbuildPlatform --no-incremental "-p:AppxPackageDir=$packageDir\"
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for $arch" }

    $msix = Get-ChildItem $packageDir -Recurse -Filter *.msix | Where-Object { $_.FullName -notmatch '\\Dependencies\\' } | Select-Object -First 1
    if (-not $msix) { throw "No MSIX produced for $arch in $packageDir" }

    $output = Join-Path $artifacts "WebSearchShortcut-$version-$arch.msix"
    Copy-Item $msix.FullName $output -Force
    Remove-Item -Recurse -Force $packageDir

    if ($CertificateThumbprint) {
        if (-not $signtool) { throw "signtool.exe not found (restore the project first)" }
        & $signtool sign /fd SHA256 /sha1 $CertificateThumbprint /s My $output
        if ($LASTEXITCODE -ne 0) { throw "Signing failed for $arch" }
    }
}

$packages = Get-ChildItem $artifacts -Filter "WebSearchShortcut-$version-*.msix" | Sort-Object Name
$sums = $packages | ForEach-Object { "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name }
Set-Content -Path (Join-Path $artifacts "SHA256SUMS.txt") -Value $sums -Encoding ascii

Write-Host "`nArtifacts$(if ($CertificateThumbprint) { ' (signed)' } else { ' (unsigned)' }):" -ForegroundColor Green
$sums | ForEach-Object { Write-Host "  $_" }
