import { describe, expect, it } from 'vitest'
import { recordedInteractionSchema, testCaseSchema } from '../electron/ipc-schemas'

describe('IPC schemas', () => {
  it('rejects recorded interactions with non-http page URLs', () => {
    const parsed = recordedInteractionSchema.safeParse({
      id: crypto.randomUUID(),
      action: 'click',
      label: 'Open file',
      pageUrl: 'file:///etc/passwd',
      element: {
        id: 'open-file',
        name: 'Open file',
        tag: 'button',
        action: 'click',
        locator: { strategy: 'id', value: 'open-file' },
      },
    })

    expect(parsed.success).toBe(false)
  })

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
})
