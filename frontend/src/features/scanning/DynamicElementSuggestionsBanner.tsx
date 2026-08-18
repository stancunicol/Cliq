import { Plus, Sparkles } from 'lucide-react'
import { CollapsibleBanner } from '../../components/CollapsibleBanner'
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
  if (suggestions.length === 0) return null

  return (
    <CollapsibleBanner
      tone="dynamic-suggestions"
      icon={<Sparkles />}
      summary={`${suggestions.length} possible dynamic element${suggestions.length === 1 ? '' : 's'} found`}
      dismissLabel="Dismiss dynamic element suggestions"
      onDismiss={onDismiss}
    >
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
    </CollapsibleBanner>
  )
}
