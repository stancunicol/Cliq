import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Camera, CirclePlay, Code2, LoaderCircle, ScanSearch } from 'lucide-react'
import { BrowserPane } from './features/browser/BrowserPane'
import { GenerationWarningBanner } from './features/generation/GenerationWarningBanner'
import { RunResults } from './features/runs/RunResults'
import { DynamicElementSuggestionsBanner } from './features/scanning/DynamicElementSuggestionsBanner'
import { LocatorDiffBanner } from './features/scanning/LocatorDiffBanner'
import { TestEditor } from './features/tests/TestEditor'
import { Sidebar } from './features/workspace/Sidebar'
import { screenshotService, workspaceService } from './services'
import type {
  BrowserState,
  CustomMethodRecoveryWarning,
  DynamicElement,
  DynamicElementSuggestion,
  LocatorDiffReport,
  TestCase,
  TestRun,
  WorkspaceSnapshot,
} from './types'

const defaultBrowser: BrowserState = {
  url: '',
  title: 'New tab',
  loading: false,
  canGoBack: false,
  canGoForward: false,
}

export function App() {
  const [snapshot, setSnapshot] = useState<WorkspaceSnapshot>()
  const [selectedId, setSelectedId] = useState<string>()
  const [active, setActive] = useState<'tests' | 'runs'>('tests')
  const [search, setSearch] = useState('')
  const [browser, setBrowser] = useState(defaultBrowser)
  const [url, setUrl] = useState('https://example.com')
  const [run, setRun] = useState<TestRun>()
  const [running, setRunning] = useState(false)
  const [activeStepId, setActiveStepId] = useState<string>()
  const [working, setWorking] = useState<string>()
  const [error, setError] = useState<string>()
  const [locatorDiff, setLocatorDiff] = useState<LocatorDiffReport>()
  const [dynamicElementSuggestions, setDynamicElementSuggestions] = useState<DynamicElementSuggestion[]>([])
  const [suggestionsPageId, setSuggestionsPageId] = useState<string>()
  const [scanScreenshot, setScanScreenshot] = useState<string>()
  const [scanScreenshotOpen, setScanScreenshotOpen] = useState(false)
  const [scanScreenshotState, setScanScreenshotState] = useState<
    { status: 'ready'; url: string } | { status: 'missing' } | { status: 'error'; message: string }
  >()
  const [generationWarnings, setGenerationWarnings] = useState<CustomMethodRecoveryWarning[]>([])
  const [pageName, setPageName] = useState('')
  const snapshotRef = useRef<WorkspaceSnapshot | undefined>(undefined)
  const selectedIdRef = useRef<string | undefined>(undefined)
  const mutationQueue = useRef(Promise.resolve())
  const saveRevisions = useRef(new Map<string, number>())
  const navigatedWorkspace = useRef<string | undefined>(undefined)
  const selected = snapshot?.tests.find((test) => test.id === selectedId)
  const existingPageForUrl = snapshot?.pages.find((page) => page.url === normalizeUrl(url))
  snapshotRef.current = snapshot
  selectedIdRef.current = selectedId

  const updateSnapshot = useCallback((
    update: WorkspaceSnapshot | ((current: WorkspaceSnapshot) => WorkspaceSnapshot),
  ) => {
    setSnapshot((current) => {
      const next = typeof update === 'function'
        ? current && update(current)
        : update
      if (next) snapshotRef.current = next
      return next
    })
  }, [])

  const applySnapshot = useCallback((next: WorkspaceSnapshot) => {
    snapshotRef.current = next
    setSnapshot(next)
    setSelectedId((current) => current && next.tests.some((test) => test.id === current)
      ? current
      : next.tests[0]?.id)
    setUrl(next.project.baseUrl)
    setError(undefined)
  }, [])

  useEffect(() => {
    if (!window.cliq) return
    void workspaceService.get().then(applySnapshot).catch(() => undefined)
    return window.cliq.workspace.onOpened(applySnapshot)
  }, [applySnapshot])

  useEffect(() => window.cliq?.browser.onState((state) => {
    setBrowser(state)
    if (state.url && state.url !== 'about:blank') setUrl(state.url)
  }), [])

  useEffect(() => {
    const existing = snapshotRef.current?.pages.find((page) => page.url === normalizeUrl(url))
    setPageName(existing?.name ?? pageNameFor(url))
  }, [url])

  useEffect(() => {
    if (!snapshot) return
    const workspaceChanged = navigatedWorkspace.current !== snapshot.rootPath
    if (!workspaceChanged && browser.url && browser.url !== 'about:blank') return
    navigatedWorkspace.current = snapshot.rootPath
    if (browser.url === snapshot.project.baseUrl) return
    void window.cliq?.browser.navigate(snapshot.project.baseUrl).catch((cause) => {
      setError(message(cause))
    })
  }, [snapshot, browser.url])

  useEffect(() => window.cliq?.workspace.onRunEvent((event) => {
    if (event.type === 'step-start') {
      setActiveStepId(event.stepId)
    } else if (event.type === 'step') {
      setActiveStepId((current) => current === event.step.stepId ? undefined : current)
      setRun((current) => current
        ? { ...current, status: 'running', steps: [...current.steps, event.step] }
        : current)
    } else {
      setActiveStepId(undefined)
      setRun(event.run)
      setRunning(false)
    }
  }), [])

  useLayoutEffect(() => {
    const sync = () => {
      const pane = document.querySelector('.browser-pane')?.getBoundingClientRect()
      if (pane) {
        void window.cliq?.browser.setBounds({
          x: Math.round(pane.x),
          y: Math.round(pane.y + 102),
          width: Math.round(pane.width),
          height: Math.max(0, Math.round(pane.height - 102)),
        })
      }
    }
    const observer = typeof ResizeObserver === 'undefined' ? undefined : new ResizeObserver(sync)
    const pane = document.querySelector('.browser-pane')
    if (pane) observer?.observe(pane)
    window.addEventListener('resize', sync)
    const frame = requestAnimationFrame(sync)
    return () => {
      observer?.disconnect()
      window.removeEventListener('resize', sync)
      cancelAnimationFrame(frame)
    }
  }, [snapshot])

  async function openWorkspace() {
    try {
      const next = await workspaceService.chooseAndOpen()
      if (next) applySnapshot(next)
    } catch (cause) {
      setError(message(cause))
    }
  }

  function saveTest(test: TestCase) {
    if (!snapshotRef.current) return
    const revision = (saveRevisions.current.get(test.id) ?? 0) + 1
    saveRevisions.current.set(test.id, revision)
    updateSnapshot((current) => ({ ...current, tests: upsert(current.tests, test) }))
    mutationQueue.current = mutationQueue.current
      .then(async () => {
        const saved = await workspaceService.saveTest({ ...test, updatedAt: new Date().toISOString() })
        if (saveRevisions.current.get(test.id) === revision) {
          updateSnapshot((current) => ({ ...current, tests: upsert(current.tests, saved) }))
        }
      })
      .catch((cause) => setError(message(cause)))
  }

  async function createTest() {
    if (!snapshot) return
    const test: TestCase = {
      schemaVersion: 1,
      id: `test-${crypto.randomUUID().slice(0, 8)}`,
      name: pageName.trim() || 'New test',
      tags: [],
      steps: [],
      updatedAt: new Date().toISOString(),
    }
    const saved = await workspaceService.saveTest(test)
    updateSnapshot({ ...snapshot, tests: [...snapshot.tests, saved] })
    setSelectedId(saved.id)
    setActive('tests')
  }

  function navigateBrowser() {
    const target = normalizeUrl(url)
    void window.cliq?.browser.navigate(target).catch((cause) => setError(message(cause)))
  }

  async function renamePageIfChanged() {
    if (!existingPageForUrl) return
    const trimmed = pageName.trim()
    if (!trimmed || trimmed === existingPageForUrl.name) return
    try {
      const saved = await workspaceService.savePage({ ...existingPageForUrl, name: trimmed })
      updateSnapshot((current) => ({ ...current, pages: upsert(current.pages, saved) }))
    } catch (cause) {
      setError(message(cause))
      setPageName(existingPageForUrl.name)
    }
  }

  // Returns whether the save actually succeeded, rather than throwing, so a caller that only
  // wants "fire and forget" (DynamicElementsPanel's onSaveDynamicElement) can ignore it, while one
  // that needs to react to failure (acceptDynamicElementSuggestion, which should keep a rejected
  // suggestion visible instead of discarding it) can check it.
  async function saveDynamicElement(pageId: string, element: DynamicElement): Promise<boolean> {
    const page = snapshotRef.current?.pages.find((candidate) => candidate.id === pageId)
    if (!page) return false
    try {
      const existing = page.dynamicElements ?? []
      const dynamicElements = existing.some((candidate) => candidate.id === element.id)
        ? existing.map((candidate) => candidate.id === element.id ? element : candidate)
        : [...existing, element]
      const saved = await workspaceService.savePage({ ...page, dynamicElements })
      updateSnapshot((current) => ({ ...current, pages: upsert(current.pages, saved) }))
      return true
    } catch (cause) {
      setError(message(cause))
      return false
    }
  }

  async function deleteDynamicElement(pageId: string, elementId: string) {
    const page = snapshotRef.current?.pages.find((candidate) => candidate.id === pageId)
    if (!page) return
    try {
      const saved = await workspaceService.savePage({
        ...page,
        dynamicElements: (page.dynamicElements ?? []).filter((candidate) => candidate.id !== elementId),
      })
      updateSnapshot((current) => ({ ...current, pages: upsert(current.pages, saved) }))
    } catch (cause) {
      setError(message(cause))
    }
  }

  async function scanPage() {
    if (!snapshot || !url) return
    setWorking('Scanning...')
    setLocatorDiff(undefined)
    setDynamicElementSuggestions([])
    setScanScreenshot(undefined)
    setScanScreenshotOpen(false)
    setScanScreenshotState(undefined)
    try {
      const name = pageName.trim() || pageNameFor(url)
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
      try {
        const dataUrl = await screenshotService.getDiscovery(scanScreenshot)
        setScanScreenshotState(dataUrl ? { status: 'ready', url: dataUrl } : { status: 'missing' })
      } catch (cause) {
        setScanScreenshotState({ status: 'error', message: message(cause) })
      }
    }
  }

  async function acceptDynamicElementSuggestion(suggestion: DynamicElementSuggestion) {
    if (!suggestionsPageId) return
    const saved = await saveDynamicElement(suggestionsPageId, {
      id: crypto.randomUUID(),
      name: suggestion.name,
      action: suggestion.action,
      strategy: suggestion.strategy,
      locatorTemplate: suggestion.locatorTemplate,
      parameterType: 'int',
    })
    // Only drop it from the list once it's actually persisted - e.g. a name collision with an
    // existing element (WorkspaceRepository.ValidateDynamicElements) fails the save and shows an
    // error, but the suggestion should stay offered rather than silently vanish with no way to
    // retry it short of re-scanning.
    if (saved) {
      setDynamicElementSuggestions((current) => current.filter((item) => item !== suggestion))
    }
  }

  async function generate() {
    if (!snapshot) return
    setWorking('Generating...')
    try {
      const pageIds = selected
        ? [...new Set([
            ...selected.steps.flatMap((step) => step.pageId ? [step.pageId] : []),
            // Include the page currently shown in the topbar even if no step references it yet -
            // otherwise a page that was just scanned/renamed but not wired into this test's steps
            // silently generates nothing, with no error to explain why.
            ...(existingPageForUrl ? [existingPageForUrl.id] : []),
          ])]
        : undefined
      const result = await workspaceService.generate(pageIds, selected ? [selected.id] : undefined)
      setWorking(`Generated ${result.artifacts.length} artifacts`)
      setTimeout(() => setWorking(undefined), 3000)
      setGenerationWarnings(result.warnings)
    } catch (cause) {
      setError(message(cause))
      setWorking(undefined)
    }
  }

  async function runTest() {
    if (!selected || !snapshot) return
    if (selected.steps.filter((step) => step.enabled).length === 0) {
      setError('Add at least one enabled step before running.')
      return
    }
    setActive('runs')
    setRunning(true)
    setActiveStepId(undefined)
    setRun(undefined)
    try {
      const started = await workspaceService.startRun(selected.id, snapshot.project.defaultBrowser)
      setRun(started)
    } catch (cause) {
      setRunning(false)
      setError(message(cause))
    }
  }

  async function cancelRun() {
    if (!run || !running) return
    try {
      await workspaceService.cancelRun(run.id)
    } catch (cause) {
      setError(message(cause))
    }
  }

  if (!snapshot) {
    return (
      <main className="welcome">
        <div className="welcome-card">
          <div className="logo">C</div>
          <span className="eyebrow">CLIQ WORKBENCH</span>
          <h1>Build workflows. Generate maintainable Selenium code.</h1>
          <p>Open a folder to initialize or load its versioned <code>.cliq</code> workspace.</p>
          <button onClick={() => void openWorkspace()}>Open workspace</button>
          {error && <div className="error-banner">{error}</div>}
        </div>
      </main>
    )
  }

  return (
    <main className="app">
      <Sidebar
        project={snapshot.project}
        tests={snapshot.tests}
        selectedId={selectedId}
        active={active}
        search={search}
        onSearch={setSearch}
        onSelect={(id) => { setSelectedId(id); setActive('tests') }}
        onCreate={() => void createTest()}
        onActive={setActive}
      />
      <section className="workspace">
        <header className="topbar">
          <div className="top-actions">
            <input
              className="page-name-input"
              aria-label="Page object name"
              type="text"
              value={pageName}
              onChange={(event) => setPageName(event.target.value)}
              onBlur={() => void renamePageIfChanged()}
              onKeyDown={(event) => { if (event.key === 'Enter') event.currentTarget.blur() }}
              placeholder="Page name"
              title={existingPageForUrl ? 'Renames the existing page object' : 'Name for the next scanned page'}
            />
            <button onClick={() => void scanPage()} disabled={Boolean(working)}><ScanSearch />Scan</button>
            <button onClick={() => void generate()} disabled={Boolean(working)}><Code2 />Generate</button>
            <button className="run" onClick={() => void runTest()} disabled={running || !selected}>
              {running ? <LoaderCircle className="spin" /> : <CirclePlay />}{running ? 'Running' : 'Run'}
            </button>
          </div>
        </header>
        {error && <button className="error-banner" onClick={() => setError(undefined)}>{error}</button>}
        {working && <div className="working-banner">{working}</div>}
        {locatorDiff && <LocatorDiffBanner diff={locatorDiff} onDismiss={() => setLocatorDiff(undefined)} />}
        <DynamicElementSuggestionsBanner
          suggestions={dynamicElementSuggestions}
          onAccept={(suggestion) => void acceptDynamicElementSuggestion(suggestion)}
          onDismiss={() => setDynamicElementSuggestions([])}
        />
        {scanScreenshot && (
          <button
            className={`screenshot-toggle ${scanScreenshotOpen ? 'active' : ''}`}
            onClick={() => void toggleScanScreenshot()}
          ><Camera />Scan screenshot</button>
        )}
        {scanScreenshotOpen && (
          <div className="screenshot-panel">
            {!scanScreenshotState && <LoaderCircle className="spin" />}
            {scanScreenshotState?.status === 'ready' && <img src={scanScreenshotState.url} alt="Scan screenshot" />}
            {scanScreenshotState?.status === 'missing' && <span>Screenshot is no longer available.</span>}
            {scanScreenshotState?.status === 'error' && (
              <span className="screenshot-error">Couldn't load screenshot: {scanScreenshotState.message}</span>
            )}
          </div>
        )}
        <GenerationWarningBanner warnings={generationWarnings} onDismiss={() => setGenerationWarnings([])} />
        {active === 'tests' && (
          <TestEditor
            test={selected}
            pages={snapshot.pages}
            onChange={saveTest}
            onSaveDynamicElement={(pageId, element) => void saveDynamicElement(pageId, element)}
            onDeleteDynamicElement={(pageId, elementId) => void deleteDynamicElement(pageId, elementId)}
          />
        )}
        {active === 'runs' && (
          <RunResults
            run={run}
            test={selected}
            running={running}
            activeStepId={activeStepId}
            onCancel={() => void cancelRun()}
          />
        )}
      </section>
      <BrowserPane
        state={browser}
        url={url}
        onUrl={setUrl}
        onNavigate={navigateBrowser}
      />
    </main>
  )
}

function normalizeUrl(value: string) {
  return /^https?:\/\//i.test(value) ? value : `https://${value}`
}

function pageNameFor(value: string) {
  try {
    const url = new URL(normalizeUrl(value))
    const segment = url.pathname.split('/').filter(Boolean).at(-1) || url.hostname
    return segment.replace(/[-_]+/g, ' ').replace(/\b\w/g, (letter) => letter.toUpperCase())
  } catch {
    return 'New page'
  }
}

function upsert<T extends { id: string }>(items: T[], item: T) {
  const index = items.findIndex((candidate) => candidate.id === item.id)
  return index < 0 ? [...items, item] : items.map((candidate) => candidate.id === item.id ? item : candidate)
}

function message(cause: unknown) {
  return cause instanceof Error ? cause.message : String(cause)
}
