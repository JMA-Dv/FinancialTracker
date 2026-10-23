import { DOMWrapper, flushPromises } from '@vue/test-utils'
import type { App } from 'vue'
import { createMemoryHistory } from 'vue-router'
import { afterEach, vi } from 'vitest'
import { createFinanceApp } from '@/app'

export interface FakeApi {
  [route: string]: () => Response
}

export function json(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
  })
}

export function fakeApi(routes: FakeApi): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn((path: string, init?: RequestInit) => {
    const handler = routes[`${init?.method ?? 'GET'} ${path}`]
    return Promise.resolve(handler === undefined ? json(404, { title: 'Not found', detail: path }) : handler())
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

const mounted: App[] = []

export async function visit(path: string): Promise<{ page: DOMWrapper<HTMLElement>; navigate: (to: string) => Promise<void> }> {
  const { app, router } = createFinanceApp(createMemoryHistory())
  await router.push(path)
  await router.isReady()
  const host = document.createElement('div')
  document.body.append(host)
  app.mount(host)
  mounted.push(app)
  await flushPromises()
  return {
    page: new DOMWrapper(host),
    navigate: async (to) => {
      await router.push(to)
      await flushPromises()
    },
  }
}

export const signedOut = { 'GET /api/user-settings': () => json(401) }
export const signedIn = { 'GET /api/user-settings': () => json(200, { email: 'ana@example.com', currency: 'USD' }) }

afterEach(() => {
  mounted.splice(0).forEach((app) => app.unmount())
  vi.unstubAllGlobals()
  document.body.innerHTML = ''
})
