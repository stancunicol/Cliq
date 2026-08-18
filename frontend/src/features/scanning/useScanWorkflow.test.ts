import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { useScanWorkflow } from './useScanWorkflow'
import { screenshotService, workspaceService } from '../../services'
import type { DynamicElementSuggestion, PageDefinition, PageScanResult, WorkspaceSnapshot } from '../../types'

vi.mock('../../services', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../services')>()
  return {
    ...actual,
    workspaceService: { ...actual.workspaceService, scanPage: vi.fn() },
    screenshotService: { ...actual.screenshotService, getDiscovery: vi.fn() },
  }
})

afterEach(() => vi.resetAllMocks())

function snapshot(overrides: Partial<WorkspaceSnapshot> = {}): WorkspaceSnapshot {
  return {
    rootPath: '/mock',
    project: {
      schemaVersion: 1,
      id: 'mock',
      name: 'Mock',
      baseUrl: 'https://example.com',
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
    ...overrides,
  }
}

function page(overrides: Partial<PageDefinition> = {}): PageDefinition {
  return {
    schemaVersion: 1,
    id: 'login',
    name: 'Login',
    url: 'https://example.com/login',
    elements: [],
    updatedAt: new Date().toISOString(),
    ...overrides,
  }
}

function suggestion(overrides: Partial<DynamicElementSuggestion> = {}): DynamicElementSuggestion {
  return {
    name: 'row_button',
    action: 'click',
    strategy: 'xpath',
    locatorTemplate: '//table/tbody/tr[{param}]/td[1]/button',
    sampleCount: 5,
    ...overrides,
  }
}

// No default value: a default parameter only applies when the argument is *omitted*, and would
// silently substitute a real snapshot for a caller explicitly passing `undefined` to test the
// no-open-workspace guard.
function setup(snapshotValue: WorkspaceSnapshot | undefined) {
  const updateSnapshot = vi.fn()
  const setWorking = vi.fn()
  const setError = vi.fn()
  const { result } = renderHook(() =>
    useScanWorkflow({ snapshot: snapshotValue, updateSnapshot, setWorking, setError }),
  )
  return { result, updateSnapshot, setWorking, setError }
}

describe('useScanWorkflow', () => {
  describe('scanPage', () => {
    it('does nothing when there is no open workspace', async () => {
      const { result, updateSnapshot } = setup(undefined)

      await act(async () => { await result.current.scanPage('https://example.com', 'Login') })

      expect(workspaceService.scanPage).not.toHaveBeenCalled()
      expect(updateSnapshot).not.toHaveBeenCalled()
    })

    it('does nothing when the URL is empty', async () => {
      const { result, updateSnapshot } = setup(snapshot())

      await act(async () => { await result.current.scanPage('', 'Login') })

      expect(workspaceService.scanPage).not.toHaveBeenCalled()
      expect(updateSnapshot).not.toHaveBeenCalled()
    })

    it('upserts the returned page and reports the first-scan count', async () => {
      const scanned = page()
      const scanResult: PageScanResult = {
        page: scanned,
        reconciliation: { matchedCount: 0, newCount: 3, missingCount: 0, isFirstScan: true },
      }
      vi.mocked(workspaceService.scanPage).mockResolvedValueOnce(scanResult)
      const { result, updateSnapshot, setWorking } = setup(snapshot())

      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })

      expect(workspaceService.scanPage).toHaveBeenCalledWith('https://example.com/login', 'Login', 'chrome')
      expect(updateSnapshot).toHaveBeenCalledWith(expect.objectContaining({ pages: [scanned] }))
      expect(setWorking).toHaveBeenCalledWith('Scanned: 3 elements found')
    })

    it('reports matched/new/missing counts for a reconciled (non-first) scan', async () => {
      const scanResult: PageScanResult = {
        page: page(),
        reconciliation: { matchedCount: 5, newCount: 1, missingCount: 2, isFirstScan: false },
      }
      vi.mocked(workspaceService.scanPage).mockResolvedValueOnce(scanResult)
      const { result, setWorking } = setup(snapshot())

      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })

      expect(setWorking).toHaveBeenCalledWith('Scanned: 5 matched · 1 new · 2 missing')
    })

    it('replaces an existing page with the same id instead of duplicating it', async () => {
      const existing = page({ name: 'Old name' })
      const rescanned = page({ name: 'New name' })
      vi.mocked(workspaceService.scanPage).mockResolvedValueOnce({
        page: rescanned,
        reconciliation: { matchedCount: 1, newCount: 0, missingCount: 0, isFirstScan: false },
      })
      const { result, updateSnapshot } = setup(snapshot({ pages: [existing] }))

      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })

      expect(updateSnapshot).toHaveBeenCalledWith(expect.objectContaining({ pages: [rescanned] }))
    })

    it('surfaces a failed scan as an error and clears the working indicator', async () => {
      vi.mocked(workspaceService.scanPage).mockRejectedValueOnce(new Error('scan failed'))
      const { result, setError, setWorking } = setup(snapshot())

      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })

      expect(setError).toHaveBeenCalledWith('scan failed')
      expect(setWorking).toHaveBeenLastCalledWith(undefined)
    })
  })

  describe('toggleScanScreenshot', () => {
    it('loads the screenshot once on first open and does not re-fetch on later toggles', async () => {
      vi.mocked(workspaceService.scanPage).mockResolvedValueOnce({
        page: page(),
        reconciliation: { matchedCount: 0, newCount: 1, missingCount: 0, isFirstScan: true },
        screenshotPath: 'scan.png',
      })
      const { result } = setup(snapshot())
      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })

      vi.mocked(screenshotService.getDiscovery).mockResolvedValueOnce('data:image/png;base64,x')
      await act(async () => { await result.current.toggleScanScreenshot() })
      expect(result.current.scanScreenshotOpen).toBe(true)
      expect(result.current.scanScreenshotState).toEqual({ status: 'ready', url: 'data:image/png;base64,x' })
      expect(screenshotService.getDiscovery).toHaveBeenCalledTimes(1)

      await act(async () => { await result.current.toggleScanScreenshot() })
      expect(result.current.scanScreenshotOpen).toBe(false)

      await act(async () => { await result.current.toggleScanScreenshot() })
      expect(result.current.scanScreenshotOpen).toBe(true)
      expect(screenshotService.getDiscovery).toHaveBeenCalledTimes(1)
    })
  })

  describe('acceptDynamicElementSuggestion', () => {
    it('removes the suggestion once it is persisted', async () => {
      vi.mocked(workspaceService.scanPage).mockResolvedValueOnce({
        page: page(),
        reconciliation: { matchedCount: 0, newCount: 0, missingCount: 0, isFirstScan: true },
        dynamicElementSuggestions: [suggestion()],
      })
      const { result } = setup(snapshot())
      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })
      expect(result.current.dynamicElementSuggestions).toHaveLength(1)

      // Passes the actual list element back in, not a freshly-built one - acceptDynamicElementSuggestion
      // removes by reference (`item !== suggestion`), matching how DynamicElementSuggestionsBanner's
      // onAccept hands back whichever array entry the user clicked.
      const offered = result.current.dynamicElementSuggestions[0]
      const saveDynamicElement = vi.fn().mockResolvedValueOnce(true)
      await act(async () => {
        await result.current.acceptDynamicElementSuggestion(offered, saveDynamicElement)
      })

      expect(saveDynamicElement).toHaveBeenCalledWith('login', expect.objectContaining({ name: 'row_button' }))
      expect(result.current.dynamicElementSuggestions).toHaveLength(0)
    })

    it('keeps the suggestion offered when persisting it fails', async () => {
      vi.mocked(workspaceService.scanPage).mockResolvedValueOnce({
        page: page(),
        reconciliation: { matchedCount: 0, newCount: 0, missingCount: 0, isFirstScan: true },
        dynamicElementSuggestions: [suggestion()],
      })
      const { result } = setup(snapshot())
      await act(async () => { await result.current.scanPage('https://example.com/login', 'Login') })

      const offered = result.current.dynamicElementSuggestions[0]
      const saveDynamicElement = vi.fn().mockResolvedValueOnce(false)
      await act(async () => {
        await result.current.acceptDynamicElementSuggestion(offered, saveDynamicElement)
      })

      expect(result.current.dynamicElementSuggestions).toHaveLength(1)
    })
  })
})
