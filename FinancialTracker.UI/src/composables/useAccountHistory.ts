import { ref, shallowRef, type Ref } from 'vue'
import { api, describeFailure } from '@/api/client'
import type { LoadStatus } from '@/composables/useLoadedList'
import type { Account } from '@/stores/accounts'

export type HistoryKind = 'Transaction' | 'Transfer' | 'BalanceAdjustment'

export interface HistoryEntry {
  kind: HistoryKind
  /** The id of the Transaction, Transfer or Balance Adjustment the entry stands for. */
  id: string
  date: string
  /** Signed. For a Balance Adjustment, the difference it made. */
  amount: number
  /** What a Balance Adjustment states the balance was; null for the others. */
  statedBalance: number | null
}

/** What a Balance Adjustment states; the API replaces every field on an edit. */
export interface BalanceAdjustmentRequest {
  date: string
  /** Signed: negative is money owed. */
  balance: number
}

/**
 * The id of the Opening Balance: nothing but the earliest Balance Adjustment, not stored apart from the others.
 * Null while the Account has none.
 */
export function openingBalanceId(entries: HistoryEntry[]): string | null {
  const earliest = entries
    .filter((entry) => entry.kind === 'BalanceAdjustment')
    .reduce<HistoryEntry | null>((first, entry) => (first === null || entry.date < first.date ? entry : first), null)
  return earliest?.id ?? null
}

/** What a Balance Adjustment is called: the earliest one is the Opening Balance. */
export function adjustmentName(opening: boolean): string {
  return opening ? 'Opening Balance' : 'Balance Adjustment'
}

export interface AccountHistory {
  status: Ref<LoadStatus>
  failure: Ref<string>
  account: Ref<Account | null>
  entries: Ref<HistoryEntry[]>
  load: () => Promise<void>
  /** Each change fetches the Account and its history again, so the balance and the entries stay in step. */
  addAdjustment: (request: BalanceAdjustmentRequest) => Promise<void>
  editAdjustment: (adjustmentId: string, request: BalanceAdjustmentRequest) => Promise<void>
  deleteAdjustment: (adjustmentId: string) => Promise<void>
}

/** One Account and its history, fetched together so the balance and the entries never disagree. */
export function useAccountHistory(id: () => string): AccountHistory {
  const status = ref<LoadStatus>('idle')
  const failure = ref('')
  const account = shallowRef<Account | null>(null)
  const entries = shallowRef<HistoryEntry[]>([])
  let latestRequest = 0

  async function load(): Promise<void> {
    const request = ++latestRequest
    const accountId = id()
    status.value = 'loading'
    failure.value = ''
    try {
      const [loadedAccount, loadedEntries] = await Promise.all([
        api.get<Account>(`/api/accounts/${accountId}`),
        api.get<HistoryEntry[]>(`/api/accounts/${accountId}/history`),
      ])
      if (request === latestRequest) {
        account.value = loadedAccount
        entries.value = loadedEntries
        status.value = 'ready'
      }
    } catch (error) {
      if (request === latestRequest) {
        status.value = 'failed'
        failure.value = describeFailure(error)
      }
    }
  }

  const adjustmentsPath = (): string => `/api/accounts/${id()}/balance-adjustments`

  async function addAdjustment(request: BalanceAdjustmentRequest): Promise<void> {
    await api.post(adjustmentsPath(), request)
    await load()
  }

  async function editAdjustment(adjustmentId: string, request: BalanceAdjustmentRequest): Promise<void> {
    await api.put(`${adjustmentsPath()}/${adjustmentId}`, request)
    await load()
  }

  async function deleteAdjustment(adjustmentId: string): Promise<void> {
    await api.delete(`${adjustmentsPath()}/${adjustmentId}`)
    await load()
  }

  return { status, failure, account, entries, load, addAdjustment, editAdjustment, deleteAdjustment }
}
