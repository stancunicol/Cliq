export type BrowserKind = 'chrome' | 'edge'
export type LocatorStrategy = 'testId' | 'name' | 'css' | 'xpath'
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
  /** How long a live run pauses on an outlined element before acting on it - live runs only,
   * never the generated NUnit code. */
  highlightPauseMs: number
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
  /** Only meaningful when action is 'fill' - 'string' | 'int' | 'DateTime', inferred from the
   * scanned <input> type. Defaults to 'string' when absent. */
  dataType?: string
  /** Only meaningful when action is 'select' - true when the scanned <select> had `multiple`. */
  multiple?: boolean
  /** Only meaningful when action is 'click' - true when the scanned element was a checkbox. */
  checkbox?: boolean
}

export interface ScanReconciliationReport {
  matchedCount: number
  newCount: number
  missingCount: number
  isFirstScan: boolean
}

export interface LocatorDiffEntry {
  elementId: string
  name: string
  tag: string
  locator: Locator
}

export interface LocatorDiffReport {
  pageId: string
  scannedAt: string
  new: LocatorDiffEntry[]
  missing: LocatorDiffEntry[]
}

export interface PageScanResult {
  page: PageDefinition
  reconciliation: ScanReconciliationReport
  locatorDiff?: LocatorDiffReport
  dynamicElementSuggestions?: DynamicElementSuggestion[]
  /** Bare file name (not a path) - fetch via CliqBridge.workspace.getDiscoveryScreenshot. */
  screenshotPath?: string
}

/** Only these four actions are supported for a dynamic element - see DynamicElement. */
export type DynamicElementAction = Extract<StepAction, 'click' | 'fill' | 'select' | 'assertText'>

/**
 * A user-authored locator template for a repeating element (e.g. a table row) that a one-time
 * scan can't discover, because there isn't a single fixed instance. `locatorTemplate` is raw
 * XPath/CSS text containing the literal token `{param}`, which the generator turns into a real
 * parameter on a generated factory method instead of a fixed field.
 */
export interface DynamicElement {
  id: string
  name: string
  action: DynamicElementAction
  strategy: LocatorStrategy
  locatorTemplate: string
  /** 'int' or 'string' - the type of the generated factory method's parameter. Defaults to 'int'. */
  parameterType?: string
  /** Only meaningful when action is 'fill' - same meaning as PageElement.dataType. */
  dataType?: string
}

/**
 * A scan-detected candidate for a DynamicElement - a group of 3+ structurally similar elements
 * found at the same relative position inside a repeating row. Not persisted; the user reviews and
 * either accepts it (turning it into a real DynamicElement with a fresh id) or ignores it. Always
 * xpath - a repeating group's members don't individually carry a stable non-positional key.
 */
export interface DynamicElementSuggestion {
  name: string
  action: DynamicElementAction
  strategy: LocatorStrategy
  locatorTemplate: string
  /** How many repeated instances the scan found - shown as confidence context. */
  sampleCount: number
}

export interface PageDefinition {
  schemaVersion: 1
  id: string
  name: string
  url: string
  elements: PageElement[]
  updatedAt: string
  dynamicElements?: DynamicElement[]
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
  /** Set instead of elementId when this step targets a DynamicElement. */
  dynamicElementId?: string
  /** The concrete value (e.g. "3") this step substitutes into that element's template. */
  dynamicParameterValue?: string
  /** Set instead of value for a 'select' step targeting a multi-select element - one entry per
   * selected option, uploaded together. */
  values?: StepValue[]
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

export interface CustomMethodRecoveryWarning {
  pageObjectName: string
  expectedCount: number
  recoveredCount: number
  path: string
}

export interface GenerationResult {
  artifacts: GeneratedArtifact[]
  warnings: CustomMethodRecoveryWarning[]
}

/** Shape shared by every "fetch a screenshot data URL on demand" panel (the scan screenshot in
 * App.tsx, each step's screenshot in RunResults.tsx) - undefined means "not fetched yet". */
export type ScreenshotState =
  | { status: 'ready'; url: string }
  | { status: 'missing' }
  | { status: 'error'; message: string }

export interface BrowserState {
  url: string
  title: string
  loading: boolean
  canGoBack: boolean
  canGoForward: boolean
  error?: string
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
    getDiscoveryScreenshot(fileName: string): Promise<string | null>
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
    onState(callback: (state: BrowserState) => void): () => void
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
