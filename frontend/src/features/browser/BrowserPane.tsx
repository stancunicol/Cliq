import { ArrowLeft, ArrowRight, Ellipsis, Globe2, RefreshCw, X } from 'lucide-react'
import type { BrowserState } from '../../types'

export function BrowserPane({
  state,
  url,
  recording,
  onUrl,
  onNavigate,
  onStopRecording,
}: {
  state: BrowserState
  url: string
  recording: boolean
  onUrl(value: string): void
  onNavigate(): void
  onStopRecording(): void
}) {
  return (
    <aside className="browser-pane">
      <div className="browser-title">
        <div className="tab"><Globe2 /><span>{state.title || 'New tab'}</span></div>
        <span className="window-drag" />
      </div>
      <div className="browser-toolbar">
        <button aria-label="Go back" disabled={!state.canGoBack} onClick={() => window.cliq?.browser.back()}><ArrowLeft /></button>
        <button aria-label="Go forward" disabled={!state.canGoForward} onClick={() => window.cliq?.browser.forward()}><ArrowRight /></button>
        <button aria-label="Reload" onClick={() => window.cliq?.browser.reload()}>{state.loading ? <X /> : <RefreshCw />}</button>
        <form onSubmit={(event) => { event.preventDefault(); onNavigate() }}>
          <Globe2 />
          <input aria-label="Browser URL" value={url} onChange={(event) => onUrl(event.target.value)} />
        </form>
        <button aria-label="Browser options"><Ellipsis /></button>
      </div>
      {(state.error || !state.url || state.url === 'about:blank') && (
        <div className="browser-fallback">
          <Globe2 />
          <h2>{state.error ? 'Page failed to load' : 'Open a page'}</h2>
          <p>{state.error || 'Enter an HTTP or HTTPS URL above.'}</p>
        </div>
      )}
      {recording && (
        <div className="recording-banner">
          <span /><strong>Recording</strong><em>Interactions become test steps</em>
          <button onClick={onStopRecording}>Stop</button>
        </div>
      )}
    </aside>
  )
}
