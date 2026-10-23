import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { checking, oldSavings, visa, wallet } from '@/testing/fixtures'
import { fakeApi, json } from '@/testing/harness'
import { useAccountsStore } from './accounts'

beforeEach(() => setActivePinia(createPinia()))

describe('Accounts store', () => {
  it('offers the Accounts that accept new money, with their Currency, even when Closed ones are listed', async () => {
    fakeApi({ 'GET /api/accounts?includeClosed=true': () => json(200, [checking, wallet, visa, oldSavings]) })
    const accounts = useAccountsStore()

    await accounts.setIncludeClosed(true)

    expect(accounts.open.map((account) => [account.name, account.currency])).toEqual([
      ['Everyday', 'USD'],
      ['Wallet', 'MXN'],
      ['Visa', 'USD'],
    ])
  })

  it('refreshes what the pickers offer after a change', async () => {
    let list = [checking]
    fakeApi({
      'GET /api/accounts': () => json(200, list),
      'POST /api/accounts': () => {
        list = [checking, wallet]
        return json(201, { ...wallet })
      },
    })
    const accounts = useAccountsStore()
    await accounts.load()

    await accounts.create({ name: 'Wallet', kind: 'Cash', currency: 'MXN' })

    expect(accounts.open.map((account) => account.name)).toEqual(['Everyday', 'Wallet'])
  })
})
