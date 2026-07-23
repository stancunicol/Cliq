import { Copy, GripVertical, Plus, Trash2 } from 'lucide-react'
import type { PageDefinition, StepAction, TestCase, TestStep } from '../../types'

const actions: Record<StepAction, string> = {
  navigate: 'Navigate',
  click: 'Click',
  fill: 'Fill',
  select: 'Select',
  pressKey: 'Press key',
  assertVisible: 'Assert visible',
  assertText: 'Assert text',
  assertUrl: 'Assert URL',
}

export function TestEditor({
  test,
  pages,
  onChange,
}: {
  test?: TestCase
  pages: PageDefinition[]
  onChange(test: TestCase): void
}) {
  if (!test) {
    return <section className="editor empty"><h2>Create a test</h2><p>Record browser interactions or add a step manually.</p></section>
  }
  const updateStep = (id: string, patch: Partial<TestStep>) => onChange({
    ...test,
    steps: test.steps.map((step) => step.id === id ? { ...step, ...patch } : step),
    updatedAt: new Date().toISOString(),
  })
  const addStep = () => onChange({
    ...test,
    steps: [...test.steps, {
      id: crypto.randomUUID(),
      action: 'navigate',
      label: 'Navigate',
      enabled: true,
      url: 'https://example.com',
    }],
    updatedAt: new Date().toISOString(),
  })
  const elementFor = (step: TestStep) =>
    pages.find((page) => page.id === step.pageId)?.elements.find((element) => element.id === step.elementId)

  return (
    <section className="editor">
      <div className="editor-intro">
        <div>
          <span className="eyebrow">TEST CASE</span>
          <input
            aria-label="Test name"
            className="title-input"
            value={test.name}
            onChange={(event) => onChange({ ...test, name: event.target.value })}
          />
          <p>{test.description || 'Record or compose a maintainable browser workflow.'}</p>
        </div>
      </div>
      <div className="steps-head">
        <div><h3>Steps</h3><span>{test.steps.length}</span></div>
        <button onClick={addStep}><Plus />Add step</button>
      </div>
      <div className="steps">
        {test.steps.map((step, index) => {
          const element = elementFor(step)
          return (
            <article className={`step ${step.enabled ? '' : 'disabled'}`} key={step.id}>
              <GripVertical className="grip" />
              <span className="step-number">{index + 1}</span>
              <div className="step-copy">
                <div className="step-line">
                  <select
                    aria-label={`Action for step ${index + 1}`}
                    value={step.action}
                    onChange={(event) => updateStep(step.id, {
                      action: event.target.value as StepAction,
                      label: actions[event.target.value as StepAction],
                    })}
                  >
                    {Object.entries(actions).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
                  </select>
                  <input
                    aria-label={`Label for step ${index + 1}`}
                    value={step.label}
                    onChange={(event) => updateStep(step.id, { label: event.target.value })}
                  />
                </div>
                <div className="locator">
                  {element && <><span>{element.locator.strategy}</span><code>{element.locator.value}</code></>}
                  {step.url && <><span>url</span><code>{step.url}</code></>}
                  {step.value && <><span>{step.value.kind}</span><code>{step.value.value}</code></>}
                </div>
              </div>
              <div className="step-actions">
                <button
                  aria-label="Duplicate step"
                  onClick={() => onChange({
                    ...test,
                    steps: [...test.steps.slice(0, index + 1), { ...step, id: crypto.randomUUID() }, ...test.steps.slice(index + 1)],
                  })}
                ><Copy /></button>
                <button
                  aria-label="Delete step"
                  onClick={() => onChange({ ...test, steps: test.steps.filter((item) => item.id !== step.id) })}
                ><Trash2 /></button>
              </div>
            </article>
          )
        })}
        <button className="add-row" onClick={addStep}><Plus />Add another step</button>
      </div>
    </section>
  )
}
