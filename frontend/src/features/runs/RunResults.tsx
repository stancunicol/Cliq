import { Check, CircleAlert, LoaderCircle, X } from 'lucide-react'
import type { TestCase, TestRun } from '../../types'

export function RunResults({ run, test, running }: { run?: TestRun; test?: TestCase; running: boolean }) {
  if (running) {
    return <section className="results run-empty"><LoaderCircle className="spin" /><h2>Running {test?.name}</h2><p>Selenium is executing the recorded workflow.</p></section>
  }
  if (!run) {
    return <section className="results run-empty"><CircleAlert /><h2>No run selected</h2><p>Run a test to see progress and failures.</p></section>
  }
  const passed = run.status === 'passed'
  return (
    <section className="results">
      <div className="result-hero">
        <div className={`result-icon ${passed ? '' : 'failed'}`}>{passed ? <Check /> : <X />}</div>
        <div>
          <span className="eyebrow">RUN {run.status.toUpperCase()}</span>
          <h2>{passed ? 'All steps passed' : run.error || 'The workflow failed'}</h2>
          <p>{test?.name}</p>
        </div>
      </div>
      <div className="result-panel">
        <h3>Step timeline</h3>
        {run.steps.map((result) => (
          <div className="result-row" key={result.stepId}>
            <span>{result.status === 'passed' ? <Check /> : <X />}</span>
            <div>
              <strong>{test?.steps.find((step) => step.id === result.stepId)?.label || result.stepId}</strong>
              <small>{result.error || result.status}</small>
            </div>
            <time>{result.durationMs} ms</time>
          </div>
        ))}
      </div>
    </section>
  )
}
