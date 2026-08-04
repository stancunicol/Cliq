import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'
import type { CliqBridge, CliqSettings, WorkspaceSnapshot } from './types'

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
      project: {
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
      },
      pages: [],
      tests: [],
    }
    window.cliq = bridgeFor(snapshot, navigate)

    render(<App />)

    await waitFor(() => expect(navigate).toHaveBeenCalledWith(snapshot.project.baseUrl))
  })

})

function bridgeFor(
  snapshot: WorkspaceSnapshot,
  navigate: (url: string) => Promise<void>,
): CliqBridge {
  return {
    workspace: {
      chooseAndOpen: async () => snapshot,
      get: async () => snapshot,
      savePage: async (page) => page,
      saveTest: async (test) => test,
      deleteTest: async () => undefined,
      scanPage: async () => { throw new Error('Not used') },
      generate: async () => ({ artifacts: [] }),
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
      setRecording: async () => undefined,
      onState: () => () => undefined,
      onRecordedInteraction: () => () => undefined,
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
