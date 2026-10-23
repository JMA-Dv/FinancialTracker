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
/** A signed-in User with a Currency and no Accounts, so the landing screen has something to show. */
export const signedIn = {
  'GET /api/user-settings': () => json(200, { email: 'ana@example.com', currency: 'USD' }),
  'GET /api/accounts': () => json(200, []),
}

afterEach(() => {
  mounted.splice(0).forEach((app) => app.unmount())
  vi.unstubAllGlobals()
  document.body.innerHTML = ''
})

type Scope = Pick<DOMWrapper<Element>, 'element' | 'get'>

/** The whole document, for what Element Plus renders outside the app: dialogs and dropdowns. */
export function documentBody(): DOMWrapper<HTMLElement> {
  return new DOMWrapper(document.body)
}

/** Opens the select inside `scope` and clicks the option whose text contains `text`. */
export async function pickOption(scope: Scope, text: string): Promise<void> {
  await scope.get('.el-select__wrapper').trigger('click')
  await flushPromises()
  const options = Array.from(document.body.querySelectorAll<HTMLElement>('.el-select-dropdown__item'))
  const option = options.find((candidate) => candidate.textContent?.includes(text))
  if (option === undefined) {
    throw new Error(`No option contains "${text}"`)
  }
  await new DOMWrapper(option).trigger('click')
  await flushPromises()
}

/** Clicks the button inside `scope` whose text or accessible name is `name`. */
export async function press(scope: Scope, name: string): Promise<void> {
  const buttons = Array.from(scope.element.querySelectorAll<HTMLElement>('button'))
  const button = buttons.find(
    (candidate) => candidate.textContent?.trim() === name || candidate.getAttribute('aria-label') === name,
  )
  if (button === undefined) {
    throw new Error(`No button named "${name}"`)
  }
  await new DOMWrapper(button).trigger('click')
  await flushPromises()
}

/** The form item inside `scope` whose label is `label`, to reach its input or select. */
export function field(scope: Scope, label: string): DOMWrapper<Element> {
  const items = Array.from(scope.element.querySelectorAll('.el-form-item'))
  const item = items.find((candidate) => candidate.querySelector('.el-form-item__label')?.textContent?.trim() === label)
  if (item === undefined) {
    throw new Error(`No field labelled "${label}"`)
  }
  return new DOMWrapper(item)
}

/** What `get` returns for a selector: an element wrapper that is known to exist. */
export type FoundElement = Omit<DOMWrapper<Element>, 'exists'>

/** The open dialog, wherever Element Plus put it. */
export function dialog(): FoundElement {
  return documentBody().get('[role="dialog"]')
}
