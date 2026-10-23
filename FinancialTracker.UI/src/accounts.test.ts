import { flushPromises } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { checking, oldSavings, visa, wallet } from './testing/fixtures'
import { dialog as openDialogBox, documentBody, fakeApi, field, type FoundElement, json, pickOption, press, signedIn, visit } from './testing/harness'

const confirmationBox = () => documentBody().get('.el-message-box')

describe('Accounts screen', () => {
  it('splits what the User owns from what they owe, each balance in its own Currency', async () => {
    fakeApi({ ...signedIn, 'GET /api/accounts': () => json(200, [checking, wallet, visa]) })

    const { page } = await visit('/accounts')

    const owned = page.get('section[aria-labelledby="own-heading"]')
    expect(owned.get('h2').text()).toBe('What you own')
    expect(owned.text()).toContain('Everyday')
    expect(owned.text()).toContain('$1,250.50')
    expect(owned.text()).toContain('Wallet')
    expect(owned.text()).toContain('MX$3,000.00')
    expect(owned.text()).not.toContain('Visa')

    const owed = page.get('section[aria-labelledby="owe-heading"]')
    expect(owed.get('h2').text()).toBe('What you owe')
    expect(owed.text()).toContain('Visa')
    expect(owed.text()).toContain('-$300.00')
  })

  it('hides Closed Accounts until the toggle is on, then marks them', async () => {
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, [checking]),
      'GET /api/accounts?includeClosed=true': () => json(200, [checking, oldSavings]),
    })
    const { page } = await visit('/accounts')
    expect(page.text()).not.toContain('Old savings')

    await page.get('[role="switch"]').trigger('click')
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/api/accounts?includeClosed=true', expect.anything())
    const closed = page.findAll('li').find((row) => row.text().includes('Old savings'))
    expect(closed?.text()).toContain('Closed')

    await page.get('[role="switch"]').trigger('click')
    await flushPromises()
    expect(page.text()).not.toContain('Old savings')
  })

  it('invites the User to open their first Account when there are none', async () => {
    fakeApi({ ...signedIn, 'GET /api/accounts': () => json(200, []) })

    const { page } = await visit('/accounts')

    expect(page.get('main').text()).toContain('You have no Accounts yet.')
    expect(page.find('section[aria-labelledby="own-heading"]').exists()).toBe(false)
  })

  it('shows the API failure with a way to try again', async () => {
    let healthy = false
    fakeApi({
      ...signedIn,
      'GET /api/accounts': () =>
        healthy ? json(200, [checking]) : json(500, { title: 'Oops', detail: 'Database unavailable.' }),
    })
    const { page } = await visit('/accounts')
    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.text()).toContain('Everyday')
  })
})

describe('opening an Account', () => {
  async function openDialog(page: Awaited<ReturnType<typeof visit>>['page']): Promise<FoundElement> {
    await press(page, 'New account')
    return openDialogBox()
  }

  it('sends a Credit Card\'s amount owed as a negative Opening Balance', async () => {
    const fetchMock = fakeApi({
      ...signedIn,
      'POST /api/accounts': () => json(201, { ...visa }),
    })
    const { page } = await visit('/accounts')
    const dialog = await openDialog(page)

    await field(dialog, 'Name').get('input').setValue('Visa')
    await pickOption(field(dialog, 'Kind'), 'Credit card')
    await field(dialog, 'Amount owed').get('input').setValue('300')
    await field(dialog, 'As of').get('input').setValue('2026-10-01')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          name: 'Visa',
          kind: 'CreditCard',
          currency: 'USD',
          openingBalance: { amount: -300, date: '2026-10-01' },
        }),
      }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
  })

  it('sends the Opening Balance of any other kind as entered, in the Currency picked', async () => {
    const fetchMock = fakeApi({ ...signedIn, 'POST /api/accounts': () => json(201, { ...wallet }) })
    const { page } = await visit('/accounts')
    const dialog = await openDialog(page)

    await field(dialog, 'Name').get('input').setValue('Wallet')
    await pickOption(field(dialog, 'Kind'), 'Cash')
    expect(field(dialog, 'Currency').get('.el-select__placeholder').text()).toBe('USD – US Dollar')
    await pickOption(field(dialog, 'Currency'), 'Mexican Peso')
    await field(dialog, 'Opening balance').get('input').setValue('3000.5')
    await field(dialog, 'As of').get('input').setValue('2026-10-01')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts',
      expect.objectContaining({
        body: JSON.stringify({
          name: 'Wallet',
          kind: 'Cash',
          currency: 'MXN',
          openingBalance: { amount: 3000.5, date: '2026-10-01' },
        }),
      }),
    )
  })

  it('starts the Account at zero when no Opening Balance is given', async () => {
    const fetchMock = fakeApi({ ...signedIn, 'POST /api/accounts': () => json(201, { ...checking }) })
    const { page } = await visit('/accounts')
    const dialog = await openDialog(page)

    await field(dialog, 'Name').get('input').setValue('Everyday')
    await pickOption(field(dialog, 'Kind'), 'Checking')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts',
      expect.objectContaining({ body: JSON.stringify({ name: 'Everyday', kind: 'Checking', currency: 'USD' }) }),
    )
  })

  it('shows the new Account in the list after saving', async () => {
    let accounts = [checking]
    fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, accounts),
      'POST /api/accounts': () => {
        accounts = [checking, wallet]
        return json(201, { ...wallet })
      },
    })
    const { page } = await visit('/accounts')
    const dialog = await openDialog(page)
    await field(dialog, 'Name').get('input').setValue('Wallet')
    await pickOption(field(dialog, 'Kind'), 'Cash')

    await press(dialog, 'Save')

    expect(page.get('section[aria-labelledby="own-heading"]').text()).toContain('Wallet')
  })

  it('stops an amount with more decimals than the Currency has', async () => {
    fakeApi(signedIn)
    const { page } = await visit('/accounts')
    const dialog = await openDialog(page)
    await field(dialog, 'Name').get('input').setValue('Everyday')
    await pickOption(field(dialog, 'Kind'), 'Checking')

    await field(dialog, 'Opening balance').get('input').setValue('10.123')
    await flushPromises()

    expect(field(dialog, 'Opening balance').text()).toContain('USD has 2 decimal places.')
    expect(dialog.get('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('shows what the API says when it refuses', async () => {
    fakeApi({
      ...signedIn,
      'POST /api/accounts': () => json(409, { title: 'Account exists', detail: 'You already have an Account named Visa.' }),
    })
    const { page } = await visit('/accounts')
    const dialog = await openDialog(page)
    await field(dialog, 'Name').get('input').setValue('Visa')
    await pickOption(field(dialog, 'Kind'), 'Credit card')

    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('You already have an Account named Visa.')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
  })
})

describe('renaming an Account', () => {
  it('starts from the current name and shows the new one after saving', async () => {
    let name = 'Everyday'
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, [{ ...checking, name }]),
      'PUT /api/accounts/checking/name': () => {
        name = 'Main'
        return json(200, { ...checking, name })
      },
    })
    const { page } = await visit('/accounts')

    await press(page, 'Rename Everyday')
    const dialog = openDialogBox()
    const input = field(dialog, 'Name').get('input')
    expect((input.element as HTMLInputElement).value).toBe('Everyday')
    await input.setValue('Main')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/checking/name',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ name: 'Main' }) }),
    )
    expect(page.get('section[aria-labelledby="own-heading"]').text()).toContain('Main')
  })
})

describe('changing the Currency of an Account', () => {
  async function openChangeDialog(page: Awaited<ReturnType<typeof visit>>['page']): Promise<FoundElement> {
    await press(page, 'Change currency of Everyday')
    return openDialogBox()
  }

  it('sends the new Currency and shows the balance in it', async () => {
    let currency = 'USD'
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, [{ ...checking, currency }]),
      'PUT /api/accounts/checking/currency': () => {
        currency = 'EUR'
        return json(200, { ...checking, currency })
      },
    })
    const { page } = await visit('/accounts')
    const dialog = await openChangeDialog(page)

    expect(field(dialog, 'Currency').get('.el-select__placeholder').text()).toBe('USD – US Dollar')
    await pickOption(field(dialog, 'Currency'), 'Euro')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/checking/currency',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ currency: 'EUR' }) }),
    )
    expect(page.get('section[aria-labelledby="own-heading"]').text()).toContain('€1,250.50')
  })

  it('explains why when the Account already has Transactions or Transfers', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, [checking]),
      'PUT /api/accounts/checking/currency': () =>
        json(409, {
          title: 'Currency locked',
          detail: 'Everyday already has Transactions, so its Currency can no longer change.',
        }),
    })
    const { page } = await visit('/accounts')
    const dialog = await openChangeDialog(page)
    await pickOption(field(dialog, 'Currency'), 'Euro')

    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('already has Transactions, so its Currency can no longer change.')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
  })
})

describe('closing and reopening an Account', () => {
  const emptyChecking = { ...checking, balance: 0 }

  it('asks first, and does nothing when the User backs out', async () => {
    const fetchMock = fakeApi({ ...signedIn, 'GET /api/accounts': () => json(200, [emptyChecking]) })
    const { page } = await visit('/accounts')

    await press(page, 'Close Everyday')
    expect(confirmationBox().text()).toContain('Everyday')
    await press(confirmationBox(), 'Cancel')

    expect(fetchMock).not.toHaveBeenCalledWith('/api/accounts/checking/close', expect.anything())
    expect(page.text()).toContain('Everyday')
  })

  it('closes an Account once confirmed, and it leaves the list', async () => {
    let accounts = [emptyChecking]
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, accounts),
      'POST /api/accounts/checking/close': () => {
        accounts = []
        return json(200, { ...emptyChecking, isClosed: true })
      },
    })
    const { page } = await visit('/accounts')

    await press(page, 'Close Everyday')
    await press(confirmationBox(), 'Close account')

    expect(fetchMock).toHaveBeenCalledWith('/api/accounts/checking/close', expect.objectContaining({ method: 'POST' }))
    expect(page.text()).not.toContain('Everyday')
  })

  it('shows the API\'s explanation and keeps the Account open when it still holds money', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, [checking]),
      'POST /api/accounts/checking/close': () =>
        json(409, { title: 'Balance not zero', detail: 'Everyday still holds 1,250.50 USD. Move or spend it first.' }),
    })
    const { page } = await visit('/accounts')

    await press(page, 'Close Everyday')
    await press(confirmationBox(), 'Close account')

    expect(page.get('[role="alert"]').text()).toContain('Everyday still holds 1,250.50 USD. Move or spend it first.')
    expect(page.get('section[aria-labelledby="own-heading"]').text()).toContain('Everyday')
    expect(page.text()).not.toContain('Closed')
  })

  it('reopens a Closed Account once confirmed', async () => {
    let closed = true
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/accounts': () => json(200, []),
      'GET /api/accounts?includeClosed=true': () => json(200, [{ ...oldSavings, isClosed: closed }]),
      'POST /api/accounts/old-savings/reopen': () => {
        closed = false
        return json(200, { ...oldSavings, isClosed: false })
      },
    })
    const { page } = await visit('/accounts')
    await page.get('[role="switch"]').trigger('click')
    await flushPromises()

    await press(page, 'Reopen Old savings')
    await press(confirmationBox(), 'Reopen account')

    expect(fetchMock).toHaveBeenCalledWith('/api/accounts/old-savings/reopen', expect.objectContaining({ method: 'POST' }))
    const row = page.findAll('li').find((item) => item.text().includes('Old savings'))
    expect(row?.text()).not.toContain('Closed')
  })
})
