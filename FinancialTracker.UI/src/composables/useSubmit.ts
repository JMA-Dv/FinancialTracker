import { ref, type Ref } from 'vue'
import { ApiClientError, UnauthorizedError, describeFailure } from '@/api/client'

export interface Submit<Args extends unknown[]> {
  pending: Ref<boolean>
  /** What the API said about the last attempt, or empty. */
  error: Ref<string>
  /** Runs the action once at a time; true when it went through. */
  submit: (...args: Args) => Promise<boolean>
}

/** Runs an API call from a form or a button with the pending and error states every one of them shows. */
export function useSubmit<Args extends unknown[] = []>(action: (...args: Args) => Promise<void>): Submit<Args> {
  const pending = ref(false)
  const error = ref('')

  async function submit(...args: Args): Promise<boolean> {
    if (pending.value) {
      return false
    }
    pending.value = true
    error.value = ''
    try {
      await action(...args)
      return true
    } catch (failure) {
      if (!(failure instanceof ApiClientError)) {
        // Not the API's doing, so it is a bug: the User still hears that it failed, and it is raised for the logs.
        error.value = describeFailure(failure)
        throw failure
      }
      // A 401 is already sending the User to sign-in.
      if (!(failure instanceof UnauthorizedError)) {
        error.value = describeFailure(failure)
      }
      return false
    } finally {
      pending.value = false
    }
  }

  return { pending, error, submit }
}
