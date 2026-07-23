import { BaseWindow, session, WebContentsView, type Rectangle } from 'electron'
import { mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { recordedInteractionSchema } from './ipc-schemas.js'

const partition = 'persist:cliq-browser'
const beforeActionDelayMs = 650
const afterActionDelayMs = 450

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
  locator: EmbeddedLocator
}

export interface EmbeddedPage {
  id: string
  elements: EmbeddedPageElement[]
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

export class BrowserController {
  readonly view: WebContentsView
  private recording = false
  private visible = true
  private error?: string
  private bounds: Rectangle = { x: 0, y: 102, width: 0, height: 0 }
  private pendingNavigation?: { url: string; promise: Promise<void> }

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
    signal: AbortSignal,
    onEvent: (event: EmbeddedRunEvent) => void,
  ): Promise<EmbeddedTestRun> {
    const results: EmbeddedStepResult[] = []
    try {
      for (const step of test.steps.filter((candidate) => candidate.enabled)) {
        signal.throwIfAborted()
        onEvent({ type: 'step-start', stepId: step.id })
        const started = Date.now()
        try {
          await this.executeStep(step, pages, secrets, signal)
          const result: EmbeddedStepResult = {
            stepId: step.id,
            status: 'passed',
            durationMs: Date.now() - started,
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
            screenshotPath: await this.captureFailure(workspaceRoot, run.id, step.id),
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
    signal: AbortSignal,
  ): Promise<void> {
    if (step.action === 'navigate') {
      const url = step.url
      if (!url || !isHttpUrl(url)) throw new Error('Navigate step has no valid HTTP or HTTPS URL.')
      await abortableDelay(300, signal)
      this.error = undefined
      await this.view.webContents.loadURL(url)
      this.applyBounds()
      this.publish()
      await abortableDelay(afterActionDelayMs, signal)
      return
    }

    if (step.action === 'assertUrl') {
      await abortableDelay(beforeActionDelayMs, signal)
      const expected = resolveValue(step, secrets)
      if (!new RegExp(expected).test(this.view.webContents.getURL())) {
        throw new Error(`URL '${this.view.webContents.getURL()}' does not match '${expected}'.`)
      }
      await abortableDelay(afterActionDelayMs, signal)
      return
    }

    const element = resolveElement(step, pages)
    const highlight = await this.highlight(element.locator)
    try {
      await abortableDelay(beforeActionDelayMs, signal)
      signal.throwIfAborted()
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
          await this.focusElement(element.locator, true)
          this.selectAll()
          await this.view.webContents.insertText(resolveValue(step, secrets))
          await this.commitField(element.locator)
          break
        case 'select':
          await this.selectElement(element.locator, resolveValue(step, secrets))
          break
        case 'pressKey':
          await this.focusElement(element.locator, false)
          this.pressKey(resolveValue(step, secrets))
          break
        case 'assertVisible':
          if (!highlight.visible) throw new Error(`Element '${element.name}' is not visible.`)
          break
        case 'assertText': {
          const actual = await this.elementText(element.locator)
          const expected = resolveValue(step, secrets)
          if (actual !== expected) {
            throw new Error(`Expected text '${expected}' but found '${actual}'.`)
          }
          break
        }
      }
      await abortableDelay(afterActionDelayMs, signal)
      if (this.view.webContents.isLoading()) {
        await this.waitForLoad(signal)
      }
    } finally {
      await this.restoreHighlight(element.locator, highlight).catch(() => undefined)
    }
  }

  private async highlight(locator: EmbeddedLocator) {
    return this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator)};
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
        const element = ${elementLookupScript(locator)};
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
        const element = ${elementLookupScript(locator)};
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
        const element = ${elementLookupScript(locator)};
        element.dispatchEvent(new Event('change', { bubbles: true }));
        element.blur();
      })()
    `)
  }

  private async selectElement(locator: EmbeddedLocator, value: string) {
    await this.view.webContents.executeJavaScript(`
      (() => {
        const element = ${elementLookupScript(locator)};
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
        const element = ${elementLookupScript(locator)};
        return (element.innerText ?? element.textContent ?? '').replace(/\\s+/g, ' ').trim();
      })()
    `) as Promise<string>
  }

  private async waitForLoad(signal: AbortSignal) {
    const deadline = Date.now() + 15_000
    while (this.view.webContents.isLoading()) {
      signal.throwIfAborted()
      if (Date.now() >= deadline) throw new Error('The page did not finish loading within 15 seconds.')
      await abortableDelay(100, signal)
    }
  }

  private async captureFailure(workspaceRoot: string, runId: string, stepId: string) {
    try {
      const directory = path.join(workspaceRoot, '.cliq', 'runs', safeFilePart(runId))
      await mkdir(directory, { recursive: true })
      const screenshotPath = path.join(directory, `${safeFilePart(stepId)}.png`)
      const image = await this.view.webContents.capturePage()
      await writeFile(screenshotPath, image.toPNG())
      return screenshotPath
    } catch {
      return undefined
    }
  }
}

function elementLookupScript(locator: EmbeddedLocator) {
  const serialized = JSON.stringify(locator)
  return `(() => {
    const locator = ${serialized};
    let element;
    if (locator.strategy === 'testId') {
      element = [...document.querySelectorAll('[data-testid]')]
        .find(candidate => candidate.getAttribute('data-testid') === locator.value);
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

function resolveElement(step: EmbeddedTestStep, pages: EmbeddedPage[]) {
  const page = pages.find((candidate) => candidate.id === step.pageId)
  const element = page?.elements.find((candidate) => candidate.id === step.elementId)
  if (!element) throw new Error(`Step '${step.label}' has no valid page and element reference.`)
  return element
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
