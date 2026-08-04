import { ChevronDown, ChevronUp, TriangleAlert, X } from 'lucide-react'
import { useState } from 'react'
import type { CustomMethodRecoveryWarning } from '../../types'

/** Surfaces custom-method recovery warnings from a generate() call: the backend detected that
 * fewer [CliqCustomMethod]-marked methods came back out of a Page Object file than its marker
 * count suggests it should have, backed the pre-regeneration file up rather than silently
 * dropping the difference, and reports it here so a human can look. */
export function GenerationWarningBanner({
  warnings,
  onDismiss,
}: {
  warnings: CustomMethodRecoveryWarning[]
  onDismiss(): void
}) {
  const [expanded, setExpanded] = useState(false)
  if (warnings.length === 0) return null

  return (
    <div className="locator-diff-banner">
      <button className="locator-diff-summary" onClick={() => setExpanded((value) => !value)}>
        <TriangleAlert />
        <span>
          {warnings.length} page object{warnings.length === 1 ? '' : 's'} may have lost custom
          method{warnings.length === 1 ? '' : 's'} on regeneration
        </span>
        {expanded ? <ChevronUp /> : <ChevronDown />}
      </button>
      <button className="locator-diff-dismiss" aria-label="Dismiss generation warning" onClick={onDismiss}><X /></button>
      {expanded && (
        <div className="locator-diff-details">
          <section>
            <strong>Recovery mismatch</strong>
            <ul>
              {warnings.map((warning) => (
                <li key={warning.pageObjectName}>
                  <span>{warning.pageObjectName}</span>
                  <code>
                    {warning.recoveredCount}/{warning.expectedCount} recovered · backup: {warning.backupPath}
                  </code>
                </li>
              ))}
            </ul>
          </section>
        </div>
      )}
    </div>
  )
}
