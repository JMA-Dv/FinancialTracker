import { api } from '@/api/client'
import { useLoadedList, type LoadedList } from '@/composables/useLoadedList'

/** A Transfer as `GET /api/transfers` returns it. */
export interface Transfer {
  id: string
  fromAccountId: string
  toAccountId: string
  date: string
  /** What left the from-Account, in its Currency. */
  amount: number
  /** What arrived in the to-Account, in its Currency. */
  receivedAmount: number
  note: string | null
}

/** What the API replaces every field with on an edit. */
export interface SaveTransferRequest {
  fromAccountId: string
  toAccountId: string
  date: string
  amount: number
  /** Required between Currencies; leave it out within one. */
  receivedAmount?: number
  note?: string
}

export interface Transfers extends Pick<LoadedList<Transfer>, 'status' | 'failure' | 'load'> {
  transfers: LoadedList<Transfer>['items']
  create: (request: SaveTransferRequest) => Promise<void>
  edit: (id: string, request: SaveTransferRequest) => Promise<void>
  remove: (id: string) => Promise<void>
}

/** The User's Transfers, newest first. Each change fetches the list again. */
export function useTransfers(): Transfers {
  const { status, failure, items: transfers, load, mutate } = useLoadedList(() => api.get<Transfer[]>('/api/transfers'))

  return {
    status,
    failure,
    transfers,
    load,
    create: (request) => mutate(() => api.post('/api/transfers', request)),
    edit: (id, request) => mutate(() => api.put(`/api/transfers/${id}`, request)),
    remove: (id) => mutate(() => api.delete(`/api/transfers/${id}`)),
  }
}
