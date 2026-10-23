import { api } from '@/api/client'
import { useLoadedList, type LoadedList } from '@/composables/useLoadedList'
import type { Account } from '@/stores/accounts'

/**
 * Every Account, closed or not: what a Transaction or Transfer refers to may be on a Closed Account. Kept apart from
 * the Accounts store, which holds whatever its own screen toggled.
 */
export function useAllAccounts(): LoadedList<Account> {
  return useLoadedList(() => api.get<Account[]>('/api/accounts?includeClosed=true'))
}
