import { describe, expect, it } from 'vitest'
import { type FixturePayee, corner, groceries, landlord } from './testing/fixtures'
import { documentBody, fakeApi, json, press, signedIn, visit } from './testing/harness'

describe('Payees screen', () => {
  it('lists the Payees in the order the API gave, each with its suggested Category by name', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/payees': () => json(200, [corner, landlord]),
      'GET /api/categories': () => json(200, [groceries]),
    })

    const { page } = await visit('/payees')

    expect(page.get('h1').text()).toBe('Payees')
    const rows = page.findAll('li')
    expect(rows.map((row) => row.get('.payee-row__name').text())).toEqual(['Corner Market', 'Landlord'])
    expect(rows[0]?.text()).toContain('Groceries')
    expect(rows[1]?.text()).toContain('No suggested Category')
  })

  it('still lists the Payees when the Categories could not be loaded', async () => {
    fakeApi({ ...signedIn, 'GET /api/payees': () => json(200, [corner]) })

    const { page } = await visit('/payees')

    expect(page.get('li').text()).toContain('Corner Market')
    expect(page.get('li').text()).not.toContain('Groceries')
  })

  it('explains that Payees are created by naming them on a Transaction when there are none', async () => {
    fakeApi({ ...signedIn, 'GET /api/payees': () => json(200, []) })

    const { page } = await visit('/payees')

    expect(page.text()).toContain('No Payees yet.')
    expect(page.text()).toContain('naming them on a Transaction')
  })

  it('shows the API failure with a way to try again', async () => {
    let healthy = false
    fakeApi({
      ...signedIn,
      'GET /api/payees': () => (healthy ? json(200, [corner]) : json(500, { title: 'Oops', detail: 'Database unavailable.' })),
    })
    const { page } = await visit('/payees')

    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.text()).toContain('Corner Market')
  })
})

describe('renaming a Payee', () => {
  it('updates the list', async () => {
    let list: FixturePayee[] = [corner, landlord]
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/payees': () => json(200, list),
      'PUT /api/payees/corner/name': () => {
        list = [{ ...corner, name: 'Zeta Market' }, landlord]
        return json(200, list[0])
      },
    })
    const { page } = await visit('/payees')

    await press(page, 'Rename Corner Market')
    const dialog = documentBody().get('[role="dialog"]')
    const input = dialog.get('input.el-input__inner')
    expect((input.element as HTMLInputElement).value).toBe('Corner Market')
    await input.setValue('Zeta Market')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/payees/corner/name',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ name: 'Zeta Market' }) }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
    expect(page.findAll('.payee-row__name').map((name) => name.text())).toEqual(['Zeta Market', 'Landlord'])
  })

  it('shows the API message when another Payee has the name, and leaves the name unchanged', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/payees': () => json(200, [corner, landlord]),
      'PUT /api/payees/corner/name': () =>
        json(409, { title: 'Conflict', detail: 'Another Payee is already named "landlord".' }),
    })
    const { page } = await visit('/payees')

    await press(page, 'Rename Corner Market')
    const dialog = documentBody().get('[role="dialog"]')
    await dialog.get('input.el-input__inner').setValue('landlord')
    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('Another Payee is already named "landlord".')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
    expect(page.findAll('.payee-row__name').map((name) => name.text())).toEqual(['Corner Market', 'Landlord'])
  })
})

describe('a Payee whose suggested Category is not in the loaded Categories', () => {
  it('does not claim the Payee has no suggestion', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/payees': () => json(200, [corner]),
      'GET /api/categories': () => json(200, []),
    })

    const { page } = await visit('/payees')

    expect(page.get('li').text()).not.toContain('No suggested Category')
  })
})
