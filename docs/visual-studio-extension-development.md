# Cliq for Visual Studio — developer guide

This document explains how the Visual Studio extension is structured and how it works
internally. It complements the user-facing
[`docs/visual-studio-extension.md`](visual-studio-extension.md), which covers the preview
prerequisites, building the VSIX, installation, and the acceptance checklist.

## Solution layout

`visualstudio/Cliq.VisualStudio.sln` contains three projects plus build scripts:

| Project / script | Responsibility |
|---|---|
| `Cliq.VisualStudio` | The VSIX package: `AsyncPackage` lifecycle, commands, tool windows, dialogs, WPF controls, and the sidecar runtime host. Depends on the VS SDK. |
| `Cliq.VisualStudio.Core` | Reusable, non-UI logic: the API client and stores, authoring view models, workspace resolution, sidecar supervision, desktop handoff, and run-results plumbing. Testable without Visual Studio. |
| `Cliq.VisualStudio.Tests` | Unit tests over `Cliq.VisualStudio.Core` using fake stores (NUnit + Moq patterns in `FakeStores.cs`). |
| `build/Build-Vsix.ps1` | Deterministic staging and packaging of the self-contained payload into the VSIX. |
| `build/Test-PackagedRuntime.ps1` | End-to-end smoke test of the exact staged runtime against installed Chrome. |

`Cliq.VisualStudio` is the thin VS shell; `Cliq.VisualStudio.Core` holds nearly all the
behavior. The extension carries a **private, self-contained Cliq API and scanner runtime** inside
the VSIX payload — it does not rely on a separately installed backend or Node.js.

## Package lifecycle

`CliqPackage` (`Cliq.VisualStudio/CliqPackage.cs`) is an `AsyncPackage` (background loading,
`AllowsBackgroundLoading = true`) that:

1. creates a `CliqOutputLogger` for the "Cliq" output pane (diagnostics are sanitized before
   display);
2. creates the `CliqSidecarRuntime`, which owns the sidecar process;
3. creates the `VisualStudioContextService` (IDE context tracking) and the
   `VisualStudioCliqSession` (workspace + API access);
4. builds the view models:
   - `WorkbenchViewModel` backed by `ApiWorkbenchStore` (implements both `IWorkbenchStore` and
     `IWorkspaceConfigurationStore`),
   - `WorkspaceActionsViewModel` backed by `ApiWorkspaceActionStore` (scan/generate/run/merge
     actions),
   - `RunResultsViewModel` backed by `ApiRunStore`,
   - `MergeViewModel` for the custom-method merge UI;
5. registers commands through `CliqCommandRegistrar`;
6. subscribes to `VisualStudioContextService.ContextChanged` to re-bind the workspace when the
   active solution/folder changes (prompting to save or discard dirty edits first).

On dispose it tears down the session, stores, runtime, and context service, which terminates the
sidecar process tree.

Three tool windows are registered (`CliqToolWindows.cs`): **Cliq Workbench**, **Run Results**,
and **Pending Merges**, each hosting a WPF control (`WorkbenchControl`, `RunResultsControl`,
`MergeControl`).

## Commands and menus

Commands are declared in `Menus.vsct` and identified by `CommandIds`. The extension exposes a
top-level **Cliq** menu and toolbar under **Extensions**, plus context-menu commands that appear
dynamically when a compatible Cliq workspace, `.cliq` document, or a locator field is selected:

| Command | Placement |
|---|---|
| Open Workbench, Scan Page..., Run Selected Test, Generate Selected, Generate All, Record in Cliq Desktop, Refresh, Settings, Pending Merges... | Cliq menu + toolbar |
| Use as Cliq Workspace, Open Cliq Workbench, Generate All Cliq Artifacts | Solution context menu |
| Bind Project and Open Cliq Workbench, Open in Cliq Workbench, Run Cliq Test, Generate Cliq Artifact, Rescan Cliq Page... | Item/Project context menu |
| Scan URL in Cliq..., Locate in Browser | Editor context menu |

Command dispatch flows through `CliqCommandRegistrar` (visibility + `QueryStatus`) into
`CliqPackage.ExecuteAsync`, which routes each `CliqShellCommand` to a package method. The package
also implements `ICliqWorkbenchCommands`, the interface the `WorkbenchControl` uses to invoke
scan/generate/run operations.

## Sidecar runtime hosting

The extension never talks to the Cliq backend directly from UI code. Every API call goes through
`CliqSidecarRuntime` (`CliqRuntimeHost.cs`) → `SidecarSupervisor` (`Cliq.VisualStudio.Core/Runtime`).

### Startup handshake

`SidecarSupervisor.EnsureStartedAsync` is lazy: the sidecar process starts on the first command.
The flow is:

1. Generate a fresh random bearer token (`CreateToken`, 32 bytes, URL-safe Base64).
2. Start the API process with:
   - `--urls http://127.0.0.1:0` (random loopback port),
   - `CLIQ_API_TOKEN=<token>`,
   - `CLIQ_HOST_KIND=visualstudio`,
   - `CLIQ_NODE_PATH` and `CLIQ_PUPPETEER_SCANNER_PATH` pointing at the bundled Node and scanner
     when a packaged payload is present.
3. Read stdout until the machine-readable `CLIQ_READY {"address":"..."}` line. The published
   address must be loopback HTTP/HTTPS or startup fails.
4. Health-check `GET /health/ready` within `StartupTimeout` (default 45 s).
5. Wrap the result in a `SidecarConnection` (address + token + process id); `CreateApiClient`
   builds a `CliqApiClient` from it.

The supervisor chooses between the packaged backend executable
(`Payload/runtime/api/Cliq.Api.exe`) when it exists and a `dotnet run` of the repository's
`Cliq.Api.csproj` in development. `SidecarOptions` is built by `CliqSidecarRuntime.CreateOptions`
from the extension install directory, walking up to find the repository root for the dev fallback.

### Supervision

- **Restart on unexpected exit**: `ObserveExitAsync` clears the state and reports that the sidecar
  will restart on the next command; `EnsureStartedAsync` detects an exited process and starts a new
  one.
- **Clean shutdown**: `StopAsync` calls `KillTree()` and waits up to five seconds for the process
  tree to exit.
- **Diagnostics**: stdout/stderr lines are drained (`DrainErrorsAsync`) into the Cliq output pane,
  with the token stripped by `SidecarDiagnosticSanitizer`.
- The supervisor is exercised directly in `Cliq.VisualStudio.Tests/SidecarSupervisorTests.cs`
  with fake process/health-probe implementations.

### Packaged payload

`Build-Vsix.ps1` (details in `docs/visual-studio-extension.md`) stages:

- `Cliq.Api` published as a self-contained .NET `win-x64` executable (no single-file packaging,
  no debug symbols),
- a pinned Node.js 24 Windows x64 runtime (verified against SHA-256 and the official checksums;
  only needed files are packaged),
- the Puppeteer scanner JavaScript and production `puppeteer-core` dependencies (downloaded with
  `npm ci --omit=dev` in an isolated scratch dir, browser downloads disabled),
- license notices.

Node.js does not need to be installed by the user; nothing from the npm cache or `node_modules`
tree is committed.

## Workspace resolution and binding

Workspace discovery is the bridge between the IDE selection and the `.cliq` workspace.

- `VisualStudioContextService` captures the current IDE state into an `IdeContextSnapshot`
  (solution directory, selected project directory, selected item path, active document path,
  selected text, and the line above the selection). It raises `ContextChanged` on solution/selection
  changes.
- `WorkspacePathResolver.ResolveDefault` picks the workspace in this order: an explicit user
  override, the solution directory, an upward search for a `.cliq/project.json` from the selected
  item, the selected project directory, then an upward search from the active document.
- `WorkspacePathResolver.FindWorkspaceRoot` walks up from a file/directory until it finds
  `.cliq/project.json`.
- An explicit override (from a context command such as **Use as Cliq Workspace**) is stored per
  solution/folder keyed by `OverrideKey` (a SHA-256 of the normalized path) and re-applied by the
  context service.
- `CliqDocumentClassifier` maps a path to a Cliq entity: `.json` files under `.cliq/tests/` are
  tests, `.json` files under `.cliq/pages/` are pages, everything else is `Other`. It also
  recovers the `// Locator (...): ...` comment above a generated locator field so **Locate in
  Browser** works when the user double-clicks the field name in the generated Page Object.
- `CliqShellContext` and `CliqShellContextTests` wrap the context capture/resolve logic for the
  shell commands.

## Session and API access

`VisualStudioCliqSession` implements `ICliqApiSession` (`ApiStores.cs`). It:

- resolves the workspace root from the current IDE context (or uses the explicit override),
- lazily starts the sidecar and calls `POST /workspaces/open` once per distinct root,
  remembering the bound `_workspaceRoot` and reopening when it changes,
- exposes `CloseAsync` to release the workspace lease.

Clients get a `CliqApiClient` from the session. The API layer is in `Cliq.VisualStudio.Core/Api`:

- `CliqApiClient` — typed methods for every backend endpoint (see `docs/api.md`), bearer-token
  auth, JSON serialization via `CliqJson.Options`, and translation of Problem Details into typed
  exceptions (`CliqApiException`, `WorkspaceInUseApiException`,
  `ScanReviewConflictApiException`).
- `ApiStores.cs` — `ApiWorkbenchStore` (implements `IWorkbenchStore` and
  `IWorkspaceConfigurationStore`), used by the Workbench view models; `ApiWorkspaceActionStore`
  and `ApiRunStore` back the action view models.
- `CliqApiContracts.cs` and `CliqApiException.cs` — the client-side contract records and
  exceptions.

View models never reference HTTP or the sidecar directly; they program against `IWorkbenchStore`
and friends, which keeps them unit-testable with `FakeStores`.

## Authoring view models

The Workbench is driven by view models in `Cliq.VisualStudio.Core/Authoring`:

| View model | Purpose |
|---|---|
| `WorkbenchViewModel` | Overall workspace state: pages, tests, settings, secrets, selected entity, dirty tracking, save/reload orchestration, `WorkbenchSection` navigation. |
| `TestEditorViewModel` / `TestStepEditorViewModel` | Test case and per-step editing (action, element, value/secret, dynamic parameter, enabled, timeout). |
| `ProjectEditorViewModel` | `project.json` editing (base URL, default browser, output directories/namespaces). |
| `SettingsEditorViewModel` | `settings.json` editing (element identification strategies, test execution, scanning). |
| `SecretEntryViewModel` | Adding/updating a masked secret value. |
| `PageInspectorViewModel` | Inspecting a scanned page's elements. |
| `ScanReviewViewModel` | The staged scan-review diff: baseline vs. proposed page, per-item accept/deny decisions, apply/discard. |
| `MergeViewModel` | The custom-method merge dialog (see below). |
| `RunResultsViewModel` | Streaming run progress and results from `ApiRunStore`. |

`EditableViewModelBase`/`BindableBase` provide change tracking; the Workbench prompts via
`IWorkbenchPrompt` (real implementation uses `CliqMessageBox`; tests use
`PermissiveWorkbenchPrompt`).

## Desktop handoff (recording)

Browser recording lives in Cliq Desktop, not Visual Studio. **Record in Cliq Desktop**
(`CliqPackage.DesktopHandoff.cs`) hands the exclusive workspace lease to the desktop app:

1. Resolves the Cliq Desktop executable (from `Tools > Options > Cliq > General`, or
   `CliqDesktopDiscovery.FindExecutable`).
2. `DesktopHandoffCoordinator.HandoffAsync` closes the Visual Studio workspace lease, launches the
   desktop app with the workspace path, then polls `TryAcquireAsync` until Cliq Desktop owns the
   workspace (a 30-second acquisition timeout).
3. When the desktop app closes its workspace, Visual Studio re-acquires the lease and the Workbench
   reloads.

The lease itself is enforced server-side by `WorkspaceLeaseManager`
(`backend/src/Cliq.Infrastructure/WorkspaceLeaseManager.cs`): opening a workspace writes
`.cliq/workspace.lock` with an owner record (`hostKind`, `processId`, `acquiredAt`) and keeps an
exclusive `FileStream` for the lifetime of the session. A second host gets `409 workspace_in_use`
from `POST /workspaces/open`. `WorkspaceLeaseManager` also guards against two `WorkspaceRepository`
instances in the *same* process via a static registry keyed by the lock file path.

## Custom-method merge flow

When generation cannot safely fold hand-written `[CliqCustomMethod]` methods into the regenerated
Page Object file (see `CustomMethodRecoveryWarning` and `PendingCustomMethodMerge` in the domain
model), the file is left untouched and the methods surface in the extension's merge UI:

- `MergeViewModel` loads `PendingCustomMethodMerge` candidates from the backend.
- The user can include a candidate in the next generate (`customMethodResolutions`), discard it
  permanently (`customMethodDiscards`), or leave it deferred (persisted to the global waiting
  list, `.cliq/pending-custom-methods.json`, exposed via
  `GET /pending-custom-methods`).
- Syntax-broken candidates are shown flagged (`hasSyntaxError`) but still offered, since their
  source text is always recoverable.

`Cliq.VisualStudio.Tests/MergeViewModelTests.cs` covers the resolution/discard/defer decisions.

## Testing

Unit tests live in `Cliq.VisualStudio.Tests` and target `Cliq.VisualStudio.Core` only (no VS SDK
needed for the Core logic):

- `CliqApiClientTests` — request building, token auth, SSE parsing, Problem Details → exception
  translation.
- `SidecarSupervisorTests` — startup handshake, ready parsing, restart, shutdown, malformed output.
- `DesktopHandoffCoordinatorTests` — lease close/launch/reacquire sequencing and timeouts.
- `ScanReviewViewModelTests`, `MergeViewModelTests`, `RunResultsViewModelTests`,
  `WorkbenchAuthoringTests`, `WorkspaceActionsViewModelTests`,
  `WorkspaceConfigurationViewModelTests`, `CliqDocumentClassifierTests`, `CliqShellContextTests`,
  `WorkspaceContextTests`.

The build workflow (`.github/workflows/ci.yml`) restores and builds the solution, then
`Build-Vsix.ps1 -Target Build` stages and validates the preview VSIX on Windows; the end-to-end
runtime smoke test is `build/Test-PackagedRuntime.ps1`.

## Key file map

| File | Responsibility |
|---|---|
| `Cliq.VisualStudio/CliqPackage.cs` | Package lifecycle, command routing, view-model wiring, context-change handling. |
| `Cliq.VisualStudio/CliqRuntimeHost.cs` | `CliqOutputLogger`, `CliqSidecarRuntime`, `VisualStudioCliqSession`. |
| `Cliq.VisualStudio/CliqCommandRegistrar.cs`, `Menus.vsct`, `CommandIds.cs` | Command registration and placement. |
| `Cliq.VisualStudio/CliqPackage.*.cs` | Scan, generate, run, merge, desktop-handoff command implementations. |
| `Cliq.VisualStudio.Core/Runtime/SidecarSupervisor.cs` | Sidecar process supervision, `CLIQ_READY` handshake, health probe. |
| `Cliq.VisualStudio.Core/Runtime/SidecarContracts.cs` | `SidecarOptions`, process/health interfaces, `SidecarConnection`. |
| `Cliq.VisualStudio.Core/WorkspaceContext.cs` | `IdeContextSnapshot`, `WorkspacePathResolver`. |
| `Cliq.VisualStudio.Core/CliqDocumentClassifier.cs` | `.cliq` document classification, locator-comment recovery. |
| `Cliq.VisualStudio.Core/Api/*` | `CliqApiClient`, `ApiStores`, contracts, exceptions. |
| `Cliq.VisualStudio.Core/Authoring/*` | Workbench/editor/scan-review/merge view models. |
| `Cliq.VisualStudio.Core/Desktop/DesktopHandoffCoordinator.cs` | Desktop recording handoff. |
| `visualstudio/build/Build-Vsix.ps1` | Deterministic VSIX staging/packaging. |
