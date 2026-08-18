import { useLayoutEffect } from 'react'
import type { WorkspaceSnapshot } from '../../types'

/** Keeps the embedded browser's OS-level window bounds aligned with the `.browser-pane` element's
 * on-screen rect - resize/layout can move it, and a workspace switch can change whether it's
 * shown at all, so this re-measures on both. `snapshot` is only a re-run trigger (its identity
 * changing is what "the layout might have changed" means here), not read directly. */
export function useBrowserBoundsSync(snapshot: WorkspaceSnapshot | undefined) {
  useLayoutEffect(() => {
    const sync = () => {
      const pane = document.querySelector('.browser-pane')?.getBoundingClientRect()
      if (pane) {
        void window.cliq?.browser.setBounds({
          x: Math.round(pane.x),
          y: Math.round(pane.y + 102),
          width: Math.round(pane.width),
          height: Math.max(0, Math.round(pane.height - 102)),
        })
      }
    }
    const observer = typeof ResizeObserver === 'undefined' ? undefined : new ResizeObserver(sync)
    const pane = document.querySelector('.browser-pane')
    if (pane) observer?.observe(pane)
    window.addEventListener('resize', sync)
    const frame = requestAnimationFrame(sync)
    return () => {
      observer?.disconnect()
      window.removeEventListener('resize', sync)
      cancelAnimationFrame(frame)
    }
  }, [snapshot])
}
