import { BaseWindow, session, WebContentsView, type Rectangle } from 'electron'
import path from 'node:path'
import { recordedInteractionSchema } from './ipc-schemas.js'

const partition = 'persist:cliq-browser'

export class BrowserController {
  readonly view: WebContentsView
  private recording = false
  private visible = true
  private error?: string
  private bounds: Rectangle = { x: 0, y: 102, width: 0, height: 0 }

  constructor(
    private readonly window: BaseWindow,
    private readonly shell: WebContentsView,
    preloadDirectory: string,
  ) {
    const browserSession = session.fromPartition(partition)
    browserSession.setPermissionRequestHandler((_contents, _permission, callback) => callback(false))
    browserSession.setPermissionCheckHandler(() => false)
    browserSession.on('will-download', (event) => event.preventDefault())

    this.view = new WebContentsView({
      webPreferences: {
        preload: path.join(preloadDirectory, 'recorder-preload.cjs'),
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
      this.view.webContents.on(event as never, () => this.publish())
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
    this.view.webContents.ipc.on('recorder:interaction', (event, interaction) => {
      if (this.recording && event.sender === this.view.webContents) {
        const parsed = recordedInteractionSchema.safeParse(interaction)
        if (parsed.success) {
          this.shell.webContents.send('browser:recorded-interaction', parsed.data)
        }
      }
    })
    void this.view.webContents.loadURL('about:blank')
  }

  async navigate(url: string): Promise<void> {
    if (!isHttpUrl(url)) throw new Error('Only HTTP and HTTPS URLs are supported.')
    this.error = undefined
    this.applyBounds()
    await this.view.webContents.loadURL(url)
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

  setRecording(recording: boolean): void {
    this.recording = recording
    this.view.webContents.send('recorder:enabled', recording)
    this.publish()
  }

  resize(): void {
    this.applyBounds()
  }

  close(): void {
    this.view.webContents.close()
  }

  private applyBounds(): void {
    if (!this.visible) return
    if (this.error) {
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
      recording: this.recording,
      error: this.error,
    })
  }
}

function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}
