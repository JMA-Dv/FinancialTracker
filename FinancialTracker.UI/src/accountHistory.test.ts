import { flushPromises, type DOMWrapper } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { checking, checkingHistory, oldSavings, visa } from './testing/fixtures'
import { dialog as openDialogBox, documentBody, fakeApi, field, json, press, signedIn, visit } from './testing/harness'

const accountApi = {
  ...signedIn,
  'GET /api/accounts': () => json(200, [checking]),
  'GET /api/accounts/checking': () => json(200, checking),
  'GET /api/accounts/checking/history': () => json(200, checkingHistory),
}

/** The `index`th row of the history table, newest first. */
function historyRow(page: Awaited<ReturnType<typeof visit>>['page'], index: number): DOMWrapper<Element> {
  const row = page.findAll('tbody tr')[index]
  if (row === undefined) {
    throw new Error(`No history row ${index}`)
  }
  return row
}

async function openAccount(): Promise<Awaited<ReturnType<typeof visit>>['page']> {
  const { page } = await visit('/accounts/checking')
  await flushPromises()
  return page
}

describe('Account history', () => {
  it('opens from the list and shows every entry newest first with its signed effect', async () => {
    fakeApi(accountApi)
    const { page } = await visit('/accounts')

    await page.get('a[href="/accounts/checking"]').trigger('click')
    // The route is a lazy chunk, so the page arrives a moment after the click.
    await vi.waitFor(() => expect(page.get('h1').text()).toContain('Everyday'))
    await flushPromises()

    const dates = page.findAll('tbody tr').map((row) => row.get('td').text())
    expect(dates).toEqual(['2026-10-05', '2026-10-01', '2026-09-20', '2026-09-01'])
    expect(historyRow(page, 0).text()).toContain('Transaction')
    expect(historyRow(page, 0).text()).toContain('-$400.00')
    expect(historyRow(page, 2).text()).toContain('Transfer')
    expect(historyRow(page, 2).text()).toContain('+$50.00')
  })

  it('labels the earliest Balance Adjustment the Opening Balance and shows what each one states', async () => {
    fakeApi(accountApi)
    const page = await openAccount()

    expect(historyRow(page, 1).text()).toContain('Balance Adjustment')
    expect(historyRow(page, 1).text()).toContain('States $90.00')
    expect(historyRow(page, 1).text()).toContain('+$20.00')
    expect(historyRow(page, 3).text()).toContain('Opening Balance')
    expect(historyRow(page, 3).text()).toContain('States $70.00')
    expect(historyRow(page, 0).text()).not.toContain('States')
    expect(page.get('main').text()).toContain('never income or spending')
  })

  it('says so when the Account has no history yet', async () => {
    fakeApi({ ...accountApi, 'GET /api/accounts/checking/history': () => json(200, []) })
    const page = await openAccount()

    expect(page.get('main').text()).toContain('Nothing has happened on this Account yet.')
    expect(page.find('table').exists()).toBe(false)
  })

  it('shows the API failure with a way to try again', async () => {
    let healthy = false
    fakeApi({
      ...accountApi,
      'GET /api/accounts/checking/history': () =>
        healthy ? json(200, checkingHistory) : json(500, { title: 'Oops', detail: 'Database unavailable.' }),
    })
    const page = await openAccount()
    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.findAll('tbody tr')).toHaveLength(4)
  })

  it('tells the User when the Account is not theirs to open', async () => {
    fakeApi({
      ...accountApi,
      'GET /api/accounts/checking': () => json(404, { title: 'Account not found', detail: 'No such Account.' }),
    })
    const page = await openAccount()

    expect(page.get('[role="alert"]').text()).toContain('No such Account.')
    expect(page.find('table').exists()).toBe(false)
  })
})

describe('Opening Balance', () => {
  it('explains that the earliest Balance Adjustment is the Opening Balance, before there is one', async () => {
    fakeApi({ ...accountApi, 'GET /api/accounts/checking/history': () => json(200, []) })
    const page = await openAccount()

    expect(page.get('main').text()).toContain('The earliest Balance Adjustment is the Account\'s Opening Balance.')
  })
})

describe('Balance on a date', () => {
  it('asks the API for the date and shows the answer in the Account\'s Currency', async () => {
    const fetchMock = fakeApi({
      ...accountApi,
      'GET /api/accounts/checking/balance?date=2026-09-25': () => json(200, { date: '2026-09-25', balance: 120 }),
    })
    const page = await openAccount()

    await field(page, 'Date').get('input').setValue('2026-09-25')
    await press(page, 'Show balance')

    expect(fetchMock).toHaveBeenCalledWith('/api/accounts/checking/balance?date=2026-09-25', expect.anything())
    expect(page.get('[data-testid="balance-on-date"]').text()).toBe('Everyday held $120.00 at the end of 2026-09-25.')
  })

  it('does not offer days after today', async () => {
    fakeApi(accountApi)
    const page = await openAccount()

    const today = new Date().toISOString().slice(0, 10)
    expect(field(page, 'Date').get('input').attributes('max')).toBe(today)
  })

  it('will not ask for a day after today', async () => {
    const fetchMock = fakeApi(accountApi)
    const page = await openAccount()

    await field(page, 'Date').get('input').setValue('2999-01-01')

    expect(field(page, 'Date').text()).toContain('The date can\'t be after today.')
    expect(page.get('button[type="submit"]').attributes('disabled')).toBeDefined()
    expect(fetchMock).not.toHaveBeenCalledWith(expect.stringContaining('/balance?'), expect.anything())
  })

  it('shows what the API says when it cannot answer', async () => {
    fakeApi({
      ...accountApi,
      'GET /api/accounts/checking/balance?date=2026-09-25': () =>
        json(500, { title: 'Oops', detail: 'Database unavailable.' }),
    })
    const page = await openAccount()

    await field(page, 'Date').get('input').setValue('2026-09-25')
    await press(page, 'Show balance')

    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')
    expect(page.find('[data-testid="balance-on-date"]').exists()).toBe(false)
  })
})

describe('stating the real balance', () => {
  it('sends the date and balance, then shows the new balance and the entry it made', async () => {
    let account = checking
    let history = checkingHistory
    const fetchMock = fakeApi({
      ...accountApi,
      'GET /api/accounts/checking': () => json(200, account),
      'GET /api/accounts/checking/history': () => json(200, history),
      'POST /api/accounts/checking/balance-adjustments': () => {
        account = { ...checking, balance: 1300 }
        history = [{ kind: 'BalanceAdjustment', id: 'new', date: '2026-10-10', amount: 49.5, statedBalance: 1300 }, ...checkingHistory]
        return json(201, { id: 'new', date: '2026-10-10', balance: 1300, difference: 49.5 })
      },
    })
    const page = await openAccount()
    expect(page.get('[data-testid="balance"]').text()).toBe('$1,250.50')

    await press(page, 'Add Balance Adjustment')
    const dialog = openDialogBox()
    await field(dialog, 'Balance').get('input').setValue('1300')
    await field(dialog, 'As of').get('input').setValue('2026-10-10')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/checking/balance-adjustments',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ date: '2026-10-10', balance: 1300 }) }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
    expect(page.get('[data-testid="balance"]').text()).toBe('$1,300.00')
    expect(historyRow(page, 0).text()).toContain('States $1,300.00')
  })

  it('shows what the API says when it refuses, such as a date that already has one', async () => {
    fakeApi({
      ...accountApi,
      'POST /api/accounts/checking/balance-adjustments': () =>
        json(409, { title: 'Date taken', detail: 'Everyday already has a Balance Adjustment on 2026-10-01.' }),
    })
    const page = await openAccount()
    await press(page, 'Add Balance Adjustment')
    const dialog = openDialogBox()
    await field(dialog, 'Balance').get('input').setValue('10')
    await field(dialog, 'As of').get('input').setValue('2026-10-01')

    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('already has a Balance Adjustment on 2026-10-01.')
  })

  it('stops a balance with more decimals than the Currency has, or one that is not a number', async () => {
    fakeApi(accountApi)
    const page = await openAccount()
    await press(page, 'Add Balance Adjustment')
    const dialog = openDialogBox()

    await field(dialog, 'Balance').get('input').setValue('10.123')
    await flushPromises()
    expect(field(dialog, 'Balance').text()).toContain('USD has 2 decimal places.')
    expect(dialog.get('button[type="submit"]').attributes('disabled')).toBeDefined()

    await field(dialog, 'Balance').get('input').setValue('1,5')
    await flushPromises()
    expect(field(dialog, 'Balance').text()).toContain('Enter a number such as 1250.50.')
  })

  it('asks a Credit Card for the amount owed and sends it negative', async () => {
    const fetchMock = fakeApi({
      ...accountApi,
      'GET /api/accounts/visa': () => json(200, visa),
      'GET /api/accounts/visa/history': () => json(200, []),
      'POST /api/accounts/visa/balance-adjustments': () => json(201, { id: 'x', date: '2026-10-10', balance: -250, difference: 50 }),
    })
    const { page } = await visit('/accounts/visa')
    await flushPromises()

    await press(page, 'Add Balance Adjustment')
    const dialog = openDialogBox()
    await field(dialog, 'Amount owed').get('input').setValue('250')
    await field(dialog, 'As of').get('input').setValue('2026-10-10')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/visa/balance-adjustments',
      expect.objectContaining({ body: JSON.stringify({ date: '2026-10-10', balance: -250 }) }),
    )
  })
})

describe('editing and deleting a Balance Adjustment', () => {
  const confirmationBox = () => documentBody().get('.el-message-box')

  it('starts from what it states and sends every field again', async () => {
    const fetchMock = fakeApi({
      ...accountApi,
      'PUT /api/accounts/checking/balance-adjustments/recount': () =>
        json(200, { id: 'recount', date: '2026-10-02', balance: 95, difference: 25 }),
    })
    const page = await openAccount()

    await press(page, 'Edit Balance Adjustment of 2026-10-01')
    const dialog = openDialogBox()
    expect(dialog.text()).toContain('Edit Balance Adjustment')
    expect((field(dialog, 'Balance').get('input').element as HTMLInputElement).value).toBe('90')
    expect((field(dialog, 'As of').get('input').element as HTMLInputElement).value).toBe('2026-10-01')
    await field(dialog, 'Balance').get('input').setValue('95')
    await field(dialog, 'As of').get('input').setValue('2026-10-02')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/checking/balance-adjustments/recount',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ date: '2026-10-02', balance: 95 }) }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
  })

  it('edits the Opening Balance like any other', async () => {
    fakeApi(accountApi)
    const page = await openAccount()

    await press(page, 'Edit Opening Balance of 2026-09-01')

    expect(openDialogBox().text()).toContain('Edit Opening Balance')
  })

  it('offers no edit for a Transaction or a Transfer', async () => {
    fakeApi(accountApi)
    const page = await openAccount()

    expect(historyRow(page, 0).find('button').exists()).toBe(false)
    expect(historyRow(page, 2).find('button').exists()).toBe(false)
  })

  it('does nothing when the User backs out of deleting', async () => {
    const fetchMock = fakeApi(accountApi)
    const page = await openAccount()

    await press(page, 'Delete Balance Adjustment of 2026-10-01')
    await press(confirmationBox(), 'Cancel')

    expect(fetchMock).not.toHaveBeenCalledWith(expect.stringContaining('/balance-adjustments/recount'), expect.anything())
    expect(page.findAll('tbody tr')).toHaveLength(4)
  })

  it('deletes once confirmed, and the entry leaves the history', async () => {
    let history = checkingHistory
    const fetchMock = fakeApi({
      ...accountApi,
      'GET /api/accounts/checking/history': () => json(200, history),
      'DELETE /api/accounts/checking/balance-adjustments/recount': () => {
        history = checkingHistory.filter((entry) => entry.id !== 'recount')
        return json(204)
      },
    })
    const page = await openAccount()

    await press(page, 'Delete Balance Adjustment of 2026-10-01')
    await press(confirmationBox(), 'Delete')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/checking/balance-adjustments/recount',
      expect.objectContaining({ method: 'DELETE' }),
    )
    expect(page.findAll('tbody tr')).toHaveLength(3)
  })

  it('warns that deleting the Opening Balance starts the Account from zero', async () => {
    fakeApi(accountApi)
    const page = await openAccount()

    await press(page, 'Delete Opening Balance of 2026-09-01')

    expect(confirmationBox().text()).toContain('Opening Balance')
    expect(confirmationBox().text()).toContain('zero')
  })

  it('shows what the API says when it refuses a change, and keeps the entry', async () => {
    fakeApi({
      ...accountApi,
      'DELETE /api/accounts/checking/balance-adjustments/recount': () =>
        json(409, { title: 'Account closed', detail: 'Everyday is closed. Reopen it first.' }),
    })
    const page = await openAccount()

    await press(page, 'Delete Balance Adjustment of 2026-10-01')
    await press(confirmationBox(), 'Delete')

    expect(page.get('[role="alert"]').text()).toContain('Everyday is closed. Reopen it first.')
    expect(page.findAll('tbody tr')).toHaveLength(4)
  })
})

describe('a Closed Account', () => {
  const closedApi = {
    ...accountApi,
    'GET /api/accounts/checking': () => json(200, { ...oldSavings, id: 'checking' }),
  }

  it('offers no way to change Balance Adjustments, and says to reopen it first', async () => {
    fakeApi(closedApi)
    const page = await openAccount()

    expect(page.get('main').text()).toContain('Reopen it first to add, edit or delete Balance Adjustments.')
    const buttons = ['Add Balance Adjustment', 'Edit Balance Adjustment of 2026-10-01', 'Delete Balance Adjustment of 2026-10-01']
    for (const name of buttons) {
      const button = Array.from(page.element.querySelectorAll('button')).find(
        (candidate) => candidate.textContent?.trim() === name || candidate.getAttribute('aria-label') === name,
      )
      expect(button?.hasAttribute('disabled'), name).toBe(true)
    }
  })

  it('still shows its history and answers a balance on a date', async () => {
    fakeApi({
      ...closedApi,
      'GET /api/accounts/checking/balance?date=2026-09-25': () => json(200, { date: '2026-09-25', balance: 120 }),
    })
    const page = await openAccount()
    await field(page, 'Date').get('input').setValue('2026-09-25')
    await press(page, 'Show balance')

    expect(page.findAll('tbody tr')).toHaveLength(4)
    expect(page.get('[data-testid="balance-on-date"]').text()).toContain('$120.00')
  })
})

describe('after the balance changes', () => {
  it('drops an earlier answer to "balance on a date", which may no longer hold', async () => {
    fakeApi({
      ...accountApi,
      'GET /api/accounts/checking/balance?date=2026-09-25': () => json(200, { date: '2026-09-25', balance: 120 }),
      'POST /api/accounts/checking/balance-adjustments': () => json(201, { id: 'n', date: '2026-09-24', balance: 5, difference: 1 }),
    })
    const page = await openAccount()
    await field(page, 'Date').get('input').setValue('2026-09-25')
    await press(page, 'Show balance')
    expect(page.find('[data-testid="balance-on-date"]').exists()).toBe(true)

    await press(page, 'Add Balance Adjustment')
    const dialog = openDialogBox()
    await field(dialog, 'Balance').get('input').setValue('5')
    await press(dialog, 'Save')

    expect(page.find('[data-testid="balance-on-date"]').exists()).toBe(false)
  })
})

describe('a Credit Card\'s Balance Adjustments', () => {
  const cardHistory = [{ kind: 'BalanceAdjustment', id: 'statement', date: '2026-10-01', amount: -300, statedBalance: -300 }]

  it('are edited as the amount owed, starting from what it states', async () => {
    const fetchMock = fakeApi({
      ...accountApi,
      'GET /api/accounts/visa': () => json(200, visa),
      'GET /api/accounts/visa/history': () => json(200, cardHistory),
      'PUT /api/accounts/visa/balance-adjustments/statement': () =>
        json(200, { id: 'statement', date: '2026-10-01', balance: -250, difference: 50 }),
    })
    const { page } = await visit('/accounts/visa')
    await flushPromises()

    await press(page, 'Edit Opening Balance of 2026-10-01')
    const dialog = openDialogBox()
    expect((field(dialog, 'Amount owed').get('input').element as HTMLInputElement).value).toBe('300')
    await field(dialog, 'Amount owed').get('input').setValue('250')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/accounts/visa/balance-adjustments/statement',
      expect.objectContaining({ body: JSON.stringify({ date: '2026-10-01', balance: -250 }) }),
    )
  })
})
