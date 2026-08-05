import { ChevronDown, ChevronUp, TriangleAlert, X } from 'lucide-react'
import { useState } from 'react'
import type { LocatorDiffEntry, LocatorDiffReport } from '../../types'

function EntryList({ title, entries }: { title: string; entries: LocatorDiffEntry[] }) {
  if (entries.length === 0) return null
  return (
    <section>
      <strong>{title}</strong>
      <ul>
        {entries.map((entry) => (
          <li key={entry.elementId}>
            <span>{entry.name}</span>
            <code>{entry.locator.strategy}: {entry.locator.value}</code>
          </li>
        ))}
      </ul>
    </section>
  )
}

/** Surfaces the locators-only scan diff (see backend `LocatorDiffReport`) that replaced
 * self-healing: nothing is auto-fixed, this just tells a human what appeared/disappeared. */
export function LocatorDiffBanner({ diff, onDismiss }: { diff: LocatorDiffReport; onDismiss(): void }) {
  const [expanded, setExpanded] = useState(false)
  const total = diff.new.length + diff.missing.length
  if (total === 0) return null

  return (
    <div className="locator-diff-banner">
      <button className="locator-diff-summary" onClick={() => setExpanded((value) => !value)}>
        <TriangleAlert />
        <span>{diff.new.length} new · {diff.missing.length} missing locator{total === 1 ? '' : 's'}</span>
        {expanded ? <ChevronUp /> : <ChevronDown />}
      </button>
      <button className="locator-diff-dismiss" aria-label="Dismiss locator diff" onClick={onDismiss}><X /></button>
      {expanded && (
        <div className="locator-diff-details">
          <EntryList title="New" entries={diff.new} />
          <EntryList title="Missing" entries={diff.missing} />
        </div>
      )}
    </div>
  )
}
