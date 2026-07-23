import { ipcRenderer } from 'electron'

let enabled = false
ipcRenderer.on('recorder:enabled', (_event, value: boolean) => {
  enabled = value
})

function unique(selector: string, element: Element) {
  try {
    const matches = document.querySelectorAll(selector)
    return matches.length === 1 && matches[0] === element
  } catch {
    return false
  }
}

function cssPath(element: Element): string {
  if (element.id && unique(`#${CSS.escape(element.id)}`, element)) {
    return `#${CSS.escape(element.id)}`
  }
  const parent = element.parentElement
  if (!parent) return element.tagName.toLowerCase()
  const peers = [...parent.children].filter((child) => child.tagName === element.tagName)
  const suffix = peers.length > 1 ? `:nth-of-type(${peers.indexOf(element) + 1})` : ''
  return `${cssPath(parent)} > ${element.tagName.toLowerCase()}${suffix}`
}

function locator(target: Element) {
  const testId = target.getAttribute('data-testid')
  if (testId && unique(`[data-testid="${CSS.escape(testId)}"]`, target)) {
    return { strategy: 'testId', value: testId }
  }
  if (target.id && unique(`#${CSS.escape(target.id)}`, target)) {
    return { strategy: 'id', value: target.id }
  }
  const name = target.getAttribute('name')
  if (name && unique(`${target.tagName.toLowerCase()}[name="${CSS.escape(name)}"]`, target)) {
    return { strategy: 'name', value: name }
  }
  return { strategy: 'css', value: cssPath(target) }
}

function elementName(target: Element) {
  return (
    target.getAttribute('data-testid') ||
    target.getAttribute('aria-label') ||
    target.getAttribute('name') ||
    target.id ||
    target.textContent ||
    target.tagName
  )
    .trim()
    .slice(0, 80)
}

function slug(value: string) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 48)
}

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
    if (!enabled || !event.isTrusted || !(event.target instanceof Element)) return
    const target = event.target
    const tag = target.tagName.toLowerCase()
    if (tag === 'button' || tag === 'a' || (target instanceof HTMLInputElement && ['button', 'submit', 'checkbox', 'radio'].includes(target.type))) {
      send('click', target)
    }
  },
  true,
)

window.addEventListener(
  'change',
  (event) => {
    if (!enabled || !event.isTrusted) return
    if (event.target instanceof HTMLSelectElement) {
      send('select', event.target, event.target.value)
    } else if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
      send('fill', event.target, event.target.value)
    }
  },
  true,
)
