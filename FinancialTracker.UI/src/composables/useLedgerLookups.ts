import { computed, type ComputedRef } from 'vue'
import { api } from '@/api/client'
import { flattenCategories, type CategoryOption } from '@/categoryRules'
import { useAllAccounts } from '@/composables/useAllAccounts'
import { useLoadedList, type LoadStatus } from '@/composables/useLoadedList'
import type { Account } from '@/stores/accounts'
import type { CategoryNode } from '@/stores/categories'

export interface LedgerLookups {
  status: ComputedRef<LoadStatus>
  /** What the API said about the first lookup that failed, or empty. */
  failure: ComputedRef<string>
  /** Every Account, closed or not: a Transaction stays on the Account it was recorded on. */
  accounts: ComputedRef<Account[]>
  /** Every Category, archived or not, for the same reason. */
  categories: ComputedRef<CategoryOption[]>
  load: () => Promise<void>
}

/** The Accounts and Categories a Transaction can refer to, archived and closed ones too. */
export function useLedgerLookups(): LedgerLookups {
  const accountList = useAllAccounts()
  const categoryList = useLoadedList(() => api.get<CategoryNode[]>('/api/categories?includeArchived=true'))
  const lists = [accountList, categoryList]

  const status = computed<LoadStatus>(() => {
    const statuses = lists.map((list) => list.status.value)
    if (statuses.includes('failed')) {
      return 'failed'
    }
    if (statuses.includes('loading')) {
      return 'loading'
    }
    return statuses.every((each) => each === 'ready') ? 'ready' : 'idle'
  })
  const failure = computed(() => lists.find((list) => list.failure.value !== '')?.failure.value ?? '')

  async function load(): Promise<void> {
    await Promise.all(lists.map((list) => list.load()))
  }

  return {
    status,
    failure,
    accounts: computed(() => accountList.items.value),
    categories: computed(() => flattenCategories(categoryList.items.value)),
    load,
  }
}
