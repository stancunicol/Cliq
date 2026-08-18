import type {
  BrowserKind,
  GenerationResult,
  PageDefinition,
  PageScanResult,
  ScreenshotState,
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

export const screenshotService = {
  get: (runId: string, fileName: string): Promise<string | null> => bridge().getScreenshot(runId, fileName),
  getDiscovery: (fileName: string): Promise<string | null> => bridge().getDiscoveryScreenshot(fileName),
}

/** Shared by every screenshot panel: runs `fetcher`, maps its result to a `ScreenshotState`, and
 * turns a thrown error into `{ status: 'error' }` instead of letting it propagate - the caller
 * only needs to store the result, not repeat the try/catch shape. */
export async function loadScreenshotState(fetcher: () => Promise<string | null>): Promise<ScreenshotState> {
  try {
    const dataUrl = await fetcher()
    return dataUrl ? { status: 'ready', url: dataUrl } : { status: 'missing' }
  } catch (cause) {
    return { status: 'error', message: cause instanceof Error ? cause.message : String(cause) }
  }
}
