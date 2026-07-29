import {
  app,
  BaseWindow,
  dialog,
  ipcMain,
  Menu,
  session,
  WebContentsView,
} from 'electron'
import { randomUUID } from 'node:crypto'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { BackendSupervisor } from './backend-supervisor.js'
import {
  BrowserController,
  type EmbeddedPage,
  type EmbeddedTest,
  type EmbeddedTestRun,
} from './browser-controller.js'
import {
  booleanSchema,
  boundsSchema,
  cliqSettingsSchema,
  generationRequestSchema,
  httpNavigationSchema,
  identifierSchema,
  pageSchema,
  scanRequestSchema,
  screenshotRequestSchema,
  secretValuesSchema,
  startRunRequestSchema,
  testCaseSchema,
} from './ipc-schemas.js'

const dirname = path.dirname(fileURLToPath(import.meta.url))
const repositoryRoot = path.resolve(dirname, '..', '..')
const backend = new BackendSupervisor(repositoryRoot)
let window: BaseWindow | undefined
let shell: WebContentsView | undefined
let browser: BrowserController | undefined
let activeRun: { id: string; cancellation: AbortController } | undefined

interface EmbeddedWorkspace {
  rootPath: string
  pages: EmbeddedPage[]
  tests: EmbeddedTest[]
}

async function createWindow() {
  window = new BaseWindow({
    width: 1680,
    height: 1000,
    minWidth: 1120,
    minHeight: 720,
    titleBarStyle: 'hidden',
    backgroundColor: '#111211',
  })
  shell = new WebContentsView({
    webPreferences: {
      preload: path.join(dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  })
  window.contentView.addChildView(shell)
  browser = new BrowserController(window, shell, dirname)
  const resize = () => {
    if (!window || !shell) return
    const bounds = window.getContentBounds()
    shell.setBounds({ x: 0, y: 0, width: bounds.width, height: bounds.height })
    browser?.resize()
  }
  window.on('resize', resize)
  window.on('closed', () => {
    shell?.webContents.close()
    browser?.close()
    shell = undefined
    browser = undefined
    window = undefined
  })
  resize()
  await shell.webContents.loadURL(
    process.env.VITE_DEV_SERVER_URL || `file://${path.join(dirname, '../dist/index.html')}`,
  )
}

async function chooseAndOpenWorkspace() {
  if (!window) return null
  const result = await dialog.showOpenDialog(window, {
    properties: ['openDirectory', 'createDirectory'],
    title: 'Open Cliq workspace',
  })
  if (result.canceled) return null
  return backend.request('/api/v1/workspaces/open', {
    method: 'POST',
    body: JSON.stringify({ rootPath: result.filePaths[0] }),
  })
}

function installIpc() {
  ipcMain.handle('workspace:choose-open', chooseAndOpenWorkspace)
  ipcMain.handle('workspace:get', () => backend.request('/api/v1/workspace'))
  ipcMain.handle('workspace:save-page', (_event, candidate) => {
    const page = pageSchema.parse(candidate)
    return backend.request(`/api/v1/pages/${encodeURIComponent(page.id)}`, {
      method: 'PUT',
      body: JSON.stringify(page),
    })
  })
  ipcMain.handle('workspace:save-test', (_event, candidate) => {
    const test = testCaseSchema.parse(candidate)
    return backend.request(`/api/v1/tests/${encodeURIComponent(test.id)}`, {
      method: 'PUT',
      body: JSON.stringify(test),
    })
  })
  ipcMain.handle('workspace:delete-test', (_event, candidate) => {
    const id = identifierSchema.parse(candidate)
    return backend.request(`/api/v1/tests/${encodeURIComponent(id)}`, { method: 'DELETE' })
  })
  ipcMain.handle('workspace:scan-page', (_event, candidate) => {
    const request = scanRequestSchema.parse(candidate)
    return backend.request('/api/v1/pages/scan', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  })
  ipcMain.handle(
    'workspace:generate',
    (_event, candidate) => {
      const request = generationRequestSchema.parse(candidate)
      return backend.request('/api/v1/generation', {
        method: 'POST',
        body: JSON.stringify(request),
      })
    },
  )
  ipcMain.handle(
    'workspace:start-run',
    async (_event, candidate) => {
      const request = startRunRequestSchema.parse(candidate)
      if (activeRun) throw new Error('Another in-app browser run is already active.')
      if (!browser) throw new Error('The in-app browser is not ready.')
      const snapshot = await backend.request<EmbeddedWorkspace>('/api/v1/workspace')
      const test = snapshot.tests.find((item) => item.id === request.testId)
      if (!test) throw new Error(`Test '${request.testId}' was not found.`)
      const secrets = await backend.request<Record<string, string>>('/api/v1/secrets')
      const run: EmbeddedTestRun = {
        id: randomUUID().replaceAll('-', ''),
        testId: test.id,
        status: 'queued',
        startedAt: new Date().toISOString(),
        steps: [],
      }
      const cancellation = new AbortController()
      activeRun = { id: run.id, cancellation }
      await persistRun(run)
      void browser
        .runTest(
          run,
          test,
          snapshot.pages,
          secrets,
          snapshot.rootPath,
          cancellation.signal,
          (event) => shell?.webContents.send('workspace:run-event', event),
        )
        .then(async (completed) => {
          await persistRun(completed)
          shell?.webContents.send('workspace:run-event', { type: 'done', run: completed })
        })
        .catch(async (error: Error) => {
          const failed: EmbeddedTestRun = {
            ...run,
            status: 'failed',
            completedAt: new Date().toISOString(),
            error: error.message,
          }
          await persistRun(failed).catch(() => undefined)
          shell?.webContents.send('workspace:run-event', { type: 'done', run: failed })
        })
        .finally(() => {
          if (activeRun?.id === run.id) activeRun = undefined
        })
      return run
    },
  )
  ipcMain.handle('workspace:cancel-run', (_event, candidate) => {
    const runId = identifierSchema.parse(candidate)
    if (!activeRun || activeRun.id !== runId) throw new Error(`Run '${runId}' is not active.`)
    activeRun.cancellation.abort(new DOMException('Run cancelled.', 'AbortError'))
  })
  ipcMain.handle('workspace:save-secrets', (_event, candidate) => {
    const values = secretValuesSchema.parse(candidate)
    return backend.request('/api/v1/secrets', {
      method: 'PUT',
      body: JSON.stringify(values),
    })
  })
  ipcMain.handle('workspace:get-settings', () => backend.request('/api/v1/settings'))
  ipcMain.handle('workspace:save-settings', (_event, candidate) => {
    const settings = cliqSettingsSchema.parse(candidate)
    return backend.request('/api/v1/settings', {
      method: 'PUT',
      body: JSON.stringify(settings),
    })
  })
  ipcMain.handle('workspace:get-screenshot', (_event, candidate) => {
    const request = screenshotRequestSchema.parse(candidate)
    return backend.requestDataUrl(
      `/api/v1/runs/${encodeURIComponent(request.runId)}/screenshots/${encodeURIComponent(request.fileName)}`,
    )
  })

  ipcMain.handle('browser:navigate', (_event, candidate) =>
    browser?.navigate(httpNavigationSchema.parse(candidate)),
  )
  ipcMain.handle('browser:back', () => browser?.back())
  ipcMain.handle('browser:forward', () => browser?.forward())
  ipcMain.handle('browser:reload', () => browser?.reload())
  ipcMain.handle('browser:bounds', (_event, value) => browser?.setBounds(boundsSchema.parse(value)))
  ipcMain.handle('browser:visible', (_event, candidate) =>
    browser?.setVisible(booleanSchema.parse(candidate)),
  )
  ipcMain.handle('browser:recording', (_event, candidate) =>
    browser?.setRecording(booleanSchema.parse(candidate)),
  )
}

function persistRun(run: EmbeddedTestRun) {
  return backend.request(`/api/v1/runs/${encodeURIComponent(run.id)}`, {
    method: 'PUT',
    body: JSON.stringify(run),
  })
}

function installMenu() {
  const isMac = process.platform === 'darwin'
  Menu.setApplicationMenu(
    Menu.buildFromTemplate([
      ...(isMac ? [{ role: 'appMenu' as const }] : []),
      {
        label: 'Project',
        submenu: [
          {
            label: 'Open Workspace…',
            accelerator: 'CmdOrCtrl+O',
            click: async () => {
              const snapshot = await chooseAndOpenWorkspace()
              if (snapshot) shell?.webContents.send('workspace:opened', snapshot)
            },
          },
        ],
      },
      { role: 'editMenu' },
      { role: 'viewMenu' },
      { role: 'windowMenu' },
    ]),
  )
}

app.whenReady().then(async () => {
  session.defaultSession.setPermissionRequestHandler((_contents, _permission, callback) =>
    callback(false),
  )
  session.defaultSession.setPermissionCheckHandler(() => false)
  await backend.start()
  if (process.env.CLIQ_WORKSPACE_PATH) {
    await backend.request('/api/v1/workspaces/open', {
      method: 'POST',
      body: JSON.stringify({ rootPath: process.env.CLIQ_WORKSPACE_PATH }),
    })
  }
  installIpc()
  installMenu()
  await createWindow()
  if (process.env.CLIQ_E2E_AUTORUN === '1') {
    setTimeout(() => {
      void shell?.webContents.executeJavaScript(
        `document.querySelector('.top-actions .run')?.click()`,
      )
    }, 1_000)
  }
}).catch((error: unknown) => {
  dialog.showErrorBox(
    'Cliq could not start',
    error instanceof Error ? error.message : String(error),
  )
  app.quit()
})

app.on('activate', () => {
  if (!window) void createWindow()
})
app.on('before-quit', () => backend.stop())
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})
