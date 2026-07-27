import type { ReactElement } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { vi } from 'vitest'
import { useAppStore } from '@/app/store'

export interface ApiCall {
  method: string
  url: string
  body?: unknown
}

/**
 * Stubs global fetch with a `${METHOD} ${url}` route table and records every call.
 * Unstubbed routes return a problem+json 404 so missing stubs fail loudly.
 */
export function stubApi(routes: Record<string, unknown>): { calls: ApiCall[] } {
  const calls: ApiCall[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const call: ApiCall = {
        method: init?.method ?? 'GET',
        url: String(input),
        body: init?.body ? JSON.parse(String(init.body)) : undefined,
      }
      calls.push(call)
      const key = `${call.method} ${call.url}`
      if (!(key in routes)) {
        return {
          ok: false,
          status: 404,
          json: async () => ({ errorCode: 'test.no_stub', detail: `No stub for ${key}` }),
        } as unknown as Response
      }
      return { ok: true, status: 200, json: async () => routes[key] } as unknown as Response
    }),
  )
  return { calls }
}

/** Fresh QueryClient per test so server-state caches never leak between cases. */
export function renderWithQuery(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>)
}

/** Zustand module state and jsdom localStorage survive between tests — reset both. */
export function resetAppState(): void {
  localStorage.clear()
  useAppStore.setState({ learnerId: null, screen: 'today' })
}
