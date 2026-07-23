import { Copy, Plus, Trash2 } from 'lucide-react'
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
  const configureAction = (step: TestStep, action: StepAction) => {
    if (action === 'navigate') {
      updateStep(step.id, {
        action,
        label: actions[action],
        pageId: undefined,
        elementId: undefined,
        value: undefined,
        url: step.url || pages[0]?.url || 'https://example.com',
      })
      return
    }
    if (action === 'assertUrl') {
      updateStep(step.id, {
        action,
        label: actions[action],
        pageId: undefined,
        elementId: undefined,
        url: undefined,
        value: step.value ?? { kind: 'literal', value: pages[0]?.url || 'https://example.com' },
      })
      return
    }
    const page = pages.find((candidate) => candidate.id === step.pageId) ?? pages[0]
    updateStep(step.id, {
      action,
      label: actions[action],
      url: undefined,
      pageId: page?.id,
      elementId: page?.elements.some((element) => element.id === step.elementId)
        ? step.elementId
        : page?.elements[0]?.id,
      value: needsValue(action) ? step.value ?? { kind: 'literal', value: '' } : undefined,
    })
  }

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
              <span className="step-number">{index + 1}</span>
              <div className="step-copy">
                <div className="step-line">
                  <select
                    aria-label={`Action for step ${index + 1}`}
                    value={step.action}
                    onChange={(event) => configureAction(step, event.target.value as StepAction)}
                  >
                    {Object.entries(actions).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
                  </select>
                  <input
                    aria-label={`Label for step ${index + 1}`}
                    value={step.label}
                    onChange={(event) => updateStep(step.id, { label: event.target.value })}
                  />
                  <label className="step-enabled">
                    <input
                      aria-label={`Enabled for step ${index + 1}`}
                      type="checkbox"
                      checked={step.enabled}
                      onChange={(event) => updateStep(step.id, { enabled: event.target.checked })}
                    />
                    Enabled
                  </label>
                </div>
                <div className="step-config">
                  {step.action === 'navigate' && (
                    <input
                      aria-label={`URL for step ${index + 1}`}
                      value={step.url ?? ''}
                      onChange={(event) => updateStep(step.id, { url: event.target.value })}
                      placeholder="https://example.com"
                    />
                  )}
                  {step.action !== 'navigate' && step.action !== 'assertUrl' && (
                    <>
                      <select
                        aria-label={`Page for step ${index + 1}`}
                        value={step.pageId ?? ''}
                        onChange={(event) => {
                          const page = pages.find((candidate) => candidate.id === event.target.value)
                          updateStep(step.id, { pageId: page?.id, elementId: page?.elements[0]?.id })
                        }}
                      >
                        <option value="">Select page</option>
                        {pages.map((page) => <option value={page.id} key={page.id}>{page.name}</option>)}
                      </select>
                      <select
                        aria-label={`Element for step ${index + 1}`}
                        value={step.elementId ?? ''}
                        onChange={(event) => updateStep(step.id, { elementId: event.target.value || undefined })}
                      >
                        <option value="">Select element</option>
                        {(pages.find((page) => page.id === step.pageId)?.elements ?? []).map((candidate) => (
                          <option value={candidate.id} key={candidate.id}>{candidate.name}</option>
                        ))}
                      </select>
                    </>
                  )}
                  {(needsValue(step.action) || step.action === 'assertUrl') && (
                    <>
                      <select
                        aria-label={`Value type for step ${index + 1}`}
                        value={step.value?.kind ?? 'literal'}
                        onChange={(event) => updateStep(step.id, {
                          value: { kind: event.target.value as 'literal' | 'secret', value: step.value?.value ?? '' },
                        })}
                      >
                        <option value="literal">Literal</option>
                        <option value="secret">Secret</option>
                      </select>
                      <input
                        aria-label={`Value for step ${index + 1}`}
                        value={step.value?.value ?? ''}
                        onChange={(event) => updateStep(step.id, {
                          value: { kind: step.value?.kind ?? 'literal', value: event.target.value },
                        })}
                        placeholder={step.value?.kind === 'secret' ? 'secret_key' : 'Value'}
                      />
                    </>
                  )}
                </div>
                {element && (
                  <div className="locator"><span>{element.locator.strategy}</span><code>{element.locator.value}</code></div>
                )}
              </div>
              <div className="step-actions">
                <button
                  aria-label={`Duplicate step ${index + 1}`}
                  onClick={() => onChange({
                    ...test,
                    steps: [...test.steps.slice(0, index + 1), { ...step, id: crypto.randomUUID() }, ...test.steps.slice(index + 1)],
                    updatedAt: new Date().toISOString(),
                  })}
                ><Copy /></button>
                <button
                  aria-label={`Delete step ${index + 1}`}
                  onClick={() => onChange({
                    ...test,
                    steps: test.steps.filter((item) => item.id !== step.id),
                    updatedAt: new Date().toISOString(),
                  })}
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

function needsValue(action: StepAction) {
  return action === 'fill'
    || action === 'select'
    || action === 'pressKey'
    || action === 'assertText'
}
