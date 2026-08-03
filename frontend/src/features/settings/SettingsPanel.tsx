import { useEffect, useState } from 'react'
import { ArrowDown, ArrowUp, Plus, Save, Trash2 } from 'lucide-react'
import type { CliqSettings, IdentificationStrategy } from '../../types'

const strategyLabels: Record<IdentificationStrategy, string> = {
  testId: 'Test ID attribute',
  xpath: 'XPath',
}

const allStrategies: IdentificationStrategy[] = ['testId', 'xpath']

export function SettingsPanel({
  settings,
  saving,
  error,
  saved,
  onSave,
}: {
  settings?: CliqSettings
  saving: boolean
  error?: string
  saved: boolean
  onSave(settings: CliqSettings): void
}) {
  const [draft, setDraft] = useState<CliqSettings | undefined>(settings)
  const [patternErrors, setPatternErrors] = useState<Record<number, string>>({})

  useEffect(() => setDraft(settings), [settings])

  if (!draft) {
    return <section className="editor empty"><h2>Loading settings…</h2></section>
  }

  const update = (patch: Partial<CliqSettings>) => setDraft({ ...draft, ...patch })

  const toggleStrategy = (strategy: IdentificationStrategy) => {
    const order = draft.elementIdentification.strategyOrder
    const next = order.includes(strategy)
      ? order.filter((item) => item !== strategy)
      : [...order, strategy]
    update({ elementIdentification: { ...draft.elementIdentification, strategyOrder: next } })
  }

  const moveStrategy = (index: number, direction: -1 | 1) => {
    const order = [...draft.elementIdentification.strategyOrder]
    const target = index + direction
    if (target < 0 || target >= order.length) return
    const [moved] = order.splice(index, 1)
    order.splice(target, 0, moved)
    update({ elementIdentification: { ...draft.elementIdentification, strategyOrder: order } })
  }

  const updatePattern = (index: number, value: string) => {
    const patterns = draft.elementIdentification.ignoreIdPatterns.map((item, itemIndex) =>
      itemIndex === index ? value : item)
    update({ elementIdentification: { ...draft.elementIdentification, ignoreIdPatterns: patterns } })
    setPatternErrors((current) => {
      const next = { ...current }
      try {
        new RegExp(value)
        delete next[index]
      } catch (cause) {
        next[index] = cause instanceof Error ? cause.message : 'Invalid regular expression.'
      }
      return next
    })
  }

  const addPattern = () => update({
    elementIdentification: {
      ...draft.elementIdentification,
      ignoreIdPatterns: [...draft.elementIdentification.ignoreIdPatterns, ''],
    },
  })

  const removePattern = (index: number) => {
    update({
      elementIdentification: {
        ...draft.elementIdentification,
        ignoreIdPatterns: draft.elementIdentification.ignoreIdPatterns.filter((_, itemIndex) => itemIndex !== index),
      },
    })
    setPatternErrors((current) => {
      const next: Record<number, string> = {}
      for (const [key, message] of Object.entries(current)) {
        const keyIndex = Number(key)
        if (keyIndex < index) next[keyIndex] = message
        else if (keyIndex > index) next[keyIndex - 1] = message
      }
      return next
    })
  }

  const hasPatternErrors = Object.keys(patternErrors).length > 0
  const hasEmptyStrategyOrder = draft.elementIdentification.strategyOrder.length === 0

  return (
    <section className="editor settings">
      <div className="editor-intro">
        <div>
          <span className="eyebrow">WORKSPACE SETTINGS</span>
          <h1 className="title-input">Settings</h1>
          <p>Controls element identification, timeouts, and screenshot capture for this workspace.</p>
        </div>
      </div>

      <div className="settings-section">
        <h3>Element identification</h3>
        <label className="settings-field">
          <span>Test ID attribute</span>
          <input
            aria-label="Test ID attribute"
            type="text"
            value={draft.elementIdentification.testIdAttribute}
            onChange={(event) => update({
              elementIdentification: { ...draft.elementIdentification, testIdAttribute: event.target.value },
            })}
            placeholder="data-testid"
          />
        </label>

        <div className="settings-field">
          <span>Strategy order {hasEmptyStrategyOrder && <em className="field-error">Select at least one strategy</em>}</span>
          <div className="strategy-list">
            {allStrategies.map((strategy) => {
              const index = draft.elementIdentification.strategyOrder.indexOf(strategy)
              const active = index >= 0
              return (
                <div className={`strategy-row ${active ? 'active' : ''}`} key={strategy}>
                  <label>
                    <input
                      type="checkbox"
                      checked={active}
                      onChange={() => toggleStrategy(strategy)}
                    />
                    {strategyLabels[strategy]}
                  </label>
                  {active && (
                    <div className="strategy-move">
                      <button
                        aria-label={`Move ${strategy} up`}
                        disabled={index === 0}
                        onClick={() => moveStrategy(index, -1)}
                      ><ArrowUp /></button>
                      <button
                        aria-label={`Move ${strategy} down`}
                        disabled={index === draft.elementIdentification.strategyOrder.length - 1}
                        onClick={() => moveStrategy(index, 1)}
                      ><ArrowDown /></button>
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>

        <div className="settings-field">
          <span>Ignore ID patterns <em>Regexes matching framework-generated, unstable ids</em></span>
          <div className="pattern-list">
            {draft.elementIdentification.ignoreIdPatterns.map((pattern, index) => (
              <div className="pattern-row" key={index}>
                <input
                  aria-label={`Ignore id pattern ${index + 1}`}
                  className={patternErrors[index] ? 'invalid' : ''}
                  value={pattern}
                  onChange={(event) => updatePattern(index, event.target.value)}
                  placeholder="^ember\d+$"
                />
                <button aria-label={`Remove pattern ${index + 1}`} onClick={() => removePattern(index)}><Trash2 /></button>
                {patternErrors[index] && <small className="field-error">{patternErrors[index]}</small>}
              </div>
            ))}
            <button className="add-row" onClick={addPattern}><Plus />Add pattern</button>
          </div>
        </div>
      </div>

      <div className="settings-section">
        <h3>Test execution</h3>
        <label className="settings-field">
          <span>Default step timeout (ms)</span>
          <input
            aria-label="Default step timeout"
            type="number"
            min={1}
            value={draft.testExecution.defaultStepTimeoutMs}
            onChange={(event) => update({
              testExecution: { ...draft.testExecution, defaultStepTimeoutMs: Number(event.target.value) },
            })}
          />
        </label>
        <label className="settings-field">
          <span>Page ready timeout (ms)</span>
          <input
            aria-label="Page ready timeout"
            type="number"
            min={1}
            value={draft.testExecution.pageReadyTimeoutMs}
            onChange={(event) => update({
              testExecution: { ...draft.testExecution, pageReadyTimeoutMs: Number(event.target.value) },
            })}
          />
        </label>
        <label className="settings-checkbox">
          <input
            type="checkbox"
            checked={draft.testExecution.captureScreenshotOnFailure}
            onChange={(event) => update({
              testExecution: { ...draft.testExecution, captureScreenshotOnFailure: event.target.checked },
            })}
          />
          Capture screenshot on step failure
        </label>
      </div>

      <div className="settings-section">
        <h3>Page scanning</h3>
        <label className="settings-field">
          <span>Load timeout (ms)</span>
          <input
            aria-label="Load timeout"
            type="number"
            min={1}
            value={draft.scanning.loadTimeoutMs}
            onChange={(event) => update({
              scanning: { ...draft.scanning, loadTimeoutMs: Number(event.target.value) },
            })}
          />
        </label>
        <label className="settings-field">
          <span>Network idle grace (ms)</span>
          <input
            aria-label="Network idle grace"
            type="number"
            min={1}
            value={draft.scanning.networkIdleGraceMs}
            onChange={(event) => update({
              scanning: { ...draft.scanning, networkIdleGraceMs: Number(event.target.value) },
            })}
          />
        </label>
        <label className="settings-checkbox">
          <input
            type="checkbox"
            checked={draft.scanning.captureScreenshot}
            onChange={(event) => update({
              scanning: { ...draft.scanning, captureScreenshot: event.target.checked },
            })}
          />
          Capture screenshot while scanning
        </label>
      </div>

      {error && <div className="error-banner">{error}</div>}
      <div className="settings-actions">
        <button
          className="save-settings"
          disabled={saving || hasPatternErrors || hasEmptyStrategyOrder}
          onClick={() => onSave(draft)}
        >
          <Save />{saving ? 'Saving…' : 'Save changes'}
        </button>
        {saved && <span className="settings-saved">Saved</span>}
      </div>
    </section>
  )
}
