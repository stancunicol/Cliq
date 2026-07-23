import { useCallback, useEffect, useLayoutEffect, useState } from 'react'
import { CirclePlay, Code2, LoaderCircle, Radio, ScanSearch, Square } from 'lucide-react'
import { BrowserPane } from './features/browser/BrowserPane'
import { RunResults } from './features/runs/RunResults'
import { TestEditor } from './features/tests/TestEditor'
import { Sidebar } from './features/workspace/Sidebar'
import { workspaceService } from './services'
import type {
  BrowserState,
  PageDefinition,
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
  const [active, setActive] = useState<'tests' | 'runs'>('tests')
  const [search, setSearch] = useState('')
  const [browser, setBrowser] = useState(defaultBrowser)
  const [url, setUrl] = useState('https://example.com')
  const [recording, setRecording] = useState(false)
  const [run, setRun] = useState<TestRun>()
  const [running, setRunning] = useState(false)
  const [working, setWorking] = useState<string>()
  const [error, setError] = useState<string>()
  const selected = snapshot?.tests.find((test) => test.id === selectedId)

  const applySnapshot = useCallback((next: WorkspaceSnapshot) => {
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

  useEffect(() => window.cliq?.workspace.onRunEvent((event) => {
    if (event.type === 'step') {
      setRun((current) => current
        ? { ...current, status: 'running', steps: [...current.steps, event.step] }
        : current)
    } else {
      setRun(event.run)
      setRunning(false)
    }
  }), [])

  useEffect(() => window.cliq?.browser.onRecordedInteraction((interaction) => {
    void persistInteraction(interaction)
  }), [snapshot, selectedId])

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
    if (!snapshot || !selected) return
    try {
      if (interaction.value?.kind === 'secret' && interaction.secretValue !== undefined) {
        await window.cliq?.workspace.saveSecrets({
          [interaction.value.value]: interaction.secretValue,
        })
      }
      const pageId = pageIdFor(interaction.pageUrl)
      const existingPage = snapshot.pages.find((page) => page.id === pageId)
      const existingElement = existingPage?.elements.find((element) =>
        element.locator.strategy === interaction.element.locator.strategy
        && element.locator.value === interaction.element.locator.value)
      const element = existingElement ?? interaction.element
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
      const updatedTest: TestCase = {
        ...selected,
        steps: [...selected.steps, {
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
      setSnapshot((current) => current && ({
        ...current,
        pages: upsert(current.pages, savedPage),
        tests: upsert(current.tests, savedTest),
      }))
    } catch (cause) {
      setError(message(cause))
    }
  }

  async function openWorkspace() {
    try {
      const next = await workspaceService.chooseAndOpen()
      if (next) applySnapshot(next)
    } catch (cause) {
      setError(message(cause))
    }
  }

  async function saveTest(test: TestCase) {
    if (!snapshot) return
    setSnapshot({ ...snapshot, tests: upsert(snapshot.tests, test) })
    try {
      const saved = await workspaceService.saveTest({ ...test, updatedAt: new Date().toISOString() })
      setSnapshot((current) => current && ({ ...current, tests: upsert(current.tests, saved) }))
    } catch (cause) {
      setError(message(cause))
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
    setSnapshot({ ...snapshot, tests: [...snapshot.tests, saved] })
    setSelectedId(saved.id)
    setActive('tests')
  }

  async function toggleRecording() {
    if (!selected) {
      setError('Create or select a test before recording.')
      return
    }
    const next = !recording
    setRecording(next)
    await window.cliq?.browser.setRecording(next)
  }

  async function scanPage() {
    if (!snapshot || !url) return
    setWorking('Scanning')
    try {
      const page = await workspaceService.scanPage(url, pageNameFor(url), snapshot.project.defaultBrowser)
      setSnapshot({ ...snapshot, pages: upsert(snapshot.pages, page) })
    } catch (cause) {
      setError(message(cause))
    } finally {
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
    setActive('runs')
    setRunning(true)
    setRun(undefined)
    try {
      const started = await workspaceService.startRun(selected.id, snapshot.project.defaultBrowser)
      setRun(started)
    } catch (cause) {
      setRunning(false)
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
          <div><small>{snapshot.project.name}</small><h1>{active === 'runs' ? 'Run results' : selected?.name || 'Tests'}</h1></div>
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
        {active === 'tests'
          ? <TestEditor test={selected} pages={snapshot.pages} onChange={(test) => void saveTest(test)} />
          : <RunResults run={run} test={selected} running={running} />}
      </section>
      <BrowserPane
        state={browser}
        url={url}
        recording={recording}
        onUrl={setUrl}
        onNavigate={() => void window.cliq?.browser.navigate(normalizeUrl(url))}
        onStopRecording={() => void toggleRecording()}
      />
    </main>
  )
}

function normalizeUrl(value: string) {
  return /^https?:\/\//i.test(value) ? value : `https://${value}`
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

function message(cause: unknown) {
  return cause instanceof Error ? cause.message : String(cause)
}
