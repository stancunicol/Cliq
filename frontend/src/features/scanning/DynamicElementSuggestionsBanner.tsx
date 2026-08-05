import { ChevronDown, ChevronUp, Plus, Sparkles, X } from 'lucide-react'
import { useState } from 'react'
import type { DynamicElementSuggestion } from '../../types'

const actionLabels: Record<DynamicElementSuggestion['action'], string> = {
  click: 'Click',
  fill: 'Fill',
  select: 'Select',
  assertText: 'Assert text',
}

/** Surfaces scan-detected repeating-element candidates (see backend `DynamicElementSuggestion`) -
 * purely additive, nothing is created until the user accepts an individual suggestion. Accepting
 * or dismissing is the parent's job (it owns the suggestion list, same as `LocatorDiffBanner`). */
export function DynamicElementSuggestionsBanner({
  suggestions,
  onAccept,
  onDismiss,
}: {
  suggestions: DynamicElementSuggestion[]
  onAccept(suggestion: DynamicElementSuggestion): void
  onDismiss(): void
}) {
  const [expanded, setExpanded] = useState(false)
  if (suggestions.length === 0) return null

  return (
    <div className="dynamic-suggestions-banner">
      <button className="dynamic-suggestions-summary" onClick={() => setExpanded((value) => !value)}>
        <Sparkles />
        <span>{suggestions.length} possible dynamic element{suggestions.length === 1 ? '' : 's'} found</span>
        {expanded ? <ChevronUp /> : <ChevronDown />}
      </button>
      <button
        className="dynamic-suggestions-dismiss"
        aria-label="Dismiss dynamic element suggestions"
        onClick={onDismiss}
      ><X /></button>
      {expanded && (
        <ul className="dynamic-suggestions-list">
          {suggestions.map((suggestion) => (
            <li key={`${suggestion.name}-${suggestion.locatorTemplate}`}>
              <strong>{suggestion.name}</strong>
              <span>{actionLabels[suggestion.action]} · seen {suggestion.sampleCount}×</span>
              <code>{suggestion.locatorTemplate}</code>
              <button aria-label={`Add dynamic element ${suggestion.name}`} onClick={() => onAccept(suggestion)}>
                <Plus />Add
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
