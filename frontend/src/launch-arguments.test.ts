import { describe, expect, it } from 'vitest'
import {
  parseWorkspaceLaunch,
  workspaceFromAdditionalData,
} from '../electron/launch-arguments'

describe('workspace launch arguments', () => {
  const workspace = 'C:\\Cliq\\workspace'

  it('parses a workspace supplied as the next argument', () => {
    expect(parseWorkspaceLaunch(['electron', '.', '--workspace', workspace]))
      .toEqual({ workspacePath: workspace })
  })

  it('parses the equals form and gives it precedence over the environment fallback', () => {
    expect(parseWorkspaceLaunch(
      [`--workspace=${workspace}`],
      'C:\\Cliq\\fallback',
    )).toEqual({ workspacePath: workspace })
  })

  it('preserves the environment fallback when no CLI argument is present', () => {
    expect(parseWorkspaceLaunch([], workspace))
      .toEqual({ workspacePath: workspace })
  })

  it('rejects missing and relative workspace values', () => {
    expect(() => parseWorkspaceLaunch(['--workspace']))
      .toThrow('--workspace requires an absolute directory path.')
    expect(() => parseWorkspaceLaunch(['--workspace', 'relative/path']))
      .toThrow('The Cliq workspace path must be absolute.')
  })

  it('validates workspace paths forwarded through Electron additional data', () => {
    expect(workspaceFromAdditionalData({ workspacePath: workspace }))
      .toEqual({ workspacePath: workspace })
    expect(workspaceFromAdditionalData({ workspacePath: 42 })).toEqual({})
  })
})
