import { ref, shallowRef, type Ref } from 'vue'
import { describeFailure } from '@/api/client'

/** Whether the data has been asked for yet, is on its way, arrived, or could not be fetched. */
export type LoadStatus = 'idle' | 'loading' | 'ready' | 'failed'

export interface Loaded<T> {
  status: Ref<LoadStatus>
  /** What the API said about the last failed load, or empty. */
  failure: Ref<string>
  data: Ref<T>
  load: () => Promise<void>
}

/**
 * Something fetched from the API. A failed load keeps the data already shown, and an older answer
 * that arrives after a newer request never replaces it.
 */
export function useLoaded<T>(fetchData: () => Promise<T>, initial: T): Loaded<T> {
  const status = ref<LoadStatus>('idle')
  const failure = ref('')
  const data = shallowRef<T>(initial)
  let latestRequest = 0

  async function load(): Promise<void> {
    const request = ++latestRequest
    status.value = 'loading'
    failure.value = ''
    try {
      const loaded = await fetchData()
      if (request === latestRequest) {
        data.value = loaded
        status.value = 'ready'
      }
    } catch (error) {
      if (request === latestRequest) {
        status.value = 'failed'
        failure.value = describeFailure(error)
      }
    }
  }

  return { status, failure, data, load }
}
