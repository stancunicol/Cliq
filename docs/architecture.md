# Cliq architecture

## Runtime boundary

Electron is the desktop host. At startup its main process:

1. creates a random per-launch bearer token;
2. starts `Cliq.Api` on `127.0.0.1:0`;
3. waits for the machine-readable `CLIQ_READY` message and health check;
4. proxies validated domain operations through the sandboxed preload bridge; and
5. terminates the sidecar when the application quits.

The renderer has no filesystem or process access and never receives the sidecar address or token.
Remote pages run in a separate `WebContentsView` with their own persistent session, denied
permissions/downloads, sandboxing, context isolation, and restricted navigation.

## Backend layers

- `Cliq.Domain` defines versioned workspace records and run states.
- `Cliq.Application` owns use cases and infrastructure abstractions.
- `Cliq.Infrastructure` implements workspace persistence, Puppeteer-based page discovery,
  Selenium test execution, secrets, screenshots, and C#/NUnit generation.
- `Cliq.Api` is a thin authenticated HTTP/SSE adapter with RFC Problem Details.

Page discovery runs as a background Puppeteer operation: `PuppeteerPageDiscovery` shells out to a
standalone Node script (`backend/tools/puppeteer-scanner`) that drives a headless/headed browser,
extracts candidate elements, and hands back the same `PageDefinition`/locator model the app has
always used. Interactive test runs no longer execute inside Electron's `WebContentsView` at all -
that view is navigation-only now (`BrowserController`, used while scanning). `SeleniumTestRunner`
launches a separate, non-headless Selenium browser window for every run, the same engine generated
NUnit workflows use for CI and external execution, so a step behaves identically live and in CI.
Live-recording (capturing clicks/input from the embedded browser into test steps) was removed
entirely; steps are now composed by hand against scanned elements. The main process still owns one
active run, cancellation, failure screenshots, and persistence through the authenticated sidecar.

## Workspace model

`project.json`, `pages/*.json`, `tests/*.json`, and `settings.json` carry `schemaVersion: 1`.
Writes are atomic and all configured output paths are checked to remain inside the open workspace.

Automatic DOM scans create the `PageDefinition` and its stable locator model. Locator priority - by default `data-testid`, then a generated XPath (label, ancestor test-id,
own text/attribute, or nearest-id-ancestor based), then a structural CSS fallback - is configurable
per workspace via `settings.json`'s `elementIdentification` section (custom test-id attribute,
strategy order), validated on save. `id` is deliberately excluded from locator strategies -
framework-generated ids are too often volatile - and is only ever used as a naming hint.
`settings.json` also carries `testExecution` (step/page-ready timeouts, failure-screenshot toggle)
and `scanning` (load/network-idle timeouts, screenshot toggle).

Re-scanning a page reconciles against its previous `PageDefinition` (`ElementReconciler`): elements
still present (matched by test id, then xpath, then an exact css-value match, in that order - no
positional or other fuzzy fallback beyond these) keep their original `Id` so existing test
references stay valid; anything that matches none of the three tiers is reported as a new element,
with its previous counterpart kept and flagged `NotFound` rather than silently dropped. The scan
response reports matched/new/missing counts, and - only when there was a previous scan to diff
against and something actually changed - a locators-only `LocatorDiffReport` (new/missing entries,
each with tag, name and locator) is written to `.cliq/runs/diffs/<pageId>_<timestamp>.json` and
returned alongside the scan result. This replaces self-healing: at test-run time, if a saved
locator stops resolving, the step simply fails with the Selenium timeout, and the next scan's diff
report is how a stale locator gets surfaced for a human to fix.

Test steps reference page and element IDs rather than duplicating selectors; saving a test
validates that every element-touching step's page/element reference actually exists, so a broken
reference is caught at save time rather than discovered mid-run. Generation emits one Page Object
class per page plus an NUnit workflow per test, and a shared `CliqCustomMethodAttribute.cs` once
per namespace. Every generation run replaces the Page Object file outright, but first extracts any
method marked `[CliqCustomMethod]` from the existing file (`CustomMethodExtractor`, a
comment/string-aware brace scan, not a full C# parse) and re-emits it verbatim at the end of the
new one - that attribute is the only thing that survives regeneration; anything else hand-added
without it is replaced like the rest of the file.

## Security posture

- API binding is loopback-only with a random port and fixed-time bearer-token comparison.
- CORS is not enabled; renderer-to-backend access is main-process mediated.
- IPC inputs are validated before entering the backend.
- Remote pages cannot request permissions, open native windows, download files, or navigate to
  unexpected protocols.
- Workspace identifiers and output paths are validated against traversal.
- Sensitive inputs persist only references; local values and run artifacts are ignored by Git.

The application intentionally accepts user-selected HTTP/HTTPS targets for discovery and testing.
Users should treat those targets as untrusted and avoid opening a workspace containing code they
do not intend Cliq to generate or update.
