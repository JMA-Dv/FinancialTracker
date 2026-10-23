import type { Ref } from 'vue'
import { useLoaded, type LoadStatus } from './useLoaded'

export type { LoadStatus }

export interface LoadedList<T> {
  status: Ref<LoadStatus>
  /** What the API said about the last failed load, or empty. */
  failure: Ref<string>
  items: Ref<T[]>
  load: () => Promise<void>
  /** Runs a change, then fetches the list again so every screen sees it. */
  mutate: (change: () => Promise<unknown>) => Promise<void>
}

/** A list fetched from the API: what `useLoaded` does, plus changes that refresh it. */
export function useLoadedList<T>(fetchItems: () => Promise<T[]>): LoadedList<T> {
  const { status, failure, data: items, load } = useLoaded<T[]>(fetchItems, [])

  async function mutate(change: () => Promise<unknown>): Promise<void> {
    await change()
    await load()
  }

  return { status, failure, items, load, mutate }
}
