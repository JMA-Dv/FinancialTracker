import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api } from '@/api/client'
import { useLoadedList } from '@/composables/useLoadedList'

export type AccountKind = 'Checking' | 'Savings' | 'Cash' | 'CreditCard'

export interface Account {
  id: string
  name: string
  kind: AccountKind
  /** An ISO 4217 code; every amount on the Account is in it. */
  currency: string
  /** Signed: negative is money owed. */
  balance: number
  isLiability: boolean
  isClosed: boolean
  openingBalance: { amount: number; date: string } | null
}

export interface CreateAccountRequest {
  name: string
  kind: AccountKind
  currency: string
  /** Signed, as the API wants it: a Credit Card's amount owed is negative. */
  openingBalance?: { amount: number; date: string }
}

export const useAccountsStore = defineStore('accounts', () => {
  const includeClosed = ref(false)
  const {
    status,
    failure,
    items: accounts,
    load,
    mutate,
  } = useLoadedList(() =>
    api.get<Account[]>(includeClosed.value ? '/api/accounts?includeClosed=true' : '/api/accounts'),
  )

  /** The Accounts that accept new money, for pickers in later forms. */
  const open = computed<Account[]>(() => accounts.value.filter((account) => !account.isClosed))

  function create(request: CreateAccountRequest): Promise<void> {
    return mutate(() => api.post('/api/accounts', request))
  }

  function rename(id: string, name: string): Promise<void> {
    return mutate(() => api.put(`/api/accounts/${id}/name`, { name }))
  }

  /** The API refuses once the Account has Transactions or Transfers. */
  function changeCurrency(id: string, currency: string): Promise<void> {
    return mutate(() => api.put(`/api/accounts/${id}/currency`, { currency }))
  }

  /** The API refuses unless the balance is zero. */
  function close(id: string): Promise<void> {
    return mutate(() => api.post(`/api/accounts/${id}/close`))
  }

  function reopen(id: string): Promise<void> {
    return mutate(() => api.post(`/api/accounts/${id}/reopen`))
  }

  async function setIncludeClosed(value: boolean): Promise<void> {
    includeClosed.value = value
    await load()
  }

  return { status, failure, accounts, includeClosed, open, load, create, rename, changeCurrency, close, reopen, setIncludeClosed }
})
