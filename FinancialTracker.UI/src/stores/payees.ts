import { defineStore } from 'pinia'
import { api } from '@/api/client'
import { useLoadedList } from '@/composables/useLoadedList'

/** A Payee as `GET /api/payees` returns it. */
export interface Payee {
  id: string
  name: string
  /** The Category of the most recent Transaction with the Payee, or null if there is none. */
  suggestedCategoryId: string | null
}

export const usePayeesStore = defineStore('payees', () => {
  const { status, failure, items: payees, load, mutate } = useLoadedList(() => api.get<Payee[]>('/api/payees'))

  /** The Payees whose name contains `query`, whatever the case, as the API ordered them; all of them for an empty query. */
  function suggest(query: string): Payee[] {
    const needle = query.trim().toLowerCase()
    return payees.value.filter((payee) => payee.name.toLowerCase().includes(needle))
  }

  /** The Category the Transaction form should offer for the Payee called `name` (case ignored), or null. */
  function suggestedCategoryId(name: string): string | null {
    const wanted = name.trim().toLowerCase()
    return payees.value.find((payee) => payee.name.toLowerCase() === wanted)?.suggestedCategoryId ?? null
  }

  /** The name of the Payee with this id, or null when it isn't in the loaded list. */
  function nameOf(id: string): string | null {
    return payees.value.find((payee) => payee.id === id)?.name ?? null
  }

  /** Renames the Payee everywhere it is used. */
  function rename(id: string, name: string): Promise<void> {
    return mutate(() => api.put(`/api/payees/${id}/name`, { name }))
  }

  return { status, failure, payees, load, suggest, suggestedCategoryId, nameOf, rename }
})
