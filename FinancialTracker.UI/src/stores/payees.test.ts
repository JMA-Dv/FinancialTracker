import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { corner, landlord } from '@/testing/fixtures'
import { fakeApi, json } from '@/testing/harness'
import { usePayeesStore } from './payees'

beforeEach(() => setActivePinia(createPinia()))

describe('Payees store', () => {
  it('looks up the suggested Category of a Payee by name, whatever the case', async () => {
    fakeApi({ 'GET /api/payees': () => json(200, [corner, landlord]) })
    const payees = usePayeesStore()

    await payees.load()

    expect(payees.suggestedCategoryId('corner market')).toBe('groceries')
    expect(payees.suggestedCategoryId('Landlord')).toBeNull()
    expect(payees.suggestedCategoryId('Nobody')).toBeNull()
  })

  it('offers the Payees whose name contains what was typed, for autocomplete', async () => {
    fakeApi({ 'GET /api/payees': () => json(200, [corner, landlord]) })
    const payees = usePayeesStore()
    await payees.load()

    expect(payees.suggest('MARK').map((payee) => payee.name)).toEqual(['Corner Market'])
    expect(payees.suggest('').map((payee) => payee.name)).toEqual(['Corner Market', 'Landlord'])
  })

  it('shows the new name after a rename', async () => {
    let list = [corner]
    fakeApi({
      'GET /api/payees': () => json(200, list),
      'PUT /api/payees/corner/name': () => {
        list = [{ ...corner, name: 'Market' }]
        return json(200, list[0])
      },
    })
    const payees = usePayeesStore()
    await payees.load()

    await payees.rename('corner', 'Market')

    expect(payees.payees.map((payee) => payee.name)).toEqual(['Market'])
  })
})
