import { TriangleAlert } from 'lucide-react'
import { CollapsibleBanner } from '../../components/CollapsibleBanner'
import type { CustomMethodRecoveryWarning } from '../../types'

/** Surfaces custom-method recovery warnings from a generate() call: the backend found a Page
 * Object file with hand-written [CliqCustomMethod] content it couldn't safely fold into the
 * current output - either a same-name regeneration recovered fewer marked methods than expected,
 * or a page was renamed and its old file still has some - and left the file in place rather than
 * silently dropping the difference. */
export function GenerationWarningBanner({
  warnings,
  onDismiss,
}: {
  warnings: CustomMethodRecoveryWarning[]
  onDismiss(): void
}) {
  if (warnings.length === 0) return null

  return (
    <CollapsibleBanner
      tone="locator-diff"
      icon={<TriangleAlert />}
      summary={
        <>
          {warnings.length} page object{warnings.length === 1 ? '' : 's'} may have lost custom
          method{warnings.length === 1 ? '' : 's'} on regeneration
        </>
      }
      dismissLabel="Dismiss generation warning"
      onDismiss={onDismiss}
    >
      <div className="locator-diff-details">
        <section>
          <strong>Recovery mismatch</strong>
          <ul>
            {warnings.map((warning) => (
              <li key={warning.pageObjectName}>
                <span>{warning.pageObjectName}</span>
                <code>
                  {warning.recoveredCount}/{warning.expectedCount} recovered · see: {warning.path}
                </code>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </CollapsibleBanner>
  )
}
