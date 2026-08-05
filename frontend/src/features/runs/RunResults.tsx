import { Camera, Check, CircleAlert, Hourglass, LoaderCircle, Square, X } from 'lucide-react'
import { useState } from 'react'
import { screenshotService } from '../../services'
import type { StepResult, TestCase, TestRun } from '../../types'

export function RunResults({
  run,
  test,
  running,
  activeStepId,
  onCancel,
}: {
  run?: TestRun
  test?: TestCase
  running: boolean
  activeStepId?: string
  onCancel(): void
}) {
  type ScreenshotState = { status: 'ready'; url: string } | { status: 'missing' } | { status: 'error'; message: string }
  const [screenshots, setScreenshots] = useState<Record<string, ScreenshotState>>({})
  const [openScreenshot, setOpenScreenshot] = useState<string>()

  if (!run) {
    return running
      ? <section className="results run-empty"><LoaderCircle className="spin" /><h2>Starting {test?.name}</h2><p>Preparing the Selenium browser.</p></section>
      : <section className="results run-empty"><CircleAlert /><h2>No run selected</h2><p>Run a test to see progress and failures.</p></section>
  }

  async function toggleScreenshot(fileName: string) {
    if (openScreenshot === fileName) {
      setOpenScreenshot(undefined)
      return
    }
    setOpenScreenshot(fileName)
    if (!(fileName in screenshots) && run) {
      try {
        const dataUrl = await screenshotService.get(run.id, fileName)
        setScreenshots((current) => ({
          ...current,
          [fileName]: dataUrl ? { status: 'ready', url: dataUrl } : { status: 'missing' },
        }))
      } catch (cause) {
        setScreenshots((current) => ({
          ...current,
          [fileName]: { status: 'error', message: cause instanceof Error ? cause.message : String(cause) },
        }))
      }
    }
  }

  const passed = run.status === 'passed'
  const active = running || run.status === 'queued' || run.status === 'running'
  const activeStep = test?.steps.find((step) => step.id === activeStepId)
  const plannedSteps = test?.steps.filter((step) => step.enabled) ?? []
  return (
    <section className="results">
      <div className="result-hero">
        <div className={`result-icon ${passed || active ? '' : 'failed'}`}>
          {active ? <LoaderCircle className="spin" /> : passed ? <Check /> : <X />}
        </div>
        <div>
          <span className="eyebrow">RUN {(active ? 'running' : run.status).toUpperCase()}</span>
          <h2>
            {active
              ? activeStep ? `Executing: ${activeStep.label}` : 'Preparing the in-app browser'
              : passed ? 'All steps passed' : run.error || 'The workflow failed'}
          </h2>
          <p>{test?.name}</p>
        </div>
        {active && <button className="cancel-run" onClick={onCancel}><Square />Cancel run</button>}
      </div>
      <div className="result-panel">
        <h3>Step timeline</h3>
        {plannedSteps.length === 0 && <p className="result-waiting">Waiting for the first step…</p>}
        {plannedSteps.map((step) => {
          const result = run.steps.find((candidate) => candidate.stepId === step.id)
          const isCurrent = step.id === activeStepId
          const screenshotOpen = result?.screenshotPath !== undefined && openScreenshot === result.screenshotPath
          return (
          <div key={step.id}>
            <div className={`result-row ${isCurrent ? 'current' : result ? '' : 'pending'}`}>
              <span className={isCurrent ? 'current' : result?.status === 'failed' ? 'failed' : ''}>
                {isCurrent ? <LoaderCircle className="spin" /> : result?.status === 'passed' ? <Check /> : result ? <X /> : <span />}
              </span>
              <div>
                <strong>
                  {step.label}
                  {result?.extendedTimeoutMs !== undefined && (
                    <span
                      className="extended-badge"
                      title="This step's history shows it routinely takes longer, so the wait was extended automatically."
                    >
                      <Hourglass />Timeout extended to {result.extendedTimeoutMs}ms
                    </span>
                  )}
                </strong>
                <small>{isCurrent ? 'Executing in the Cliq browser…' : result?.error || result?.status || 'Waiting'}</small>
              </div>
              {result?.screenshotPath && (
                <button
                  className={`screenshot-toggle ${screenshotOpen ? 'active' : ''}`}
                  onClick={() => void toggleScreenshot(result.screenshotPath!)}
                >
                  <Camera />Screenshot
                </button>
              )}
              <time>
                {result ? `${result.durationMs} ms` : ''}
                {result && timingBreakdown(result) && (
                  <small className="timing-breakdown">{timingBreakdown(result)}</small>
                )}
              </time>
            </div>
            {screenshotOpen && (() => {
              const state = screenshots[result!.screenshotPath!]
              return (
                <div className="screenshot-panel">
                  {!state && <LoaderCircle className="spin" />}
                  {state?.status === 'ready' && <img src={state.url} alt={`Screenshot for step '${step.label}'`} />}
                  {state?.status === 'missing' && <span>Screenshot is no longer available.</span>}
                  {state?.status === 'error' && <span className="screenshot-error">Couldn't load screenshot: {state.message}</span>}
                </div>
              )
            })()}
          </div>
        )})}
      </div>
    </section>
  )
}

function timingBreakdown(result: StepResult) {
  const parts: string[] = []
  if (result.locateMs !== undefined) parts.push(`locate ${result.locateMs}ms`)
  if (result.actionMs !== undefined) parts.push(`action ${result.actionMs}ms`)
  if (result.syncMs !== undefined) parts.push(`sync ${result.syncMs}ms`)
  return parts.join(' · ')
}
