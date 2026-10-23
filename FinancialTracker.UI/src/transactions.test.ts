import { DOMWrapper, flushPromises } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import {
  type FixtureTransaction,
  checking,
  corner,
  groceries,
  landlord,
  oldPurchase,
  oldSavings,
  pay,
  refund,
  salary,
  shopping,
  travel,
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
  'GET /api/categories?includeArchived=true': () => json(200, [salary, groceries, travel]),
  'GET /api/payees': () => json(200, [corner, landlord]),
}

const savedResponse = { ...shopping, id: 'new' }

/** Types into the Payee field and clicks the suggestion with `text`. */
async function pickPayee(dialog: FoundElement, typed: string, text: string): Promise<void> {
  await field(dialog, 'Payee').get('input').setValue(typed)
  await new Promise((resolve) => setTimeout(resolve, 20))
  await flushPromises()
  const suggestions = Array.from(document.body.querySelectorAll<HTMLElement>('.el-autocomplete-suggestion li'))
  const suggestion = suggestions.find((candidate) => candidate.textContent?.includes(text))
  if (suggestion === undefined) {
    throw new Error(`No suggestion contains "${text}"`)
  }
  await new DOMWrapper(suggestion).trigger('click')
  await flushPromises()
}

async function chooseDirection(dialog: FoundElement, direction: 'In' | 'Out'): Promise<void> {
  await field(dialog, 'Direction').get(`input[type="radio"][value="${direction}"]`).setValue(true)
  await flushPromises()
}

async function openNewTransaction(): Promise<{ page: DOMWrapper<HTMLElement>; form: FoundElement }> {
  const { page } = await visit('/transactions')
  await press(page, 'Record transaction')
  return { page, form: documentBody().get('[role="dialog"]') }
}

describe('Transactions list', () => {
  it('shows each Transaction with its Account, Category, Payee and signed amount, newest first', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, [shopping, refund, pay]) })

    const { page } = await visit('/transactions')

    const rows = page.findAll('tbody tr')
    expect(rows).toHaveLength(3)
    expect(rows[0]?.text()).toContain('2026-10-05')
    expect(rows[0]?.text()).toContain('Corner Market')
    expect(rows[0]?.text()).toContain('Weekly shop')
    expect(rows[0]?.text()).toContain('Groceries')
    expect(rows[0]?.text()).toContain('Everyday')
    expect(rows[0]?.text()).toContain('-$40.50')
    expect(rows[2]?.text()).toContain('+$2,000.00')
  })

  it('shows an amount in its Account\'s Currency and marks a Refund', async () => {
    fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, [{ ...shopping, accountId: 'wallet' }, refund]),
    })

    const { page } = await visit('/transactions')

    const rows = page.findAll('tbody tr')
    expect(rows[0]?.text()).toContain('-MX$40.50')
    expect(rows[0]?.text()).not.toContain('Refund')
    expect(rows[1]?.text()).toContain('Refund')
  })

  it('says so when there are no Transactions', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, []) })

    const { page } = await visit('/transactions')

    expect(page.text()).toContain('No Transactions yet.')
  })

  it('shows the API failure with a way to try again', async () => {
    let healthy = false
    fakeApi({
      ...everything,
      'GET /api/transactions': () =>
        healthy ? json(200, [pay]) : json(500, { title: 'Oops', detail: 'Database unavailable.' }),
    })
    const { page } = await visit('/transactions')

    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.text()).toContain('+$2,000.00')
  })
})

describe('filtering Transactions', () => {
  it('reads the filters from the URL, asks the API for them and shows them', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions?accountId=checking&categoryId=groceries&payeeId=corner&from=2026-10-01&to=2026-10-31': () =>
        json(200, [shopping]),
    })

    const { page } = await visit(
      '/transactions?to=2026-10-31&payeeId=corner&from=2026-10-01&categoryId=groceries&accountId=checking',
    )

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transactions?accountId=checking&categoryId=groceries&payeeId=corner&from=2026-10-01&to=2026-10-31',
      expect.anything(),
    )
    expect(field(page, 'Account').text()).toContain('Everyday')
    expect(field(page, 'Category').text()).toContain('Groceries')
    expect(field(page, 'Payee').text()).toContain('Corner Market')
    expect((field(page, 'From').get('input').element as HTMLInputElement).value).toBe('2026-10-01')
    expect((field(page, 'To').get('input').element as HTMLInputElement).value).toBe('2026-10-31')
    expect(page.findAll('tbody tr')).toHaveLength(1)
  })

  it('puts a changed filter in the URL and asks the API again', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, [shopping, pay]),
      'GET /api/transactions?categoryId=salary': () => json(200, [pay]),
    })
    const { page, location } = await visit('/transactions')

    await pickOption(field(page, 'Category'), 'Salary')

    expect(location()).toBe('/transactions?categoryId=salary')
    expect(fetchMock).toHaveBeenCalledWith('/api/transactions?categoryId=salary', expect.anything())
    expect(page.findAll('tbody tr')).toHaveLength(1)
  })

  it('follows the URL when it changes, and clears a filter', async () => {
    fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, [shopping, pay]),
      'GET /api/transactions?accountId=wallet': () => json(200, []),
    })
    const { page, navigate, location } = await visit('/transactions')

    await navigate('/transactions?accountId=wallet')
    expect(field(page, 'Account').text()).toContain('Wallet')
    expect(page.text()).toContain('No Transactions match these filters.')

    await field(page, 'Account').get('.el-select').trigger('mouseenter')
    await field(page, 'Account').get('.el-select__clear').trigger('click')
    await flushPromises()

    expect(location()).toBe('/transactions')
    expect(page.findAll('tbody tr')).toHaveLength(2)
  })

  it('can filter by a Closed Account and an Archived Category', async () => {
    fakeApi({
      ...everything,
      'GET /api/transactions?accountId=old-savings&categoryId=travel': () => json(200, []),
    })

    const { page } = await visit('/transactions?accountId=old-savings&categoryId=travel')

    expect(field(page, 'Account').text()).toContain('Old savings')
    expect(field(page, 'Category').text()).toContain('Travel')
  })
})

describe('recording a Transaction', () => {
  it('sends a Payee name nobody has yet as payeeName', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, []),
      'POST /api/transactions': () => json(201, savedResponse),
    })
    const { form } = await openNewTransaction()

    await pickOption(field(form, 'Account'), 'Everyday')
    await field(form, 'Date').get('input').setValue('2026-10-05')
    await field(form, 'Amount').get('input').setValue('40.50')
    await pickOption(field(form, 'Category'), 'Groceries')
    await field(form, 'Payee').get('input').setValue('Brand New Bakery')
    await field(form, 'Note').get('input').setValue('Bread')
    await press(form, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transactions',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          accountId: 'checking',
          date: '2026-10-05',
          direction: 'Out',
          amount: 40.5,
          categoryId: 'groceries',
          payeeName: 'Brand New Bakery',
          note: 'Bread',
        }),
      }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
  })

  it('leaves out the Payee and the note when they are blank, and sends In when chosen', async () => {
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, []),
      'POST /api/transactions': () => json(201, savedResponse),
    })
    const { form } = await openNewTransaction()

    await pickOption(field(form, 'Account'), 'Everyday')
    await field(form, 'Date').get('input').setValue('2026-10-01')
    await chooseDirection(form, 'In')
    await field(form, 'Amount').get('input').setValue('2000')
    await pickOption(field(form, 'Category'), 'Salary')
    await press(form, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transactions',
      expect.objectContaining({
        body: JSON.stringify({
          accountId: 'checking',
          date: '2026-10-01',
          direction: 'In',
          amount: 2000,
          categoryId: 'salary',
        }),
      }),
    )
  })

  it('refreshes the list and the Payees after saving', async () => {
    let saved = false
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, saved ? [pay] : []),
      'POST /api/transactions': () => {
        saved = true
        return json(201, savedResponse)
      },
    })
    const { page, form } = await openNewTransaction()

    await pickOption(field(form, 'Account'), 'Everyday')
    await field(form, 'Date').get('input').setValue('2026-10-01')
    await field(form, 'Amount').get('input').setValue('2000')
    await pickOption(field(form, 'Category'), 'Salary')
    await press(form, 'Save')

    expect(page.findAll('tbody tr')).toHaveLength(1)
    expect(fetchMock.mock.calls.filter(([path]) => path === '/api/payees')).toHaveLength(2)
  })

  it('offers only open Accounts and no Archived Category', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, []) })
    const { form } = await openNewTransaction()

    expect(await optionLabels(field(form, 'Account'))).toEqual(['Everyday', 'Wallet', 'Visa'])

    const categories = await optionLabels(field(form, 'Category'))
    expect(categories).toContain('Groceries › Market')
    expect(categories).not.toContain('Travel')
    expect(categories).not.toContain('Travel › Flights')
  })

  it('does not accept a date after today, or an amount of zero or with too many decimals', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, []) })
    const { form } = await openNewTransaction()
    await pickOption(field(form, 'Account'), 'Everyday')
    await pickOption(field(form, 'Category'), 'Salary')

    await field(form, 'Date').get('input').setValue('2999-01-01')
    await field(form, 'Amount').get('input').setValue('0')
    expect(form.text()).toContain('The date can\'t be after today.')
    expect(form.text()).toContain('The amount must be above zero.')
    expect(form.get('button[type="submit"]').attributes('disabled')).toBeDefined()

    await field(form, 'Date').get('input').setValue('2026-10-01')
    await field(form, 'Amount').get('input').setValue('1.234')
    expect(form.text()).toContain('USD has 2 decimal places.')
    expect(form.get('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('shows what the API refuses, field by field, and keeps the form open', async () => {
    fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, []),
      'POST /api/transactions': () =>
        json(400, { title: 'Invalid', errors: { PayeeName: ['The field PayeeName must be a string with a maximum length of 100.'] } }),
    })
    const { form } = await openNewTransaction()
    await pickOption(field(form, 'Account'), 'Everyday')
    await field(form, 'Date').get('input').setValue('2026-10-01')
    await field(form, 'Amount').get('input').setValue('5')
    await pickOption(field(form, 'Category'), 'Salary')

    await press(form, 'Save')

    expect(form.get('[role="alert"]').text()).toContain('maximum length of 100')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
  })
})

describe('the Payee of a new Transaction', () => {
  it('fills the Category the Payee was last used with', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, []) })
    const { form } = await openNewTransaction()

    await pickPayee(form, 'corn', 'Corner Market')

    expect(field(form, 'Category').text()).toContain('Groceries')
    expect((field(form, 'Payee').get('input').element as HTMLInputElement).value).toBe('Corner Market')
  })

  it('keeps a Category the User already chose', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, []) })
    const { form } = await openNewTransaction()
    await pickOption(field(form, 'Category'), 'Salary')

    await pickPayee(form, 'corn', 'Corner Market')

    expect(field(form, 'Category').text()).toContain('Salary')
    expect(field(form, 'Category').text()).not.toContain('Groceries')
  })

  it('follows the Payee when it changes, until the User picks a Category themselves', async () => {
    fakeApi({
      ...everything,
      'GET /api/payees': () => json(200, [corner, { id: 'pay-co', name: 'Pay Co', suggestedCategoryId: 'salary' }]),
      'GET /api/transactions': () => json(200, []),
    })
    const { form } = await openNewTransaction()

    await pickPayee(form, 'corn', 'Corner Market')
    await pickPayee(form, 'pay', 'Pay Co')
    expect(field(form, 'Category').text()).toContain('Salary')

    await pickOption(field(form, 'Category'), 'Groceries')
    await pickPayee(form, 'pay', 'Pay Co')
    expect(field(form, 'Category').text()).toContain('Groceries')
  })

  it('suggests nothing for a Payee without one, or whose Category is Archived', async () => {
    fakeApi({
      ...everything,
      'GET /api/payees': () =>
        json(200, [landlord, { id: 'agency', name: 'Travel Agency', suggestedCategoryId: 'flights' }]),
      'GET /api/transactions': () => json(200, []),
    })
    const { form } = await openNewTransaction()

    await pickPayee(form, 'land', 'Landlord')
    expect(field(form, 'Category').text()).not.toContain('Groceries')

    await pickPayee(form, 'agen', 'Travel Agency')
    expect(field(form, 'Category').text()).not.toContain('Flights')
  })
})

describe('the Refund hint', () => {
  async function formWithCategory(direction: 'In' | 'Out', category: string): Promise<FoundElement> {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, []) })
    const { form } = await openNewTransaction()
    await chooseDirection(form, direction)
    await pickOption(field(form, 'Category'), category)
    return form
  }

  it('says In an Expense Category is a Refund that reduces spending and is not income', async () => {
    const form = await formWithCategory('In', 'Groceries')

    expect(form.text()).toContain('This is a Refund: it reduces spending in Groceries and is not income.')
  })

  it('says Out of an Income Category reduces income', async () => {
    const form = await formWithCategory('Out', 'Salary')

    expect(form.text()).toContain('This reduces income in Salary.')
  })

  it('says nothing for ordinary spending or income', async () => {
    expect((await formWithCategory('Out', 'Groceries')).find('.transaction-hint').exists()).toBe(false)
    document.body.innerHTML = ''
    expect((await formWithCategory('In', 'Salary')).find('.transaction-hint').exists()).toBe(false)
  })
})

describe('editing a Transaction', () => {
  it('starts from the Transaction, shows its Payee, and replaces every field', async () => {
    let list: FixtureTransaction[] = [shopping]
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, list),
      'PUT /api/transactions/shopping': () => {
        list = [{ ...shopping, amount: 45 }]
        return json(200, list[0])
      },
    })
    const { page } = await visit('/transactions')

    await press(page, 'Edit Corner Market on 2026-10-05')
    const form = documentBody().get('[role="dialog"]')
    expect(form.text()).toContain('Edit transaction')
    expect((field(form, 'Payee').get('input').element as HTMLInputElement).value).toBe('Corner Market')
    expect((field(form, 'Amount').get('input').element as HTMLInputElement).value).toBe('40.5')
    expect((field(form, 'Note').get('input').element as HTMLInputElement).value).toBe('Weekly shop')
    await field(form, 'Amount').get('input').setValue('45')
    await press(form, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/transactions/shopping',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({
          accountId: 'checking',
          date: '2026-10-05',
          direction: 'Out',
          amount: 45,
          categoryId: 'groceries',
          payeeName: 'Corner Market',
          note: 'Weekly shop',
        }),
      }),
    )
    expect(page.text()).toContain('-$45.00')
  })

  it('will not save while the Payee\'s name is unknown, so the Payee is not dropped', async () => {
    fakeApi({
      ...everything,
      'GET /api/payees': () => json(500, { title: 'Oops', detail: 'Payees unavailable.' }),
      'GET /api/transactions': () => json(200, [shopping]),
    })
    const { page } = await visit('/transactions')

    await press(page, 'Edit Groceries on 2026-10-05')
    const form = documentBody().get('[role="dialog"]')

    expect(form.text()).toContain('Its Payee could not be loaded')
    expect(form.get('button[type="submit"]').attributes('disabled')).toBeDefined()
  })

  it('does not change the Category when the Payee is picked again', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, [pay]) })
    const { page } = await visit('/transactions')

    await press(page, 'Edit Salary on 2026-10-01')
    const form = documentBody().get('[role="dialog"]')
    await pickPayee(form, 'corn', 'Corner Market')

    expect(field(form, 'Category').text()).toContain('Salary')
  })

  it('offers only open Accounts in the Transaction\'s Currency', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, [shopping]) })
    const { page } = await visit('/transactions')

    await press(page, 'Edit Corner Market on 2026-10-05')
    const form = documentBody().get('[role="dialog"]')

    expect(await optionLabels(field(form, 'Account'))).toEqual(['Everyday', 'Visa'])
  })

  it('keeps the Archived Category the Transaction already has, and no other', async () => {
    fakeApi({
      ...everything,
      'GET /api/categories?includeArchived=true': () => json(200, [salary, groceries, travel]),
      'GET /api/transactions': () => json(200, [{ ...shopping, categoryId: 'flights' }]),
    })
    const { page } = await visit('/transactions')

    await press(page, 'Edit Corner Market on 2026-10-05')
    const form = documentBody().get('[role="dialog"]')
    expect(field(form, 'Category').text()).toContain('Travel › Flights')

    const categories = await optionLabels(field(form, 'Category'))
    expect(categories).toContain('Travel › Flights')
    expect(categories).not.toContain('Travel')
  })

  it('makes the Transactions of a Closed Account read-only and says why', async () => {
    fakeApi({ ...everything, 'GET /api/transactions': () => json(200, [oldPurchase, shopping]) })

    const { page } = await visit('/transactions')

    const reason = page.get('#transactions-locked-reason')
    expect(reason.text()).toContain('Closed Account')
    const rows = page.findAll('tbody tr')
    const lockedEdit = rows[0]?.get('button[aria-label="Edit Groceries on 2026-08-01"]')
    expect(lockedEdit?.attributes('disabled')).toBeDefined()
    expect(lockedEdit?.attributes('aria-describedby')).toBe('transactions-locked-reason')
    expect(rows[0]?.get('button[aria-label="Delete Groceries on 2026-08-01"]').attributes('disabled')).toBeDefined()
    expect(rows[1]?.get('button[aria-label="Edit Corner Market on 2026-10-05"]').attributes('disabled')).toBeUndefined()
  })

  it('shows the API\'s message when it refuses the change', async () => {
    fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, [shopping]),
      'PUT /api/transactions/shopping': () =>
        json(409, { title: 'Category archived', detail: 'Groceries is archived.' }),
    })
    const { page } = await visit('/transactions')

    await press(page, 'Edit Corner Market on 2026-10-05')
    const form = documentBody().get('[role="dialog"]')
    await press(form, 'Save')

    expect(form.get('[role="alert"]').text()).toContain('Groceries is archived.')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
  })
})

describe('deleting a Transaction', () => {
  it('asks first, then deletes and refreshes the list', async () => {
    let list: FixtureTransaction[] = [shopping, pay]
    const fetchMock = fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, list),
      'DELETE /api/transactions/shopping': () => {
        list = [pay]
        return json(204)
      },
    })
    const { page } = await visit('/transactions')

    await press(page, 'Delete Corner Market on 2026-10-05')
    expect(fetchMock).not.toHaveBeenCalledWith('/api/transactions/shopping', expect.anything())
    await press(confirmationBox(), 'Delete')

    expect(fetchMock).toHaveBeenCalledWith('/api/transactions/shopping', expect.objectContaining({ method: 'DELETE' }))
    expect(page.findAll('tbody tr')).toHaveLength(1)
  })

  it('keeps the Transaction when the User cancels', async () => {
    const fetchMock = fakeApi({ ...everything, 'GET /api/transactions': () => json(200, [shopping]) })
    const { page } = await visit('/transactions')

    await press(page, 'Delete Corner Market on 2026-10-05')
    await press(confirmationBox(), 'Cancel')

    expect(fetchMock).not.toHaveBeenCalledWith('/api/transactions/shopping', expect.anything())
    expect(page.findAll('tbody tr')).toHaveLength(1)
  })

  it('shows the API\'s message when it refuses', async () => {
    fakeApi({
      ...everything,
      'GET /api/transactions': () => json(200, [shopping]),
      'DELETE /api/transactions/shopping': () => json(404, { title: 'Not found', detail: 'That Transaction is gone.' }),
    })
    const { page } = await visit('/transactions')

    await press(page, 'Delete Corner Market on 2026-10-05')
    await press(confirmationBox(), 'Delete')

    expect(page.get('[role="alert"]').text()).toContain('That Transaction is gone.')
  })
})
