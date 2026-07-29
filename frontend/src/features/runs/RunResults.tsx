import { Camera, Check, CircleAlert, LoaderCircle, Square, Wrench, X } from 'lucide-react'
import { useState } from 'react'
import { screenshotService } from '../../services'
import type { TestCase, TestRun } from '../../types'

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
  const [screenshots, setScreenshots] = useState<Record<string, string | null>>({})
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
      const dataUrl = await screenshotService.get(run.id, fileName).catch(() => null)
      setScreenshots((current) => ({ ...current, [fileName]: dataUrl }))
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
        {active && <button onClick={onCancel}><Square />Cancel run</button>}
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
                  {result?.selfHealed && (
                    <span className="healed-badge" title="Locator was re-derived automatically after the original one no longer matched.">
                      <Wrench />Self-healed
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
                {result && (result.locateMs !== undefined || result.actionMs !== undefined) && (
                  <small className="timing-breakdown">
                    {result.locateMs !== undefined ? `locate ${result.locateMs}ms` : ''}
                    {result.locateMs !== undefined && result.actionMs !== undefined ? ' · ' : ''}
                    {result.actionMs !== undefined ? `action ${result.actionMs}ms` : ''}
                  </small>
                )}
              </time>
            </div>
            {screenshotOpen && (
              <div className="screenshot-panel">
                {screenshots[result!.screenshotPath!] === undefined
                  ? <LoaderCircle className="spin" />
                  : screenshots[result!.screenshotPath!] === null
                    ? <span>Screenshot is no longer available.</span>
                    : <img src={screenshots[result!.screenshotPath!]!} alt={`Screenshot for step '${step.label}'`} />}
              </div>
            )}
          </div>
        )})}
      </div>
    </section>
  )
}
