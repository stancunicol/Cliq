export interface WorkspaceLaunch {
  workspacePath?: string
}

export function parseWorkspaceLaunch(
  argv: readonly string[],
  environmentWorkspacePath?: string,
): WorkspaceLaunch {
  let workspacePath: string | undefined

  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index]
    if (argument === '--workspace') {
      const value = argv[index + 1]
      if (!value || value.startsWith('--')) {
        throw new Error('--workspace requires an absolute directory path.')
      }
      workspacePath = value
      index += 1
      continue
    }

    if (argument.startsWith('--workspace=')) {
      workspacePath = argument.slice('--workspace='.length)
    }
  }

  workspacePath ??= environmentWorkspacePath
  if (!workspacePath) return {}

  const trimmed = workspacePath.trim()
  if (!trimmed || !isAbsolutePath(trimmed)) {
    throw new Error('The Cliq workspace path must be absolute.')
  }

  return { workspacePath: trimmed }
}

export function workspaceFromAdditionalData(
  additionalData: unknown,
): WorkspaceLaunch {
  if (!additionalData || typeof additionalData !== 'object') return {}
  const candidate = (additionalData as Record<string, unknown>).workspacePath
  return typeof candidate === 'string'
    ? parseWorkspaceLaunch(['--workspace', candidate])
    : {}
}

function isAbsolutePath(candidate: string) {
  return candidate.startsWith('/')
    || /^[A-Za-z]:[\\/]/u.test(candidate)
    || /^\\\\[^\\]+\\[^\\]+/u.test(candidate)
}
