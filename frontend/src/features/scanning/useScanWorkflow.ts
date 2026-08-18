import { useState } from 'react'
import { loadScreenshotState, screenshotService, workspaceService } from '../../services'
import type {
  DynamicElement,
  DynamicElementSuggestion,
  LocatorDiffReport,
  ScreenshotState,
  WorkspaceSnapshot,
} from '../../types'

function upsert<T extends { id: string }>(items: T[], item: T) {
  const index = items.findIndex((candidate) => candidate.id === item.id)
  return index < 0 ? [...items, item] : items.map((candidate) => candidate.id === item.id ? item : candidate)
}

function message(cause: unknown) {
  return cause instanceof Error ? cause.message : String(cause)
}

/** Owns the "scan a page, then review what changed" state: the locator diff/dynamic-element-
 * suggestion banners, and the scan's own screenshot panel. `setWorking`/`setError` are the App-level
 * shared indicators every operation reports through (so scanning shows up the same way generate or
 * run do), not private to this hook. */
export function useScanWorkflow({
  snapshot,
  updateSnapshot,
  setWorking,
  setError,
}: {
  snapshot: WorkspaceSnapshot | undefined
  updateSnapshot(update: WorkspaceSnapshot | ((current: WorkspaceSnapshot) => WorkspaceSnapshot)): void
  setWorking(value: string | undefined): void
  setError(value: string | undefined): void
}) {
  const [locatorDiff, setLocatorDiff] = useState<LocatorDiffReport>()
  const [dynamicElementSuggestions, setDynamicElementSuggestions] = useState<DynamicElementSuggestion[]>([])
  const [suggestionsPageId, setSuggestionsPageId] = useState<string>()
  const [scanScreenshot, setScanScreenshot] = useState<string>()
  const [scanScreenshotOpen, setScanScreenshotOpen] = useState(false)
  const [scanScreenshotState, setScanScreenshotState] = useState<ScreenshotState>()

  async function scanPage(url: string, name: string) {
    if (!snapshot || !url) return
    setWorking('Scanning...')
    setLocatorDiff(undefined)
    setDynamicElementSuggestions([])
    setScanScreenshot(undefined)
    setScanScreenshotOpen(false)
    setScanScreenshotState(undefined)
    try {
      const { page, reconciliation, locatorDiff, dynamicElementSuggestions, screenshotPath } =
        await workspaceService.scanPage(url, name, snapshot.project.defaultBrowser)
      updateSnapshot({ ...snapshot, pages: upsert(snapshot.pages, page) })
      setWorking(
        reconciliation.isFirstScan
          ? `Scanned: ${reconciliation.newCount} elements found`
          : `Scanned: ${reconciliation.matchedCount} matched · ${reconciliation.newCount} new · ${reconciliation.missingCount} missing`,
      )
      setTimeout(() => setWorking(undefined), 3000)
      setLocatorDiff(locatorDiff)
      setDynamicElementSuggestions(dynamicElementSuggestions ?? [])
      setSuggestionsPageId(page.id)
      setScanScreenshot(screenshotPath)
    } catch (cause) {
      setError(message(cause))
      setWorking(undefined)
    }
  }

  async function toggleScanScreenshot() {
    if (scanScreenshotOpen) {
      setScanScreenshotOpen(false)
      return
    }
    setScanScreenshotOpen(true)
    if (!scanScreenshotState && scanScreenshot) {
      setScanScreenshotState(await loadScreenshotState(() => screenshotService.getDiscovery(scanScreenshot)))
    }
  }

  // Only drops the suggestion from the list once it's actually persisted - e.g. a name collision
  // with an existing element (WorkspaceRepository.ValidateDynamicElements) fails the save and
  // shows an error, but the suggestion should stay offered rather than silently vanish with no way
  // to retry it short of re-scanning.
  async function acceptDynamicElementSuggestion(
    suggestion: DynamicElementSuggestion,
    saveDynamicElement: (pageId: string, element: DynamicElement) => Promise<boolean>,
  ) {
    if (!suggestionsPageId) return
    const saved = await saveDynamicElement(suggestionsPageId, {
      id: crypto.randomUUID(),
      name: suggestion.name,
      action: suggestion.action,
      strategy: suggestion.strategy,
      locatorTemplate: suggestion.locatorTemplate,
      parameterType: 'int',
    })
    if (saved) {
      setDynamicElementSuggestions((current) => current.filter((item) => item !== suggestion))
    }
  }

  return {
    locatorDiff,
    setLocatorDiff,
    dynamicElementSuggestions,
    setDynamicElementSuggestions,
    scanScreenshot,
    scanScreenshotOpen,
    scanScreenshotState,
    scanPage,
    toggleScanScreenshot,
    acceptDynamicElementSuggestion,
  }
}
