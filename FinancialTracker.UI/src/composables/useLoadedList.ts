import { ref, shallowRef, type Ref } from 'vue'
import { describeFailure } from '@/api/client'

/** Whether the list has been asked for yet, is on its way, arrived, or could not be fetched. */
export type LoadStatus = 'idle' | 'loading' | 'ready' | 'failed'

export interface LoadedList<T> {
  status: Ref<LoadStatus>
  /** What the API said about the last failed load, or empty. */
  failure: Ref<string>
  items: Ref<T[]>
  load: () => Promise<void>
}

/**
 * A list fetched from the API. A failed load keeps the items already shown, and an older answer
 * that arrives after a newer request never replaces it.
 */
export function useLoadedList<T>(fetchItems: () => Promise<T[]>): LoadedList<T> {
  const status = ref<LoadStatus>('idle')
  const failure = ref('')
  const items = shallowRef<T[]>([])
  let latestRequest = 0

  async function load(): Promise<void> {
    const request = ++latestRequest
    status.value = 'loading'
    failure.value = ''
    try {
      const loaded = await fetchItems()
      if (request === latestRequest) {
        items.value = loaded
        status.value = 'ready'
      }
    } catch (error) {
      if (request === latestRequest) {
        status.value = 'failed'
        failure.value = describeFailure(error)
      }
    }
  }

  return { status, failure, items, load }
}
