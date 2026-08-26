# Cliq sidecar API reference

`Cliq.Api` is the authenticated loopback HTTP sidecar that every Cliq host (the Electron desktop
app and the Visual Studio extension) talks to. It is a thin adapter over the workspace
repository, page discovery, test execution, and code generation services defined in
`Cliq.Application` and implemented in `Cliq.Infrastructure`.

This document describes the complete HTTP/SSE surface as implemented in
`backend/src/Cliq.Api`. All request and response shapes come from the domain records in
`Cliq.Domain.Models`, serialized with camelCase property names.

## Hosting model

- The sidecar is started by its host on `http://127.0.0.1:0` (a random loopback port).
- Every launch gets a fresh random bearer token, passed as the `CLIQ_API_TOKEN` environment
  variable. The sidecar refuses to start without it.
- After startup the sidecar writes one machine-readable line to stdout:
  `CLIQ_READY {"address":"http://127.0.0.1:PORT/"}` and the host then health-checks it.
- The renderer (Electron) never receives the address or token; only the Electron main process
  and the Visual Studio extension hold them. All API calls are mediated by those hosts.
- CORS is **not** enabled. The API is intended for loopback, host-mediated clients only.

## Authentication

Every route except `/health/*` and `/swagger*` requires:

```
Authorization: Bearer <token>
```

The comparison is fixed-time (`CryptographicOperations.FixedTimeEquals`). Requests without a
matching token receive `401` with an RFC 7807 Problem Details body:

```json
{
  "status": 401,
  "title": "Unauthorized",
  "detail": "A valid sidecar token is required."
}
```

## Base URL and conventions

All application routes are grouped under `/api/v1`. Health routes live at the root.

- JSON requests/responses use camelCase (`pageId`, `strategyOrder`).
- Enum values are serialized as camelCase strings:
  - `BrowserKind`: `chrome`, `edge`
  - `LocatorStrategy`: `testId`, `name`, `css`, `xpath`
  - `StepAction`: `navigate`, `click`, `fill`, `select`, `pressKey`, `assertVisible`,
    `assertText`, `assertUrl`
  - `RunStatus`: `queued`, `running`, `passed`, `failed`, `cancelled`
  - `ScanReviewDecision`: `wait`, `accept`, `deny`
  - `ScanReviewChangeKind`: `new`, `missing`
  - `ScanReviewStatus`: `pending`, `applied`, `discarded`, `superseded`
- Successful writes that return no body use `204 No Content`.
- Errors use [RFC 7807 Problem Details](https://www.rfc-editor.org/rfc/rfc7807) with a `status`,
  `title`, `detail`, and, for a few conflict types, extensions (`code`, `owner`, `references`).

### Error mapping

The sidecar maps exceptions to status codes in `ApiErrors.cs`:

| Exception | Status | Extensions |
|---|---|---|
| `ScanReviewConflictException` | 409 | `code` (prefix `scan_review_`), `references` (when test steps still reference an element) |
| `WorkspaceInUseException` | 409 | `code: "workspace_in_use"`, `owner` (`{ hostKind, processId, acquiredAt }`) |
| `WorkspaceNotOpenException` | 409 | |
| `RunAlreadyActiveException` | 409 | |
| `WorkspaceValidationException`, `ArgumentException` | 400 | |
| `DirectoryNotFoundException`, `KeyNotFoundException` | 404 | |
| Anything else | 500 | `title: "Internal server error"` |

A `WorkspaceValidationException` is raised for invalid identifiers, output paths that escape the
workspace, and element/reference validation failures (for example a saved test step whose
page/element reference no longer exists).

## Endpoints

### Health (unauthenticated)

#### `GET /health/live`

Liveness probe. Returns `200`:

```json
{ "status": "live" }
```

#### `GET /health/ready`

Readiness probe. Returns `200`:

```json
{ "status": "ready" }
```

### Workspace

#### `POST /workspaces/open`

Opens a folder as the active Cliq workspace, creating the `.cliq/` structure if needed. Acquires
an exclusive workspace lease; fails with `409 workspace_in_use` if another host process already
holds it.

Request body:

```json
{ "rootPath": "C:\\dev\\my-app" }
```

Response `200` — a `WorkspaceSnapshot`:

```json
{
  "rootPath": "C:\\dev\\my-app",
  "project": { "schemaVersion": 1, "id": "...", "name": "my-app", "baseUrl": "https://...", "defaultBrowser": "chrome", "output": { "pageObjectsDirectory": "Generated/PageObjects", "testsDirectory": "Generated/Tests", "pageObjectsNamespace": "Generated.PageObjects", "testsNamespace": "Generated.Tests" }, "updatedAt": "2026-08-18T10:00:00Z" },
  "pages": [],
  "tests": []
}
```

#### `POST /workspaces/close`

Releases the current workspace and its lease. Returns `204`.

#### `GET /workspace`

Returns the current `WorkspaceSnapshot` (same shape as above). Requires an open workspace;
fails with `409` when none is open.

### Project

#### `GET /project`

Returns the current `ProjectManifest` (the `project` object inside the snapshot).

#### `PUT /project`

Replaces the project manifest. Request body is the full `ProjectManifest`; the saved record is
returned with a refreshed `updatedAt`.

### Settings

#### `GET /settings`

Returns the current `CliqSettings`:

```json
{
  "schemaVersion": 1,
  "elementIdentification": { "testIdAttribute": "data-testid", "strategyOrder": ["testId", "xpath", "css"], "ignoreIdPatterns": [] },
  "testExecution": { "defaultStepTimeoutMs": 5000, "pageReadyTimeoutMs": 15000, "captureScreenshotOnFailure": true, "highlightPauseMs": 400 },
  "scanning": { "loadTimeoutMs": 30000, "networkIdleGraceMs": 2000, "captureScreenshot": false }
}
```

#### `PUT /settings`

Validates and saves `CliqSettings`. Invalid settings (for example an unknown strategy order or a
blank test-id attribute) fail with `400`. Returns the saved record.

### Pages

#### `GET /pages`

Lists all page definitions in the workspace.

#### `GET /pages/{id}`

Returns one `PageDefinition`, or `404` if the page does not exist.

A `PageDefinition`:

```json
{
  "schemaVersion": 1,
  "id": "page-1",
  "name": "Login",
  "url": "https://app.example/login",
  "elements": [
    {
      "id": "elem-1",
      "name": "Username",
      "tag": "input",
      "action": "fill",
      "locator": { "strategy": "testId", "value": "username", "displayName": "Username" },
      "expectedText": null,
      "notFound": false,
      "dataType": "string",
      "multiple": false,
      "checkbox": false
    }
  ],
  "updatedAt": "2026-08-18T10:00:00Z",
  "dynamicElements": []
}
```

`PageElement` field notes:

- `action` is one of the `StepAction` values.
- `locator.strategy` is one of `testId`, `name`, `css`, `xpath`; `id` is never used as a locator
  strategy (only as a naming hint).
- `dataType` is only meaningful for `fill` elements (`string`/`int`/`DateTime`, inferred from the
  scanned `<input type>`).
- `multiple` is only meaningful for `select` (a `<select multiple>` was scanned).
- `checkbox` is only meaningful for `click` (a checkbox was scanned).
- `notFound` marks an element from a previous scan that the latest reconciliation could not match.

`DynamicElement` (a user-authored locator for a repeating element):

```json
{
  "id": "de-1",
  "name": "Row Edit Button",
  "action": "click",
  "strategy": "xpath",
  "locatorTemplate": "//tr[{param}]/td[2]/button",
  "parameterType": "int",
  "dataType": "string"
}
```

#### `PUT /pages/{id}`

Saves a page definition. The request body's `id` must equal the route `{id}`, otherwise `400`
("Page ID mismatch."). Returns the saved page.

#### `DELETE /pages/{id}`

Deletes the page definition. Returns `204`.

#### `GET /pages/{id}/generated-path`

Returns the absolute path the Page Object for this page would be generated to:

```json
{ "path": "C:\\dev\\my-app\\Generated\\PageObjects\\LoginPage.cs" }
```

`404` if the page does not exist.

#### `POST /pages/scan`

Scans a URL and **immediately saves** the resulting page definition (the legacy,
compatibility route used by the current desktop and Visual Studio clients).

Request body:

```json
{ "url": "https://app.example/login", "name": "Login", "browser": "chrome", "headless": true }
```

`browser` and `headless` are optional; `browser` defaults to the project's `defaultBrowser`.

Response `200` — a `ScanPageResponse`:

```json
{
  "page": { "schemaVersion": 1, "id": "page-1", "name": "Login", "url": "https://app.example/login", "elements": [], "updatedAt": "..." },
  "reconciliation": { "matchedCount": 0, "newCount": 12, "missingCount": 0, "isFirstScan": true },
  "locatorDiff": null,
  "dynamicElementSuggestions": [
    { "name": "Row Edit Button", "action": "click", "strategy": "xpath", "locatorTemplate": "//tr[{param}]/td[2]/button", "sampleCount": 3 }
  ],
  "screenshotPath": null
}
```

- `locatorDiff` is a `LocatorDiffReport` (`{ pageId, scannedAt, new: [...], missing: [...] }`),
  present only when there was a previous scan and something actually changed. It is also written
  to `.cliq/runs/diffs/<pageId>_<timestamp>.json`.
- `dynamicElementSuggestions` are repeating-row candidates for the user to accept or ignore.
- `screenshotPath` is a bare file name, present only when `scanning.captureScreenshot` is enabled.

#### `POST /pages/{id}/elements/{elementId}/locate`

Opens a visible browser, navigates to the page, locates the element, and highlights it. The
browser is deliberately left open afterward for visual inspection. Request body is a
`StartRunRequest` (`{ "browser": "chrome" }`, optional). Returns `204`. `404` if the page does
not exist; `400` (workspace validation) if the element cannot be found or located.

#### `POST /pages/locate-from-source`

Same locate behavior, but resolves the page and element from a generated Page Object's class
name and the `// Locator (...): ...` comment selected in its source file — the entry point for
"Locate in Browser" from an editor. Request body:

```json
{ "className": "LoginPage", "locatorComment": "// Locator (xpath): //input[@id='username']", "browser": "chrome" }
```

Returns `204`.

#### `GET /pages/discovery-screenshots/{fileName}`

Returns the PNG scan screenshot stored under `.cliq/runs/discovery/`, or `404`.

### Scan reviews (staged rescan)

The staged scan-review workflow is the non-destructive alternative to `POST /pages/scan`: a
changed scan is captured as a *proposal* against a *baseline*, and the active page is not written
until the user resolves every new/missing element decision and applies the review.

#### `POST /pages/scan-reviews`

Runs a staged scan. Request body is the same `ScanPageRequest` as `POST /pages/scan`. The active
page is **not** modified.

Response `200` — a `StagedPageScanResult`:

```json
{
  "activePage": { "schemaVersion": 1, "id": "page-1", "name": "Login", "url": "...", "elements": [], "updatedAt": "..." },
  "reconciliation": { "matchedCount": 8, "newCount": 2, "missingCount": 1, "isFirstScan": false },
  "review": {
    "schemaVersion": 1,
    "id": "review-1",
    "pageId": "page-1",
    "status": "pending",
    "createdAt": "...",
    "updatedAt": "...",
    "completedAt": null,
    "baselinePage": { "...": "..." },
    "proposedPage": { "...": "..." },
    "items": [
      { "id": "item-1", "kind": "new", "element": { "...": "..." }, "decision": "wait" },
      { "id": "item-2", "kind": "missing", "element": { "...": "..." }, "decision": "wait" }
    ]
  },
  "dynamicElementSuggestions": null,
  "screenshotPath": null
}
```

When there are no differences to review, `review` is `null`.

#### `GET /scan-reviews`

Lists reviews. Optional query parameters: `pageId`, `status` (`pending`, `applied`, `discarded`,
`superseded`).

#### `GET /scan-reviews/{reviewId}`

Returns one `ScanReview`.

#### `PUT /scan-reviews/{reviewId}/decisions`

Sets the decision for one or more review items.

Request body:

```json
{
  "decisions": [
    { "itemId": "item-1", "decision": "accept" },
    { "itemId": "item-2", "decision": "deny" }
  ]
}
```

Returns the updated `ScanReview`.

#### `POST /scan-reviews/{reviewId}/apply`

Applies a resolved review to the active page. Accepting a new element adds it; denying it omits
it. Accepting a missing element removes it; denying the removal retains the exact baseline
element. The apply is rejected with `409 scan_review_*` when:

- an item is still unresolved (`scan_review_unresolved`),
- the page changed after the review was created (`scan_review_stale`),
- an accepted removal is still referenced by a saved test (`scan_review_removal_referenced`, with
  the referencing `testId`/`stepId` pairs in `references`), or
- the review is no longer pending (`scan_review_not_pending`).

Response `200` — a `ScanReviewApplyResult`:

```json
{ "review": { "...": "..." }, "page": { "...": "..." } }
```

#### `POST /scan-reviews/{reviewId}/discard`

Discards a pending review without applying it. Returns the `ScanReview` in `discarded` status.
Applied/discarded/superseded reviews remain under `.cliq/runs/scan-reviews/` as local audit
history.

### Tests

#### `GET /tests`

Lists all test cases.

A `TestCase`:

```json
{
  "schemaVersion": 1,
  "id": "test-1",
  "name": "Complete sample workflow",
  "description": null,
  "tags": [],
  "steps": [
    { "id": "step-1", "action": "navigate", "label": "Go to login", "enabled": true, "url": "https://app.example/login" },
    { "id": "step-2", "action": "fill", "label": "Type username", "enabled": true, "pageId": "page-1", "elementId": "elem-1", "value": { "kind": "literal", "value": "ana" } },
    { "id": "step-3", "action": "fill", "label": "Type password", "enabled": true, "pageId": "page-1", "elementId": "elem-2", "value": { "kind": "secret", "value": "PASSWORD" } },
    { "id": "step-4", "action": "assertUrl", "label": "URL matches", "enabled": true, "value": { "kind": "literal", "value": "^https://app\\.example/home$" } }
  ],
  "updatedAt": "2026-08-18T10:00:00Z"
}
```

`TestStep` field notes:

- Steps reference stable page/element IDs instead of copying selectors. Saving validates that
  every element-touching step's references exist, so a broken reference fails at save time with
  `400` rather than mid-run.
- `value.kind` is `literal` or `secret`. Secret references map to `CLIQ_*` environment variables
  in generated NUnit code.
- `dynamicElementId`/`dynamicParameterValue` are set instead of `elementId` when the step targets
  a `DynamicElement`; the value is substituted for the `{param}` token in its template.
- `values` (an array of `StepValue`) is set instead of `value` for multi-select steps.
- `timeoutMs` overrides the default step timeout.

#### `GET /tests/{id}`

Returns one `TestCase`, or `404`.

#### `PUT /tests/{id}`

Saves a test case. The request body's `id` must equal the route `{id}`, otherwise `400` ("Test
ID mismatch."). Returns the saved test.

#### `DELETE /tests/{id}`

Deletes the test case. Returns `204`.

### Secrets

#### `GET /secrets`

Returns the workspace secret keys and values as a plain object:

```json
{ "PASSWORD": "correct horse battery staple" }
```

Values are read from the local `secrets.local.json` store (never committed to Git).

#### `PUT /secrets`

Saves (upserts) the given key/value pairs. Returns `204`.

#### `DELETE /secrets/{key}`

Deletes one secret key. Returns `204`.

### Generation

#### `POST /generation`

Generates C# Page Objects and NUnit workflow tests. Only the requested pages/tests are
generated; when both `pageIds` and `testIds` are omitted, the planner selects the full workspace.

Request body:

```json
{
  "pageIds": ["page-1"],
  "testIds": ["test-1"],
  "customMethodResolutions": { "page-1": ["MyCustomMethod"] },
  "customMethodDiscards": { "page-1": ["OldMethod"] }
}
```

- `customMethodResolutions`: per page, the `[CliqCustomMethod]`-marked methods to carry over into
  the regenerated file.
- `customMethodDiscards`: per page, methods the user chose to permanently discard. Anything
  neither resolved nor discarded still defaults to deferred (moved to the waiting list).

Response `200` — a `GenerationResult`:

```json
{
  "artifacts": [
    { "kind": "pageObject", "name": "LoginPage.cs", "path": "C:\\dev\\my-app\\Generated\\PageObjects\\LoginPage.cs", "created": true }
  ],
  "warnings": [
    { "pageObjectName": "LoginPage", "expectedCount": 2, "recoveredCount": 1, "path": "C:\\dev\\my-app\\Generated\\PageObjects\\LoginPage.cs" }
  ],
  "pendingMerges": [
    {
      "pageId": "page-1",
      "pageObjectName": "LoginPage",
      "candidates": [
        { "signature": "public void MyCustomMethod()", "sourceText": "...", "hasSyntaxError": false, "syntaxErrors": [] }
      ]
    }
  ]
}
```

- `artifacts` lists every file written, with `created` distinguishing new files from
  overwritten ones.
- `warnings` flags Page Object files left untouched (not overwritten) because generation could
  not safely fold in the existing custom methods.
- `pendingMerges` lists pages whose old files have custom methods needing a human decision.

#### `GET /pending-custom-methods`

Lists deferred custom methods in the global waiting list (`.cliq/pending-custom-methods.json`):

```json
[
  {
    "id": "pending-1",
    "pageId": "page-1",
    "pageObjectName": "LoginPage",
    "signature": "public void MyCustomMethod()",
    "sourceText": "...",
    "hasSyntaxError": false,
    "deferredAt": "2026-08-18T10:00:00Z"
  }
]
```

#### `DELETE /pending-custom-methods/{id}`

Removes one deferred custom method from the waiting list. Returns `204`.

### Runs

#### `POST /tests/{testId}/runs`

Starts a run of a test in a separate, visible Selenium browser window. The sidecar allows one
active run at a time; a second start fails with `409` (`RunAlreadyActiveException`).

Request body: `{ "browser": "chrome" }` (optional, defaults to the project's `defaultBrowser`).

Response `202 Accepted` with a `Location` header of `/api/v1/runs/{runId}` and the initial
`TestRun` in the body:

```json
{
  "id": "run-1",
  "testId": "test-1",
  "status": "queued",
  "startedAt": "2026-08-18T10:00:00Z",
  "completedAt": null,
  "steps": [],
  "error": null
}
```

#### `GET /runs/{runId}`

Returns the current `TestRun` (or `404`). A completed run's `steps` contains a `StepResult` per
step:

```json
{ "stepId": "step-1", "status": "passed", "durationMs": 1200, "error": null, "screenshotPath": null, "locateMs": 300, "actionMs": 900 }
```

A failed step carries `error` and, when `testExecution.captureScreenshotOnFailure` is enabled,
a bare `screenshotPath` file name resolvable via `GET /runs/{runId}/screenshots/{fileName}`.

#### `PUT /runs/{runId}`

Persists a run record (used by hosts to write a completed run back into the workspace). The
request body's `id` must equal the route `{runId}`, otherwise `400` ("Run ID mismatch.").
Returns the run.

#### `GET /runs/{runId}/screenshots/{fileName}`

Returns a failure screenshot PNG for the run, or `404`.

#### `GET /runs/{runId}/events`

Server-Sent Events stream of run progress. Headers: `Content-Type: text/event-stream`,
`Cache-Control: no-cache`, `Connection: keep-alive`.

Frames:

```
event: step
data: {"stepId":"step-1","status":"passed","durationMs":1200,...}

event: step
data: {"stepId":"step-2","status":"failed","durationMs":500,"error":"...","screenshotPath":"run-1_step-2.png",...}

event: done
data: {"id":"run-1","testId":"test-1","status":"failed","startedAt":"...","completedAt":"...","steps":[...]}
```

- `step` — one frame per completed step, payload is a `StepResult`.
- `done` — final frame, payload is the completed `TestRun`. The stream ends after it.

#### `POST /runs/{runId}/cancel`

Requests cancellation of the active run. Returns `202` if the run was cancellable, `404` if the
run is unknown.

## Worked examples

### Staged rescan and apply

```http
POST /api/v1/pages/scan-reviews
Authorization: Bearer <token>
Content-Type: application/json

{ "url": "https://app.example/login", "name": "Login", "browser": "chrome" }
```

```http
200 OK

{ "activePage": {...}, "reconciliation": { "matchedCount": 8, "newCount": 2, "missingCount": 1, "isFirstScan": false }, "review": { "id": "review-1", "items": [ {"id":"i1","kind":"new","decision":"wait"}, {"id":"i2","kind":"missing","decision":"wait"} ], ... } }
```

The user reviews the diff and makes decisions:

```http
PUT /api/v1/scan-reviews/review-1/decisions

{ "decisions": [ { "itemId": "i1", "decision": "accept" }, { "itemId": "i2", "decision": "deny" } ] }
```

Applying writes the active page only if the workspace is unchanged and no accepted removal is
still referenced by a saved test:

```http
POST /api/v1/scan-reviews/review-1/apply
```

```http
200 OK

{ "review": { "status": "applied", ... }, "page": { "id": "page-1", ... } }
```

### Running a test and streaming progress

```http
POST /api/v1/tests/test-1/runs
Authorization: Bearer <token>
Content-Type: application/json

{ "browser": "edge" }
```

```http
202 Accepted
Location: /api/v1/runs/run-1

{ "id": "run-1", "testId": "test-1", "status": "queued", ... }
```

```http
GET /api/v1/runs/run-1/events
```

```text
event: step
data: {"stepId":"step-1","status":"passed","durationMs":1200}

event: done
data: {"id":"run-1","testId":"test-1","status":"passed","steps":[...]}
```

## Concurrency and exclusivity

- A workspace is exclusive: opening it writes `.cliq/workspace.lock` and fails other hosts with
  `409 workspace_in_use` until it is closed.
- Only one browser run is active at a time; starting a second fails with `409`.
- A staged scan review apply is rejected if the page changed after the review was created, or if
  an accepted removal is still referenced by a saved test.
