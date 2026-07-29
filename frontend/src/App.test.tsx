import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
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

  it('lets the user edit and save workspace settings, rejecting invalid regex patterns', async () => {
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
    const saveSettings = vi.fn(async (settings: CliqSettings) => settings)
    window.cliq = bridgeFor(snapshot, async () => undefined, saveSettings)

    render(<App />)
    await waitFor(() => expect(screen.getByRole('button', { name: /Settings/i })).toBeTruthy())
    fireEvent.click(screen.getByRole('button', { name: /Settings/i }))

    await waitFor(() => expect(screen.getByText('Element identification')).toBeTruthy())
    expect(screen.getByText('Test execution')).toBeTruthy()
    expect(screen.getByText('Page scanning')).toBeTruthy()

    const saveButton = screen.getByRole('button', { name: /Save changes/i }) as HTMLButtonElement
    expect(saveButton.disabled).toBe(false)

    fireEvent.click(screen.getByRole('button', { name: /Add pattern/i }))
    const patternInputs = screen.getAllByLabelText(/Ignore id pattern/i)
    const newPattern = patternInputs.at(-1)!
    fireEvent.change(newPattern, { target: { value: '(' } })

    await waitFor(() => expect(screen.getByText(/valid regular expression/i)).toBeTruthy())
    expect(saveButton.disabled).toBe(true)

    fireEvent.change(newPattern, { target: { value: '^tmp-\\d+$' } })
    await waitFor(() => expect(screen.queryByText(/valid regular expression/i)).toBeNull())
    expect(saveButton.disabled).toBe(false)

    fireEvent.click(saveButton)
    await waitFor(() => expect(saveSettings).toHaveBeenCalledTimes(1))
    expect(saveSettings.mock.calls[0][0].elementIdentification.ignoreIdPatterns.at(-1)).toBe('^tmp-\\d+$')
    await waitFor(() => expect(screen.getByText('Saved')).toBeTruthy())
  })
})

function bridgeFor(
  snapshot: WorkspaceSnapshot,
  navigate: (url: string) => Promise<void>,
  saveSettings: (settings: CliqSettings) => Promise<CliqSettings> = async (settings) => settings,
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
      saveSettings,
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
    elementIdentification: { testIdAttribute: 'data-testid', strategyOrder: ['testId', 'id', 'name'], ignoreIdPatterns: [] },
    testExecution: { defaultStepTimeoutMs: 10_000, pageReadyTimeoutMs: 15_000, captureScreenshotOnFailure: true },
    scanning: { loadTimeoutMs: 30_000, networkIdleGraceMs: 3_000, captureScreenshot: true },
  }
}
