import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { App } from './App'

describe('App', () => {
  it('offers to open a workspace when none is active', () => {
    render(<App />)

    expect(screen.getByRole('button', { name: 'Open workspace' })).toBeTruthy()
    expect(screen.getByText(/Generate maintainable Selenium code/i)).toBeTruthy()
  })
})
