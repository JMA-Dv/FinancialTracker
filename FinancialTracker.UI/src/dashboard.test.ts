import { flushPromises } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  type FixtureCategoryTotal,
  groceriesTotal,
  marketTotal,
  salaryTotal,
  travelTotal,
} from './testing/fixtures'
import { containing, fakeApi, field, json, press, signedIn, visit } from './testing/harness'

// Chart.js needs a real canvas; what matters here is what each chart is given.
const charts = vi.hoisted(() => ({ created: [] as { canvas: unknown; config: { data: unknown; type: string } }[] }))
vi.mock('chart.js', () => {
  class Chart {
    static register = vi.fn()
    data: unknown
    constructor(
      readonly canvas: unknown,
      readonly config: { data: unknown; type: string },
    ) {
      this.data = config.data
      charts.created.push(this)
    }
    update = vi.fn()
    destroy = vi.fn()
  }
  return { Chart, BarController: {}, BarElement: {}, CategoryScale: {}, LinearScale: {}, Tooltip: {} }
})

beforeEach(() => {
  charts.created.length = 0
  // Only the clock the periods are worked out from; timers stay real so the screen can settle.
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-10-23T12:00:00Z'))
})
afterEach(() => vi.useRealTimers())

const thisMonth = 'from=2026-10-01&to=2026-10-23'
const lastMonth = 'from=2026-09-01&to=2026-09-30'

const netWorth = {
  date: '2026-10-23',
  totals: [
    { currency: 'USD', amount: 1550.5 },
    { currency: 'MXN', amount: 3000 },
  ],
}

function incomeSpending(range: string): Record<string, () => Response> {
  const [from, to] = range.split('&').map((part) => part.split('=')[1])
  return {
    [`GET /api/reports/income-spending?${range}`]: () =>
      json(200, {
        from,
        to,
        totals: [
          { currency: 'USD', income: 2000, spending: 450.25 },
          { currency: 'MXN', income: 0, spending: 120 },
        ],
      }),
  }
}

function categories(range: string, usd: FixtureCategoryTotal[] = [salaryTotal, groceriesTotal, travelTotal]): Record<string, () => Response> {
  const [from, to] = range.split('&').map((part) => part.split('=')[1])
  return {
    [`GET /api/reports/categories?${range}`]: () =>
      json(200, {
        from,
        to,
        currencies: [
          { currency: 'USD', categories: usd },
          { currency: 'MXN', categories: [{ ...marketTotal, total: 120, ownAmount: 120 }] },
        ],
      }),
  }
}

function payees(range: string): Record<string, () => Response> {
  const [from, to] = range.split('&').map((part) => part.split('=')[1])
  return {
    [`GET /api/reports/payees?${range}`]: () =>
      json(200, {
        from,
        to,
        currencies: [
          {
            currency: 'USD',
            payees: [
              { payeeId: 'corner', name: 'Corner Market', spending: 300 },
              { payeeId: null, name: null, spending: 150.25 },
            ],
          },
          { currency: 'MXN', payees: [{ payeeId: 'taqueria', name: 'Taqueria', spending: 120 }] },
        ],
      }),
  }
}

function dashboard(range = thisMonth): Record<string, () => Response> {
  return {
    ...signedIn,
    'GET /api/reports/net-worth': () => json(200, netWorth),
    ...incomeSpending(range),
    ...categories(range),
    ...payees(range),
  }
}

describe('Dashboard', () => {
  it('gives each Currency its own section and never adds across Currencies', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const usd = page.get('section[aria-labelledby="currency-USD"]')
    const mxn = page.get('section[aria-labelledby="currency-MXN"]')
    expect(page.findAll('section.currency-section')).toHaveLength(2)
    expect(usd.text()).toContain('$1,550.50')
    expect(usd.text()).toContain('$2,000.00')
    expect(usd.text()).toContain('$450.25')
    expect(usd.text()).not.toContain('MX$')
    expect(mxn.text()).toContain('MX$3,000.00')
    expect(mxn.text()).toContain('MX$120.00')
    expect(mxn.text()).not.toContain('$2,000.00')
    expect(page.text()).not.toContain('4,550.50')
  })

  it('shows this month by default, and says what the totals leave out', async () => {
    const fetchMock = fakeApi(dashboard())

    const { page } = await visit('/')

    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/income-spending?${thisMonth}`, expect.anything())
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/categories?${thisMonth}`, expect.anything())
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/payees?${thisMonth}`, expect.anything())
    expect(fetchMock).toHaveBeenCalledWith('/api/reports/net-worth', expect.anything())
    expect(page.text()).toContain('Transfers and Balance Adjustments are not counted')
    expect(page.text()).toContain('A Refund reduces spending')
  })

  it('asks every report for the new range when the period changes', async () => {
    const fetchMock = fakeApi({ ...dashboard(), ...incomeSpending(lastMonth), ...categories(lastMonth), ...payees(lastMonth) })
    const { page } = await visit('/')

    await page.get('input[type="radio"][value="last-month"]').setValue(true)
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/income-spending?${lastMonth}`, expect.anything())
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/categories?${lastMonth}`, expect.anything())
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/payees?${lastMonth}`, expect.anything())
  })

  it('offers last 30 days, year to date and a custom range', async () => {
    const last30 = 'from=2026-09-24&to=2026-10-23'
    const ytd = 'from=2026-01-01&to=2026-10-23'
    const custom = 'from=2026-01-05&to=2026-02-10'
    const fetchMock = fakeApi({
      ...dashboard(),
      ...incomeSpending(last30),
      ...categories(last30),
      ...payees(last30),
      ...incomeSpending(ytd),
      ...categories(ytd),
      ...payees(ytd),
      ...incomeSpending(custom),
      ...categories(custom),
      ...payees(custom),
    })
    const { page } = await visit('/')

    await page.get('input[type="radio"][value="last-30-days"]').setValue(true)
    await flushPromises()
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/payees?${last30}`, expect.anything())

    await page.get('input[type="radio"][value="year-to-date"]').setValue(true)
    await flushPromises()
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/payees?${ytd}`, expect.anything())

    await page.get('input[type="radio"][value="custom"]').setValue(true)
    await field(page, 'From').get('input').setValue('2026-01-05')
    await field(page, 'To').get('input').setValue('2026-02-10')
    await flushPromises()
    expect(fetchMock).toHaveBeenCalledWith(`/api/reports/payees?${custom}`, expect.anything())
  })

  it('does not ask for a custom range that ends before it starts', async () => {
    const fetchMock = fakeApi(dashboard())
    const { page } = await visit('/')
    const before = fetchMock.mock.calls.length

    await page.get('input[type="radio"][value="custom"]').setValue(true)
    await field(page, 'From').get('input').setValue('2026-03-01')
    await field(page, 'To').get('input').setValue('2026-02-01')
    await flushPromises()

    expect(page.text()).toContain('The start can\'t be after the end.')
    expect(fetchMock.mock.calls.slice(before).filter(([path]) => String(path).includes('from=2026-03-01'))).toHaveLength(0)
  })

  it('shows net worth as of the chosen date, and not a date after today', async () => {
    const fetchMock = fakeApi({
      ...dashboard(),
      'GET /api/reports/net-worth?date=2026-09-30': () => json(200, { ...netWorth, date: '2026-09-30' }),
    })
    const { page } = await visit('/')

    await field(page, 'Net worth as of').get('input').setValue('2999-01-01')
    await flushPromises()
    expect(page.text()).toContain('The date can\'t be after today.')
    expect(fetchMock).not.toHaveBeenCalledWith(expect.stringContaining('date=2999'), expect.anything())

    await field(page, 'Net worth as of').get('input').setValue('2026-09-30')
    await flushPromises()
    expect(fetchMock).toHaveBeenCalledWith('/api/reports/net-worth?date=2026-09-30', expect.anything())
    expect(page.get('section[aria-labelledby="currency-USD"]').text()).toContain('as of 2026-09-30')
  })

  it('shows the error of a failing report and still renders the others', async () => {
    let healthy = false
    fakeApi({
      ...dashboard(),
      [`GET /api/reports/categories?${thisMonth}`]: () =>
        healthy ? categories(thisMonth)[`GET /api/reports/categories?${thisMonth}`]!() : json(500, { title: 'Oops', detail: 'Categories report unavailable.' }),
    })
    const { page } = await visit('/')

    expect(page.get('[role="alert"]').text()).toContain('Categories report unavailable.')
    const usd = page.get('section[aria-labelledby="currency-USD"]')
    expect(usd.text()).toContain('$1,550.50')
    expect(usd.text()).toContain('Corner Market')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.text()).toContain('Groceries')
  })

  it('does not claim the period is empty while one of its reports has failed', async () => {
    fakeApi({
      ...dashboard(),
      'GET /api/reports/net-worth': () => json(200, { date: '2026-10-23', totals: [{ currency: 'USD', amount: 100 }] }),
      [`GET /api/reports/income-spending?${thisMonth}`]: () => json(200, { from: '2026-10-01', to: '2026-10-23', totals: [] }),
      [`GET /api/reports/categories?${thisMonth}`]: () => json(500, { title: 'Oops', detail: 'Categories report unavailable.' }),
      [`GET /api/reports/payees?${thisMonth}`]: () => json(200, { from: '2026-10-01', to: '2026-10-23', currencies: [] }),
    })

    const { page } = await visit('/')

    expect(page.get('[role="alert"]').text()).toContain('Categories report unavailable.')
    expect(page.text()).not.toContain('No Transactions in this period.')
  })

  it('says so when the period has no Transactions, and still shows net worth', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/reports/net-worth': () => json(200, { date: '2026-10-23', totals: [{ currency: 'USD', amount: 100 }] }),
      [`GET /api/reports/income-spending?${thisMonth}`]: () => json(200, { from: '2026-10-01', to: '2026-10-23', totals: [] }),
      [`GET /api/reports/categories?${thisMonth}`]: () => json(200, { from: '2026-10-01', to: '2026-10-23', currencies: [] }),
      [`GET /api/reports/payees?${thisMonth}`]: () => json(200, { from: '2026-10-01', to: '2026-10-23', currencies: [] }),
    })

    const { page } = await visit('/')

    const usd = page.get('section[aria-labelledby="currency-USD"]')
    expect(usd.text()).toContain('$100.00')
    expect(usd.text()).toContain('No Transactions in this period.')
  })
})

describe('Category breakdown', () => {
  it('charts spending by top-level Category and keeps Income in its own table', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const usd = page.get('section[aria-labelledby="currency-USD"]')
    const spending = containing(usd, 'section', 'Spending by Category')
    expect(spending.text()).toContain('Groceries')
    expect(spending.text()).toContain('Travel')
    expect(spending.text()).not.toContain('Salary')
    const income = containing(usd, 'section', 'Income by Category')
    expect(income.text()).toContain('Salary')
    expect(income.text()).not.toContain('Groceries')

    const chart = charts.created.find((created) =>
      JSON.stringify(created.config.data).includes('Groceries'),
    )
    expect(chart?.config.data).toMatchObject({
      labels: ['Groceries', 'Travel'],
      datasets: [{ data: [300, 150.25] }],
    })
  })

  it('has an accessible label and a data table for the chart', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const usd = page.get('section[aria-labelledby="currency-USD"]')
    const canvas = usd.get('canvas[role="img"]')
    expect(canvas.attributes('aria-label')).toContain('Spending by Category in USD')
    expect(usd.get('table').get('caption').text()).toContain('Spending by Category')
  })

  it('expands a parent to show its children with the total and what was recorded directly', async () => {
    fakeApi(dashboard())
    const { page } = await visit('/')
    const usd = page.get('section[aria-labelledby="currency-USD"]')
    const table = containing(usd, 'table', 'Groceries')

    expect(table.text()).not.toContain('Market')
    const row = containing(table, 'tr', 'Groceries')
    expect(row.text()).toContain('$300.00')
    expect(row.text()).toContain('$100.00')

    await press(row, 'Show Groceries subcategories')

    expect(table.text()).toContain('Market')
    expect(containing(table, 'tr', 'Market').text()).toContain('$200.00')
    expect(row.get('button').attributes('aria-expanded')).toBe('true')
  })

  it('marks an Archived Category', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const row = containing(page.get('section[aria-labelledby="currency-USD"]'), 'tr', 'Travel')
    expect(row.text()).toContain('Archived')
  })

  it('links a Category to the Transactions it adds up, for the period', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const link = containing(page.get('section[aria-labelledby="currency-USD"]'), 'a', 'Groceries')
    expect(link.attributes('href')).toBe(`/transactions?categoryId=groceries&${thisMonth}`)
  })
})

describe('Spending by Payee', () => {
  it('labels spending without a Payee "No Payee"', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const usd = page.get('section[aria-labelledby="currency-USD"]')
    const table = containing(usd, 'table', 'Corner Market')
    expect(containing(table, 'tr', 'No Payee').text()).toContain('$150.25')
    const chart = charts.created.find((created) => JSON.stringify(created.config.data).includes('No Payee'))
    expect(chart?.config.data).toMatchObject({ labels: ['Corner Market', 'No Payee'] })
  })

  it('links a Payee to its Transactions for the period, and "No Payee" to nothing', async () => {
    fakeApi(dashboard())

    const { page } = await visit('/')

    const usd = page.get('section[aria-labelledby="currency-USD"]')
    expect(containing(usd, 'a', 'Corner Market').attributes('href')).toBe(`/transactions?payeeId=corner&${thisMonth}`)
    expect(containing(usd, 'tr', 'No Payee').find('a').exists()).toBe(false)
  })
})
