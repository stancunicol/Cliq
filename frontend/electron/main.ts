import {
  app,
  BaseWindow,
  dialog,
  Menu,
  session,
  WebContentsView,
} from 'electron'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { BackendSupervisor } from './backend-supervisor.js'
import { BrowserController } from './browser-controller.js'
import { installIpc } from './ipc.js'
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
    backgroundColor: '#0D0B12',
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
  installIpc({
    backend,
    chooseAndOpenWorkspace,
    getShell: () => shell,
    getBrowser: () => browser,
  })
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
