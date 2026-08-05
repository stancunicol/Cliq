# Cliq

Cliq is a local desktop workbench for discovering web elements, composing browser workflows, and
generating maintainable Selenium C# Page Objects and NUnit tests.

Cliq's embedded browser is for manual navigation while scanning a page. A test itself runs in a
separate, visible Selenium-controlled browser window (`SeleniumTestRunner`) - the same engine the
generated NUnit workflow uses for CI and external execution - so a run behaves identically whether
it's launched from Cliq or from `dotnet test`.

## What it does

Given a workspace and an HTTP or HTTPS page, Cliq can:

1. open the page in an isolated browser embedded in the Electron application, to preview it
   before scanning;
2. discover its rendered elements with Puppeteer, including pages produced by React, Angular,
   Vue, or another client-side framework;
3. choose stable locators in priority order: `data-testid` > generated `XPath` > structural CSS
   fallback (`id` is excluded as a locator, used only as a naming hint);
4. compose or edit complete workflows from scanned elements, with navigation, keyboard actions,
   and assertions;
5. replay the workflow in a separate, visible Selenium browser window, report each step, support
   cancellation, and capture a screenshot when a step fails; and
6. generate C# Page Objects and NUnit workflow tests in configurable workspace directories.

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

Step values may be literals or secret references. Secret values stay in the local workspace
secret store; generated NUnit code reads the matching `CLIQ_*` environment variable.

## Technical decisions

| Dimension | Current decision |
|---|---|
| Desktop host | Electron 43 |
| User interface | React 19, TypeScript 6, Vite 8 |
| Backend | C# / .NET 10 minimal API sidecar |
| Browser automation | Puppeteer for discovery; embedded Electron browser for manual navigation only; Selenium WebDriver for interactive replay and generated tests |
| Browsers | Chrome; Edge is additionally supported on Windows |
| Generated tests | NUnit |
| Persistence | Versioned JSON files inside the selected workspace |
| Transport | Authenticated loopback HTTP plus Server-Sent Events |

## Architecture

```text
frontend/
  electron/                    Electron main/preload processes, sidecar and embedded-browser control
  src/                         React renderer and feature UI
backend/
  src/Cliq.Domain/             Versioned workspace and run models
  src/Cliq.Application/        Use cases and infrastructure abstractions
  src/Cliq.Infrastructure/     Persistence, Puppeteer discovery, Selenium execution, secrets and C# generation
  src/Cliq.Api/                Authenticated HTTP/SSE sidecar
  tests/Cliq.UnitTests/        Backend unit tests
docs/architecture.md           Runtime, data and security boundaries
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
| Electron main process | Owns application lifecycle, the embedded (navigation-only) browser and failure screenshots |
| Preload bridge | Exposes a small validated API to the React renderer |
| React renderer | Workspace navigation, test editing, browser controls and run results |
| `Cliq.Api` | Authenticated adapter for workspace, generation, secret and run operations |
| `Cliq.Application` | Coordinates use cases without depending on the UI or persistence details |
| `WorkspaceRepository` | Reads and atomically writes versioned workspace JSON |
| `PuppeteerPageDiscovery` | Scans the live DOM and produces stable page definitions |
| `SeleniumTestRunner` | Replays a test's enabled steps in a visible, non-headless browser window |
| `WorkspaceCodeGeneration` | Emits Page Objects (preserving `[CliqCustomMethod]`-marked methods) and NUnit workflows |

### Discovery vs. execution: Puppeteer and Selenium

Cliq uses two automation stacks for two different jobs, not one for everything:

| | Puppeteer | Selenium |
|---|---|---|
| Used for | Scanning a page into a `PageDefinition` | Interactive replay and every generated NUnit test |
| Why this engine | Runs as a standalone Node script (`backend/tools/puppeteer-scanner`) that evaluates extraction JS directly in-page, so element discovery has no WebDriver round-trip per candidate and no separate C# DOM-walking implementation to keep in sync |
| Why not the other job | Never drives a real test run - generated Page Objects are Selenium C#, so scanning with Selenium would mean maintaining locator-building logic twice, once in Node and once in C#. Puppeteer is Chromium-only and isn't used for execution at all |
| Browsers | Chromium only (bundled with Puppeteer) | Chrome and Edge, matching `.cliq/project.json`'s `defaultBrowser` |

Discovery used to be Selenium-based too; it moved to Puppeteer so scanning gets the extraction
logic's benefits (in-page JS evaluation, faster iteration) without touching the execution engine at
all - `SeleniumTestRunner` and generated tests were untouched by that change.

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
    CliqCustomMethodAttribute.cs
    ElementWrapper.cs
  Tests/
    <TestName>Tests.cs
```

The directories and namespaces are configurable in `.cliq/project.json`. Each `<PageName>Page.cs`
is a single file, replaced on every generation. A hand-written method survives regeneration only
if it's marked with `[CliqCustomMethod]` (defined once in the generated `CliqCustomMethodAttribute.cs`,
shared by every Page Object in the namespace); anything else in the file is regenerated content and
is not preserved. If fewer marked methods come back out than the previous file's marker count
suggests it should, generation backs that file up under `.cliq/runs/custom-method-backups/` and
reports it as a warning instead of silently dropping the difference.

Fill/Select/multi-select elements are exposed as `ElementWrapper<TData>` properties (defined once
in the generated `ElementWrapper.cs`) instead of plain methods - `page.Username.Upload("ana")` /
`page.Username.Read()` - so the locator and its associated data stay together. `TData` is `string`
by default, or `int`/`DateTime` for a Fill element when the scanned `<input type>` implies it
(`number`/`range` → `int`; `date`/`datetime-local`/`month`/`week` → `DateTime`). Click, assert,
checkbox, and radio elements have no associated data and stay as plain methods.

Every element's locator (except radio, whose selector depends on a runtime value) is a single
`private static readonly By {Name}Locator` field, built once instead of being reconstructed inline
each time a method or wrapper runs. Hand-written `[CliqCustomMethod]` methods can reference these
fields directly instead of duplicating the selector.

Test steps reference stable page and element IDs instead of copying selectors, produced by
scanning a page into a `PageDefinition`.

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
4. Use **Scan** to build a page definition.
5. Add steps referencing the scanned elements; review, duplicate, enable, disable, or edit them.
6. Run the test to watch it execute in a separate, visible Selenium browser window.
7. Generate the Page Objects and NUnit tests into the workspace.

## Validation

Frontend:

```bash
cd frontend
npm run lint
npm run typecheck
npm test
npm run build
npm audit --audit-level=high
```

Backend:

```bash
cd backend
dotnet restore Cliq.slnx --locked-mode
dotnet build Cliq.slnx --configuration Release --no-restore
dotnet test Cliq.slnx --configuration Release --no-build
dotnet format Cliq.slnx --verify-no-changes --no-restore
```

CI runs the frontend and backend checks independently (see `.github/workflows/ci.yml`).

## Security model

- The sidecar binds to loopback only, uses a random port, and requires a random per-launch bearer
  token.
- The token comparison is fixed-time, CORS is not enabled, and the renderer can reach the backend
  only through validated IPC.
- Remote pages are sandboxed and cannot request permissions, download files, open native windows,
  or navigate to unsupported protocols.
- Workspace identifiers and generated output paths are validated to prevent traversal outside the
  selected folder.
- Sensitive step values persist as references; their values remain local and are excluded
  from Git.

Cliq intentionally opens user-selected web targets. Treat those pages as untrusted and use test
accounts rather than production credentials.

## Current scope

This repository delivers a functional local proof of concept for Windows and macOS. The current
scope includes workspace persistence, live DOM discovery, visible Selenium-driven replay, failure
reporting, and C#/NUnit generation.

Installer packaging, code signing and notarization, a Visual Studio extension, AI-assisted
generation, licensing, and cloud services are not implemented.

## License

Cliq is available under the terms in [`LICENSE`](LICENSE).
