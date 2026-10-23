import { flushPromises } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { api } from './api/client'
import { fakeApi, json, signedIn, signedOut, visit } from './testing/harness'

describe('signing in', () => {
  it('shows the sign-in screen to a signed-out visitor and sends them back to the route they asked for', async () => {
    fakeApi(signedOut)

    const { page } = await visit('/accounts')

    const signIn = page.get('a[href^="/api/auth/google/sign-in"]')
    expect(signIn.text()).toContain('Sign in with Google')
    expect(signIn.attributes('href')).toBe('/api/auth/google/sign-in?returnUrl=%2Faccounts')
  })

  it.each(['//evil.example', 'https://evil.example', '/\\evil.example', 'accounts'])(
    'does not send %s as the returnUrl, only local paths',
    async (returnUrl) => {
      fakeApi(signedOut)

      const { page } = await visit(`/sign-in?returnUrl=${encodeURIComponent(returnUrl)}`)

      expect(page.get('a[href^="/api/auth/google/sign-in"]').attributes('href')).toBe(
        '/api/auth/google/sign-in?returnUrl=%2F',
      )
    },
  )
})

describe('routes that do not exist', () => {
  it('asks a signed-out visitor to sign in before saying anything about the page', async () => {
    fakeApi(signedOut)

    const { page } = await visit('/nowhere')

    expect(page.get('a[href^="/api/auth/google/sign-in"]').attributes('href')).toBe(
      '/api/auth/google/sign-in?returnUrl=%2Fnowhere',
    )
  })

  it('tells a signed-in User the page does not exist', async () => {
    fakeApi(signedIn)

    const { page } = await visit('/nowhere')

    expect(page.get('main').text()).toContain('Page not found')
  })
})

describe('the app shell', () => {
  it('lands a signed-in User on the route they asked for, showing their email and the navigation', async () => {
    fakeApi(signedIn)

    const { page } = await visit('/accounts')

    expect(page.get('header').text()).toContain('ana@example.com')
    expect(page.get('main').text()).toContain('Accounts')
    const links = page.findAll('nav a').map((link) => [link.text(), link.attributes('href')])
    expect(links).toEqual([
      ['Dashboard', '/'],
      ['Accounts', '/accounts'],
      ['Transactions', '/transactions'],
      ['Transfers', '/transfers'],
      ['Categories', '/categories'],
      ['Payees', '/payees'],
    ])
    expect(page.text()).not.toContain('Sign in with Google')
  })

  it('signs the User out and shows the sign-in screen', async () => {
    const fetchMock = fakeApi({ ...signedIn, 'POST /api/auth/sign-out': () => json(204) })
    const { page } = await visit('/accounts')

    await page.get('header button').trigger('click')
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/api/auth/sign-out', expect.objectContaining({ method: 'POST' }))
    expect(page.text()).toContain('Sign in with Google')
    expect(page.find('header').exists()).toBe(false)
  })

  it('goes to sign-in without an error when sign-out finds the session already expired', async () => {
    fakeApi({ ...signedIn, 'POST /api/auth/sign-out': () => json(401) })
    const { page } = await visit('/accounts')

    await page.get('header button').trigger('click')
    await flushPromises()

    expect(page.text()).toContain('Sign in with Google')
    expect(page.find('[role="alert"]').exists()).toBe(false)
  })

  it('keeps the User signed in and says so when sign-out fails', async () => {
    fakeApi({ ...signedIn, 'POST /api/auth/sign-out': () => json(500, { title: 'Oops', detail: 'Database unavailable.' }) })
    const { page } = await visit('/accounts')

    await page.get('header button').trigger('click')
    await flushPromises()

    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')
    expect(page.get('header').text()).toContain('ana@example.com')
  })
})

describe('an expired session', () => {
  it('sends the User back to sign-in from where they were when any call answers 401', async () => {
    fakeApi({ ...signedIn, 'GET /api/accounts': () => json(401) })
    const { page } = await visit('/transfers')

    await api.get('/api/accounts').catch(() => undefined)
    await flushPromises()

    expect(page.find('header').exists()).toBe(false)
    expect(page.get('a[href^="/api/auth/google/sign-in"]').attributes('href')).toBe(
      '/api/auth/google/sign-in?returnUrl=%2Ftransfers',
    )
  })

  it('asks for sign-in again on the next visit after the session is gone', async () => {
    fakeApi({ ...signedIn, 'GET /api/accounts': () => json(401) })
    const { page, navigate } = await visit('/transfers')
    await api.get('/api/accounts').catch(() => undefined)
    await flushPromises()

    await navigate('/payees')

    expect(page.get('a[href^="/api/auth/google/sign-in"]').attributes('href')).toBe(
      '/api/auth/google/sign-in?returnUrl=%2Fpayees',
    )
  })
})

describe('when the API cannot be reached', () => {
  it('says so instead of showing the sign-in screen, and recovers on retry', async () => {
    let healthy = false
    fakeApi({
      'GET /api/user-settings': () =>
        healthy ? json(200, { email: 'ana@example.com', currency: null }) : new Response('Bad gateway', { status: 502 }),
    })
    const { page } = await visit('/settings')

    expect(page.get('[role="alert"]').text()).toContain('The server could not be reached or failed (502).')
    expect(page.text()).not.toContain('Sign in with Google')

    healthy = true
    await page.get('button').trigger('click')
    await flushPromises()

    expect(page.get('header').text()).toContain('ana@example.com')
    expect(page.find('[role="alert"]').exists()).toBe(false)
  })
})
