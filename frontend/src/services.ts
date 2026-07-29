import type {
  BrowserKind,
  CliqSettings,
  GenerationResult,
  PageDefinition,
  PageScanResult,
  TestCase,
  TestRun,
  WorkspaceSnapshot,
} from './types'

function bridge() {
  if (!window.cliq) {
    throw new Error('Cliq must run inside Electron. Start it with npm run dev.')
  }

  return window.cliq.workspace
}

export const workspaceService = {
  chooseAndOpen: (): Promise<WorkspaceSnapshot | null> => bridge().chooseAndOpen(),
  get: (): Promise<WorkspaceSnapshot> => bridge().get(),
  savePage: (page: PageDefinition): Promise<PageDefinition> => bridge().savePage(page),
  saveTest: (test: TestCase): Promise<TestCase> => bridge().saveTest(test),
  deleteTest: (id: string): Promise<void> => bridge().deleteTest(id),
  scanPage: (url: string, name: string, browser?: BrowserKind): Promise<PageScanResult> =>
    bridge().scanPage(url, name, browser),
  generate: (pageIds?: string[], testIds?: string[]): Promise<GenerationResult> =>
    bridge().generate(pageIds, testIds),
  startRun: (testId: string, browser?: BrowserKind): Promise<TestRun> =>
    bridge().startRun(testId, browser),
  cancelRun: (runId: string): Promise<void> => bridge().cancelRun(runId),
}

export const settingsService = {
  get: (): Promise<CliqSettings> => bridge().getSettings(),
  save: (settings: CliqSettings): Promise<CliqSettings> => bridge().saveSettings(settings),
}

export const screenshotService = {
  get: (runId: string, fileName: string): Promise<string | null> => bridge().getScreenshot(runId, fileName),
}
