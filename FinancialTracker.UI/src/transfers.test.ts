import { describe, expect, it } from 'vitest'
import {
  type FixtureTransfer,
  cardPayment,
  checking,
  fromOldSavings,
  oldSavings,
  toWallet,
  visa,
  wallet,
} from './testing/fixtures'
import {
  type FoundElement,
  confirmationBox,
  documentBody,
  fakeApi,
  field,
  json,
  optionLabels,
  pickOption,
  press,
  signedIn,
  visit,
} from './testing/harness'

const everything = {
  ...signedIn,
  'GET /api/accounts?includeClosed=true': () => json(200, [checking, wallet, visa, oldSavings]),
}

async function openNewTransfer(routes: Parameters<typeof fakeApi>[0] = {}): Promise<{ form: FoundElement; page: Awaited<ReturnType<typeof visit>>['page'] }> {
  fakeApi({ ...everything, 'GET /api/transfers': () => json(200, []), ...routes })
  const { page } = await visit('/transfers')
  await press(page, 'Record transfer')
  return { page, form: documentBody().get('[role="dialog"]') }
}

async function fillSameCurrency(form: FoundElement): Promise<void> {
  await pickOption(field(form, 'From'), 'Everyday')
  await pickOption(field(form, 'To'), 'Visa')
  await field(form, 'Date').get('input').setValue('2026-10-05')
  await field(form, 'Amount sent').get('input').setValue('100')
}

describe('Transfers list', () => {
  it('shows each Transfer with both Accounts, the date, and what was sent and received in each Currency', async () => {
    fakeApi({ ...everything, 'GET /api/transfers': () => json(200, [cardPayment, toWallet]) })

    const { page } = await visit('/transfers')

    const rows = page.findAll('tbody tr')
    expect(rows).toHaveLength(2)
    expect(rows[0]?.text()).toContain('2026-10-05')
    expect(rows[0]?.text()).toContain('Everyday')
    expect(rows[0]?.text()).toContain('Visa')
    expect(rows[0]?.text()).toContain('$100.00')
    expect(rows[0]?.text()).toContain('October bill')
    expect(rows[1]?.text()).toContain('$50.00')
    expect(rows[1]?.text()).toContain('MX$900.00')
  })

  it('says so when there are no Transfers', async () => {
    fakeApi({ ...everything, 'GET /api/transfers': () => json(200, []) })

    const { page } = await visit('/transfers')

    expect(page.text()).toContain('No Transfers yet.')
  })

  it('shows the API failure with a way to try again', async () => {
    let healthy = false
    fakeApi({
      ...everything,
      'GET /api/transfers': () =>
        healthy ? json(200, [cardPayment]) : json(500, { title: 'Oops', detail: 'Database unavailable.' }),
    })
    const { page } = await visit('/transfers')

    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.text()).toContain('October bill')
  })

  it('cannot record a Transfer until there are two open Accounts', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/accounts?includeClosed=true': () => json(200, [checking, oldSavings]),
      'GET /api/transfers': () => json(200, []),
    })

    const { page } = await visit('/transfers')

    expect(page.text()).toContain('Open two Accounts first')
    const button = page.findAll('button').find((candidate) => candidate.text() === 'Record transfer')
    expect(button?.attributes('disabled')).toBeDefined()
  })
})

describe('recording a Transfer', () => {
  it('between Accounts in one Currency sends no received amount and hides the field', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transfers': () => json(200, []),
      'POST /api/transfers': () => json(201, cardPayment),
    })
    const { page } = await visit('/transfers')
    await press(page, 'Record transfer')
    const form = documentBody().get('[role="dialog"]')

    await fillSameCurrency(form)
    expect(form.text()).not.toContain('Amount received')
    await field(form, 'Note').get('input').setValue('October bill')
    await press(form, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transfers',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          fromAccountId: 'checking',
          toAccountId: 'visa',
          date: '2026-10-05',
          amount: 100,
          note: 'October bill',
        }),
      }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
  })

  it('between Currencies requires and sends receivedAmount, and says the rate is implied', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transfers': () => json(200, []),
      'POST /api/transfers': () => json(201, toWallet),
    })
    const { page } = await visit('/transfers')
    await press(page, 'Record transfer')
    const form = documentBody().get('[role="dialog"]')

    await pickOption(field(form, 'From'), 'Everyday')
    await pickOption(field(form, 'To'), 'Wallet')
    await field(form, 'Date').get('input').setValue('2026-10-03')
    await field(form, 'Amount sent').get('input').setValue('50')

    expect(form.text()).toContain('rate is implied by the two amounts and never stored')
    expect(form.get('button[type="submit"]').attributes('disabled')).toBeDefined()

    await field(form, 'Amount received in MXN').get('input').setValue('900')
    await press(form, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transfers',
      expect.objectContaining({
        body: JSON.stringify({
          fromAccountId: 'checking',
          toAccountId: 'wallet',
          date: '2026-10-03',
          amount: 50,
          receivedAmount: 900,
        }),
      }),
    )
  })

  it('does not send a received amount typed before the Accounts shared a Currency', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transfers': () => json(200, []),
      'POST /api/transfers': () => json(201, cardPayment),
    })
    const { page } = await visit('/transfers')
    await press(page, 'Record transfer')
    const form = documentBody().get('[role="dialog"]')
    await pickOption(field(form, 'From'), 'Everyday')
    await pickOption(field(form, 'To'), 'Wallet')
    await field(form, 'Amount received in MXN').get('input').setValue('900')

    await pickOption(field(form, 'To'), 'Visa')
    await field(form, 'Date').get('input').setValue('2026-10-05')
    await field(form, 'Amount sent').get('input').setValue('100')
    await press(form, 'Save')

    const post = fetchMock.mock.calls.find(([path, init]) => path === '/api/transfers' && init?.method === 'POST')
    expect(JSON.parse(String(post?.[1]?.body))).not.toHaveProperty('receivedAmount')
  })

  it('blocks choosing the same Account twice and says why', async () => {
    const { form } = await openNewTransfer()

    await pickOption(field(form, 'From'), 'Everyday')
    await pickOption(field(form, 'To'), 'Everyday')
    await field(form, 'Date').get('input').setValue('2026-10-05')
    await field(form, 'Amount sent').get('input').setValue('10')

    expect(form.text()).toContain('Choose two different Accounts.')
    expect(form.get('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('offers open Accounts only', async () => {
    const { form } = await openNewTransfer()

    expect(await optionLabels(field(form, 'From'))).toEqual(['Everyday', 'Wallet', 'Visa'])
    expect(await optionLabels(field(form, 'To'))).toEqual(['Everyday', 'Wallet', 'Visa'])
  })

  it('says paying off a Credit Card is a Transfer and not spending', async () => {
    const { form } = await openNewTransfer()

    expect(form.text()).toContain('Paying off a Credit Card is a Transfer to it')
    expect(form.text()).toContain('not spending')
  })

  it('does not accept a date after today, or an amount of zero or with too many decimals', async () => {
    const { form } = await openNewTransfer()
    await pickOption(field(form, 'From'), 'Everyday')
    await pickOption(field(form, 'To'), 'Visa')

    await field(form, 'Date').get('input').setValue('2999-01-01')
    await field(form, 'Amount sent').get('input').setValue('0')
    expect(form.text()).toContain('The date can\'t be after today.')
    expect(form.text()).toContain('The amount must be above zero.')

    await field(form, 'Date').get('input').setValue('2026-10-05')
    await field(form, 'Amount sent').get('input').setValue('1.234')
    expect(form.text()).toContain('USD has 2 decimal places.')
    expect(form.get('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('shows what the API refuses and keeps the form open', async () => {
    const { form } = await openNewTransfer({
      'POST /api/transfers': () =>
        json(409, { title: 'Account closed', detail: 'Visa is closed. Reopen it first.' }),
    })
    await fillSameCurrency(form)

    await press(form, 'Save')

    expect(form.get('[role="alert"]').text()).toContain('Visa is closed. Reopen it first.')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
  })

  it('shows the field message when the API refuses a field', async () => {
    const { form } = await openNewTransfer({
      'POST /api/transfers': () =>
        json(400, { title: 'Invalid', errors: { Note: ['The field Note must be a string with a maximum length of 500.'] } }),
    })
    await fillSameCurrency(form)

    await press(form, 'Save')

    expect(form.get('[role="alert"]').text()).toContain('maximum length of 500')
  })
})

describe('editing a Transfer', () => {
  it('starts from the Transfer and replaces every field', async () => {
    let list: FixtureTransfer[] = [toWallet]
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transfers': () => json(200, list),
      'PUT /api/transfers/to-wallet': () => {
        list = [{ ...toWallet, receivedAmount: 950 }]
        return json(200, list[0])
      },
    })
    const { page } = await visit('/transfers')

    await press(page, 'Edit transfer from Everyday to Wallet on 2026-10-03')
    const form = documentBody().get('[role="dialog"]')
    expect(form.text()).toContain('Edit transfer')
    expect(field(form, 'From').text()).toContain('Everyday')
    expect(field(form, 'To').text()).toContain('Wallet')
    expect((field(form, 'Amount sent').get('input').element as HTMLInputElement).value).toBe('50')
    expect((field(form, 'Amount received in MXN').get('input').element as HTMLInputElement).value).toBe('900')
    await field(form, 'Amount received in MXN').get('input').setValue('950')
    await press(form, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transfers/to-wallet',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({
          fromAccountId: 'checking',
          toAccountId: 'wallet',
          date: '2026-10-03',
          amount: 50,
          receivedAmount: 950,
        }),
      }),
    )
    expect(page.text()).toContain('MX$950.00')
  })

  it('makes a Transfer touching a Closed Account read-only and says why', async () => {
    fakeApi({ ...everything, 'GET /api/transfers': () => json(200, [fromOldSavings, cardPayment]) })

    const { page } = await visit('/transfers')

    expect(page.get('#transfers-locked-reason').text()).toContain('Closed Account')
    const rows = page.findAll('tbody tr')
    const locked = rows[0]?.get('button[aria-label="Edit transfer from Old savings to Everyday on 2026-08-01"]')
    expect(locked?.attributes('disabled')).toBeDefined()
    expect(locked?.attributes('aria-describedby')).toBe('transfers-locked-reason')
    expect(
      rows[0]?.get('button[aria-label="Delete transfer from Old savings to Everyday on 2026-08-01"]').attributes('disabled'),
    ).toBeDefined()
    const open = rows[1]?.get('button[aria-label="Edit transfer from Everyday to Visa on 2026-10-05"]')
    expect(open?.attributes('disabled')).toBeUndefined()
    expect(open?.attributes('aria-describedby')).toBeUndefined()
  })
})

describe('deleting a Transfer', () => {
  it('asks first, then deletes and refreshes the list', async () => {
    let list: FixtureTransfer[] = [cardPayment, toWallet]
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transfers': () => json(200, list),
      'DELETE /api/transfers/card-payment': () => {
        list = [toWallet]
        return json(204)
      },
    })
    const { page } = await visit('/transfers')

    await press(page, 'Delete transfer from Everyday to Visa on 2026-10-05')
    expect(fetchMock).not.toHaveBeenCalledWith('/api/transfers/card-payment', expect.anything())
    await press(confirmationBox(), 'Delete')

    expect(fetchMock).toHaveBeenCalledWith('/api/transfers/card-payment', expect.objectContaining({ method: 'DELETE' }))
    expect(page.findAll('tbody tr')).toHaveLength(1)
  })

  it('keeps the Transfer when the User cancels', async () => {
    const fetchMock = fakeApi({ ...everything, 'GET /api/transfers': () => json(200, [cardPayment]) })
    const { page } = await visit('/transfers')

    await press(page, 'Delete transfer from Everyday to Visa on 2026-10-05')
    await press(confirmationBox(), 'Cancel')

    expect(fetchMock).not.toHaveBeenCalledWith('/api/transfers/card-payment', expect.anything())
    expect(page.findAll('tbody tr')).toHaveLength(1)
  })

  it('shows the API\'s message when it refuses', async () => {
    fakeApi({
      ...everything,
      'GET /api/transfers': () => json(200, [cardPayment]),
      'DELETE /api/transfers/card-payment': () => json(404, { title: 'Not found', detail: 'That Transfer is gone.' }),
    })
    const { page } = await visit('/transfers')

    await press(page, 'Delete transfer from Everyday to Visa on 2026-10-05')
    await press(confirmationBox(), 'Delete')

    expect(page.get('[role="alert"]').text()).toContain('That Transfer is gone.')
  })
})
