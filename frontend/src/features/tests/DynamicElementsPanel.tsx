import { ChevronDown, ChevronUp, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import type { DynamicElement, DynamicElementAction, LocatorStrategy, PageDefinition } from '../../types'

const dynamicActions: Record<DynamicElementAction, string> = {
  click: 'Click',
  fill: 'Fill',
  select: 'Select',
  assertText: 'Assert text',
}

// Only testId/xpath/css - per the 2026-08-03 identification decision, id and name are never
// valid locator strategies (id is a naming hint only; name exists solely for a Radio group's own
// selector, which has no dynamic equivalent).
const strategies: LocatorStrategy[] = ['testId', 'css', 'xpath']

/** Lets the user hand-author a locator template for a repeating element (e.g. a table row) that
 * a one-time scan can't discover on its own - see DynamicElement. Collapsed by default; a page's
 * dynamic elements are saved onto that PageDefinition through the same savePage plumbing Scan
 * already uses, so there's no separate storage path to reason about. */
export function DynamicElementsPanel({
  pages,
  onSave,
  onDelete,
}: {
  pages: PageDefinition[]
  onSave(pageId: string, element: DynamicElement): void
  onDelete(pageId: string, elementId: string): void
}) {
  const [expanded, setExpanded] = useState(false)
  const [pageId, setPageId] = useState(pages[0]?.id ?? '')
  const [showForm, setShowForm] = useState(false)
  const [name, setName] = useState('')
  const [action, setAction] = useState<DynamicElementAction>('click')
  const [strategy, setStrategy] = useState<LocatorStrategy>('xpath')
  const [template, setTemplate] = useState('')
  const [parameterType, setParameterType] = useState('int')

  if (pages.length === 0) return null

  const page = pages.find((candidate) => candidate.id === pageId) ?? pages[0]
  const elements = page.dynamicElements ?? []
  const canSubmit = name.trim().length > 0 && template.includes('{param}')

  const reset = () => {
    setName('')
    setAction('click')
    setStrategy('xpath')
    setTemplate('')
    setParameterType('int')
    setShowForm(false)
  }

  const submit = () => {
    if (!canSubmit) return
    onSave(page.id, {
      id: crypto.randomUUID(),
      name: name.trim(),
      action,
      strategy,
      locatorTemplate: template,
      parameterType,
    })
    reset()
  }

  return (
    <div className="dynamic-elements">
      <button className="dynamic-elements-summary" onClick={() => setExpanded((value) => !value)}>
        <span>Dynamic elements{elements.length > 0 ? ` (${elements.length})` : ''}</span>
        {expanded ? <ChevronUp /> : <ChevronDown />}
      </button>
      {expanded && (
        <div className="dynamic-elements-body">
          <select
            aria-label="Page for dynamic elements"
            value={page.id}
            onChange={(event) => { setPageId(event.target.value); reset() }}
          >
            {pages.map((candidate) => <option value={candidate.id} key={candidate.id}>{candidate.name}</option>)}
          </select>
          <ul className="dynamic-elements-list">
            {elements.map((element) => (
              <li key={element.id}>
                <strong>{element.name}</strong>
                <span>{dynamicActions[element.action]}</span>
                <code>{element.locatorTemplate}</code>
                <button
                  aria-label={`Delete dynamic element ${element.name}`}
                  onClick={() => onDelete(page.id, element.id)}
                ><Trash2 /></button>
              </li>
            ))}
            {elements.length === 0 && <li className="dynamic-elements-empty">No dynamic elements for this page yet.</li>}
          </ul>
          {showForm ? (
            <div className="dynamic-elements-form">
              <input
                aria-label="Dynamic element name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Name"
              />
              <select
                aria-label="Dynamic element action"
                value={action}
                onChange={(event) => setAction(event.target.value as DynamicElementAction)}
              >
                {Object.entries(dynamicActions).map(([value, label]) => <option value={value} key={value}>{label}</option>)}
              </select>
              <select
                aria-label="Dynamic element locator strategy"
                value={strategy}
                onChange={(event) => setStrategy(event.target.value as LocatorStrategy)}
              >
                {strategies.map((value) => <option value={value} key={value}>{value}</option>)}
              </select>
              <input
                aria-label="Dynamic element locator template"
                value={template}
                onChange={(event) => setTemplate(event.target.value)}
                placeholder="//table/tbody/tr[{param}]/td[2]"
              />
              <select
                aria-label="Dynamic element parameter type"
                value={parameterType}
                onChange={(event) => setParameterType(event.target.value)}
              >
                <option value="int">Index (int)</option>
                <option value="string">Key (string)</option>
              </select>
              <button onClick={submit} disabled={!canSubmit}><Plus />Save</button>
              <button onClick={reset}>Cancel</button>
            </div>
          ) : (
            <button className="add-row" onClick={() => setShowForm(true)}><Plus />Add dynamic element</button>
          )}
        </div>
      )}
    </div>
  )
}
