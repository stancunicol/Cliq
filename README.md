# Cliq

Cliq is a local Electron workbench for discovering web elements, recording browser workflows,
running those workflows with Selenium, and generating maintainable C# Page Objects and NUnit
tests.

## Repository layout

- `Cliq-ui/` - Electron main/preload processes and the React renderer.
- `Untitled/` - .NET 10 domain, application, infrastructure, API, and tests.
- `docs/architecture.md` - process, security, data, and dependency boundaries.

The product brief in `project-details/` is intentionally local and excluded from Git.

## Prerequisites

- Node.js 24 LTS and npm
- .NET 10 SDK
- Google Chrome; Microsoft Edge is additionally supported on Windows

## Development

Install frontend dependencies:

```bash
cd Cliq-ui
npm ci
```

Start the application:

```bash
npm run dev
```

Electron launches the .NET API automatically on a random loopback port. The renderer never
receives the API bearer token and communicates through the sandboxed preload bridge.

## Validation

Frontend:

```bash
cd Cliq-ui
npm run lint
npm run typecheck
npm test
npm run build
npm audit --audit-level=high
```

Backend:

```bash
cd Untitled
dotnet build Cliq.slnx --configuration Release
dotnet test Cliq.slnx --configuration Release --no-build
dotnet format Cliq.slnx --verify-no-changes --no-restore
```

## Workspace files

Opening a folder creates a versioned `.cliq/` workspace:

```text
.cliq/
  project.json
  pages/
  tests/
  runs/                 # ignored
  secrets.local.json    # ignored
```

Page definitions and test specifications are suitable for source control. Runs, screenshots,
caches, and local secret values are ignored. Generated source defaults to
`Generated/PageObjects/` and `Generated/Tests/`; both locations and namespaces are configurable
in `.cliq/project.json`.

Sensitive recorded fields are stored as secret references. Cliq resolves those references from
`.cliq/secrets.local.json`; generated NUnit code reads the corresponding `CLIQ_*` environment
variable.

## Current scope

This repository delivers a functional local proof of concept for Windows and macOS. Installer
packaging, signing/notarization, Visual Studio extensions, AI integration, licensing, and cloud
services are intentionally outside the current scope.
