import { Check, CircleAlert, LoaderCircle, Square, X } from 'lucide-react'
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
  if (!run) {
    return running
      ? <section className="results run-empty"><LoaderCircle className="spin" /><h2>Starting {test?.name}</h2><p>Preparing the Selenium browser.</p></section>
      : <section className="results run-empty"><CircleAlert /><h2>No run selected</h2><p>Run a test to see progress and failures.</p></section>
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
          return (
          <div className={`result-row ${isCurrent ? 'current' : result ? '' : 'pending'}`} key={step.id}>
            <span className={isCurrent ? 'current' : result?.status === 'failed' ? 'failed' : ''}>
              {isCurrent ? <LoaderCircle className="spin" /> : result?.status === 'passed' ? <Check /> : result ? <X /> : <span />}
            </span>
            <div>
              <strong>{step.label}</strong>
              <small>{isCurrent ? 'Executing in the Cliq browser…' : result?.error || result?.status || 'Waiting'}</small>
            </div>
            <time>{result ? `${result.durationMs} ms` : ''}</time>
          </div>
        )})}
      </div>
    </section>
  )
}
