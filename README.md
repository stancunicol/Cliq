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

### Why these choices — against the original proposal

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
  PageObjectGenerator.Cli/         command line entry point wiring it all together
  PageObjectGenerator.Api/         HTTP API (REST + WebSocket) over the same library
  PageObjectGenerator.Demo.Tests/  NUnit project that compiles and runs the generated example
GeneratedPages/                    CLI/API output (Page Objects + NUnit tests + metadata)
Screenshots/                       screenshots taken during generation and /run (gitignored)
```

Central decision: **one `Core` reused by two clients** (CLI, API), not two
parallel implementations.

- `PageObjectWorkflow` (generation) and `PageObjectRunner` (live execution)
  are the single source of truth.
- CLI and API are thin layers on top.

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

- **Selector uniqueness** — `locatorFor` tries `data-testid` → `id` → `name`
  against `document.querySelectorAll`, accepting a candidate only if it
  matches *exactly* the current element. Falls back to structural
  (`nth-child`), unique by construction. Protects against duplicate
  `id`/`name` in broken markup resolving to the wrong element.
- **Naming and deduplication** — `IdentifierNaming.Sanitize` turns source
  text into a valid C# identifier (fallback if empty/reserved word).
  `ElementScanner.Scan` separately dedups names with a numeric suffix
  (`_2`, `_3`...) — independent of selector uniqueness; two elements can
  share a name with completely different selectors.
- **`ElementDescriptor` / `LocatorStrategy`** — `Strategy`
  (`Id`/`Name`/`CssSelector`) + `LocatorValue`, split apart instead of one
  CSS string, so the generator can emit native `By.Id`/`By.Name`. A computed
  `Selector` property is display-only (JSON/metadata); actual lookup always
  uses `Strategy`+`LocatorValue`.
- **Live progress over WebSocket** — `PageObjectRunner.Run` is synchronous
  (blocking Selenium calls). The WebSocket endpoint runs it on a thread-pool
  thread; `onStep` writes each step into a `Channel<T>`; a separate async
  loop forwards each step over the socket without blocking the browser
  thread.
- **CWD-independent paths** — ASP.NET Core resets the working directory at
  startup, a console app doesn't. `SolutionPaths` walks up to the `.sln` file
  and anchors `GeneratedPages/`/`Screenshots/` there, for every host.
- **`/run` continues past failures, stops only on navigation** — stopping at
  the first failure used to block the whole run on one cosmetic element that
  reports `Displayed: true` but isn't really clickable. Now it only stops
  when a `Click` succeeds *and* the URL changes afterward.
- **Radio buttons are grouped, not enumerated** — `<input type="radio">`
  sharing a `name` collapse into one descriptor/method per group. Value
  selector built at runtime with single quotes (avoids escaping double
  quotes inside generated C# string literals).
- **Invisible elements are skipped entirely** — zero size or CSS-hidden
  elements never become part of the Page Object, excluded during scanning.
  A `disabled`-but-visible element is still included (a human can see and
  verify it, just not act on it).
- **Stale metadata is rejected** — a `.meta.json` from an older schema can
  deserialize "successfully" with new fields left `null` (`By.Id(null)` →
  confusing failure). `TryReadMetadata` checks every element has a non-empty
  `LocatorValue`, treats a failure as "not found."
- **The whole Page Object is verified once, before any test runs** — every
  generated test class has a `[OneTimeSetUp]` that opens its own browser,
  checks every element's locator can still be found, and fails once with all
  missing elements named if any can't — instead of the page having drifted
  and each individual `[Test]` failing separately, less informatively,
  further down the same run.
- **Generated classes are `partial`, so hand-written code survives
  regeneration** — `{ClassName}.cs` is fully regenerable and gets overwritten
  every time; a sibling `{ClassName}.Custom.cs` (same `partial class`) is
  written once, on first generation, and never touched again. A developer
  adds their own methods there (e.g. a `LoginAs` combining `FillUsername` +
  `FillPassword` + `ClickLogin`) without risking losing them the next time
  the page is rescanned.

## Comparison to the reference tools named in the proposal

Four tools/approaches named as inspiration, not requirements:

- **Ranorex** — adopted the locator-priority hierarchy idea.
- **Puppeteer** — adopted the live-DOM-via-injected-JS approach
  (`page.evaluate`-style).
- **UI Spy / Object Spy** — adopted "inspect the live, rendered UI"; not
  adopted the interactive/manual exploration model.

## Running (CLI)

```
dotnet run --project src/PageObjectGenerator.Cli -- <url> [options]

Options:
  --browser chrome|edge   Browser to drive (default: chrome)
  --headless              Run without a visible window
  --class <Name>          Page Object class name (default: derived from the URL)
  --namespace <Ns>        Namespace for generated code (default: Generated)
  --out <dir>             Output directory (default: GeneratedPages)
```

Example (the end-to-end demo):

```
dotnet run --project src/PageObjectGenerator.Cli -- "https://the-internet.herokuapp.com/login" --browser edge --headless --class LoginPage --namespace GeneratedPages
```

Generated files are overwritten on every run — source code meant to be
compiled and tracked in git, not throwaway artifacts.

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
- `className`/`namespace` are **not** request fields — see "Security."
- No persistent browser sessions across requests (unlike the previous Python
  API) — each request launches and closes its own browser. Simpler server
  state, higher per-request latency; acceptable for a PoC.
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

- 32 tests, under 5 seconds, no Selenium/network/real page.
- Covers `IdentifierNaming.Sanitize`/`ToPascalCase` edge cases, generator
  output per element kind and locator strategy, `DeriveClassName`.
- Built from hand-written `ElementDescriptor` values — no DOM scanning.
- `InternalsVisibleTo` (`AssemblyInfo.cs`) lets tests reach `internal`
  helpers directly.
- Does **not** cover `ElementScanner.Scan`, `BrowserFactory`, or
  `PageObjectRunner`'s live execution — those depend on a real page and are
  only exercised by `Demo.Tests` / manual `/run` calls.

## AI integration — feature ideas

### Fragility scoring — no AI needed for a first version

Every element already carries a `LocatorStrategy`, which is itself a
fragility signal:

| Strategy | Fragility |
|---|---|
| `Id` / `Name` | Low — stable, developer-controlled attributes |
| `CssSelector` via `data-testid` | Low — attribute added specifically for testing |
| `CssSelector` structural (`nth-child` fallback) | High — breaks on any DOM reordering |

A report of "which generated elements are most likely to break next" falls
out of data already computed during generation — no model call required.
Where AI could add value *on top* of this: recognizing framework-specific
fragility patterns (e.g. a page whose classes are regenerated on every build)
that a flat strategy-based score can't see.

### Self-healing — three separate pieces

1. **Detection** — already exists: `/run` catches `NoSuchElementException`
   per step today, it just reports the failure instead of acting on it.
2. **Re-identification** — the actual gap. `ElementScanner` collects
   `testId`/`id`/`name`/`placeholder`/`text` for every element during
   scanning, but only the *winning* selector is persisted
   (`ElementDescriptor`/`.meta.json`); the runner-up signals are discarded.
   Self-healing needs a richer stored fingerprint per element, so that when
   the primary locator fails, the page can be rescanned and the best-matching
   candidate found by comparing against that fingerprint (tag + text/attribute
   similarity + structural proximity) — classic fuzzy matching, not
   inherently an AI task.
3. **What to do with a match**, from safest to most aggressive:
   - Report only: "selector X broke, likely replacement is Y" — no code or
     behavior changes.
   - Heal the current `/run` only: substitute the new candidate for this
     execution, leave the generated `.cs` file untouched.
   - Rewrite the generated file with the new selector — changes already
     committed code, should require explicit human approval.

AI's plausible edge in this flow: disambiguating cases with several
similarly-plausible candidates, or explaining in natural language why a
given replacement was chosen — not the core matching itself.

## Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Sending scanned page data (URLs, element text, ids) to an external AI service | High if the page under test contains internal/proprietary content | Prefer a local model or an enterprise zero-retention agreement for anything beyond generic structural metadata |
| Non-deterministic AI output breaking reproducible generation (same page → different names on every regeneration) | Medium — undermines the "regenerating is safe" property the tool has today | Validate all AI output through the existing `IdentifierNaming` gate before accepting it |
| Single DOM snapshot misses dynamically-revealed elements | Medium — incomplete Page Objects on menu/modal-heavy pages | Documented as a known limitation; would need interactive exploration to fix |
| Vendor dependency if using an external LLM API (pricing, rate limits, availability) | Medium, only if that option is chosen | Keep the AI integration point isolated (e.g. behind an interface) so the provider can be swapped without touching `Core`'s generation logic |