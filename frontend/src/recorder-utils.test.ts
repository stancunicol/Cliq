import { beforeEach, describe, expect, it } from 'vitest'
import {
  elementName,
  findClickTarget,
  isFillTarget,
  locator,
} from '../electron/recorder-utils.cjs'

describe('recorder interaction targeting', () => {
  beforeEach(() => {
    document.body.replaceChildren()
  })

  it('records the interactive button when a nested child is clicked without blocking the click', () => {
    document.body.innerHTML = '<button id="save"><span>Save changes</span></button>'
    const button = document.querySelector('button')!
    const child = document.querySelector('span')!
    let handled = 0
    let recorded: Element | undefined
    button.addEventListener('click', (event) => {
      recorded = findClickTarget(event.target)
      handled += 1
    })

    child.click()

    expect(handled).toBe(1)
    expect(recorded).toBe(button)
    expect(elementName(recorded!)).toBe('Save changes')
  })

  it('resolves a checkbox label to the checkbox and does not treat it as a fill', () => {
    document.body.innerHTML = '<label><input type="checkbox" name="terms"> Accept terms</label>'
    const labelText = document.querySelector('label')!
    const checkbox = document.querySelector('input')!

    expect(findClickTarget(labelText)).toBe(checkbox)
    expect(isFillTarget(checkbox)).toBe(false)
  })

  it('builds a valid test-id locator when the attribute contains quotes', () => {
    document.body.innerHTML = '<button data-testid="save&quot;draft">Save</button>'
    const button = document.querySelector('button')!

    expect(locator(button)).toEqual({ strategy: 'testId', value: 'save"draft' })
  })

  it('only treats editable inputs and textareas as fill targets', () => {
    document.body.innerHTML = `
      <input type="text">
      <input type="radio">
      <input type="file">
      <textarea></textarea>
    `
    const [text, radio, file] = [...document.querySelectorAll('input')]
    const textarea = document.querySelector('textarea')!

    expect(isFillTarget(text)).toBe(true)
    expect(isFillTarget(textarea)).toBe(true)
    expect(isFillTarget(radio)).toBe(false)
    expect(isFillTarget(file)).toBe(false)
  })
})
