export type BrowserKind = 'chrome' | 'edge'
export type LocatorStrategy = 'testId' | 'id' | 'name' | 'css' | 'xpath'
export type StepAction =
  | 'navigate'
  | 'click'
  | 'fill'
  | 'select'
  | 'pressKey'
  | 'assertVisible'
  | 'assertText'
  | 'assertUrl'
export type RunStatus = 'queued' | 'running' | 'passed' | 'failed' | 'cancelled'

export type IdentificationStrategy = 'testId' | 'xpath'

export interface ElementIdentificationSettings {
  testIdAttribute: string
  strategyOrder: IdentificationStrategy[]
  ignoreIdPatterns: string[]
}

export interface TestExecutionSettings {
  defaultStepTimeoutMs: number
  pageReadyTimeoutMs: number
  captureScreenshotOnFailure: boolean
}

export interface ScanningSettings {
  loadTimeoutMs: number
  networkIdleGraceMs: number
  captureScreenshot: boolean
}

export interface CliqSettings {
  schemaVersion: 1
  elementIdentification: ElementIdentificationSettings
  testExecution: TestExecutionSettings
  scanning: ScanningSettings
}

export interface OutputSettings {
  pageObjectsDirectory: string
  testsDirectory: string
  pageObjectsNamespace: string
  testsNamespace: string
}

export interface ProjectManifest {
  schemaVersion: 1
  id: string
  name: string
  baseUrl: string
  defaultBrowser: BrowserKind
  output: OutputSettings
  updatedAt: string
}

export interface Locator {
  strategy: LocatorStrategy
  value: string
  displayName?: string
}

export interface PageElement {
  id: string
  name: string
  tag: string
  action: StepAction
  locator: Locator
  expectedText?: string
  notFound?: boolean
}

export interface ScanReconciliationReport {
  matchedCount: number
  newCount: number
  missingCount: number
}

export interface PageScanResult {
  page: PageDefinition
  reconciliation: ScanReconciliationReport
}

export interface PageDefinition {
  schemaVersion: 1
  id: string
  name: string
  url: string
  elements: PageElement[]
  updatedAt: string
}

export interface StepValue {
  kind: 'literal' | 'secret'
  value: string
}

export interface TestStep {
  id: string
  action: StepAction
  label: string
  enabled: boolean
  pageId?: string
  elementId?: string
  url?: string
  value?: StepValue
  timeoutMs?: number
}

export interface TestCase {
  schemaVersion: 1
  id: string
  name: string
  description?: string
  tags: string[]
  steps: TestStep[]
  updatedAt: string
}

export interface StepResult {
  stepId: string
  status: RunStatus
  durationMs: number
  error?: string
  screenshotPath?: string
  locateMs?: number
  actionMs?: number
  syncMs?: number
  extendedTimeoutMs?: number
}

export interface TestRun {
  id: string
  testId: string
  status: RunStatus
  startedAt: string
  completedAt?: string
  steps: StepResult[]
  error?: string
}

export interface WorkspaceSnapshot {
  rootPath: string
  project: ProjectManifest
  pages: PageDefinition[]
  tests: TestCase[]
}

export interface GeneratedArtifact {
  kind: string
  name: string
  path: string
  created: boolean
}

export interface GenerationResult {
  artifacts: GeneratedArtifact[]
}

export interface BrowserState {
  url: string
  title: string
  loading: boolean
  canGoBack: boolean
  canGoForward: boolean
  recording: boolean
  error?: string
}

export interface RecordedInteraction {
  id: string
  action: Extract<StepAction, 'click' | 'fill' | 'select'>
  label: string
  pageUrl: string
  element: PageElement
  value?: StepValue
  secretValue?: string
}

export interface CliqBridge {
  workspace: {
    chooseAndOpen(): Promise<WorkspaceSnapshot | null>
    get(): Promise<WorkspaceSnapshot>
    savePage(page: PageDefinition): Promise<PageDefinition>
    saveTest(test: TestCase): Promise<TestCase>
    deleteTest(id: string): Promise<void>
    scanPage(url: string, name: string, browser?: BrowserKind): Promise<PageScanResult>
    generate(pageIds?: string[], testIds?: string[]): Promise<GenerationResult>
    startRun(testId: string, browser?: BrowserKind): Promise<TestRun>
    cancelRun(runId: string): Promise<void>
    saveSecrets(values: Record<string, string>): Promise<void>
    getSettings(): Promise<CliqSettings>
    saveSettings(settings: CliqSettings): Promise<CliqSettings>
    getScreenshot(runId: string, fileName: string): Promise<string | null>
    onRunEvent(callback: (event: RunEvent) => void): () => void
    onOpened(callback: (snapshot: WorkspaceSnapshot) => void): () => void
  }
  browser: {
    navigate(url: string): Promise<void>
    back(): Promise<void>
    forward(): Promise<void>
    reload(): Promise<void>
    setBounds(bounds: { x: number; y: number; width: number; height: number }): Promise<void>
    setVisible(visible: boolean): Promise<void>
    setRecording(value: boolean): Promise<void>
    onState(callback: (state: BrowserState) => void): () => void
    onRecordedInteraction(callback: (interaction: RecordedInteraction) => void): () => void
  }
}

export type RunEvent =
  | { type: 'step-start'; stepId: string }
  | { type: 'step'; step: StepResult }
  | { type: 'done'; run: TestRun }

declare global {
  interface Window {
    cliq?: CliqBridge
  }
}
