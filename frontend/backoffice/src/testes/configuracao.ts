import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// O jsdom não tem ResizeObserver (todos os browsers têm); o Switch do Radix usa-o dentro de formulários.
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  document.cookie.split('; ').forEach((c) => {
    document.cookie = `${c.split('=')[0]}=; expires=Thu, 01 Jan 1970 00:00:00 GMT`
  })
})
