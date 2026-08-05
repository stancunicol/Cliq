import { describe, expect, it } from 'vitest'
import { testCaseSchema } from '../electron/ipc-schemas'

describe('IPC schemas', () => {
  it('accepts versioned test specifications', () => {
    const parsed = testCaseSchema.safeParse({
      schemaVersion: 1,
      id: 'login',
      name: 'Login',
      tags: ['critical'],
      steps: [{
        id: 'open',
        action: 'navigate',
        label: 'Open login',
        enabled: true,
        url: 'https://example.com/login',
      }],
      updatedAt: new Date().toISOString(),
    })

    expect(parsed.success).toBe(true)
  })

  it('normalizes nullable optional fields returned by the .NET backend', () => {
    const parsed = testCaseSchema.parse({
      schemaVersion: 1,
      id: 'round-trip',
      name: 'Round trip',
      description: null,
      tags: [],
      steps: [{
        id: 'open',
        action: 'navigate',
        label: 'Open',
        enabled: true,
        pageId: null,
        elementId: null,
        url: 'https://example.com',
        value: null,
        timeoutMs: null,
      }],
      updatedAt: new Date().toISOString(),
    })

    expect(parsed.description).toBeUndefined()
    expect(parsed.steps[0].pageId).toBeUndefined()
    expect(parsed.steps[0].value).toBeUndefined()
  })
})
