export type RecorderAction = 'click' | 'fill' | 'select'

export function findClickTarget(target: EventTarget | null): Element | undefined {
  if (!(target instanceof Element)) return undefined

  const label = target.closest('label')
  if (label instanceof HTMLLabelElement
      && label.control instanceof HTMLInputElement
      && ['checkbox', 'radio'].includes(label.control.type)) {
    return label.control
  }

  return target.closest(
    'button, a[href], input[type="button"], input[type="submit"], input[type="reset"], '
    + 'input[type="checkbox"], input[type="radio"], [role="button"]',
  ) ?? undefined
}

export function isFillTarget(target: EventTarget | null): target is HTMLInputElement | HTMLTextAreaElement {
  if (target instanceof HTMLTextAreaElement) return true
  if (!(target instanceof HTMLInputElement)) return false
  return !['button', 'submit', 'reset', 'checkbox', 'radio', 'file', 'hidden'].includes(target.type)
}

export function elementName(target: Element): string {
  const labelledBy = target.getAttribute('aria-labelledby')
    ?.split(/\s+/)
    .map((id) => document.getElementById(id)?.textContent)
    .filter(Boolean)
    .join(' ')
  const explicitLabel = target.id
    ? document.querySelector(`label[for="${cssAttributeValue(target.id)}"]`)?.textContent
    : undefined
  const enclosingLabel = target.closest('label')?.textContent

  return (
    target.getAttribute('data-testid')
    || target.getAttribute('aria-label')
    || labelledBy
    || explicitLabel
    || enclosingLabel
    || target.textContent
    || target.getAttribute('name')
    || target.getAttribute('placeholder')
    || target.id
    || target.tagName
  )
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 80)
}

export function locator(target: Element) {
  const testId = target.getAttribute('data-testid')
  if (testId && unique(`[data-testid="${cssAttributeValue(testId)}"]`, target)) {
    return { strategy: 'testId' as const, value: testId }
  }
  if (target.id && unique(`#${CSS.escape(target.id)}`, target)) {
    return { strategy: 'id' as const, value: target.id }
  }
  const name = target.getAttribute('name')
  if (name && unique(`${target.tagName.toLowerCase()}[name="${cssAttributeValue(name)}"]`, target)) {
    return { strategy: 'name' as const, value: name }
  }
  return { strategy: 'css' as const, value: cssPath(target) }
}

export function slug(value: string) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 48)
}

function cssAttributeValue(value: string) {
  return value.replace(/\\/g, '\\\\').replace(/"/g, '\\"')
}

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
