import {
  app,
  BaseWindow,
  dialog,
  ipcMain,
  Menu,
  session,
  WebContentsView,
} from 'electron'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { BackendSupervisor } from './backend-supervisor.js'
import { BrowserController } from './browser-controller.js'
import {
  booleanSchema,
  boundsSchema,
  cliqSettingsSchema,
  discoveryScreenshotRequestSchema,
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
import {
  parseWorkspaceLaunch,
  workspaceFromAdditionalData,
  type WorkspaceLaunch,
} from './launch-arguments.js'

const dirname = path.dirname(fileURLToPath(import.meta.url))
const repositoryRoot = path.resolve(dirname, '..', '..')
const backend = new BackendSupervisor(repositoryRoot)
let window: BaseWindow | undefined
let shell: WebContentsView | undefined
let browser: BrowserController | undefined
let pendingWorkspaceSnapshot: EmbeddedWorkspace | undefined
let workspaceOpenQueue = Promise.resolve()
let isBackendReady = false
let queuedWorkspaceLaunch: WorkspaceLaunch | undefined

interface EmbeddedWorkspace {
  rootPath: string
  pages: unknown[]
  tests: unknown[]
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
  browser = new BrowserController(window, shell)
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
  shell.webContents.on('did-finish-load', () => {
    if (pendingWorkspaceSnapshot) {
      shell?.webContents.send('workspace:opened', pendingWorkspaceSnapshot)
      pendingWorkspaceSnapshot = undefined
    }
  })
  await shell.webContents.loadURL(
    process.env.VITE_DEV_SERVER_URL || `file://${path.join(dirname, '../dist/index.html')}`,
  )
}

async function openWorkspace(rootPath: string, notifyRenderer = true) {
  const snapshot = await backend.request<EmbeddedWorkspace>('/api/v1/workspaces/open', {
    method: 'POST',
    body: JSON.stringify({ rootPath }),
  })
  if (notifyRenderer) {
    if (shell && !shell.webContents.isLoading()) {
      shell.webContents.send('workspace:opened', snapshot)
    } else {
      pendingWorkspaceSnapshot = snapshot
    }
  }
  return snapshot
}

function showWorkspaceLaunchFailure(error: unknown) {
  const message = error instanceof Error ? error.message : 'The workspace could not be opened.'
  if (window) {
    void dialog.showMessageBox(window, {
      type: 'error',
      title: 'Cliq workspace could not be opened',
      message: 'Cliq could not open the requested workspace.',
      detail: message,
    })
  } else {
    dialog.showErrorBox('Cliq workspace could not be opened', message)
  }
}

function focusWindow() {
  if (!app.isReady()) return
  if (!window) {
    void createWindow()
    return
  }
  if (window.isMinimized()) window.restore()
  window.show()
  window.focus()
}

function handleWorkspaceLaunch(launch: WorkspaceLaunch) {
  if (!isBackendReady) {
    queuedWorkspaceLaunch = launch
    return
  }
  focusWindow()
  if (!launch.workspacePath) return

  workspaceOpenQueue = workspaceOpenQueue
    .catch(() => undefined)
    .then(async () => {
      await openWorkspace(launch.workspacePath!)
      focusWindow()
    })
    .catch(showWorkspaceLaunchFailure)
}

async function chooseAndOpenWorkspace() {
  if (!window) return null
  const result = await dialog.showOpenDialog(window, {
    properties: ['openDirectory', 'createDirectory'],
    title: 'Open Cliq workspace',
  })
  if (result.canceled) return null
  return openWorkspace(result.filePaths[0], false)
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
      const run = await backend.request<{ id: string }>(
        `/api/v1/tests/${encodeURIComponent(request.testId)}/runs`,
        { method: 'POST', body: JSON.stringify({ browser: request.browser }) },
      )
      void backend
        .streamRunEvents(run.id, (event, payload) => {
          shell?.webContents.send(
            'workspace:run-event',
            event === 'step' ? { type: 'step', step: payload } : { type: 'done', run: payload },
          )
        })
        .catch((error: unknown) => {
          console.error(`Run '${run.id}' event stream failed:`, error)
        })
      return run
    },
  )
  ipcMain.handle('workspace:cancel-run', (_event, candidate) => {
    const runId = identifierSchema.parse(candidate)
    return backend.request(`/api/v1/runs/${encodeURIComponent(runId)}/cancel`, { method: 'POST' })
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
  ipcMain.handle('workspace:get-discovery-screenshot', (_event, candidate) => {
    const request = discoveryScreenshotRequestSchema.parse(candidate)
    return backend.requestDataUrl(
      `/api/v1/pages/discovery-screenshots/${encodeURIComponent(request.fileName)}`,
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

let initialLaunch: WorkspaceLaunch
try {
  initialLaunch = parseWorkspaceLaunch(process.argv, process.env.CLIQ_WORKSPACE_PATH)
} catch (error) {
  showWorkspaceLaunchFailure(error)
  app.exit(1)
  initialLaunch = {}
}

const hasSingleInstanceLock = app.requestSingleInstanceLock(initialLaunch)
if (!hasSingleInstanceLock) {
  app.quit()
}

app.on('second-instance', (_event, argv, _workingDirectory, additionalData) => {
  try {
    const forwarded = workspaceFromAdditionalData(additionalData)
    handleWorkspaceLaunch(
      forwarded.workspacePath ? forwarded : parseWorkspaceLaunch(argv),
    )
  } catch (error) {
    focusWindow()
    showWorkspaceLaunchFailure(error)
  }
})

if (hasSingleInstanceLock) app.whenReady().then(async () => {
  session.defaultSession.setPermissionRequestHandler((_contents, _permission, callback) =>
    callback(false),
  )
  session.defaultSession.setPermissionCheckHandler(() => false)
  await backend.start()
  isBackendReady = true
  if (initialLaunch.workspacePath) {
    await openWorkspace(initialLaunch.workspacePath, false)
  }
  installIpc()
  installMenu()
  await createWindow()
  if (queuedWorkspaceLaunch) {
    const queued = queuedWorkspaceLaunch
    queuedWorkspaceLaunch = undefined
    handleWorkspaceLaunch(queued)
  }
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
