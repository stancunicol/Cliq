# Cliq for Visual Studio preview

Cliq for Visual Studio is an unsigned Windows x64 preview extension for Visual Studio 17.0 and later, including Visual Studio 2026. It hosts native Cliq tooling in the IDE and carries a private, self-contained Cliq API and scanner runtime. The payload does not include a browser; Microsoft Edge or Google Chrome must already be installed.

## Prerequisites

- Windows x64.
- Visual Studio 2026 with the **Visual Studio extension development** workload for development and experimental-instance testing.
- .NET 10 SDK.
- Windows PowerShell 5.1 or PowerShell 7.
- Microsoft Edge or Google Chrome for scanning and test execution.

Node.js does not need to be installed. The build downloads the pinned official Node.js 24.18.0 Windows x64 archive into the local application-data build cache, verifies it against both the pinned SHA-256 and the official `SHASUMS256.txt`, and packages only the required runtime files.

## Build the preview VSIX

From a PowerShell prompt at the repository root:

```powershell
.\visualstudio\build\Build-Vsix.ps1 -Target Build -Configuration Release
```

The script performs these deterministic staging steps:

1. Restores and publishes `Cliq.Api` as a self-contained .NET 10 `win-x64` application without single-file packaging or debug symbols.
2. Retrieves the pinned Node distribution only when it is absent from the local cache and verifies its official checksum.
3. Runs `npm ci --omit=dev` in an isolated scratch directory with all Puppeteer browser downloads disabled.
4. Stages scanner JavaScript, production `puppeteer-core` dependencies, Node, the API, and license notices into the VSIX.
5. Builds the extension and validates the finished archive.

Payload and scratch directories are under ignored `obj` paths. No downloaded binary, `node_modules` tree, npm cache, or browser is committed. The script ends by printing the VSIX path, archive size, entry count, and SHA-256.

Building `Cliq.VisualStudio.csproj` directly also stages the payload through its MSBuild integration:

```powershell
dotnet build .\visualstudio\Cliq.VisualStudio\Cliq.VisualStudio.csproj --configuration Release
```

To validate an existing archive independently:

```powershell
.\visualstudio\build\Build-Vsix.ps1 -Target Validate -VsixPath C:\path\to\Cliq.VisualStudio.vsix
```

Validation requires the extension assembly and manifest, API executable, Node executable, scanner entry point, `puppeteer-core`, and license notices. It rejects secrets, token assignments, repository absolute paths, Node/npm caches, and Chromium/Chrome payloads.

To smoke-test the exact staged runtime with installed Chrome (a visible browser window
opens briefly for the Selenium run):

```powershell
.\visualstudio\build\Test-PackagedRuntime.ps1 -Browser chrome
```

This opens a temporary workspace, performs a headless page scan, generates artifacts,
runs and cancels tests, retrieves a failure screenshot, closes the workspace lease, and
terminates the private sidecar.

## Install and uninstall

Close all Visual Studio instances before installing or removing the preview.

To install, double-click the generated `.vsix`, select the intended Visual Studio installation in VSIX Installer, and choose **Install**. Start Visual Studio and open **Extensions > Manage Extensions > Installed** to confirm that **Cliq for Visual Studio** is enabled.

Open the extension commands from **Extensions > Cliq**. Visual Studio groups extension-owned top-level menus beneath **Extensions**, so **Open Workbench**, scan, generation, run, recording, and settings commands are found there. Toolbar and context-menu commands appear when a compatible Cliq workspace is active.

To uninstall, close Visual Studio, open **Visual Studio Installer > More > Modify > Installed extensions** or launch **Extensions > Manage Extensions** from Visual Studio, select **Cliq for Visual Studio**, and choose **Uninstall**. Complete the removal after Visual Studio closes.

Because the preview is unsigned, Windows or organizational security policy may require explicit approval. Production distribution, Marketplace publication, and signing are not part of this preview.

## Experimental-instance workflow

1. Open `visualstudio\Cliq.VisualStudio.sln` in Visual Studio 2026.
2. Set `Cliq.VisualStudio` as the startup project.
3. Ensure the configuration is `Debug`.
4. Press **F5**. Visual Studio launches an experimental instance with an isolated extension profile.
5. Open a representative Cliq solution or folder and exercise the checklist below.
6. Close the experimental instance and verify that its private `Cliq.Api.exe`, `node.exe`, and any child browser processes exit.

If the experimental instance needs to be reset, close it and run the Visual Studio `CreateExpInstance.exe /Reset` utility for the active suffix before pressing F5 again.

## Preview acceptance checklist

- **Extensions > Cliq**, the toolbar, and scoped context commands appear only in their intended contexts.
- Solution and open-folder workspaces bind correctly, including an explicit project/folder override.
- Cliq Workbench opens, follows light/dark themes, supports keyboard navigation and DPI scaling, and prompts for dirty edits.
- Tests, pages, settings, and masked secrets can be edited and explicitly saved.
- Page scan uses installed Edge or Chrome, supports progress and cancellation, and runs the interactive staged scan review workflow when page differences are detected.
- Selected and full generation refreshes files and can open or reveal artifacts without editing legacy project files.
- A visible run streams steps and timings, shows failures and screenshots, and cancels cleanly.
- Recording hands the exclusive workspace lease to Cliq Desktop and Visual Studio reacquires it after Desktop exits.
- Two Visual Studio instances use independent authenticated sidecars and random ports.
- Closing Visual Studio terminates the sidecar process tree.
- The Release VSIX passes `Build-Vsix.ps1 -Target Validate`.
- Installation, upgrade, disablement, and uninstall complete without files being left locked.

## Releasing

Releases are driven by a semantic-version tag. Pushing `v0.2.0` to GitHub runs the
`.github/workflows/release.yml` pipeline, which blocks until every stage passes:

1. Restores, builds, and tests the self-contained `Cliq.Api` payload.
2. Builds and validates the VSIX with the version taken from the tag, stamping the
   `<Identity Version>` in `source.extension.vsixmanifest` (the tag is the single
   source of truth; the checked-in manifest version is used for local development).
3. Smoke-tests the exact packaged runtime with `Test-PackagedRuntime.ps1`
   (scan, generate, run, failure screenshot, cancel, lease release).
4. Creates a GitHub Release titled `Cliq for Visual Studio <version>` and attaches
   `Cliq.VisualStudio-<version>.vsix` plus its SHA-256 checksum. The release notes
   come from `RELEASE-NOTES.txt`.

Use a three-part version tag such as `v0.2.0`; other tag formats are rejected. The
VSIX remains unsigned, so Windows may require explicit approval on install.

## Preview limitations

- Windows x64 only.
- An installed Edge or Chrome browser is required.
- Browser recording remains in Cliq Desktop rather than Visual Studio.
- The VSIX is unsigned and is not published to Visual Studio Marketplace.
- Generic external-action providers, AI features, and Roslyn analysis are outside this preview.
- Visual Studio 2026 is the primary acceptance environment; Visual Studio 2022 receives basic compatibility smoke testing only.
