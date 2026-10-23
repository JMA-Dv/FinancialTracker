import { DOMWrapper, flushPromises } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { fakeApi, json, pickOption, signedIn, visit } from './testing/harness'

const withoutCurrency = { 'GET /api/user-settings': () => json(200, { email: 'ana@example.com', currency: null }) }

const pickCurrency = pickOption

async function save(page: DOMWrapper<HTMLElement>): Promise<void> {
  await page.get('button[type="submit"]').trigger('click')
  await flushPromises()
}

describe('Currency setup', () => {
  it('asks a User without a Currency to choose one before any ledger screen', async () => {
    fakeApi(withoutCurrency)

    const { page } = await visit('/accounts')

    expect(page.get('main h1').text()).toBe('Choose your Currency')
    expect(page.text()).not.toContain('Coming soon')
  })

  it('does not ask a User who already has a Currency', async () => {
    fakeApi(signedIn)

    const { page } = await visit('/accounts')

    expect(page.get('main h1').text()).toBe('Accounts')
  })

  it('saves the chosen Currency and continues to the screen the User wanted', async () => {
    let currency: string | null = null
    const fetchMock = fakeApi({
      'GET /api/user-settings': () => json(200, { email: 'ana@example.com', currency }),
      'PUT /api/user-settings/currency': () => {
        currency = 'USD'
        return json(200, { email: 'ana@example.com', currency })
      },
    })
    const { page, navigate } = await visit('/accounts')

    await pickCurrency(page, 'US Dollar')
    await save(page)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/user-settings/currency',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ currency: 'USD' }) }),
    )
    expect(page.get('main h1').text()).toBe('Accounts')

    await navigate('/transfers')
    expect(page.get('main h1').text()).toBe('Transfers')
  })

  it('finds a Currency by its name as well as its code', async () => {
    fakeApi(withoutCurrency)
    const { page } = await visit('/accounts')

    await page.get('.el-select__wrapper').trigger('click')
    await page.get('.el-select input').setValue('mexican')
    await flushPromises()

    const shown = Array.from(document.body.querySelectorAll<HTMLElement>('.el-select-dropdown__item'))
      .filter((option) => option.style.display !== 'none')
      .map((option) => option.textContent?.trim())
    expect(shown).toContain('MXN – Mexican Peso')
    expect(shown).not.toContain('USD – US Dollar')
  })

  it.each([
    ['a rule', json(400, { title: 'Unknown Currency', detail: '"XTS" is not a Currency we can use.' }), '"XTS" is not a Currency we can use.'],
    ['a field', json(400, { title: 'Invalid', errors: { Currency: ['Currency must be 3 letters.'] } }), 'Currency must be 3 letters.'],
  ])('shows what the API says about %s and keeps the choice', async (_kind, refusal, shown) => {
    fakeApi({ ...withoutCurrency, 'PUT /api/user-settings/currency': () => refusal })
    const { page } = await visit('/accounts')
    await pickCurrency(page, 'US Dollar')

    await save(page)

    expect(page.get('[role="alert"]').text()).toContain(shown)
    expect(page.get('main h1').text()).toBe('Choose your Currency')
    expect(page.get('.el-select__placeholder').text()).toBe('USD – US Dollar')
  })
})

describe('Settings', () => {
  it('shows the email and Currency, says what the Currency is for, and lets the User change it', async () => {
    let currency = 'USD'
    const fetchMock = fakeApi({
      'GET /api/user-settings': () => json(200, { email: 'ana@example.com', currency }),
      'PUT /api/user-settings/currency': () => {
        currency = 'MXN'
        return json(200, { email: 'ana@example.com', currency })
      },
    })
    const { page } = await visit('/settings')

    expect(page.get('main').text()).toContain('ana@example.com')
    expect(page.get('.el-select__placeholder').text()).toBe('USD – US Dollar')
    expect(page.get('main').text()).toContain('only the default for new Accounts')
    expect(page.get('main').text()).toContain('nothing that already exists is converted')

    await pickCurrency(page, 'Mexican Peso')
    await save(page)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/user-settings/currency',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ currency: 'MXN' }) }),
    )
    expect(page.get('[role="status"]').text()).toContain('Currency saved')
    expect(page.get('.el-select__placeholder').text()).toBe('MXN – Mexican Peso')
  })

  it('is reachable from the shell, even before a Currency is chosen', async () => {
    fakeApi(withoutCurrency)
    const { page } = await visit('/settings')

    expect(page.get('main h1').text()).toBe('Settings')
    expect(page.get('header a[href="/settings"]').text()).toBe('Settings')
  })

  it('stops saying the Currency was saved once a later change is refused', async () => {
    let refuse = false
    fakeApi({
      'GET /api/user-settings': () => json(200, { email: 'ana@example.com', currency: 'USD' }),
      'PUT /api/user-settings/currency': () =>
        refuse ? json(400, { title: 'Unknown Currency', detail: 'Not a Currency.' }) : json(200, { email: 'ana@example.com', currency: 'MXN' }),
    })
    const { page } = await visit('/settings')
    await pickCurrency(page, 'Mexican Peso')
    await save(page)
    expect(page.get('[role="status"]').text()).toBe('Currency saved.')

    refuse = true
    await pickCurrency(page, 'Euro')
    await save(page)

    expect(page.get('[role="status"]').text()).toBe('')
    expect(page.get('[role="alert"]').text()).toContain('Not a Currency.')
  })

  it('still asks for a Currency when the session only loads on retry', async () => {
    let healthy = false
    fakeApi({
      'GET /api/user-settings': () =>
        healthy ? json(200, { email: 'ana@example.com', currency: null }) : new Response('Bad gateway', { status: 502 }),
    })
    const { page } = await visit('/accounts')

    healthy = true
    await page.get('button').trigger('click')
    await flushPromises()

    expect(page.get('main h1').text()).toBe('Choose your Currency')
  })

  it('shows the current Currency when a User who has one opens the setup screen', async () => {
    fakeApi(signedIn)

    const { page } = await visit('/setup/currency')

    expect(page.get('.el-select__placeholder').text()).toBe('USD – US Dollar')
  })
})
