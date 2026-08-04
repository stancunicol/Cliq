import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import { randomBytes } from 'node:crypto'
import { existsSync } from 'node:fs'
import { homedir } from 'node:os'
import path from 'node:path'
import { createInterface } from 'node:readline'

interface ReadyMessage {
  address: string
}

export class BackendSupervisor {
  private child?: ChildProcessWithoutNullStreams
  private address?: string
  private readonly token = randomBytes(32).toString('base64url')

  constructor(private readonly repositoryRoot: string) {}

  async start(): Promise<void> {
    if (this.child) return
    const executable = resolveDotnet()
    const project = path.join(
      this.repositoryRoot,
      'backend',
      'src',
      'Cliq.Api',
      'Cliq.Api.csproj',
    )
    const child = spawn(
      executable,
      ['run', '--project', project, '--no-launch-profile', '--urls', 'http://127.0.0.1:0'],
      {
        cwd: path.dirname(project),
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Development',
          CLIQ_API_TOKEN: this.token,
          DOTNET_CLI_TELEMETRY_OPTOUT: '1',
        },
        stdio: ['pipe', 'pipe', 'pipe'],
      },
    )
    this.child = child

    await new Promise<void>((resolve, reject) => {
      const timeout = setTimeout(
        () => reject(new Error('The .NET backend did not become ready within 45 seconds.')),
        45_000,
      )
      let errors = ''
      const output = createInterface({ input: child.stdout })
      output.on('line', (line) => {
        if (!line.startsWith('CLIQ_READY ')) return
        try {
          const ready = JSON.parse(line.slice('CLIQ_READY '.length)) as ReadyMessage
          this.address = ready.address.replace('://localhost:', '://127.0.0.1:')
          console.log(`[backend] listening on ${this.address}`)
          clearTimeout(timeout)
          resolve()
        } catch (error) {
          clearTimeout(timeout)
          reject(error)
        }
      })
      child.stderr.on('data', (chunk: Buffer) => {
        errors = (errors + chunk.toString()).slice(-8_000)
      })
      child.once('error', (error) => {
        clearTimeout(timeout)
        reject(error)
      })
      child.once('exit', (code) => {
        if (!this.address) {
          clearTimeout(timeout)
          reject(new Error(`The .NET backend exited with code ${code}.\n${errors}`))
        }
      })
    })

    await this.request('/health/ready', { authenticated: false })
  }

  async request<T>(
    route: string,
    options: RequestInit & { authenticated?: boolean } = {},
  ): Promise<T> {
    if (!this.address) throw new Error('The .NET backend is not ready.')
    const { authenticated = true, ...requestOptions } = options
    const headers = new Headers(requestOptions.headers)
    headers.set('Accept', 'application/json')
    if (requestOptions.body) headers.set('Content-Type', 'application/json')
    if (authenticated) headers.set('Authorization', `Bearer ${this.token}`)
    const response = await fetch(`${this.address}${route}`, { ...requestOptions, headers })
    if (!response.ok) {
      const problem = (await response.json().catch(() => undefined)) as
        | { detail?: string; title?: string }
        | undefined
      throw new Error(problem?.detail || problem?.title || `Backend request failed (${response.status}).`)
    }

    if (response.status === 204) return undefined as T
    // Some 2xx responses (e.g. Results.Accepted() with no value, from the run-cancel endpoint)
    // have an empty body - response.json() throws on empty input, so check first.
    const text = await response.text()
    return (text ? JSON.parse(text) : undefined) as T
  }

  async requestDataUrl(route: string): Promise<string | null> {
    if (!this.address) throw new Error('The .NET backend is not ready.')
    const response = await fetch(`${this.address}${route}`, {
      headers: { Authorization: `Bearer ${this.token}` },
    })
    if (response.status === 404) return null
    if (!response.ok) throw new Error(`Backend request failed (${response.status}).`)
    const contentType = response.headers.get('content-type') ?? 'application/octet-stream'
    const buffer = Buffer.from(await response.arrayBuffer())
    return `data:${contentType};base64,${buffer.toString('base64')}`
  }

  async streamRunEvents(
    runId: string,
    onEvent: (event: string, payload: unknown) => void,
  ): Promise<void> {
    if (!this.address) throw new Error('The .NET backend is not ready.')
    const response = await fetch(`${this.address}/api/v1/runs/${runId}/events`, {
      headers: { Authorization: `Bearer ${this.token}` },
    })
    if (!response.ok || !response.body) {
      throw new Error(`Unable to stream run '${runId}'.`)
    }

    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''
    while (true) {
      const { value, done } = await reader.read()
      buffer += decoder.decode(value, { stream: !done })
      const messages = buffer.split('\n\n')
      buffer = messages.pop() ?? ''
      for (const message of messages) {
        const event = message.match(/^event: (.+)$/m)?.[1]
        const data = message.match(/^data: (.+)$/m)?.[1]
        if (event && data) onEvent(event, JSON.parse(data))
      }
      if (done) break
    }
  }

  stop(): void {
    this.child?.kill()
    this.child = undefined
    this.address = undefined
  }
}

function resolveDotnet(): string {
  if (process.env.CLIQ_DOTNET_PATH) return process.env.CLIQ_DOTNET_PATH
  const local = path.join(homedir(), '.local', 'share', 'dotnet', process.platform === 'win32' ? 'dotnet.exe' : 'dotnet')
  return existsSync(local) ? local : 'dotnet'
}
