import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'
import type {
  BrowserKind,
  CliqBridge,
  CliqSettings,
  PageDefinition,
  PageScanResult,
  ProjectManifest,
  WorkspaceSnapshot,
} from './types'

afterEach(() => {
  cleanup()
  delete window.cliq
})

describe('App', () => {
  it('offers to open a workspace when none is active', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: 'Open workspace' })).toBeTruthy()
    expect(screen.getByText(/Generate maintainable Selenium code/i)).toBeTruthy()
  })

  it('opens the workspace base URL in the embedded browser', async () => {
    const navigate = vi.fn(async () => undefined)
    const snapshot: WorkspaceSnapshot = {
      rootPath: '/mock',
      project: mockProject(),
      pages: [],
      tests: [],
    }
    window.cliq = bridgeFor(snapshot, navigate)

    render(<App />)

    await waitFor(() => expect(navigate).toHaveBeenCalledWith(snapshot.project.baseUrl))
  })

  it('lets the user name a new page before scanning', async () => {
    const scanPage = vi.fn(async (url: string, name: string): Promise<PageScanResult> => ({
      page: { schemaVersion: 1, id: 'p1', name, url, elements: [], updatedAt: new Date().toISOString() },
      reconciliation: { matchedCount: 0, newCount: 0, missingCount: 0 },
    }))
    const snapshot: WorkspaceSnapshot = {
      rootPath: '/mock',
      project: mockProject(),
      pages: [],
      tests: [],
    }
    window.cliq = bridgeFor(snapshot, async () => undefined, scanPage)

    render(<App />)
    const nameInput = await screen.findByLabelText('Page object name') as HTMLInputElement
    await waitFor(() => expect(nameInput.disabled).toBe(false))

    fireEvent.change(nameInput, { target: { value: 'Custom Login' } })
    fireEvent.click(screen.getByRole('button', { name: /Scan/i }))

    await waitFor(() => expect(scanPage).toHaveBeenCalledWith(snapshot.project.baseUrl, 'Custom Login', 'chrome'))
  })

  it('renames an already-scanned page when the name field is edited and loses focus', async () => {
    const existingPage = mockPage()
    const savePage = vi.fn(async (page: PageDefinition) => page)
    const snapshot: WorkspaceSnapshot = {
      rootPath: '/mock',
      project: mockProject(),
      pages: [existingPage],
      tests: [],
    }
    window.cliq = bridgeFor(snapshot, async () => undefined, undefined, savePage)

    render(<App />)
    const nameInput = await screen.findByLabelText('Page object name') as HTMLInputElement
    await waitFor(() => expect(nameInput.value).toBe('Existing Login'))
    expect(nameInput.disabled).toBe(false)

    fireEvent.change(nameInput, { target: { value: 'Sign In' } })
    fireEvent.blur(nameInput)

    await waitFor(() => expect(savePage).toHaveBeenCalledWith({ ...existingPage, name: 'Sign In' }))
  })

  it('does not call savePage when the name field loses focus unchanged', async () => {
    const existingPage = mockPage()
    const savePage = vi.fn(async (page: PageDefinition) => page)
    const snapshot: WorkspaceSnapshot = {
      rootPath: '/mock',
      project: mockProject(),
      pages: [existingPage],
      tests: [],
    }
    window.cliq = bridgeFor(snapshot, async () => undefined, undefined, savePage)

    render(<App />)
    const nameInput = await screen.findByLabelText('Page object name') as HTMLInputElement
    await waitFor(() => expect(nameInput.value).toBe('Existing Login'))

    fireEvent.blur(nameInput)

    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(savePage).not.toHaveBeenCalled()
  })
})

function mockProject(): ProjectManifest {
  return {
    schemaVersion: 1,
    id: 'mock',
    name: 'Mock',
    baseUrl: 'http://127.0.0.1:4174/',
    defaultBrowser: 'chrome',
    output: {
      pageObjectsDirectory: 'Generated/PageObjects',
      testsDirectory: 'Generated/Tests',
      pageObjectsNamespace: 'Generated.PageObjects',
      testsNamespace: 'Generated.Tests',
    },
    updatedAt: new Date().toISOString(),
  }
}

function mockPage(): PageDefinition {
  return {
    schemaVersion: 1,
    id: 'login',
    name: 'Existing Login',
    url: 'http://127.0.0.1:4174/',
    elements: [],
    updatedAt: new Date().toISOString(),
  }
}

function bridgeFor(
  snapshot: WorkspaceSnapshot,
  navigate: (url: string) => Promise<void>,
  scanPage: (url: string, name: string, browser?: BrowserKind) => Promise<PageScanResult> =
    async () => { throw new Error('Not used') },
  savePage: (page: PageDefinition) => Promise<PageDefinition> = async (page) => page,
): CliqBridge {
  return {
    workspace: {
      chooseAndOpen: async () => snapshot,
      get: async () => snapshot,
      savePage,
      saveTest: async (test) => test,
      deleteTest: async () => undefined,
      scanPage,
      generate: async () => ({ artifacts: [], warnings: [] }),
      startRun: async () => { throw new Error('Not used') },
      cancelRun: async () => undefined,
      saveSecrets: async () => undefined,
      getSettings: async () => testSettings(),
      saveSettings: async (settings: CliqSettings) => settings,
      getScreenshot: async () => null,
      onRunEvent: () => () => undefined,
      onOpened: () => () => undefined,
    },
    browser: {
      navigate,
      back: async () => undefined,
      forward: async () => undefined,
      reload: async () => undefined,
      setBounds: async () => undefined,
      setVisible: async () => undefined,
      onState: () => () => undefined,
    },
  }
}

function testSettings(): CliqSettings {
  return {
    schemaVersion: 1,
    elementIdentification: { testIdAttribute: 'data-testid', strategyOrder: ['testId', 'xpath'], ignoreIdPatterns: [] },
    testExecution: { defaultStepTimeoutMs: 10_000, pageReadyTimeoutMs: 15_000, captureScreenshotOnFailure: true },
    scanning: { loadTimeoutMs: 30_000, networkIdleGraceMs: 3_000, captureScreenshot: true },
  }
}
