import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react'
import { CirclePlay, Code2, LoaderCircle, Radio, ScanSearch, Square } from 'lucide-react'
import { BrowserPane } from './features/browser/BrowserPane'
import { RunResults } from './features/runs/RunResults'
import { SettingsPanel } from './features/settings/SettingsPanel'
import { TestEditor } from './features/tests/TestEditor'
import { Sidebar } from './features/workspace/Sidebar'
import { settingsService, workspaceService } from './services'
import type {
  BrowserState,
  CliqSettings,
  PageDefinition,
  PageElement,
  RecordedInteraction,
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
  recording: false,
}

export function App() {
  const [snapshot, setSnapshot] = useState<WorkspaceSnapshot>()
  const [selectedId, setSelectedId] = useState<string>()
  const [active, setActive] = useState<'tests' | 'runs' | 'settings'>('tests')
  const [settings, setSettings] = useState<CliqSettings>()
  const [settingsSaving, setSettingsSaving] = useState(false)
  const [settingsSaved, setSettingsSaved] = useState(false)
  const [settingsError, setSettingsError] = useState<string>()
  const [search, setSearch] = useState('')
  const [browser, setBrowser] = useState(defaultBrowser)
  const [url, setUrl] = useState('https://example.com')
  const [recording, setRecording] = useState(false)
  const [run, setRun] = useState<TestRun>()
  const [running, setRunning] = useState(false)
  const [activeStepId, setActiveStepId] = useState<string>()
  const [working, setWorking] = useState<string>()
  const [error, setError] = useState<string>()
  const snapshotRef = useRef<WorkspaceSnapshot | undefined>(undefined)
  const selectedIdRef = useRef<string | undefined>(undefined)
  const mutationQueue = useRef(Promise.resolve())
  const saveRevisions = useRef(new Map<string, number>())
  const navigatedWorkspace = useRef<string | undefined>(undefined)
  const selected = snapshot?.tests.find((test) => test.id === selectedId)
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

  useEffect(() => {
    if (!snapshot) return
    setSettingsError(undefined)
    void settingsService.get().then(setSettings).catch((cause) => setSettingsError(message(cause)))
  }, [snapshot?.rootPath])

  useEffect(() => window.cliq?.browser.onState((state) => {
    setBrowser(state)
    if (state.url && state.url !== 'about:blank') setUrl(state.url)
  }), [])

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

  useEffect(() => window.cliq?.browser.onRecordedInteraction((interaction) => {
    mutationQueue.current = mutationQueue.current
      .then(() => persistInteraction(interaction))
      .catch((cause) => setError(message(cause)))
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

  async function persistInteraction(interaction: RecordedInteraction) {
    const current = snapshotRef.current
    const currentSelectedId = selectedIdRef.current
    const currentTest = current?.tests.find((test) => test.id === currentSelectedId)
    if (!current || !currentTest) return

    if (interaction.value?.kind === 'secret' && interaction.secretValue !== undefined) {
      await window.cliq?.workspace.saveSecrets({
        [interaction.value.value]: interaction.secretValue,
      })
    }
    const pageId = pageIdFor(interaction.pageUrl)
    const existingPage = current.pages.find((page) => page.id === pageId)
    const existingElement = existingPage?.elements.find((element) =>
      element.locator.strategy === interaction.element.locator.strategy
      && element.locator.value === interaction.element.locator.value)
    const element = existingElement ?? uniqueElementName(interaction.element, existingPage?.elements ?? [])
    const page: PageDefinition = existingPage
      ? { ...existingPage, elements: existingElement ? existingPage.elements : [...existingPage.elements, element] }
      : {
          schemaVersion: 1,
          id: pageId,
          name: pageNameFor(interaction.pageUrl),
          url: interaction.pageUrl,
          elements: [element],
          updatedAt: new Date().toISOString(),
        }
    const savedPage = await workspaceService.savePage(page)
    const latest = snapshotRef.current
    const latestTest = latest?.tests.find((test) => test.id === currentSelectedId)
    if (!latest || !latestTest) return
    const updatedTest: TestCase = {
      ...latestTest,
      steps: [...latestTest.steps, {
        id: interaction.id,
        action: interaction.action,
        label: interaction.label,
        enabled: true,
        pageId: savedPage.id,
        elementId: element.id,
        value: interaction.value,
      }],
      updatedAt: new Date().toISOString(),
    }
    const savedTest = await workspaceService.saveTest(updatedTest)
    updateSnapshot((value) => ({
      ...value,
      pages: upsert(value.pages, savedPage),
      tests: upsert(value.tests, savedTest),
    }))
  }

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

  async function saveSettings(next: CliqSettings) {
    setSettingsSaving(true)
    setSettingsError(undefined)
    setSettingsSaved(false)
    try {
      const saved = await settingsService.save(next)
      setSettings(saved)
      setSettingsSaved(true)
      setTimeout(() => setSettingsSaved(false), 2000)
    } catch (cause) {
      setSettingsError(message(cause))
    } finally {
      setSettingsSaving(false)
    }
  }

  async function createTest() {
    if (!snapshot) return
    const test: TestCase = {
      schemaVersion: 1,
      id: `test-${crypto.randomUUID().slice(0, 8)}`,
      name: 'New test',
      tags: [],
      steps: [],
      updatedAt: new Date().toISOString(),
    }
    const saved = await workspaceService.saveTest(test)
    updateSnapshot({ ...snapshot, tests: [...snapshot.tests, saved] })
    setSelectedId(saved.id)
    setActive('tests')
  }

  async function toggleRecording() {
    if (!selected) {
      setError('Create or select a test before recording.')
      return
    }
    const next = !recording
    try {
      if (next && isHttpUrl(browser.url)) appendNavigationStep(browser.url)
      await window.cliq?.browser.setRecording(next)
      setRecording(next)
    } catch (cause) {
      setError(message(cause))
    }
  }

  function appendNavigationStep(targetUrl: string) {
    const current = snapshotRef.current
    const currentTest = current?.tests.find((test) => test.id === selectedIdRef.current)
    if (!current || !currentTest) return
    const normalized = normalizeUrl(targetUrl)
    const previous = currentTest.steps.at(-1)
    if (previous?.action === 'navigate' && previous.url === normalized) return
    saveTest({
      ...currentTest,
      steps: [...currentTest.steps, {
        id: crypto.randomUUID(),
        action: 'navigate',
        label: `Navigate to ${new URL(normalized).hostname}`,
        enabled: true,
        url: normalized,
      }],
      updatedAt: new Date().toISOString(),
    })
  }

  function navigateBrowser() {
    const target = normalizeUrl(url)
    if (recording && selected) appendNavigationStep(target)
    void window.cliq?.browser.navigate(target).catch((cause) => setError(message(cause)))
  }

  async function scanPage() {
    if (!snapshot || !url) return
    setWorking('Scanning')
    try {
      const { page, reconciliation } = await workspaceService.scanPage(url, pageNameFor(url), snapshot.project.defaultBrowser)
      updateSnapshot({ ...snapshot, pages: upsert(snapshot.pages, page) })
      setWorking(`Scanned: ${reconciliation.matchedCount} matched · ${reconciliation.newCount} new · ${reconciliation.missingCount} missing`)
      setTimeout(() => setWorking(undefined), 3000)
    } catch (cause) {
      setError(message(cause))
      setWorking(undefined)
    }
  }

  async function generate() {
    if (!snapshot) return
    setWorking('Generating')
    try {
      const pageIds = selected
        ? [...new Set(selected.steps.flatMap((step) => step.pageId ? [step.pageId] : []))]
        : undefined
      const result = await workspaceService.generate(pageIds, selected ? [selected.id] : undefined)
      setWorking(`Generated ${result.artifacts.length} artifacts`)
      setTimeout(() => setWorking(undefined), 3000)
    } catch (cause) {
      setError(message(cause))
      setWorking(undefined)
    }
  }

  async function runTest() {
    if (!selected || !snapshot) return
    if (selected.steps.filter((step) => step.enabled).length === 0) {
      setError('Add or record at least one enabled step before running.')
      return
    }
    setActive('runs')
    setRunning(true)
    setActiveStepId(undefined)
    setRun(undefined)
    try {
      if (recording) {
        await window.cliq?.browser.setRecording(false)
        setRecording(false)
      }
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
          <h1>Record workflows. Generate maintainable Selenium code.</h1>
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
          <div><small>{snapshot.project.name}</small><h1>{active === 'runs' ? 'Run results' : active === 'settings' ? 'Settings' : selected?.name || 'Tests'}</h1></div>
          <div className="top-actions">
            <button onClick={() => void scanPage()} disabled={Boolean(working)}><ScanSearch />Scan</button>
            <button onClick={() => void generate()} disabled={Boolean(working)}><Code2 />Generate</button>
            <button className={`record ${recording ? 'on' : ''}`} onClick={() => void toggleRecording()}>
              {recording ? <Square /> : <Radio />}{recording ? 'Stop' : 'Record'}
            </button>
            <button className="run" onClick={() => void runTest()} disabled={running || !selected}>
              {running ? <LoaderCircle className="spin" /> : <CirclePlay />}{running ? 'Running' : 'Run'}
            </button>
          </div>
        </header>
        {error && <button className="error-banner" onClick={() => setError(undefined)}>{error}</button>}
        {working && <div className="working-banner">{working}</div>}
        {active === 'tests' && <TestEditor test={selected} pages={snapshot.pages} onChange={saveTest} />}
        {active === 'runs' && (
          <RunResults
            run={run}
            test={selected}
            running={running}
            activeStepId={activeStepId}
            onCancel={() => void cancelRun()}
          />
        )}
        {active === 'settings' && (
          <SettingsPanel
            settings={settings}
            saving={settingsSaving}
            saved={settingsSaved}
            error={settingsError}
            onSave={(next) => void saveSettings(next)}
          />
        )}
      </section>
      <BrowserPane
        state={browser}
        url={url}
        recording={recording}
        onUrl={setUrl}
        onNavigate={navigateBrowser}
        onStopRecording={() => void toggleRecording()}
      />
    </main>
  )
}

function normalizeUrl(value: string) {
  return /^https?:\/\//i.test(value) ? value : `https://${value}`
}

function isHttpUrl(value: string) {
  try {
    const protocol = new URL(value).protocol
    return protocol === 'http:' || protocol === 'https:'
  } catch {
    return false
  }
}

function pageIdFor(value: string) {
  const url = new URL(value)
  const slug = `${url.hostname}-${url.pathname}`.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
  return slug.slice(0, 120) || 'page'
}

function pageNameFor(value: string) {
  try {
    const url = new URL(normalizeUrl(value))
    const segment = url.pathname.split('/').filter(Boolean).at(-1) || url.hostname
    return segment.replace(/[-_]+/g, ' ').replace(/\b\w/g, (letter) => letter.toUpperCase())
  } catch {
    return 'Recorded page'
  }
}

function upsert<T extends { id: string }>(items: T[], item: T) {
  const index = items.findIndex((candidate) => candidate.id === item.id)
  return index < 0 ? [...items, item] : items.map((candidate) => candidate.id === item.id ? item : candidate)
}

function uniqueElementName(element: PageElement, existing: PageElement[]) {
  const used = new Set(existing.map((candidate) => candidate.name.toLowerCase()))
  if (!used.has(element.name.toLowerCase())) return element
  let suffix = 2
  while (used.has(`${element.name} ${suffix}`.toLowerCase())) suffix += 1
  return { ...element, name: `${element.name} ${suffix}` }
}

function message(cause: unknown) {
  return cause instanceof Error ? cause.message : String(cause)
}
