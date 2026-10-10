# Releasing

Package identifier (winget): `kud3n013.WebSearchShortcut`. Package identity (MSIX): `kud3n013.WebSearchShortcut`,
publisher `CN=kud3n013`.

Related files: `scripts/check-version.ps1`, `scripts/build-msix.ps1`, `scripts/new-winget-manifest.ps1`,
`.github/workflows/ci.yml`.

## Status

**Nothing is published yet.** The extension builds as an MSIX, and installing it with winget has been tested
end to end on a local machine (below), but a public release needs a **signed** MSIX and the signing route
(Microsoft Store or a code-signing certificate) hasn't been chosen. CI only builds unsigned packages.

## Why MSIX (and not an exe installer)

Command Palette discovers extensions through the Windows **AppExtension catalog**, which only contains apps with
package identity (an MSIX, a sparse package, or a dev registration). An installer that copies an exe and registers
its COM class under `HKCU\Software\Classes\CLSID` is never loaded: this was tested with an Inno Setup installer
(the exe activated fine over COM, but Command Palette never launched it).

Windows only installs an MSIX that is signed by a certificate the machine trusts, or registers loose files in
Developer Mode. So every way of shipping this extension comes down to who signs the package:

| Route | Who signs | Notes |
|---|---|---|
| Microsoft Store | The Store | Upload the **unsigned** MSIX from `build-msix.ps1` to Partner Center. The Store assigns the package Name and Publisher, so `Package.appxmanifest` must be updated to match. winget can then point at the Store-signed package (this is how the upstream project shipped). |
| Code-signing certificate | You | Sign with `build-msix.ps1 -CertificateThumbprint`, upload to GitHub Releases, submit the manifests from `new-winget-manifest.ps1` to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs). The certificate subject must equal the `Publisher` in `Package.appxmanifest`. |
| Developer Mode (dev only) | Nobody | `Add-AppxPackage -Register` on the build output. For development, not distribution. See the README. |

## Bumping the version

The single source of truth is `<Version>` in `CmdPalWebSearchShortcut/Directory.Build.props`. Also set the same
version (as `x.y.z.0`) in `CmdPalWebSearchShortcut/WebSearchShortcut/Package.appxmanifest` (`Identity Version`) and
`app.manifest` (`assemblyIdentity version`). `./scripts/check-version.ps1` fails if they disagree, and also checks
that the COM CLSID is the same in `WebSearchShortcut.cs` and `Package.appxmanifest`.

## Building the packages

```powershell
dotnet test CmdPalWebSearchShortcut/WebSearchShortcut.Tests/WebSearchShortcut.Tests.csproj -p:Platform=x64
./scripts/build-msix.ps1                                  # unsigned, artifacts/WebSearchShortcut-<v>-<arch>.msix
./scripts/build-msix.ps1 -CertificateThumbprint <thumb>   # signed with a cert from Cert:\CurrentUser\My
```

## Testing a winget install locally

This installs the extension through winget exactly as a user would, without publishing anything. It uses a
throwaway self-signed certificate and needs two admin actions (trusting the certificate, enabling local manifests).
Remove the dev registration first if you have one (`Get-AppxPackage kud3n013.WebSearchShortcut | Remove-AppxPackage`),
since both use the same package identity.

1. **Create a short-lived signing certificate** (subject must match the manifest's `Publisher`):
   ```powershell
   $cert = New-SelfSignedCertificate -Type Custom -Subject "CN=kud3n013" -KeyUsage DigitalSignature `
     -FriendlyName "WebSearchShortcut local winget test" -CertStoreLocation Cert:\CurrentUser\My `
     -NotAfter (Get-Date).AddDays(30) -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
   Export-Certificate -Cert $cert -FilePath "$env:TEMP\kud3n013-test.cer"
   ```
2. **Build, sign and generate manifests pointing at localhost:**
   ```powershell
   ./scripts/build-msix.ps1 -Platform x64 -CertificateThumbprint $cert.Thumbprint
   ./scripts/new-winget-manifest.ps1 -Version <v> -ArtifactsDir artifacts -Platform x64 -BaseUrl http://localhost:8765
   ```
3. **Trust the certificate and enable local manifests** (one elevated prompt):
   ```powershell
   sudo pwsh -NoProfile -Command "Import-Certificate -FilePath '$env:TEMP\kud3n013-test.cer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople; winget settings --enable LocalManifestFiles"
   ```
4. **Serve the package and install it with winget** (in a second terminal: `python -m http.server 8765 --bind 127.0.0.1 --directory artifacts`):
   ```powershell
   winget install --manifest winget/manifests/k/kud3n013/WebSearchShortcut/<v>
   ```
   Restart Command Palette (or run **Reload**) and the extension appears.
5. **Clean up.** A package installed from a local manifest isn't tied to a winget source, so uninstall it by its
   MSIX id (shown by `winget list WebSearchShortcut`), then remove the certificate and disable local manifests:
   ```powershell
   winget uninstall --id "MSIX\kud3n013.WebSearchShortcut_<v>.0_x64__5ncnfbw6m6sya"
   Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)"
   sudo pwsh -NoProfile -Command "Remove-Item 'Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)'; winget settings --disable LocalManifestFiles"
   ```

## Publishing to winget (once packages are signed by a trusted certificate)

1. Bump the version, build and sign: `./scripts/build-msix.ps1 -CertificateThumbprint <thumb>`.
2. Create a GitHub Release `v<version>` with `artifacts/WebSearchShortcut-<version>-x64.msix`, `-arm64.msix` and
   `SHA256SUMS.txt`.
3. Generate manifests from the uploaded files (hashes must match what users download):
   `./scripts/new-winget-manifest.ps1 -Version <version>`, then `winget validate --manifest <folder>`.
4. Submit the folder under `manifests/` in a fork of microsoft/winget-pkgs (or `wingetcreate submit <folder>`).
   Keep the `windows-commandpalette-extension` tag: Command Palette uses it to list the extension in its gallery.
   Never re-upload a changed file under an existing release; publish a new patch version instead.
