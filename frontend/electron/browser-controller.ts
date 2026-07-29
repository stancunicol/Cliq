import { BaseWindow, session, WebContentsView, type Rectangle } from 'electron'
import { randomUUID } from 'node:crypto'
import { mkdir, readFile, rename, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { recordedInteractionSchema } from './ipc-schemas.js'

const partition = 'persist:cliq-browser'
const beforeActionCeilingMs = 650
const afterActionCeilingMs = 450
const locatePollIntervalMs = 200
const syncPollIntervalMs = 80
const timingStatsSampleWindow = 20
const timingStatsMinSamples = 3
const timingStatsSafetyFactor = 1.3

type StepAction =
  | 'navigate'
  | 'click'
  | 'fill'
  | 'select'
  | 'pressKey'
  | 'assertVisible'
  | 'assertText'
  | 'assertUrl'

interface EmbeddedLocator {
  strategy: 'testId' | 'id' | 'name' | 'css' | 'xpath'
  value: string
}

interface EmbeddedPageElement {
  id: string
  name: string
  tag: string
  action: StepAction
  locator: EmbeddedLocator
  expectedText?: string
  notFound?: boolean
}

export interface EmbeddedPage {
  schemaVersion: 1
  id: string
  name: string
  url: string
  elements: EmbeddedPageElement[]
  updatedAt: string
}

export interface EmbeddedTestStep {
  id: string
  action: StepAction
  label: string
  enabled: boolean
  pageId?: string
  elementId?: string
  url?: string
  value?: { kind: 'literal' | 'secret'; value: string }
  timeoutMs?: number
}

export interface EmbeddedTest {
  id: string
  steps: EmbeddedTestStep[]
}

export interface EmbeddedStepResult {
  stepId: string
  status: 'passed' | 'failed'
  durationMs: number
  error?: string
  screenshotPath?: string
  selfHealed?: boolean
  locateMs?: number
  actionMs?: number
  syncMs?: number
  extendedTimeoutMs?: number
}

export interface EmbeddedTestRun {
  id: string
  testId: string
  status: 'queued' | 'running' | 'passed' | 'failed' | 'cancelled'
  startedAt: string
  completedAt?: string
  steps: EmbeddedStepResult[]
  error?: string
}

export type EmbeddedRunEvent =
  | { type: 'step-start'; stepId: string }
  | { type: 'step'; step: EmbeddedStepResult }

export interface EmbeddedSettings {
  elementIdentification: {
    testIdAttribute: string
    strategyOrder: ('testId' | 'id' | 'name')[]
    ignoreIdPatterns: string[]
  }
  testExecution: {
    defaultStepTimeoutMs: number
    pageReadyTimeoutMs: number
    captureScreenshotOnFailure: boolean
  }
}

export class BrowserController {
  readonly view: WebContentsView
  private recording = false
  private visible = true
  private error?: string
  private bounds: Rectangle = { x: 0, y: 102, width: 0, height: 0 }
  private pendingNavigation?: { url: string; promise: Promise<void> }
  private testIdAttribute = 'data-testid'

  constructor(
    private readonly window: BaseWindow,
    private readonly shell: WebContentsView,
    preloadDirectory: string,
  ) {
    const browserSession = session.fromPartition(partition)
    browserSession.setPermissionRequestHandler((_contents, _permission, callback) => callback(false))
    browserSession.setPermissionCheckHandler(() => false)
    browserSession.on('will-download', (event) => event.preventDefault())

    this.view = new WebContentsView({
      webPreferences: {
        preload: path.join(preloadDirectory, 'recorder-preload.cjs'),
        contextIsolation: true,
        nodeIntegration: false,
        sandbox: true,
        partition,
      },
    })
    this.window.contentView.addChildView(this.view)
    for (const event of [
      'did-start-loading',
      'did-stop-loading',
      'did-navigate',
      'did-navigate-in-page',
      'page-title-updated',
    ] as const) {
      this.view.webContents.on(event as never, () => {
        this.applyBounds()
        this.publish()
      })
    }
    this.view.webContents.on('did-fail-load', (_event, code, description, _url, isMainFrame) => {
      if (isMainFrame && code !== -3) {
        this.error = `${description} (${code})`
        this.applyBounds()
        this.publish()
      }
    })
    this.view.webContents.setWindowOpenHandler(({ url }) => {
      if (isHttpUrl(url)) void this.navigate(url)
      return { action: 'deny' }
    })
    this.view.webContents.on('will-navigate', (event, url) => {
      if (!isHttpUrl(url)) event.preventDefault()
    })
    this.view.webContents.ipc.on('recorder:interaction', (event, interaction) => {
      if (this.recording && event.sender === this.view.webContents) {
        const parsed = recordedInteractionSchema.safeParse(interaction)
        if (parsed.success) {
          this.shell.webContents.send('browser:recorded-interaction', parsed.data)
        }
      }
    })
    this.view.webContents.ipc.on('recorder:ready', (event) => {
      if (event.sender === this.view.webContents) {
        event.sender.send('recorder:enabled', this.recording)
      }
    })
  }

  async navigate(url: string): Promise<void> {
    if (!isHttpUrl(url)) throw new Error('Only HTTP and HTTPS URLs are supported.')
    if (this.view.webContents.getURL() === url) return
    if (this.pendingNavigation?.url === url) return this.pendingNavigation.promise
    this.error = undefined
    this.applyBounds()
    const promise = this.view.webContents.loadURL(url)
      .catch((error: unknown) => {
        if (!isNavigationAborted(error)) throw error
      })
      .then(() => {
        this.applyBounds()
        this.publish()
      })
      .finally(() => {
        if (this.pendingNavigation?.promise === promise) this.pendingNavigation = undefined
      })
    this.pendingNavigation = { url, promise }
    return promise
  }

  back(): void {
    if (this.view.webContents.navigationHistory.canGoBack()) {
      this.view.webContents.navigationHistory.goBack()
    }
  }

  forward(): void {
    if (this.view.webContents.navigationHistory.canGoForward()) {
      this.view.webContents.navigationHistory.goForward()
    }
  }

  reload(): void {
    this.view.webContents.reload()
  }

  setBounds(bounds: Rectangle): void {
    this.bounds = bounds
    this.applyBounds()
  }

  setVisible(visible: boolean): void {
    if (this.visible === visible) return
    this.visible = visible
    if (visible) {
      this.window.contentView.addChildView(this.view)
      this.applyBounds()
    } else {
      this.view.setBounds({ x: 0, y: 0, width: 0, height: 0 })
      this.window.contentView.removeChildView(this.view)
    }
  }

  setRecording(recording: boolean): void {
    this.recording = recording
    this.view.webContents.send('recorder:enabled', recording)
    this.publish()
  }

  resize(): void {
    this.applyBounds()
  }

  close(): void {
    this.view.webContents.close()
  }

  async runTest(
    run: EmbeddedTestRun,
    test: EmbeddedTest,
    pages: EmbeddedPage[],
    secrets: Record<string, string>,
    workspaceRoot: string,
    settings: EmbeddedSettings,
    persistPage: (page: EmbeddedPage) => Promise<void>,
    signal: AbortSignal,
    onEvent: (event: EmbeddedRunEvent) => void,
  ): Promise<EmbeddedTestRun> {
    this.testIdAttribute = settings.elementIdentification.testIdAttribute
    const timingStats = await loadTimingStats(workspaceRoot)
    const results: EmbeddedStepResult[] = []
    try {
      try {
        for (const step of test.steps.filter((candidate) => candidate.enabled)) {
          signal.throwIfAborted()
          onEvent({ type: 'step-start', stepId: step.id })
          const started = Date.now()
          try {
            const timing = await this.executeStep(step, pages, secrets, settings, persistPage, timingStats, signal)
            const result: EmbeddedStepResult = {
              stepId: step.id,
              status: 'passed',
              durationMs: Date.now() - started,
              selfHealed: timing.selfHealed,
              locateMs: timing.locateMs,
              actionMs: timing.actionMs,
              syncMs: timing.syncMs,
              extendedTimeoutMs: timing.extendedTimeoutMs,
            }
            results.push(result)
            onEvent({ type: 'step', step: result })
          } catch (error) {
            if (isAbortError(error)) throw error
            const result: EmbeddedStepResult = {
              stepId: step.id,
              status: 'failed',
              durationMs: Date.now() - started,
              error: error instanceof Error ? error.message : String(error),
              screenshotPath: settings.testExecution.captureScreenshotOnFailure
                ? await this.captureFailure(workspaceRoot, run.id, step.id)
                : undefined,
            }
            results.push(result)
            onEvent({ type: 'step', step: result })
            return {
              ...run,
              status: 'failed',
              completedAt: new Date().toISOString(),
              steps: results,
              error: `Step '${step.label}' failed.`,
            }
          }
        }
        return {
          ...run,
          status: 'passed',
          completedAt: new Date().toISOString(),
          steps: results,
        }
      } catch (error) {
        if (!isAbortError(error)) throw error
        return {
          ...run,
          status: 'cancelled',
          completedAt: new Date().toISOString(),
          steps: results,
          error: 'Run cancelled.',
        }
      }
    } finally {
      await saveTimingStats(workspaceRoot, timingStats).catch(() => undefined)
    }
  }

  private applyBounds(): void {
    if (!this.visible) return
    const currentUrl = this.view.webContents.getURL()
    if (this.error || !currentUrl || currentUrl === 'about:blank') {
      this.view.setBounds({ x: 0, y: 0, width: 0, height: 0 })
      return
    }
    const content = this.window.getContentBounds()
    const x = Math.max(0, Math.min(this.bounds.x, content.width))
    const y = Math.max(0, Math.min(this.bounds.y, content.height))
    this.view.setBounds({
      x,
      y,
      width: Math.max(0, Math.min(this.bounds.width, content.width - x)),
      height: Math.max(0, Math.min(this.bounds.height, content.height - y)),
    })
  }

  private publish(): void {
    if (this.shell.webContents.isDestroyed()) return
    this.shell.webContents.send('browser:state', {
      url: this.view.webContents.getURL(),
      title: this.view.webContents.getTitle() || 'New tab',
      loading: this.view.webContents.isLoading(),
      canGoBack: this.view.webContents.navigationHistory.canGoBack(),
      canGoForward: this.view.webContents.navigationHistory.canGoForward(),
      recording: this.recording,
      error: this.error,
    })
  }

  private async executeStep(
    step: EmbeddedTestStep,
    pages: EmbeddedPage[],
    secrets: Record<string, string>,
    settings: EmbeddedSettings,
    persistPage: (page: EmbeddedPage) => Promise<void>,
    timingStats: TimingStats,
    signal: AbortSignal,
  ): Promise<{
    selfHealed: boolean
    locateMs?: number
    actionMs?: number
    syncMs?: number
    extendedTimeoutMs?: number
  }> {
    if (step.action === 'navigate') {
      const url = step.url
      if (!url || !isHttpUrl(url)) throw new Error('Navigate step has no valid HTTP or HTTPS URL.')
      await abortableDelay(300, signal)
      this.error = undefined
      await this.view.webContents.loadURL(url)
      this.applyBounds()
      this.publish()
      await abortableDelay(afterActionCeilingMs, signal)
      return { selfHealed: false }
    }

    if (step.action === 'assertUrl') {
      await abortableDelay(beforeActionCeilingMs, signal)
      const expected = resolveValue(step, secrets)
      if (!new RegExp(expected).test(this.view.webContents.getURL())) {
        throw new Error(`URL '${this.view.webContents.getURL()}' does not match '${expected}'.`)
      }
      await abortableDelay(afterActionCeilingMs, signal)
      return { selfHealed: false }
    }

    const { page, element } = resolveElement(step, pages)
    const configuredTimeoutMs = step.timeoutMs && step.timeoutMs > 0
      ? step.timeoutMs
      : settings.testExecution.defaultStepTimeoutMs
    const timeoutMs = suggestedTimeoutMs(timingStats, step.id, configuredTimeoutMs)
    const extendedTimeoutMs = timeoutMs > configuredTimeoutMs ? timeoutMs : undefined
    const locateStarted = Date.now()
    const { locator, healed } = await this.locateElement(page, element, pages, settings, timeoutMs, persistPage, signal)
    const locateMs = Date.now() - locateStarted
    if (!healed) recordLocateSample(timingStats, step.id, locateMs)

    const highlight = await this.highlight(locator)
    let syncMs = 0
    let actionMs: number
    try {
      syncMs += await this.waitForStable(locator, beforeActionCeilingMs, signal)
      signal.throwIfAborted()
      const actionStarted = Date.now()
      switch (step.action) {
        case 'click':
          this.view.webContents.focus()
          this.view.webContents.sendInputEvent({
            type: 'mouseMove',
            x: Math.round(highlight.x),
            y: Math.round(highlight.y),
          })
          this.view.webContents.sendInputEvent({
            type: 'mouseDown',
            button: 'left',
            clickCount: 1,
            x: Math.round(highlight.x),
            y: Math.round(highlight.y),
          })
          this.view.webContents.sendInputEvent({
            type: 'mouseUp',
            button: 'left',
            clickCount: 1,
            x: Math.round(highlight.x),
            y: Math.round(highlight.y),
          })
          break
        case 'fill':
          await this.focusElement(locator, true)
          this.selectAll()
          await this.view.webContents.insertText(resolveValue(step, secrets))
          await this.commitField(locator)
          break
        case 'select':
          await this.selectElement(locator, resolveValue(step, secrets))
          break
        case 'pressKey':
          await this.focusElement(locator, false)
          this.pressKey(resolveValue(step, secrets))
          break
        case 'assertVisible':
          if (!highlight.visible) throw new Error(`Element '${element.name}' is not visible.`)
          break
        case 'assertText': {
          const actual = await this.elementText(locator)
          const expected = resolveValue(step, secrets)
          if (actual !== expected) {
            throw new Error(`Expected text '${expected}' but found '${actual}'.`)
          }
          break
        }
      }
      actionMs = Date.now() - actionStarted
      syncMs += await this.waitForQuiet(afterActionCeilingMs, signal)
      if (this.view.webContents.isLoading()) {
        const loadStarted = Date.now()
        await this.waitForLoad(signal, settings.testExecution.pageReadyTimeoutMs)
        syncMs += Date.now() - loadStarted
      }
    } finally {
      await this.restoreHighlight(locator, highlight).catch(() => undefined)
    }
    return { selfHealed: healed, locateMs, actionMs, syncMs, extendedTimeoutMs }
  }

  private async locateElement(
    page: EmbeddedPage,
    element: EmbeddedPageElement,
    pages: EmbeddedPage[],
    settings: EmbeddedSettings,
    timeoutMs: number,
    persistPage: (page: EmbeddedPage) => Promise<void>,
    signal: AbortSignal,
  ): Promise<{ locator: EmbeddedLocator; healed: boolean }> {
    const deadline = Date.now() + timeoutMs
    let lastError: unknown
    for (;;) {
      signal.throwIfAborted()
      try {
        await this.view.webContents.executeJavaScript(elementLookupScript(element.locator, this.testIdAttribute))
        return { locator: element.locator, healed: false }
      } catch (error) {
        lastError = error
      }
      if (Date.now() >= deadline) break
      await abortableDelay(Math.min(locatePollIntervalMs, Math.max(0, deadline - Date.now())), signal)
    }

    const healedLocator = await this.tryHeal(element, settings).catch(() => null)
    if (!healedLocator) {
      throw lastError instanceof Error ? lastError : new Error(String(lastError))
    }

    const updatedPage: EmbeddedPage = {
      ...page,
      elements: page.elements.map((candidate) =>
        candidate.id === element.id ? { ...candidate, locator: healedLocator } : candidate),
    }
    const pageIndex = pages.findIndex((candidate) => candidate.id === page.id)
    if (pageIndex >= 0) pages[pageIndex] = updatedPage
    await persistPage(updatedPage).catch(() => undefined)
    return { locator: healedLocator, healed: true }
  }

  /** Waits (up to `ceilingMs`) for the target element's position/size to stop changing between
   * two consecutive samples, instead of always sleeping the full ceiling. Returns actual elapsed ms. */
  private async waitForStable(locator: EmbeddedLocator, ceilingMs: number, signal: AbortSignal): Promise<number> {
    const started = Date.now()
    const deadline = started + ceilingMs
    let previous: string | undefined
    for (;;) {
      signal.throwIfAborted()
      let current: string
      try {
        current = await this.view.webContents.executeJavaScript(`
          (() => {
            const element = ${elementLookupScript(locator, this.testIdAttribute)};
            const rect = element.getBoundingClientRect();
            return JSON.stringify([rect.x, rect.y, rect.width, rect.height]);
          })()
        `) as string
      } catch {
        break
      }
      if (current === previous) break
      previous = current
      if (Date.now() >= deadline) break
      await abortableDelay(Math.min(syncPollIntervalMs, Math.max(0, deadline - Date.now())), signal)
    }
    return Date.now() - started
  }

  /** Waits (up to `ceilingMs`) for the DOM to stop changing shape between two consecutive samples,
   * as a lightweight proxy for "the page settled after the action". Returns actual elapsed ms. */
  private async waitForQuiet(ceilingMs: number, signal: AbortSignal): Promise<number> {
    const started = Date.now()
    const deadline = started + ceilingMs
    let previous: string | undefined
    for (;;) {
      signal.throwIfAborted()
      const current = await this.view.webContents.executeJavaScript(
        `document.body ? document.body.childElementCount + ':' + document.readyState : 'none'`,
      ) as string
      if (current === previous) break
      previous = current
      if (Date.now() >= deadline) break
      await abortableDelay(Math.min(syncPollIntervalMs, Math.max(0, deadline - Date.now())), signal)
    }
    return Date.now() - started
  }

  private async tryHeal(element: EmbeddedPageElement, settings: EmbeddedSettings): Promise<EmbeddedLocator | null> {
    const json = await this.view.webContents.executeJavaScript(
      healCandidatesScript(
        element.tag,
        this.testIdAttribute,
        settings.elementIdentification.strategyOrder,
        settings.elementIdentification.ignoreIdPatterns,
      ),
    ) as string
    const candidates = JSON.parse(json) as HealCandidate[]
    return selectHealMatch(candidates, element.name, this.testIdAttribute)
  }

  private async highlight(locator: EmbeddedLocator) {
    return this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator, this.testIdAttribute)};
        element.scrollIntoView({ block: 'center', inline: 'center' });
        const previous = {
          outline: element.style.outline,
          outlineOffset: element.style.outlineOffset,
          transition: element.style.transition,
        };
        element.style.transition = 'outline-color 120ms ease, box-shadow 120ms ease';
        element.style.outline = '3px solid #a98df5';
        element.style.outlineOffset = '4px';
        const rect = element.getBoundingClientRect();
        const style = window.getComputedStyle(element);
        return {
          x: rect.left + rect.width / 2,
          y: rect.top + rect.height / 2,
          visible: rect.width > 0 && rect.height > 0
            && style.display !== 'none' && style.visibility !== 'hidden',
          previous,
        };
      })()
    `) as Promise<{
      x: number
      y: number
      visible: boolean
      previous: { outline: string; outlineOffset: string; transition: string }
    }>
  }

  private async restoreHighlight(
    locator: EmbeddedLocator,
    state: { previous: { outline: string; outlineOffset: string; transition: string } },
  ) {
    await this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator, this.testIdAttribute)};
        const previous = ${JSON.stringify(state.previous)};
        element.style.outline = previous.outline;
        element.style.outlineOffset = previous.outlineOffset;
        element.style.transition = previous.transition;
      })()
    `)
  }

  private async focusElement(locator: EmbeddedLocator, selectText: boolean) {
    await this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator, this.testIdAttribute)};
        element.focus();
        if (${selectText} && typeof element.select === 'function') element.select();
      })()
    `)
  }

  private selectAll() {
    const modifier = process.platform === 'darwin' ? 'meta' : 'control'
    this.view.webContents.sendInputEvent({ type: 'keyDown', keyCode: 'A', modifiers: [modifier] })
    this.view.webContents.sendInputEvent({ type: 'keyUp', keyCode: 'A', modifiers: [modifier] })
  }

  private pressKey(value: string) {
    const keyCode = value.trim() || 'Enter'
    this.view.webContents.sendInputEvent({ type: 'keyDown', keyCode })
    this.view.webContents.sendInputEvent({ type: 'keyUp', keyCode })
  }

  private async commitField(locator: EmbeddedLocator) {
    await this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator, this.testIdAttribute)};
        element.dispatchEvent(new Event('change', { bubbles: true }));
        element.blur();
      })()
    `)
  }

  private async selectElement(locator: EmbeddedLocator, value: string) {
    await this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator, this.testIdAttribute)};
        if (!(element instanceof HTMLSelectElement)) throw new Error('Recorded select target is not a select element.');
        const value = ${JSON.stringify(value)};
        if (![...element.options].some(option => option.value === value)) {
          throw new Error("Select option '" + value + "' was not found.");
        }
        element.value = value;
        element.dispatchEvent(new Event('input', { bubbles: true }));
        element.dispatchEvent(new Event('change', { bubbles: true }));
      })()
    `)
  }

  private async elementText(locator: EmbeddedLocator) {
    return this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator, this.testIdAttribute)};
        return (element.innerText ?? element.textContent ?? '').replace(/\\s+/g, ' ').trim();
      })()
    `) as Promise<string>
  }

  private async waitForLoad(signal: AbortSignal, timeoutMs: number) {
    const deadline = Date.now() + timeoutMs
    while (this.view.webContents.isLoading()) {
      signal.throwIfAborted()
      if (Date.now() >= deadline) throw new Error(`The page did not finish loading within ${timeoutMs}ms.`)
      await abortableDelay(100, signal)
    }
  }

  private async captureFailure(workspaceRoot: string, runId: string, stepId: string) {
    try {
      const directory = path.join(workspaceRoot, '.cliq', 'runs', safeFilePart(runId))
      await mkdir(directory, { recursive: true })
      const fileName = `${safeFilePart(stepId)}.png`
      const image = await this.view.webContents.capturePage()
      await writeFile(path.join(directory, fileName), image.toPNG())
      return fileName
    } catch {
      return undefined
    }
  }
}

interface HealCandidate {
  testId: string | null
  id: string | null
  name: string | null
  placeholder: string | null
  text: string | null
  locatorStrategy: string
  locatorValue: string
}

// Ported from Cliq.Infrastructure.SelfHealingLocator / backend/tools/puppeteer-scanner/extract-elements.js -
// keep these three in sync if the identification priority rules ever change.
const RESERVED_WORDS = new Set([
  'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char', 'checked', 'class',
  'const', 'continue', 'decimal', 'default', 'delegate', 'do', 'double', 'else', 'enum', 'event',
  'explicit', 'extern', 'false', 'finally', 'fixed', 'float', 'for', 'foreach', 'goto', 'if',
  'implicit', 'in', 'int', 'interface', 'internal', 'is', 'lock', 'long', 'namespace', 'new',
  'null', 'object', 'operator', 'out', 'override', 'params', 'private', 'protected', 'public',
  'readonly', 'ref', 'return', 'sbyte', 'sealed', 'short', 'sizeof', 'stackalloc', 'static',
  'string', 'struct', 'switch', 'this', 'throw', 'true', 'try', 'typeof', 'uint', 'ulong',
  'unchecked', 'unsafe', 'ushort', 'using', 'virtual', 'void', 'volatile', 'while',
])

function sanitizeName(raw: string, fallback: string): string {
  let value = raw.replace(/[^0-9a-zA-Z]+/g, '_').replace(/^_+|_+$/g, '').toLowerCase()
  value = value.replace(/^[0-9_]+/, '')
  if (!value || RESERVED_WORDS.has(value)) value = fallback
  return value
}

function selectHealMatch(candidates: HealCandidate[], expectedName: string, testIdAttribute: string): EmbeddedLocator | null {
  let match: EmbeddedLocator | null = null
  let matchCount = 0
  for (const candidate of candidates) {
    const baseName = candidate.testId ?? candidate.id ?? candidate.name ?? candidate.placeholder ?? candidate.text ?? ''
    const sanitized = sanitizeName(baseName, randomUUID().replaceAll('-', ''))
    if (sanitized !== expectedName) continue
    matchCount++
    match = toLocator(candidate.locatorStrategy, candidate.locatorValue, testIdAttribute)
  }
  return matchCount === 1 ? match : null
}

function toLocator(strategy: string, value: string, testIdAttribute: string): EmbeddedLocator {
  const testIdPrefix = `[${testIdAttribute}="`
  if (strategy === 'css' && value.startsWith(testIdPrefix) && value.endsWith('"]')) {
    return { strategy: 'testId', value: value.slice(testIdPrefix.length, -2) }
  }
  if (strategy === 'id') return { strategy: 'id', value }
  if (strategy === 'name') return { strategy: 'name', value }
  return { strategy: 'css', value }
}

function healCandidatesScript(
  tag: string,
  testIdAttribute: string,
  strategyOrder: string[],
  ignoreIdPatterns: string[],
) {
  return `(() => {
    const tag = ${JSON.stringify(tag)};
    const testIdAttribute = ${JSON.stringify(testIdAttribute)};
    const strategyOrder = ${JSON.stringify(strategyOrder)};
    const ignoreIdRegexes = ${JSON.stringify(ignoreIdPatterns)}.map((pattern) => new RegExp(pattern, 'i'));

    function isIgnoredId(id) {
      return ignoreIdRegexes.some((regex) => regex.test(id));
    }
    function cssEscape(value) {
      return value.replace(/["\\\\]/g, '\\\\$&');
    }
    function isVisible(el) {
      const rect = el.getBoundingClientRect();
      if (rect.width <= 0 || rect.height <= 0) return false;
      const style = window.getComputedStyle(el);
      return style.visibility !== 'hidden' && style.display !== 'none';
    }
    function isUniqueMatch(selector, el) {
      try {
        const matches = document.querySelectorAll(selector);
        return matches.length === 1 && matches[0] === el;
      } catch {
        return false;
      }
    }
    function structuralSelector(el) {
      const parts = [];
      let node = el;
      while (node && node.parentElement && node.nodeName.toLowerCase() !== 'body') {
        const siblings = Array.from(node.parentElement.children);
        const index = siblings.indexOf(node) + 1;
        parts.unshift(node.nodeName.toLowerCase() + ':nth-child(' + index + ')');
        node = node.parentElement;
      }
      return parts.join(' > ');
    }
    const strategyChecks = {
      testId: (el) => {
        const testId = el.getAttribute(testIdAttribute);
        if (!testId) return null;
        const candidate = '[' + testIdAttribute + '="' + cssEscape(testId) + '"]';
        return isUniqueMatch(candidate, el) ? { strategy: 'css', value: candidate } : null;
      },
      id: (el) => (el.id && !isIgnoredId(el.id) && isUniqueMatch('#' + CSS.escape(el.id), el))
        ? { strategy: 'id', value: el.id }
        : null,
      name: (el) => {
        if (!el.name) return null;
        const candidate = el.tagName.toLowerCase() + '[name="' + cssEscape(el.name) + '"]';
        return isUniqueMatch(candidate, el) ? { strategy: 'name', value: el.name } : null;
      },
    };
    function locatorFor(el) {
      for (const key of strategyOrder) {
        const result = strategyChecks[key] && strategyChecks[key](el);
        if (result) return result;
      }
      return { strategy: 'css', value: structuralSelector(el) };
    }
    const candidates = Array.from(document.querySelectorAll(tag)).filter(isVisible);
    return JSON.stringify(candidates.map((el) => {
      const locator = locatorFor(el);
      const text = (el.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 40);
      return {
        testId: el.getAttribute(testIdAttribute),
        id: (el.id && !isIgnoredId(el.id)) ? el.id : null,
        name: el.getAttribute('name'),
        placeholder: el.getAttribute('placeholder'),
        text: text,
        locatorStrategy: locator.strategy,
        locatorValue: locator.value,
      };
    }));
  })()`
}

function elementLookupScript(locator: EmbeddedLocator, testIdAttribute: string) {
  const serialized = JSON.stringify(locator)
  return `(() => {
    const locator = ${serialized};
    const testIdAttribute = ${JSON.stringify(testIdAttribute)};
    let element;
    if (locator.strategy === 'testId') {
      element = [...document.querySelectorAll('[' + testIdAttribute + ']')]
        .find(candidate => candidate.getAttribute(testIdAttribute) === locator.value);
    } else if (locator.strategy === 'id') {
      element = document.getElementById(locator.value);
    } else if (locator.strategy === 'name') {
      element = [...document.querySelectorAll('[name]')]
        .find(candidate => candidate.getAttribute('name') === locator.value);
    } else if (locator.strategy === 'xpath') {
      element = document.evaluate(locator.value, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null)
        .singleNodeValue;
    } else {
      element = document.querySelector(locator.value);
    }
    if (!(element instanceof Element)) {
      throw new Error("Element was not found using " + locator.strategy + " '" + locator.value + "'.");
    }
    return element;
  })()`
}

interface TimingStats {
  schemaVersion: 1
  steps: Record<string, { locateMsSamples: number[]; updatedAt: string }>
}

function emptyTimingStats(): TimingStats {
  return { schemaVersion: 1, steps: {} }
}

async function loadTimingStats(workspaceRoot: string): Promise<TimingStats> {
  try {
    const raw = await readFile(timingStatsPath(workspaceRoot), 'utf8')
    const parsed = JSON.parse(raw) as Partial<TimingStats>
    if (parsed.schemaVersion === 1 && parsed.steps) return parsed as TimingStats
  } catch {
    // Missing or corrupt file - start with a clean slate rather than fail the run over stats.
  }
  return emptyTimingStats()
}

async function saveTimingStats(workspaceRoot: string, stats: TimingStats): Promise<void> {
  const directory = path.join(workspaceRoot, '.cliq')
  await mkdir(directory, { recursive: true })
  const filePath = timingStatsPath(workspaceRoot)
  const temporary = `${filePath}.${randomUUID()}.tmp`
  await writeFile(temporary, JSON.stringify(stats, null, 2), 'utf8')
  await rename(temporary, filePath)
}

function timingStatsPath(workspaceRoot: string) {
  return path.join(workspaceRoot, '.cliq', 'timing-stats.json')
}

/** Only call for a locate that resolved without healing - a healed locate's duration is
 * dominated by "waited out the timeout, then healed", not representative of normal speed. */
function recordLocateSample(stats: TimingStats, stepId: string, locateMs: number): void {
  const entry = stats.steps[stepId] ?? { locateMsSamples: [], updatedAt: '' }
  entry.locateMsSamples = [...entry.locateMsSamples, locateMs].slice(-timingStatsSampleWindow)
  entry.updatedAt = new Date().toISOString()
  stats.steps[stepId] = entry
}

/** Never returns less than `configuredMs` (the user's setting is always a floor) - only extends
 * the wait when history shows this step routinely needs longer, to cut down on spurious timeouts. */
function suggestedTimeoutMs(stats: TimingStats, stepId: string, configuredMs: number): number {
  const samples = stats.steps[stepId]?.locateMsSamples ?? []
  if (samples.length < timingStatsMinSamples) return configuredMs
  const sorted = [...samples].sort((a, b) => a - b)
  const p95 = sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * 0.95) - 1)]
  return Math.max(configuredMs, Math.round(p95 * timingStatsSafetyFactor))
}

function resolveElement(step: EmbeddedTestStep, pages: EmbeddedPage[]) {
  const page = pages.find((candidate) => candidate.id === step.pageId)
  const element = page?.elements.find((candidate) => candidate.id === step.elementId)
  if (!page || !element) throw new Error(`Step '${step.label}' has no valid page and element reference.`)
  return { page, element }
}

function resolveValue(step: EmbeddedTestStep, secrets: Record<string, string>) {
  if (!step.value) return ''
  if (step.value.kind === 'literal') return step.value.value
  if (!(step.value.value in secrets)) throw new Error(`Secret '${step.value.value}' is not configured.`)
  return secrets[step.value.value]
}

function abortableDelay(durationMs: number, signal: AbortSignal) {
  return new Promise<void>((resolve, reject) => {
    if (signal.aborted) {
      reject(signal.reason ?? new DOMException('Run cancelled.', 'AbortError'))
      return
    }
    const onAbort = () => {
      clearTimeout(timeout)
      reject(signal.reason ?? new DOMException('Run cancelled.', 'AbortError'))
    }
    const timeout = setTimeout(() => {
      signal.removeEventListener('abort', onAbort)
      resolve()
    }, durationMs)
    signal.addEventListener('abort', onAbort, { once: true })
  })
}

function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === 'AbortError'
}

function safeFilePart(value: string) {
  return value.replace(/[^a-zA-Z0-9_-]+/g, '-').slice(0, 128) || 'step'
}

function isNavigationAborted(error: unknown) {
  return typeof error === 'object'
    && error !== null
    && 'code' in error
    && error.code === 'ERR_ABORTED'
}

function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}
