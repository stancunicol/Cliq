import { ipcRenderer } from 'electron'
import {
  elementName,
  findClickTarget,
  isFillTarget,
  locator,
  slug,
} from './recorder-utils.cjs'

let enabled = false
ipcRenderer.on('recorder:enabled', (_event, value: boolean) => {
  enabled = value
})

function send(
  action: 'click' | 'fill' | 'select',
  target: Element,
  rawValue?: string,
) {
  const name = elementName(target)
  const isSecret = target instanceof HTMLInputElement && target.type === 'password'
  const id = `${slug(name) || target.tagName.toLowerCase()}-${crypto.randomUUID().slice(0, 8)}`
  ipcRenderer.send('recorder:interaction', {
    id: crypto.randomUUID(),
    action,
    label: `${action === 'fill' ? 'Fill' : action === 'select' ? 'Select' : 'Click'} ${name}`,
    pageUrl: window.location.href,
    element: {
      id,
      name,
      tag: target.tagName.toLowerCase(),
      action,
      locator: { ...locator(target), displayName: name },
    },
    value:
      rawValue === undefined
        ? undefined
        : {
            kind: isSecret ? 'secret' : 'literal',
            value: isSecret ? slug(name).replace(/-/g, '_') || 'password' : rawValue,
          },
    secretValue: isSecret ? rawValue : undefined,
  })
}

window.addEventListener(
  'click',
  (event) => {
    if (!enabled || !event.isTrusted) return
    const target = findClickTarget(event.target)
    if (target) send('click', target)
  },
  false,
)

window.addEventListener(
  'change',
  (event) => {
    if (!enabled || !event.isTrusted) return
    if (event.target instanceof HTMLSelectElement) {
      send('select', event.target, event.target.value)
    } else if (isFillTarget(event.target)) {
      send('fill', event.target, event.target.value)
    }
  },
  false,
)

ipcRenderer.send('recorder:ready')
