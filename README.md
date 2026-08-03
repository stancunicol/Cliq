# Cliq

Cliq is a local desktop workbench for discovering web elements, recording and visibly replaying
browser workflows, and generating maintainable Selenium C# Page Objects and NUnit tests.

Interactive runs execute inside Cliq's embedded browser, where each action is highlighted and
paced so the workflow remains visible. Generated NUnit workflows use Selenium WebDriver for CI
and external execution.

## What it does

Given a workspace and an HTTP or HTTPS page, Cliq can:

1. open the page in an isolated browser embedded in the Electron application;
2. discover its rendered elements with Selenium, including pages produced by React, Angular,
   Vue, or another client-side framework;
3. choose stable locators in priority order: `data-testid` > generated `XPath` > structural CSS
   fallback (`id` is excluded as a locator, used only as a naming hint);
4. record clicks, text entry, and selections as reusable test steps;
5. compose or edit complete workflows with navigation, keyboard actions, and assertions;
6. replay the workflow visibly, report each step, support cancellation, and capture a screenshot
   when a step fails; and
7. generate partial C# Page Objects and NUnit workflow tests in configurable workspace
   directories.

Discovery and generation are rule-based. There is no AI component in the current pipeline.

### Supported workflow actions

| Action | Purpose |
|---|---|
| `navigate` | Open an HTTP or HTTPS URL |
| `click` | Click a discovered element |
| `fill` | Clear a field and enter a value |
| `select` | Choose a select or radio value |
| `pressKey` | Send a key or text value to an element |
| `assertVisible` | Wait for an element and verify that it is displayed |
| `assertText` | Compare an element's text with an expected value |
| `assertUrl` | Match the current URL against a regular expression |

Recorded values may be literals or secret references. Secret values stay in the local workspace
secret store; generated NUnit code reads the matching `CLIQ_*` environment variable.

## Technical decisions

| Dimension | Current decision |
|---|---|
| Desktop host | Electron 43 |
| User interface | React 19, TypeScript 6, Vite 8 |
| Backend | C# / .NET 10 minimal API sidecar |
| Browser automation | Embedded Electron browser for visible replay; Selenium WebDriver for discovery and generated tests |
| Browsers | Chrome; Edge is additionally supported on Windows |
| Generated tests | NUnit |
| Persistence | Versioned JSON files inside the selected workspace |
| Transport | Authenticated loopback HTTP plus Server-Sent Events |

## Architecture

```text
frontend/
  electron/                    Electron main/preload processes, sidecar and browser control
  src/                         React renderer and feature UI
  scripts/                     End-to-end smoke tests
backend/
  src/Cliq.Domain/             Versioned workspace and run models
  src/Cliq.Application/        Use cases and infrastructure abstractions
  src/Cliq.Infrastructure/     Persistence, Selenium, secrets and C# generation
  src/Cliq.Api/                Authenticated HTTP/SSE sidecar
  tests/Cliq.UnitTests/        Backend unit tests
docs/architecture.md           Runtime, data and security boundaries
test-fixtures/                 Deterministic pages and workspaces used by E2E tests
```

Electron is the desktop host. At startup, the main process:

1. creates a random bearer token for that launch;
2. starts `Cliq.Api` on a random loopback port;
3. waits for its machine-readable ready message and health check;
4. validates renderer requests and proxies them through the sandboxed preload bridge; and
5. terminates the sidecar when Cliq exits.

The renderer never receives the sidecar address or token and has no direct filesystem or process
access. Remote pages run in a separate sandboxed `WebContentsView`, isolated from the application
UI.

### Main component responsibilities

| Component | Responsibility |
|---|---|
| Electron main process | Owns application lifecycle, the embedded browser, recording, visible replay and failure screenshots |
| Preload bridge | Exposes a small validated API to the React renderer |
| React renderer | Workspace navigation, test editing, browser controls and run results |
| `Cliq.Api` | Authenticated adapter for workspace, generation, secret and run operations |
| `Cliq.Application` | Coordinates use cases without depending on the UI or persistence details |
| `WorkspaceRepository` | Reads and atomically writes versioned workspace JSON |
| `SeleniumPageDiscovery` | Scans the live DOM and produces stable page definitions |
| `WorkspaceCodeGeneration` | Emits Page Objects, protected custom partials and NUnit workflows |

The detailed runtime and security boundaries are documented in
[`docs/architecture.md`](docs/architecture.md).

## Workspace format

Opening a folder creates a `.cliq/` directory:

```text
.cliq/
  project.json
  pages/
    <page-id>.json
  tests/
    <test-id>.json
  runs/                         # ignored by Git
  secrets.local.json            # ignored by Git
```

`project.json`, page definitions, and test cases currently use `schemaVersion: 1`. Those files
are suitable for source control. Runs, screenshots, temporary files, and local secret values are
ignored by the workspace's own `.gitignore`.

Generated source defaults to:

```text
Generated/
  PageObjects/
    <PageName>Page.cs
    <PageName>Page.Custom.cs
  Tests/
    <TestName>Tests.cs
```

The directories and namespaces are configurable in `.cliq/project.json`. Generated `.cs` files
may be replaced on the next generation. A matching `.Custom.cs` partial is created only once, so
hand-written extensions survive regeneration.

Test steps reference stable page and element IDs instead of copying selectors. Automatic scans
and browser recording both produce the same `PageDefinition` model.

## Prerequisites

- Node.js 24 LTS and npm
- .NET 10 SDK
- Google Chrome
- Microsoft Edge when using Edge on Windows

The exact Node and .NET versions are declared in `frontend/.nvmrc`, `frontend/package.json`, and
`backend/global.json`.

## Development

Install the frontend dependencies:

```bash
cd frontend
npm ci
```

Start the application:

```bash
npm run dev
```

Vite starts the renderer and Electron launches the .NET sidecar automatically. The API is not
intended to be started manually during normal desktop development because Electron supplies its
per-launch token and port configuration.

### Typical workflow

1. Start Cliq and choose a workspace folder.
2. Enter an HTTP or HTTPS URL in the embedded browser.
3. Create or select a test.
4. Use **Scan** to build a page definition, or enable **Record** and interact with the page.
5. Review, duplicate, enable, disable, or edit the resulting steps.
6. Run the test to watch the workflow execute inside Cliq.
7. Generate the Page Objects and NUnit tests into the workspace.

## Validation

Frontend:

```bash
cd frontend
npm run lint
npm run typecheck
npm test
npm run build
npm run test:e2e
npm audit --audit-level=high
```

The E2E smoke test serves a deterministic local fixture, scans it with Selenium, records and
replays a workflow inside Electron, regenerates the Page Object/NUnit artifacts, and executes the
generated NUnit test in Chrome.

Backend:

```bash
cd backend
dotnet restore Cliq.slnx --locked-mode
dotnet build Cliq.slnx --configuration Release --no-restore
dotnet test Cliq.slnx --configuration Release --no-build
dotnet format Cliq.slnx --verify-no-changes --no-restore
```

CI runs the frontend, backend, and Linux E2E checks independently. Contribution and repository
naming conventions are in [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Security model

- The sidecar binds to loopback only, uses a random port, and requires a random per-launch bearer
  token.
- The token comparison is fixed-time, CORS is not enabled, and the renderer can reach the backend
  only through validated IPC.
- Remote pages are sandboxed and cannot request permissions, download files, open native windows,
  or navigate to unsupported protocols.
- Workspace identifiers and generated output paths are validated to prevent traversal outside the
  selected folder.
- Sensitive recorded fields persist as references; their values remain local and are excluded
  from Git.

Cliq intentionally opens user-selected web targets. Treat those pages as untrusted and use test
accounts rather than production credentials.

## Current scope

This repository delivers a functional local proof of concept for Windows and macOS. The current
scope includes workspace persistence, live DOM discovery, interaction recording, visible
in-application replay, failure reporting, and C#/NUnit generation.

Installer packaging, code signing and notarization, a Visual Studio extension, AI-assisted
generation, licensing, and cloud services are not implemented.

## License

Cliq is available under the terms in [`LICENSE`](LICENSE).
