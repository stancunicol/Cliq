import { BaseWindow, session, WebContentsView, type Rectangle } from 'electron'

const partition = 'persist:cliq-browser'

/**
 * Owns the embedded browsing pane: a second `WebContentsView` the user
 * navigates manually to explore a site before scanning it. It no longer
 * drives test execution or recording - both moved to the backend
 * (Selenium) and were dropped entirely, respectively - so this is now just
 * navigation/lifecycle plumbing.
 */
export class BrowserController {
  readonly view: WebContentsView
  private visible = true
  private error?: string
  private bounds: Rectangle = { x: 0, y: 102, width: 0, height: 0 }
  private pendingNavigation?: { url: string; promise: Promise<void> }

  constructor(
    private readonly window: BaseWindow,
    private readonly shell: WebContentsView,
  ) {
    const browserSession = session.fromPartition(partition)
    browserSession.setPermissionRequestHandler((_contents, _permission, callback) => callback(false))
    browserSession.setPermissionCheckHandler(() => false)
    browserSession.on('will-download', (event) => event.preventDefault())

    this.view = new WebContentsView({
      webPreferences: {
        contextIsolation: true,
        nodeIntegration: false,
        sandbox: true,
        partition,
      },
    })
    this.window.contentView.addChildView(this.view)
    for (const event of [
      'did-start-loading',
      'did-stop-loading',
      'did-navigate',
      'did-navigate-in-page',
      'page-title-updated',
    ] as const) {
      this.view.webContents.on(event as never, () => {
        this.applyBounds()
        this.publish()
      })
    }
    this.view.webContents.on('did-fail-load', (_event, code, description, _url, isMainFrame) => {
      if (isMainFrame && code !== -3) {
        this.error = `${description} (${code})`
        this.applyBounds()
        this.publish()
      }
    })
    this.view.webContents.setWindowOpenHandler(({ url }) => {
      if (isHttpUrl(url)) void this.navigate(url)
      return { action: 'deny' }
    })
    this.view.webContents.on('will-navigate', (event, url) => {
      if (!isHttpUrl(url)) event.preventDefault()
    })
  }

  async navigate(url: string): Promise<void> {
    if (!isHttpUrl(url)) throw new Error('Only HTTP and HTTPS URLs are supported.')
    if (this.view.webContents.getURL() === url) return
    if (this.pendingNavigation?.url === url) return this.pendingNavigation.promise
    this.error = undefined
    this.applyBounds()
    const promise = this.view.webContents.loadURL(url)
      .catch((error: unknown) => {
        if (!isNavigationAborted(error)) throw error
      })
      .then(() => {
        this.applyBounds()
        this.publish()
      })
      .finally(() => {
        if (this.pendingNavigation?.promise === promise) this.pendingNavigation = undefined
      })
    this.pendingNavigation = { url, promise }
    return promise
  }

  back(): void {
    if (this.view.webContents.navigationHistory.canGoBack()) {
      this.view.webContents.navigationHistory.goBack()
    }
  }

  forward(): void {
    if (this.view.webContents.navigationHistory.canGoForward()) {
      this.view.webContents.navigationHistory.goForward()
    }
  }

  reload(): void {
    this.view.webContents.reload()
  }

  setBounds(bounds: Rectangle): void {
    this.bounds = bounds
    this.applyBounds()
  }

  setVisible(visible: boolean): void {
    if (this.visible === visible) return
    this.visible = visible
    if (visible) {
      this.window.contentView.addChildView(this.view)
      this.applyBounds()
    } else {
      this.view.setBounds({ x: 0, y: 0, width: 0, height: 0 })
      this.window.contentView.removeChildView(this.view)
    }
  }

  resize(): void {
    this.applyBounds()
  }

  close(): void {
    this.view.webContents.close()
  }

  private applyBounds(): void {
    if (!this.visible) return
    const currentUrl = this.view.webContents.getURL()
    if (this.error || !currentUrl || currentUrl === 'about:blank') {
      this.view.setBounds({ x: 0, y: 0, width: 0, height: 0 })
      return
    }
    const content = this.window.getContentBounds()
    const x = Math.max(0, Math.min(this.bounds.x, content.width))
    const y = Math.max(0, Math.min(this.bounds.y, content.height))
    this.view.setBounds({
      x,
      y,
      width: Math.max(0, Math.min(this.bounds.width, content.width - x)),
      height: Math.max(0, Math.min(this.bounds.height, content.height - y)),
    })
  }

  private publish(): void {
    if (this.shell.webContents.isDestroyed()) return
    this.shell.webContents.send('browser:state', {
      url: this.view.webContents.getURL(),
      title: this.view.webContents.getTitle() || 'New tab',
      loading: this.view.webContents.isLoading(),
      canGoBack: this.view.webContents.navigationHistory.canGoBack(),
      canGoForward: this.view.webContents.navigationHistory.canGoForward(),
      error: this.error,
    })
  }
}

function isNavigationAborted(error: unknown) {
  return typeof error === 'object'
    && error !== null
    && 'code' in error
    && error.code === 'ERR_ABORTED'
}

function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}
