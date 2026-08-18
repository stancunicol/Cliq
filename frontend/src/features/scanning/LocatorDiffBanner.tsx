import { TriangleAlert } from 'lucide-react'
import { CollapsibleBanner } from '../../components/CollapsibleBanner'
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
  const total = diff.new.length + diff.missing.length
  if (total === 0) return null

  return (
    <CollapsibleBanner
      tone="locator-diff"
      icon={<TriangleAlert />}
      summary={`${diff.new.length} new · ${diff.missing.length} missing locator${total === 1 ? '' : 's'}`}
      dismissLabel="Dismiss locator diff"
      onDismiss={onDismiss}
    >
      <div className="locator-diff-details">
        <EntryList title="New" entries={diff.new} />
        <EntryList title="Missing" entries={diff.missing} />
      </div>
    </CollapsibleBanner>
  )
}
