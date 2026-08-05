import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { DynamicElementSuggestionsBanner } from './DynamicElementSuggestionsBanner'
import type { DynamicElementSuggestion } from '../../types'

afterEach(() => cleanup())

function suggestion(overrides: Partial<DynamicElementSuggestion> = {}): DynamicElementSuggestion {
  return {
    name: 'row_button',
    action: 'click',
    strategy: 'xpath',
    locatorTemplate: '//table/tbody/tr[{param}]/td[1]/button',
    sampleCount: 5,
    ...overrides,
  }
}

describe('DynamicElementSuggestionsBanner', () => {
  it('renders nothing when there are no suggestions', () => {
    const { container } = render(
      <DynamicElementSuggestionsBanner suggestions={[]} onAccept={vi.fn()} onDismiss={vi.fn()} />,
    )

    expect(container.firstChild).toBeNull()
  })

  it('shows the suggestion count and reveals entries once expanded', () => {
    render(
      <DynamicElementSuggestionsBanner
        suggestions={[suggestion(), suggestion({ name: 'row_status', locatorTemplate: '//tr[{param}]/td[2]' })]}
        onAccept={vi.fn()}
        onDismiss={vi.fn()}
      />,
    )

    expect(screen.getByText('2 possible dynamic elements found')).toBeTruthy()
    expect(screen.queryByText('row_button')).toBeNull()

    fireEvent.click(screen.getByText('2 possible dynamic elements found'))

    expect(screen.getByText('row_button')).toBeTruthy()
    expect(screen.getByText('row_status')).toBeTruthy()
  })

  it('calls onAccept with the clicked suggestion', () => {
    const onAccept = vi.fn()
    const target = suggestion()
    render(
      <DynamicElementSuggestionsBanner suggestions={[target]} onAccept={onAccept} onDismiss={vi.fn()} />,
    )

    fireEvent.click(screen.getByText('1 possible dynamic element found'))
    fireEvent.click(screen.getByRole('button', { name: 'Add dynamic element row_button' }))

    expect(onAccept).toHaveBeenCalledWith(target)
  })

  it('calls onDismiss when the dismiss button is clicked', () => {
    const onDismiss = vi.fn()
    render(
      <DynamicElementSuggestionsBanner suggestions={[suggestion()]} onAccept={vi.fn()} onDismiss={onDismiss} />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Dismiss dynamic element suggestions' }))

    expect(onDismiss).toHaveBeenCalled()
  })
})
