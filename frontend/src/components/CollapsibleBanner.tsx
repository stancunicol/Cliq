import { ChevronDown, ChevronUp, X } from 'lucide-react'
import { useState, type ReactNode } from 'react'

/** Shared expand/dismiss shell behind every scan/generation banner (`LocatorDiffBanner`,
 * `GenerationWarningBanner`, `DynamicElementSuggestionsBanner`) - each owns its own icon, summary
 * text, and details markup, but the expand/collapse/dismiss chrome used to be copy-pasted into
 * every one of them. `tone` picks which banner's CSS class prefix (and color theme, see
 * layout.css) applies; `children` is only mounted once expanded, matching each banner's previous
 * "just don't render the details" behavior rather than hiding them with CSS. */
export function CollapsibleBanner({
  tone,
  icon,
  summary,
  dismissLabel,
  onDismiss,
  children,
}: {
  tone: 'locator-diff' | 'dynamic-suggestions'
  icon: ReactNode
  summary: ReactNode
  dismissLabel: string
  onDismiss(): void
  children: ReactNode
}) {
  const [expanded, setExpanded] = useState(false)

  return (
    <div className={`${tone}-banner`}>
      <button className={`${tone}-summary`} onClick={() => setExpanded((value) => !value)}>
        {icon}
        <span>{summary}</span>
        {expanded ? <ChevronUp /> : <ChevronDown />}
      </button>
      <button className={`${tone}-dismiss`} aria-label={dismissLabel} onClick={onDismiss}><X /></button>
      {expanded && children}
    </div>
  )
}
