import { ipcMain, type WebContentsView } from 'electron'
import type { BackendSupervisor } from './backend-supervisor.js'
import type { BrowserController } from './browser-controller.js'
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

/**
 * Registers every `workspace:*`/`browser:*` IPC handler the renderer calls through `preload.cts`.
 * `getShell`/`getBrowser` are accessors rather than fixed references because the window (and so
 * the shell view and browser controller) can be torn down and recreated after this runs once at
 * startup - each handler needs whatever is current at invocation time, not what existed when
 * `installIpc` was called.
 */
export function installIpc({
  backend,
  chooseAndOpenWorkspace,
  getShell,
  getBrowser,
}: {
  backend: BackendSupervisor
  chooseAndOpenWorkspace(): Promise<unknown>
  getShell(): WebContentsView | undefined
  getBrowser(): BrowserController | undefined
}) {
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
          getShell()?.webContents.send(
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
    getBrowser()?.navigate(httpNavigationSchema.parse(candidate)),
  )
  ipcMain.handle('browser:back', () => getBrowser()?.back())
  ipcMain.handle('browser:forward', () => getBrowser()?.forward())
  ipcMain.handle('browser:reload', () => getBrowser()?.reload())
  ipcMain.handle('browser:bounds', (_event, value) => getBrowser()?.setBounds(boundsSchema.parse(value)))
  ipcMain.handle('browser:visible', (_event, candidate) =>
    getBrowser()?.setVisible(booleanSchema.parse(candidate)),
  )
}
