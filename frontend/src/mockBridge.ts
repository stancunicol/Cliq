import type {
  BrowserState,
  CliqBridge,
  CliqSettings,
  PageDefinition,
  RunEvent,
  TestCase,
  TestRun,
  WorkspaceSnapshot,
} from './types'

export function installMockBridge() {
  let snapshot = mockSnapshot()
  let settings = mockSettings()
  let browserState: BrowserState = {
    url: snapshot.project.baseUrl,
    title: 'Cliq recorder fixture',
    loading: false,
    canGoBack: false,
    canGoForward: false,
  }
  const openedListeners = new Set<(value: WorkspaceSnapshot) => void>()
  const stateListeners = new Set<(value: BrowserState) => void>()
  const runListeners = new Set<(event: RunEvent) => void>()

  const bridge: CliqBridge = {
    workspace: {
      chooseAndOpen: async () => snapshot,
      get: async () => snapshot,
      savePage: async (page) => {
        snapshot = { ...snapshot, pages: upsert(snapshot.pages, page) }
        return page
      },
      saveTest: async (test) => {
        snapshot = { ...snapshot, tests: upsert(snapshot.tests, test) }
        return test
      },
      deleteTest: async (id) => {
        snapshot = { ...snapshot, tests: snapshot.tests.filter((test) => test.id !== id) }
      },
      scanPage: async (url, name) => {
        const existing = snapshot.pages.find((candidate) => candidate.id === slug(name))
        const page = { ...mockPage(), url, name, id: slug(name), updatedAt: new Date().toISOString() }
        snapshot = { ...snapshot, pages: upsert(snapshot.pages, page) }
        return {
          page,
          reconciliation: existing
            ? { matchedCount: page.elements.length, newCount: 0, missingCount: 0, isFirstScan: false }
            : { matchedCount: 0, newCount: page.elements.length, missingCount: 0, isFirstScan: true },
        }
      },
      generate: async (pageIds, testIds) => ({
        artifacts: [
          ...(pageIds ?? snapshot.pages.map((page) => page.id)).map((id) => ({
            kind: 'pageObject',
            name: `${id}Page`,
            path: `/mock/Generated/PageObjects/${id}Page.cs`,
            created: true,
          })),
          ...(testIds ?? snapshot.tests.map((test) => test.id)).map((id) => ({
            kind: 'test',
            name: `${id}Tests`,
            path: `/mock/Generated/Tests/${id}Tests.cs`,
            created: true,
          })),
        ],
        warnings: [],
      }),
      startRun: async (testId) => {
        const test = snapshot.tests.find((candidate) => candidate.id === testId)
        if (!test) throw new Error(`Test '${testId}' was not found.`)
        const run: TestRun = {
          id: crypto.randomUUID().replaceAll('-', ''),
          testId,
          status: 'queued',
          startedAt: new Date().toISOString(),
          steps: [],
        }
        window.setTimeout(() => {
          test.steps.filter((step) => step.enabled).forEach((step, index) => {
            window.setTimeout(() => {
              runListeners.forEach((listener) => listener({ type: 'step-start', stepId: step.id }))
              window.setTimeout(() => runListeners.forEach((listener) => listener({
                type: 'step',
                step: { stepId: step.id, status: 'passed', durationMs: 12 + index },
              })), 650)
            }, 900 * index + 100)
          })
          window.setTimeout(() => {
            const completed: TestRun = {
              ...run,
              status: 'passed',
              completedAt: new Date().toISOString(),
              steps: test.steps.filter((step) => step.enabled).map((step, index) => ({
                stepId: step.id,
                status: 'passed',
                durationMs: 12 + index,
              })),
            }
            runListeners.forEach((listener) => listener({ type: 'done', run: completed }))
          }, 900 * test.steps.length + 850)
        }, 0)
        return run
      },
      cancelRun: async () => undefined,
      saveSecrets: async () => undefined,
      getSettings: async () => settings,
      saveSettings: async (next) => {
        settings = next
        return settings
      },
      getScreenshot: async () => null,
      getDiscoveryScreenshot: async () => null,
      onRunEvent: (callback) => subscribe(runListeners, callback),
      onOpened: (callback) => subscribe(openedListeners, callback),
    },
    browser: {
      navigate: async (url) => {
        browserState = { ...browserState, url, title: new URL(url).hostname }
        stateListeners.forEach((listener) => listener(browserState))
      },
      back: async () => undefined,
      forward: async () => undefined,
      reload: async () => undefined,
      setBounds: async () => undefined,
      setVisible: async () => undefined,
      onState: (callback) => {
        callback(browserState)
        return subscribe(stateListeners, callback)
      },
    },
  }

  window.cliq = bridge
}

function subscribe<T>(listeners: Set<(value: T) => void>, callback: (value: T) => void) {
  listeners.add(callback)
  return () => listeners.delete(callback)
}

function upsert<T extends { id: string }>(items: T[], item: T) {
  return items.some((candidate) => candidate.id === item.id)
    ? items.map((candidate) => candidate.id === item.id ? item : candidate)
    : [...items, item]
}

function slug(value: string) {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'page'
}

function mockSnapshot(): WorkspaceSnapshot {
  const page = mockPage()
  const test: TestCase = {
    schemaVersion: 1,
    id: 'recorded-workflow',
    name: 'Recorded workflow',
    description: 'Browser fixture used for full UI verification.',
    tags: ['critical'],
    steps: [
      {
        id: 'navigate-fixture',
        action: 'navigate',
        label: 'Open recorder fixture',
        enabled: true,
        url: page.url,
      },
      {
        id: 'click-counter',
        action: 'click',
        label: 'Click counter',
        enabled: true,
        pageId: page.id,
        elementId: 'counter-button',
      },
      {
        id: 'assert-counter',
        action: 'assertText',
        label: 'Verify counter changed',
        enabled: true,
        pageId: page.id,
        elementId: 'counter-output',
        value: { kind: 'literal', value: 'Count: 1' },
      },
    ],
    updatedAt: new Date().toISOString(),
  }
  return {
    rootPath: '/mock/cliq-project',
    project: {
      schemaVersion: 1,
      id: 'cliq-mock',
      name: 'Cliq mock project',
      baseUrl: page.url,
      defaultBrowser: 'chrome',
      output: {
        pageObjectsDirectory: 'Generated/PageObjects',
        testsDirectory: 'Generated/Tests',
        pageObjectsNamespace: 'Generated.PageObjects',
        testsNamespace: 'Generated.Tests',
      },
      updatedAt: new Date().toISOString(),
    },
    pages: [page],
    tests: [test],
  }
}

function mockSettings(): CliqSettings {
  return {
    schemaVersion: 1,
    elementIdentification: {
      testIdAttribute: 'data-testid',
      strategyOrder: ['testId', 'xpath'],
      ignoreIdPatterns: [
        '^ember\\d+$',
        '^react-select-\\d+-',
        '^mat-[a-z]+(-[a-z]+)*-\\d+$',
        '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$',
      ],
    },
    testExecution: {
      defaultStepTimeoutMs: 10_000,
      pageReadyTimeoutMs: 15_000,
      captureScreenshotOnFailure: true,
      highlightPauseMs: 400,
    },
    scanning: {
      loadTimeoutMs: 30_000,
      networkIdleGraceMs: 3_000,
      captureScreenshot: true,
    },
  }
}

function mockPage(): PageDefinition {
  return {
    schemaVersion: 1,
    id: 'recorder-fixture',
    name: 'Recorder fixture',
    url: 'http://127.0.0.1:4174/',
    elements: [
      {
        id: 'counter-button',
        name: 'increment counter',
        tag: 'button',
        action: 'click',
        locator: { strategy: 'testId', value: 'increment-counter' },
      },
      {
        id: 'display-name',
        name: 'display name',
        tag: 'input',
        action: 'fill',
        locator: { strategy: 'css', value: '#display-name' },
      },
      {
        id: 'counter-output',
        name: 'counter output',
        tag: 'output',
        action: 'assertText',
        locator: { strategy: 'css', value: 'output' },
      },
    ],
    updatedAt: new Date().toISOString(),
  }
}
