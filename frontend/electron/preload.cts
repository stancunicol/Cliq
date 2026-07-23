import { contextBridge, ipcRenderer } from 'electron'

function subscribe(channel: string, callback: (value: unknown) => void) {
  const listener = (_event: unknown, value: unknown) => callback(value)
  ipcRenderer.on(channel, listener)
  return () => ipcRenderer.removeListener(channel, listener)
}

contextBridge.exposeInMainWorld('cliq', {
  workspace: {
    chooseAndOpen: () => ipcRenderer.invoke('workspace:choose-open'),
    get: () => ipcRenderer.invoke('workspace:get'),
    savePage: (page: unknown) => ipcRenderer.invoke('workspace:save-page', page),
    saveTest: (test: unknown) => ipcRenderer.invoke('workspace:save-test', test),
    deleteTest: (id: string) => ipcRenderer.invoke('workspace:delete-test', id),
    scanPage: (url: string, name: string, browser?: string) =>
      ipcRenderer.invoke('workspace:scan-page', { url, name, browser }),
    generate: (pageIds?: string[], testIds?: string[]) =>
      ipcRenderer.invoke('workspace:generate', { pageIds, testIds }),
    startRun: (testId: string, browser?: string) =>
      ipcRenderer.invoke('workspace:start-run', { testId, browser }),
    cancelRun: (runId: string) => ipcRenderer.invoke('workspace:cancel-run', runId),
    saveSecrets: (values: Record<string, string>) =>
      ipcRenderer.invoke('workspace:save-secrets', values),
    onRunEvent: (callback: (event: unknown) => void) =>
      subscribe('workspace:run-event', callback),
    onOpened: (callback: (snapshot: unknown) => void) => subscribe('workspace:opened', callback),
  },
  browser: {
    navigate: (url: string) => ipcRenderer.invoke('browser:navigate', url),
    back: () => ipcRenderer.invoke('browser:back'),
    forward: () => ipcRenderer.invoke('browser:forward'),
    reload: () => ipcRenderer.invoke('browser:reload'),
    setBounds: (bounds: unknown) => ipcRenderer.invoke('browser:bounds', bounds),
    setVisible: (visible: boolean) => ipcRenderer.invoke('browser:visible', visible),
    setRecording: (value: boolean) => ipcRenderer.invoke('browser:recording', value),
    onState: (callback: (state: unknown) => void) => subscribe('browser:state', callback),
    onRecordedInteraction: (callback: (interaction: unknown) => void) =>
      subscribe('browser:recorded-interaction', callback),
  },
})
