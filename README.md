# PageObjectGenerator

Proof of Concept: automatic generation of Page Object classes and NUnit smoke
tests, for Selenium WebDriver, C#, Chrome and Edge.

For the Stage 1 project deliverable (technical direction, rationale, known
limitations, what's deliberately not implemented) see
[ARHITECTURA.md](ARHITECTURA.md). This file covers usage, not the "why".

## What it does

Given a URL, the tool:

1. launches Chrome or Edge through Selenium WebDriver, and waits for
   `document.readyState === "complete"` before doing anything else;
2. scans the page's live DOM (`input`, `textarea`, `select`, `button`, `a[href]`,
   `h1`-`h6`), skipping anything a human tester couldn't actually see or click
   (zero rendered size, `display:none`, `visibility:hidden`);
3. picks a locator for each surviving element, in priority order `data-testid` >
   `id` > `name` > structural (`nth-child`) fallback — each candidate is
   verified to uniquely match only that element before being accepted, so a
   duplicate `id`/`name` in broken markup falls through to the next candidate
   instead of silently resolving to the wrong element later;
4. generates a C# Page Object class, with one method per discovered element,
   using Selenium's own `By.Id`/`By.Name` where the locator allows it, and
   `By.CssSelector` only where there's no native strategy (`data-testid`,
   structural paths);
5. generates an NUnit test class (`[TestFixture]`) with one `[Test]` per element.

Generation is entirely rule-based (DOM parsing + code templates) — there is no
AI component in the pipeline yet.

Element discovery happens by injecting and running JavaScript directly in the
rendered page (`IJavaScriptExecutor.ExecuteScript`), the same way Puppeteer or a
live UI Spy tool would inspect a running app — not by parsing the raw HTML
source as static text. That matters for anything rendered client-side (React,
Angular, Vue): the raw HTML response often doesn't contain the real elements at
all, only the live, rendered DOM does.

### Recognized element types

| Source element | Page Object method | Generated NUnit test |
|---|---|---|
| text `input`/`textarea` | `Fill{Name}(string value)` | checks `Displayed`/`Enabled` |
| `button`, `a[href]`, `input[submit\|button\|reset]` | `Click{Name}()` + `Hover{Name}()` | checks `Displayed`/`Enabled` |
| `input[checkbox]` | `Set{Name}(bool isChecked)` | checks `Displayed`/`Enabled` |
| `input[radio]` (grouped by `name`) | `Select{Name}(string value)` — one method per group, not per option | checks `Displayed`/`Enabled` on the first option |
| `select` | `Select{Name}(string value)` | checks `Displayed`/`Enabled` |
| `select[multiple]` | `Select{Name}Multiple(IEnumerable<string> values)` | checks `Displayed`/`Enabled` |
| `h1`-`h6` | `Get{Name}Text()` | **snapshot assertion** — the text captured at generation time becomes the expected value (`Assert.That(element.Text, Is.EqualTo("Login Page"))`) |

Every `Click`-kind element also gets a `Hover{Name}()` method (via
`Actions.MoveToElement`) — useful for hover-triggered menus/tooltips, but not
exercised automatically by the generated tests or by `/run`.

## Structure

```
src/
  PageObjectGenerator.Core/        library: DOM scanning, naming, code generators
  PageObjectGenerator.Core.Tests/  unit tests for Core (no browser, no network)
  PageObjectGenerator.Cli/         command line entry point wiring it all together
  PageObjectGenerator.Api/         HTTP API over the same library, for an external client
  PageObjectGenerator.Demo.Tests/  NUnit project that compiles and runs the generated example
GeneratedPages/                    CLI/API output (Page Objects + NUnit tests + metadata)
Screenshots/                       screenshots taken during generation and /run (gitignored)
```

`PageObjectWorkflow` and `PageObjectFileWriter` in Core hold the shared
generate-and-save flow used identically by both the CLI and the API, so the two
hosts can't drift apart.

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

Example, the one used as the end-to-end demo:

```
dotnet run --project src/PageObjectGenerator.Cli -- "https://the-internet.herokuapp.com/login" --browser edge --headless --class LoginPage --namespace GeneratedPages
```

Generated files are overwritten on every run (no timestamped archive) — they
are source code meant to be compiled and tracked in git, not throwaway
artifacts.

## Running (API)

```
dotnet run --project src/PageObjectGenerator.Api
```

Starts a local HTTP server (default `http://localhost:5034`, or any port given
via `--urls`), with Swagger UI at `/swagger` for direct exploration/testing
from a browser. Meant as the connection point for a future client (e.g. a
Visual Studio extension) — the CORS policy is currently permissive
(`AllowAnyOrigin`) and should be tightened once that client is known.

Endpoints:

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/page-objects` | Launches the browser, scans the given URL, generates and saves a Page Object + NUnit test; returns the generated code as JSON |
| `GET` | `/api/page-objects` | Lists the names of previously generated classes |
| `GET` | `/api/page-objects/{className}` | Returns the Page Object + test source for a previously generated class |
| `GET` | `/api/page-objects/{className}/elements` | Returns the discovered elements (kind, locator, selector) as structured JSON, without the generated C# source |
| `DELETE` | `/api/page-objects/{className}` | Deletes a generated class's `.cs`/`Tests.cs`/`.meta.json` |
| `POST` | `/api/page-objects/{className}/run` | Reopens the page and actually performs every discovered action (fill/click/select/check/...), visibly; returns the full result after the whole sequence finishes |
| `WS` | `/api/page-objects/{className}/run/ws` | Same execution as `/run`, but streams each step over a WebSocket as it completes, instead of one response at the end |
| `GET` | `/api/screenshots/{fileName}` | Serves the actual PNG bytes for a screenshot referenced elsewhere by URL |

`POST /api/page-objects` request body:

```json
{
  "url": "https://the-internet.herokuapp.com/login",
  "browser": "edge",
  "headless": true
}
```

Only `url` is required (`browser` defaults to `"chrome"`, `headless` to
`true`). The class name and namespace are **not** request fields — the class
name is always derived from the URL and the namespace is always `"Generated"`.
This is deliberate: a caller-supplied class name or namespace would be spliced
directly into the generated C# source (`public sealed class {className}`) and
into the output file path (`Path.Combine(outDir, $"{className}.cs")`), which
is exactly the shape of two real vulnerabilities — C# source injection and
path traversal (a value like `../../../secrets` or a rooted path escapes
`GeneratedPages/` entirely, since `Path.Combine` treats a rooted second
argument as an override, not a suffix). Removing the field closes that off at
`POST /api/page-objects`; `GET /api/page-objects/{className}` and
`POST .../run` still take a class name from the URL route (they have to, to
know what to look up), so `PageObjectFileWriter` validates every class name
against a strict identifier pattern before it touches the filesystem,
regardless of where it came from.

Each generation request synchronously launches a fresh browser and blocks
until scanning finishes (a few seconds) — there are no persistent browser
sessions across requests, unlike the previous Python API.

Generation also writes a `{ClassName}.meta.json` file next to the generated
code, with the URL, browser and the list of discovered elements — needed by
`/run` and by `GET .../elements` so neither has to re-parse the generated C#
to know what to re-test or report. It also saves one reference screenshot
(`{ClassName}_generated_{timestamp}.png` in `Screenshots/`), included in the
response as `screenshotUrl` — the page exactly as it looked when the DOM was
scanned. This is the fixed baseline: because it is captured once, it stays
usable as a point of comparison even if the page changes later — unlike a
screenshot taken fresh at the start of every run, which would always match
"now" and could never show drift on its own.

All screenshot fields in every response (`screenshotUrl`, `baselineScreenshotUrl`,
the per-step `screenshotUrl`) are fetchable URLs (`/api/screenshots/{fileName}`),
not server filesystem paths — a remote client can `GET` them directly and get
back real `image/png` bytes. The file name is validated against the exact
shape the tool itself produces before it ever touches disk, the same way
`className` is, so a crafted file name can't read anything outside
`Screenshots/`.

`DELETE /api/page-objects/{className}` removes the generated `.cs`, `Tests.cs`,
and `.meta.json` for that class — `204` if something was actually deleted,
`404` if there was nothing to delete. It doesn't touch any screenshots (those
aren't attributable to a single class - per-step failure screenshots are named
after the element, not the class).

### Visual test execution: `POST /api/page-objects/{className}/run`

Unlike generation (which only reads the DOM), this endpoint **actually
performs** the discovered actions — fills fields, clicks — in a visible
browser (`headless: false` by default here, unlike `/generate`), with a short
pause between steps so a human can follow along.

```json
{
  "headless": false,
  "values": {
    "username": "tomsmith",
    "password": "SuperSecretPassword!"
  }
}
```

`values` is optional — it maps an element's name (the one baked into the
generated method, e.g. `FillUsername` → key `"username"`) to a value to type;
fields without a specified value get `"test"`. A step that fails (e.g. an
element reported as `Displayed` but not actually interactable in Selenium)
does not stop the rest of the sequence — every step is reported
independently. Exception: if a `Click` actually navigates the page (the URL
changes), execution stops there (`stoppedDueToNavigation: true` in the
response), because every selector after that point was resolved against a DOM
that no longer exists.

For heading elements (`kind: "Assert"`), the step doesn't fill or click — it
reads the current text and compares it against the value captured at
generation time, exactly like the matching NUnit test; if the page's text has
since changed, the step fails with an explicit
`Expected text '...' but found '...'` message.

For `Radio`, the value in `values` must be the `value` attribute of the
desired option within the group (e.g. `"snack": "peas"`); with no value, the
first option in the group is clicked. For `MultiSelect`, the value is a
comma-separated list (e.g. `"multi": "eggs,cheese"`) — each value must match a
real option `value`, otherwise the step fails with Selenium's own error.

**Screenshots**: any step that fails (for any reason) automatically captures a
screenshot, included in the response as a fetchable `screenshotUrl`; successful
steps have `screenshotUrl: null`. These are full-page captures, not
pixel-level comparisons — there is no automatic highlighting of changed
regions (yet), just reference images saved at fixed moments (generation time,
step failure).

### Live progress: `WS /api/page-objects/{className}/run/ws`

`POST /run` only responds once, after the entire sequence finishes - for a
7-step run that's 20-30+ seconds of silence before anything comes back. The
WebSocket variant runs the identical sequence but pushes a JSON message the
moment each step completes, so a client can render progress instead of a
static spinner.

Protocol: connect, then send one JSON text message shaped like the `POST /run`
body (`{"headless": false, "values": {...}}` - an empty message falls back to
defaults). The server then sends a `{"type": "step", "step": {...}}` message
per completed step, and finally one `{"type": "done", "result": {...}}`
message (`result` has the same shape as the `POST /run` response) before
closing the socket. If the class doesn't exist, it sends
`{"type": "error", "message": "..."}` and closes immediately, without waiting
for the client to send anything.

`PageObjectRunner.Run` itself is synchronous (it's driving a real browser
through blocking Selenium calls) - the endpoint runs it on a background thread
and forwards each step through an in-memory `Channel` to the async loop that's
actually allowed to `await` a socket send.

## Validation (Demo Selenium + NUnit)

`src/PageObjectGenerator.Demo.Tests` includes `GeneratedPages/LoginPage.cs` and
`GeneratedPages/LoginPageTests.cs` directly from the generation above, as a
living example:

```
dotnet test src/PageObjectGenerator.Demo.Tests
```

Runs the 6 generated NUnit tests (4 interaction checks + 2 snapshot assertions
on headings) against the real page, through Selenium + Edge. This is an
end-to-end test: it launches a real browser and depends on the target site
being reachable, so it takes ~25-60 seconds.

## Unit tests (`PageObjectGenerator.Core.Tests`)

Unlike `Demo.Tests` (E2E, real browser), these test the pure logic in `Core` —
naming, selector escaping, code generation — with no Selenium, no network, no
real page:

```
dotnet test src/PageObjectGenerator.Core.Tests
```

29 tests, under 5 seconds total. They cover `IdentifierNaming.Sanitize`/
`ToPascalCase` (edge cases: empty text, C# reserved words, leading digits,
special characters) and the output of `PageObjectCodeGenerator`/
`NUnitTestCodeGenerator` for every element kind (`Fill`/`Click`+`Hover`/
`Radio`/`MultiSelect`/`Assert`) and every locator strategy (`Id`/`Name`/
`CssSelector`), built directly from hand-written `ElementDescriptor` values —
without going through DOM scanning at all. An `InternalsVisibleTo` grant
(`AssemblyInfo.cs` in Core) lets these tests reach the `internal`
`ToCSharpStringLiteral`/`ByExpression` helpers directly.

These tests do **not** cover `ElementScanner.Scan` (the injected JS extraction
script), `BrowserFactory`, or the live execution in `PageObjectRunner` — those
still fundamentally depend on a real rendered page and are only exercised by
`Demo.Tests` / manual `/run` calls.

## Notable implementation decisions

A few choices made along the way that aren't obvious from the code alone:

- **CWD-independent paths.** ASP.NET Core resets the process's working
  directory to the project's `ContentRootPath` at startup, while a plain
  console app (the CLI) leaves it as whatever the shell's CWD was. A bare
  relative `"GeneratedPages"` default would therefore resolve to two different
  places depending on which host ran it. `SolutionPaths` fixes this by
  walking up from `AppContext.BaseDirectory` to the `.sln` file and anchoring
  both `GeneratedPages/` and `Screenshots/` there, for every host.
- **`/run` continues past failures, but stops on navigation.** The first
  version stopped at the first failed step; in practice, most real pages have
  at least one cosmetic element that reports `Displayed: true` but isn't
  truly clickable in Selenium (e.g. zero-size), which made the rule
  effectively block the entire run on the very first element. The current
  rule only stops the sequence when a `Click` step succeeds *and* the page's
  URL actually changes afterward — everything else, passed or failed, is
  recorded and the run continues.
- **Radio buttons are grouped, not enumerated.** `<input type="radio">`
  elements sharing a `name` are collapsed into a single descriptor per group
  (keyed by `name`), producing one `Select{Group}(string value)` method that
  picks among all options, instead of one method per individual radio button.
- **Radio/value selectors use single quotes.** The generated selector for
  picking a specific radio option is built at runtime as
  `groupSelector + "[value='" + value + "']"`. Using single quotes for the
  CSS attribute value (valid per the CSS spec) avoids embedding escaped
  double quotes inside a C# string literal inside *generated* C# source —
  simpler than it sounds, but easy to get wrong by hand.
- **Headings are treated as verification targets, not interactions.** `h1`-`h6`
  elements are the one kind that doesn't do anything to the page — the text
  captured when the Page Object was generated becomes a fixed expected value,
  baked into both the generated NUnit test and the `/run` step for that
  element. This is the one place the tool produces an actual assertion about
  page *content*, not just presence.
- **Locators use Selenium's own `By.Id`/`By.Name` where possible.** Only
  `data-testid` (no native strategy for arbitrary attributes) and the
  structural fallback (no native strategy for a DOM path) actually need
  `By.CssSelector`. Each candidate locator is also verified to match *only*
  its own element (`document.querySelectorAll(selector).length === 1 &&
  matches[0] === el`) before being accepted, so a duplicate `id`/`name` in
  broken markup falls through to the next candidate rather than silently
  producing a selector that resolves to the wrong element.
- **Invisible elements are skipped entirely, not just flagged.** An element
  with zero rendered size, or hidden via `display:none`/`visibility:hidden`,
  never becomes part of the Page Object at all - it's excluded during DOM
  scanning, not included-then-failed. The alternative (include it, let its
  generated test fail) produces a test that fails on every single run for a
  reason that has nothing to do with the page's actual behavior: nobody -
  human or automated - could ever have interacted with it. A `disabled`-but-visible
  element is still included, since a human tester can see it and verify its
  state, just not act on it.
- **Stale metadata is rejected, not silently misused.** `{ClassName}.meta.json`
  written by an older version of the tool can deserialize "successfully" but
  leave newer fields (like `LocatorValue`) at their CLR default - which for a
  non-nullable `string` is still `null` at runtime, since the compiler's
  nullability annotations aren't enforced by the runtime. `/run`ning against
  that would call `By.Id(null)` and fail with a confusing Selenium error deep
  in the stack. `PageObjectFileWriter.TryReadMetadata` now checks every
  element has a non-empty `LocatorValue` after deserializing and treats a
  failure as "not found" (prompting a regeneration) instead of returning
  corrupt data.

## Status against the original solution proposal

Implemented: automatic element identification, locator and Page Object class
generation, NUnit test generation (including real content assertions, not
just presence checks), Chrome and Edge support, reusable/maintainable
generated code, an HTTP API for an external client, live visual test
execution with failure screenshots, and a fast unit test suite for the core
logic.

Not implemented yet: Visual Studio integration (Add-in/Extension), an AI
component in the generation pipeline, and persistent/multi-step browser
sessions through the API. See [ARHITECTURA.md](ARHITECTURA.md) — the Stage 1
("Arhitectură și analiză") deliverable — for the full rationale behind these
gaps and the plan for Stage 2.
