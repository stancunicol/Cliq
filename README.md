# PageObjectGenerator

Proof of Concept: automatic generation of Page Object classes and NUnit smoke
tests, for Selenium WebDriver, C#, Chrome and Edge.

## What it does

Given a URL:

1. Launch Chrome or Edge through Selenium WebDriver.
2. Wait for `document.readyState === "complete"`.
3. Scan the live DOM (`input`, `textarea`, `select`, `button`, `a[href]`,
   `h1`-`h6`), skipping anything a human tester couldn't see or click (zero
   rendered size, `display:none`, `visibility:hidden`).
4. Pick a locator per element, priority order: `data-testid` > `id` > `name` >
   structural (`nth-child`) fallback. Each candidate is verified to uniquely
   match only that element before being accepted.
5. Generate a C# Page Object class (one method per element), using
   `By.Id`/`By.Name` where the locator allows it, `By.CssSelector` only where
   there's no native strategy.
6. Generate an NUnit test class (`[TestFixture]`, one `[Test]` per element).

Generation is entirely rule-based (DOM parsing + code templates).

Element discovery injects and runs JavaScript in the rendered page
(`IJavaScriptExecutor.ExecuteScript`) — the same way Puppeteer or a live UI
Spy tool inspects a running app — not by parsing raw HTML as static text.

### Decisions

| Dimension | Decision |
|---|---|
| Language/platform | C# / .NET 8 |
| Browser automation | Selenium WebDriver 4.46 |
| Browsers | Chrome, Edge |
| Test framework | NUnit 3 |
| Element discovery | Live DOM scan via injected JS |

### Recognized element types

| Source element | Page Object method | Generated NUnit test |
|---|---|---|
| text `input`/`textarea` | `Fill{Name}(string value)` | checks `Displayed`/`Enabled` |
| `button`, `a[href]`, `input[submit\|button\|reset]` | `Click{Name}()` + `Hover{Name}()` | checks `Displayed`/`Enabled` |
| `input[checkbox]` | `Set{Name}(bool isChecked)` | checks `Displayed`/`Enabled` |
| `input[radio]` (grouped by `name`) | `Select{Name}(string value)` — one method per group | checks `Displayed`/`Enabled` on the first option |
| `select` | `Select{Name}(string value)` | checks `Displayed`/`Enabled` |
| `select[multiple]` | `Select{Name}Multiple(IEnumerable<string> values)` | checks `Displayed`/`Enabled` |
| `h1`-`h6` | `Get{Name}Text()` | **snapshot assertion** — text captured at generation time becomes the expected value |

## Architecture

```
src/
  PageObjectGenerator.Core/        library: DOM scanning, naming, code generators, live execution
  PageObjectGenerator.Core.Tests/  unit tests for Core (no browser, no network)
  PageObjectGenerator.Api/         HTTP API (REST + WebSocket) over the same library
  PageObjectGenerator.Demo.Tests/  NUnit project that compiles and runs the generated example
  PageObjectGenerator.VsExtension/ (planned) VSIX client of the API
GeneratedPages/                    API output (Page Objects + NUnit tests + metadata)
  Shared/                          (planned) shared component Page Objects
Screenshots/                       screenshots taken during generation and /run (gitignored)
```

Central decision: **all generation/execution logic lives in `Core`**, called
by `Api` — not duplicated into the host.

- `PageObjectWorkflow` (generation) and `PageObjectRunner` (live execution)
  are the single source of truth.
- `Api` is a thin layer on top.

### Component responsibilities

| Component | Responsibility |
|---|---|
| `PageObjectWorkflow` | Orchestrates generation. Validates `className`/`namespace` *before* launching a browser. |
| `PageObjectRunner` | Orchestrates live execution (`/run`). Optional `onStep` callback for progress. |
| `ElementScanner` | JS extraction script + turns its JSON result into `ElementDescriptor`s, including name dedup. |
| `PageObjectCodeGenerator` / `NUnitTestCodeGenerator` | Text generators, switch on `ElementActionKind`. No real Selenium dependency — build strings. |
| `PageObjectFileWriter` | Only place that touches the filesystem. Validates `className` on every read/write. |
| `IdentifierNaming` | Identifier sanitization + strict validation used as a security gate. |
| `BrowserFactory` | Creates `IWebDriver` for Chrome/Edge; driver resolution via Selenium Manager. |
| `ScreenshotCapture`, `PageReadiness`, `SolutionPaths` | Single-purpose utilities: screenshot capture, DOM-ready wait, CWD-independent paths. |

### Key algorithms

- **Selector uniqueness** — `locatorFor` tries `data-testid` → `id` → `name` →
  structural `nth-child` fallback, each verified unique via
  `document.querySelectorAll` before being accepted.
- **Naming and deduplication** — `IdentifierNaming.Sanitize` builds valid C#
  identifiers; `ElementScanner.Scan` separately deduplicates names with a
  numeric suffix, independent of selector uniqueness.
- **`ElementDescriptor` / `LocatorStrategy`** — `Strategy` + `LocatorValue`
  are kept apart from the display-only `Selector`, so the generator can emit
  native `By.Id`/`By.Name` instead of always `By.CssSelector`.
- **Live progress over WebSocket** — `PageObjectRunner.Run` executes on a
  thread-pool thread; `onStep` pushes each step through a `Channel<T>` to the
  socket without blocking the browser thread.
- **CWD-independent paths** — `SolutionPaths` walks up to the `.sln` file and
  anchors `GeneratedPages/`/`Screenshots/` there, regardless of the process's
  working directory.
- **`/run` continues past failures, stops only on navigation** — a `Click`
  that succeeds and changes the URL stops the run; every other failure just
  gets reported and execution continues.
- **Radio buttons are grouped, not enumerated** — `<input type="radio">`
  sharing a `name` collapse into one method per group.
- **Invisible elements are skipped entirely** — zero size or CSS-hidden
  elements are excluded during scanning; a disabled-but-visible element is
  still included.
- **Stale metadata is rejected** — `TryReadMetadata` treats any element with
  an empty `LocatorValue` as "not found," instead of crashing later on
  `By.Id(null)`.
- **The whole Page Object is verified once, before any test runs** — a
  `[OneTimeSetUp]` opens its own browser, checks every locator, and fails
  once with all missing elements named, instead of each `[Test]` failing
  separately later.
- **Generated classes are `partial`** — `{ClassName}.cs` is fully
  regenerable; `{ClassName}.Custom.cs` is written once and never touched
  again, so hand-written code survives regeneration.
- **Captured text can't break the generated source** — `ToCSharpStringLiteral`
  escapes control characters (`\n`, `\r`, `\t`, others as `\uXXXX`), and
  `ElementScanner` collapses captured whitespace to match Selenium's
  `element.Text`.

## Comparison to the reference tools named in the proposal

Four tools/approaches named as inspiration, not requirements:

- **Ranorex** — adopted the locator-priority hierarchy idea; the "shared
  components across pages" next step (see below) extends that toward a
  reusable element repository, the way Ranorex's own repository works.
- **Puppeteer** — adopted the live-DOM-via-injected-JS approach
  (`page.evaluate`-style).
- **UI Spy / Object Spy** — adopted "inspect the live, rendered UI"; not
  adopted the interactive/manual exploration model.

## Running (API)

```
dotnet run --project src/PageObjectGenerator.Api
```

- Local HTTP server, default `http://localhost:5034` (or `--urls`).
- Swagger UI at `/swagger`.
- Meant as the connection point for a future client (e.g. a Visual Studio
  extension).
- CORS is currently permissive (`AllowAnyOrigin`) — tighten once the client
  is known.
- Generated files are overwritten on every generation call — source code
  meant to be compiled and tracked in git, not throwaway artifacts.

### Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/page-objects` | Scan a URL, generate + save Page Object + NUnit test; returns the code as JSON |
| `GET` | `/api/page-objects` | List previously generated class names |
| `GET` | `/api/page-objects/{className}` | Return Page Object + test source |
| `GET` | `/api/page-objects/{className}/elements` | Return discovered elements as structured JSON, no source |
| `DELETE` | `/api/page-objects/{className}` | Delete a class's `.cs`/`Tests.cs`/`.meta.json` |
| `POST` | `/api/page-objects/{className}/run` | Perform every discovered action visibly; full result at the end |
| `WS` | `/api/page-objects/{className}/run/ws` | Same as `/run`, streams each step as it completes |
| `GET` | `/api/screenshots/{fileName}` | Serve the actual PNG bytes for a screenshot |

`POST /api/page-objects` request body:

```json
{
  "url": "https://the-internet.herokuapp.com/login",
  "browser": "edge",
  "headless": true
}
```

- Only `url` is required (`browser` defaults `"chrome"`, `headless` `true`).
- No persistent browser sessions across requests — each request launches and closes its own browser. Simpler server
  state, higher per-request latency.
- Generation also writes `{ClassName}.meta.json` (URL, browser, elements),
  used by `/run` and `GET .../elements`, plus one reference screenshot
  (`screenshotUrl`) — a fixed baseline, unlike a screenshot taken fresh every
  run (which would always match "now" and show no drift).
- Generation also writes `{ClassName}.Custom.cs` **once** — `customPath` +
  `customFileCreated: true` the first time, `customFileCreated: false` on
  every later regeneration (file left untouched). See "hand-written code
  survives regeneration" above. `GET /api/page-objects/{className}` includes
  its content as `customCode` (`null` if it doesn't exist yet).
- All screenshot fields (`screenshotUrl`, `baselineScreenshotUrl`, per-step
  `screenshotUrl`) are fetchable URLs (`/api/screenshots/{fileName}`), not
  server filesystem paths.
- `DELETE` removes `.cs`/`Tests.cs`/`.meta.json` — `204` if deleted, `404`
  otherwise. Doesn't touch screenshots or `.Custom.cs` (hand-written code is
  never deleted automatically).

### Visual test execution: `POST /api/page-objects/{className}/run`

Unlike generation, this **actually performs** the discovered actions in a
visible browser (`headless: false` by default), with a pause between steps.

```json
{
  "headless": false,
  "values": {
    "username": "tomsmith",
    "password": "SuperSecretPassword!"
  }
}
```

- `values` maps an element's name to a value to type; unspecified fields get
  `"test"`.
- A failed step doesn't stop the sequence — each step is reported
  independently.
- Exception: a `Click` that navigates the page (`stoppedDueToNavigation:
  true`) stops execution, since later selectors target a DOM that's gone.
- `Assert` (headings): reads current text, compares to the value captured at
  generation; fails with `Expected text '...' but found '...'`.
- `Radio`: value = the option's `value` attribute (e.g. `"snack": "peas"`);
  no value → first option clicked.
- `MultiSelect`: comma-separated values (e.g. `"multi": "eggs,cheese"`).
- **Screenshots**: any failed step captures one (`screenshotUrl`); successful
  steps get `null`. Full-page captures, not pixel diffs.

### Live progress: `WS /api/page-objects/{className}/run/ws`

`POST /run` only responds once, after the whole sequence (20-30+ seconds of
silence for a 7-step run). The WebSocket variant pushes progress instead:

- Connect, then send one JSON text message shaped like the `POST /run` body
  (empty message → defaults).
- Server sends `{"type": "step", "step": {...}}` per completed step.
- Then one `{"type": "done", "result": {...}}` before closing.
- If the class doesn't exist: `{"type": "error", "message": "..."}`, closes
  immediately.

## Validation (Demo Selenium + NUnit)

```
dotnet test src/PageObjectGenerator.Demo.Tests
```

- Compiles and runs `GeneratedPages/LoginPage.cs` + `LoginPageTests.cs`
  directly, as a living example.
- 6 tests (4 interaction checks + 2 snapshot assertions), real browser,
  ~25-60 seconds.

## Unit tests (`PageObjectGenerator.Core.Tests`)

```
dotnet test src/PageObjectGenerator.Core.Tests
```

- 41 tests, under 5 seconds, no Selenium/network/real page.
- Covers `IdentifierNaming.Sanitize`/`ToPascalCase` edge cases, generator
  output per element kind and locator strategy, `DeriveClassName`.
- Built from hand-written `ElementDescriptor` values — no DOM scanning.
- `InternalsVisibleTo` (`AssemblyInfo.cs`) lets tests reach `internal`
  helpers directly.
- Does **not** cover `ElementScanner.Scan`, `BrowserFactory`, or
  `PageObjectRunner`'s live execution — those depend on a real page and are
  only exercised by `Demo.Tests` / manual `/run` calls.

## Next steps

Four directions agreed as the near-term scope, roughly in the order they'd
naturally build on each other:

1. **Visual Studio extension (VSIX)** — the tool's primary IDE integration.
   Consumes the existing REST/WebSocket API as a client (the reason that API
   was built in the first place) rather than calling
   `Core` in-process, so it becomes the first real client of that API instead
   of a second, parallel execution path. Risk: should own starting/stopping
   the API host itself and version-check it on connect, rather than assume
   it's already running and compatible.
2. **Pre-test failure-risk analysis** — a report of which generated elements
   are most likely to break, based on `LocatorStrategy` (`Id`/`Name`/
   `data-testid` = low risk, structural `nth-child` fallback = high risk).
3. **Self-healing** — detection exists (`/run` already catches a failed
   locator, just reports instead of acting); the actual gap is
   re-identification, which needs a stored fingerprint per element beyond
   just the winning selector so a broken locator can be matched against a
   rescan. Start with the safest action level (report only), before any
   auto-healing or rewriting generated files.
4. **Shared components across pages** (inspired by Ranorex's own element
   repository) — for an
   application with several pages, common chrome (nav bar, header, footer)
   shouldn't be regenerated and duplicated per page. Scope: an explicit,
   tester-provided list of URLs, each generated and referencing a shared
   component Page Object for the parts they have in common — not automatic
   crawling/discovery of pages, which doesn't fit a real multi-page site
   (unbounded surface, e.g. thousands of product pages) and breaks down
   further for SPA navigation that doesn't change the URL at all. Risk: keep
   it opt-in, so an existing generated page doesn't break if it doesn't
   reference a shared component.