import { api } from '@/api/client'
import { useLoadedList, type LoadedList } from '@/composables/useLoadedList'
import { usePayeesStore } from '@/stores/payees'
import { transactionsPath, type SaveTransactionRequest, type Transaction, type TransactionFilter } from '@/transactionRules'

export interface Transactions extends Pick<LoadedList<Transaction>, 'status' | 'failure' | 'load'> {
  transactions: LoadedList<Transaction>['items']
  create: (request: SaveTransactionRequest) => Promise<void>
  edit: (id: string, request: SaveTransactionRequest) => Promise<void>
  remove: (id: string) => Promise<void>
}

/** The Transactions matching `filter`, which is read again on every load. Each change fetches the list again. */
export function useTransactions(filter: () => TransactionFilter): Transactions {
  const payees = usePayeesStore()
  const { status, failure, items: transactions, load, mutate } = useLoadedList(() =>
    api.get<Transaction[]>(transactionsPath(filter())),
  )

  // Saving can name a Payee nobody had yet, so the Payees are fetched again too.
  async function save(change: () => Promise<unknown>): Promise<void> {
    await mutate(change)
    await payees.load()
  }

  return {
    status,
    failure,
    transactions,
    load,
    create: (request) => save(() => api.post('/api/transactions', request)),
    edit: (id, request) => save(() => api.put(`/api/transactions/${id}`, request)),
    remove: (id) => mutate(() => api.delete(`/api/transactions/${id}`)),
  }
}
