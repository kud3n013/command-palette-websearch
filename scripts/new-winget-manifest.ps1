<#
.SYNOPSIS
    Generates the three winget manifest files (version, installer, defaultLocale) for the signed MSIX packages.
.DESCRIPTION
    Writes winget/manifests/k/kud3n013/WebSearchShortcut/<version>/ with InstallerType msix.
    - InstallerSha256 is the SHA256 of each .msix; SignatureSha256 is the SHA256 of its AppxSignature.p7x.
    - PackageFamilyName is computed from the Name and Publisher in Package.appxmanifest.
    By default the packages are downloaded from the GitHub release (so hashes are of the *uploaded* files);
    -ArtifactsDir hashes local files instead, and -BaseUrl points the installer URLs elsewhere (e.g. a localhost
    web server for testing `winget install --manifest` before anything is published; see RELEASING.md).
    The output folder is gitignored because the hashes belong to one specific build.
.EXAMPLE
    .\scripts\new-winget-manifest.ps1 -Version 1.0.0
    .\scripts\new-winget-manifest.ps1 -Version 1.0.0 -ArtifactsDir .\artifacts -BaseUrl http://localhost:8765 -Platform x64
#>
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [string]$ArtifactsDir,
    [string]$BaseUrl,
    [ValidateSet("x64", "arm64", "all")]
    [string]$Platform = "all",
    [string]$OutRoot
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Split-Path -Parent $PSScriptRoot
if (-not $OutRoot) { $OutRoot = Join-Path $root "winget\manifests" }

$id = "kud3n013.WebSearchShortcut"
$repo = "https://github.com/kud3n013/command-palette-websearch"
$tag = "v$Version"
$schema = "1.10.0"
$packageName = "WebSearchShortcut for Command Palette"

if (-not $BaseUrl) { $BaseUrl = "$repo/releases/download/$tag" }
$BaseUrl = $BaseUrl.TrimEnd('/')

function Get-PackageFamilyName {
    $appx = [xml](Get-Content (Join-Path $root "CmdPalWebSearchShortcut\WebSearchShortcut\Package.appxmanifest") -Raw)
    $name = $appx.Package.Identity.Name
    $publisher = $appx.Package.Identity.Publisher
    # Publisher id = Crockford base32 of the first 8 bytes of SHA256(UTF-16LE publisher), 13 characters.
    $hash = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::Unicode.GetBytes($publisher))
    $bits = (($hash[0..7] | ForEach-Object { [Convert]::ToString($_, 2).PadLeft(8, '0') }) -join '').PadRight(65, '0')
    $alphabet = "0123456789abcdefghjkmnpqrstvwxyz"
    $publisherId = -join (0..12 | ForEach-Object { $alphabet[[Convert]::ToInt32($bits.Substring($_ * 5, 5), 2)] })
    "${name}_$publisherId"
}

function Get-InstallerInfo([string]$arch) {
    $name = "WebSearchShortcut-$Version-$arch.msix"
    $url = "$BaseUrl/$name"
    if ($ArtifactsDir) {
        $file = Join-Path $ArtifactsDir $name
        if (-not (Test-Path $file)) { throw "Missing $file" }
    } else {
        $file = Join-Path ([IO.Path]::GetTempPath()) "winget-$([guid]::NewGuid())-$name"
        Write-Host "Downloading $url"
        Invoke-WebRequest -Uri $url -OutFile $file -MaximumRedirection 5
    }

    $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $file).Path)
    try {
        $signature = $zip.GetEntry("AppxSignature.p7x")
        if (-not $signature) { throw "$name is not signed. winget only installs signed MSIX packages (see RELEASING.md)." }
        $stream = $signature.Open()
        try { $signatureSha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
    } finally { $zip.Dispose() }

    $installerSha = (Get-FileHash $file -Algorithm SHA256).Hash.ToUpper()
    if (-not $ArtifactsDir) { Remove-Item $file -Force }
    [pscustomobject]@{ Arch = $arch; Url = $url; Sha = $installerSha; SignatureSha = $signatureSha }
}

$platforms = if ($Platform -eq "all") { @("x64", "arm64") } else { @($Platform) }
$installers = $platforms | ForEach-Object { Get-InstallerInfo $_ }
$packageFamilyName = Get-PackageFamilyName
$releaseDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")

$installerEntries = ($installers | ForEach-Object { @"
- Architecture: $($_.Arch)
  InstallerUrl: $($_.Url)
  InstallerSha256: $($_.Sha)
  SignatureSha256: $($_.SignatureSha)
"@ }) -join "`n"

$outDir = Join-Path $OutRoot "k\kud3n013\WebSearchShortcut\$Version"
New-Item -ItemType Directory -Force $outDir | Out-Null

$files = @{}

$files["$id.yaml"] = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.$schema.schema.json
PackageIdentifier: $id
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: $schema
"@

$files["$id.installer.yaml"] = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$schema.schema.json
PackageIdentifier: $id
PackageVersion: $Version
Platform:
- Windows.Desktop
MinimumOSVersion: 10.0.19041.0
InstallerType: msix
PackageFamilyName: $packageFamilyName
ReleaseDate: $releaseDate
Installers:
$installerEntries
ManifestType: installer
ManifestVersion: $schema
"@

$files["$id.locale.en-US.yaml"] = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.$schema.schema.json
PackageIdentifier: $id
PackageVersion: $Version
PackageLocale: en-US
Publisher: kud3n013
PublisherUrl: https://github.com/kud3n013
PublisherSupportUrl: $repo/issues
PackageName: $packageName
PackageUrl: $repo
License: MIT
LicenseUrl: $repo/blob/main/LICENSE
ShortDescription: PowerToys Command Palette extension to search any website with keyword shortcuts.
Description: |-
  A PowerToys Command Palette extension for quickly searching any website. Add search engines with a URL template,
  search them with an optional keyword prefix (e.g. "g cats"), get live search suggestions (Google, Bing,
  DuckDuckGo, YouTube, Wikipedia, npm, Can I use), and manage or batch-import them from the Command Palette settings.
  A fork of Daydreamer-riri/CmdPal-WebSearchShortcut.
Tags:
- windows-commandpalette-extension
- command-palette
- powertoys
- search
- search-engine
ReleaseNotesUrl: $repo/releases/tag/$tag
ManifestType: defaultLocale
ManifestVersion: $schema
"@

foreach ($name in $files.Keys) {
    # winget-pkgs manifests are UTF-8 without BOM.
    [IO.File]::WriteAllText((Join-Path $outDir $name), ($files[$name] -replace "`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding($false)))
}

Write-Host "Wrote manifests to $outDir"
Write-Host "  PackageFamilyName $packageFamilyName"
$installers | ForEach-Object { Write-Host "  $($_.Arch)  $($_.Sha)" }
