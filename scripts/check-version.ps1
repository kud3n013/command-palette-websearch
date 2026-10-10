<#
.SYNOPSIS
    Prints the project version (the single source of truth is <Version> in
    CmdPalWebSearchShortcut/Directory.Build.props) and fails if anything else disagrees with it.
.PARAMETER Tag
    Optional git tag (e.g. v1.0.0). When given, it must equal "v" + the project version.
#>
param([string]$Tag)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "CmdPalWebSearchShortcut"

$props = [xml](Get-Content (Join-Path $project "Directory.Build.props") -Raw)
$version = ($props.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Version in Directory.Build.props is '$version'; expected x.y.z" }

$errors = @()

$appx = [xml](Get-Content (Join-Path $project "WebSearchShortcut\Package.appxmanifest") -Raw)
if ($appx.Package.Identity.Version -ne "$version.0") {
    $errors += "Package.appxmanifest Identity Version is '$($appx.Package.Identity.Version)', expected '$version.0'"
}

$appManifest = [xml](Get-Content (Join-Path $project "WebSearchShortcut\app.manifest") -Raw)
if ($appManifest.assembly.assemblyIdentity.version -ne "$version.0") {
    $errors += "app.manifest assemblyIdentity version is '$($appManifest.assembly.assemblyIdentity.version)', expected '$version.0'"
}

# The COM CLSID must be identical in the C# class and the appx manifest (Command Palette activates it by CLSID).
$clsid = "4507dbd1-cc9b-414b-a879-5a2dd4266881"
foreach ($f in @("CmdPalWebSearchShortcut\WebSearchShortcut\WebSearchShortcut.cs",
                 "CmdPalWebSearchShortcut\WebSearchShortcut\Package.appxmanifest")) {
    if (-not (Select-String -Path (Join-Path $root $f) -Pattern $clsid -SimpleMatch -Quiet)) {
        $errors += "CLSID $clsid not found in $f"
    }
}

if ($Tag -and $Tag -ne "v$version") {
    $errors += "Tag '$Tag' does not match project version 'v$version'"
}

if ($errors) { $errors | ForEach-Object { Write-Error $_ -ErrorAction Continue }; exit 1 }
Write-Output $version
