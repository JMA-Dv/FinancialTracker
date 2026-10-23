import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { UnauthorizedError, api } from '@/api/client'

export interface UserSettings {
  email: string
  currency: string | null
}

/** Whether we know who is signed in. `failed` means the API could not be asked, not that nobody is signed in. */
export type SessionStatus = 'unknown' | 'loading' | 'signedIn' | 'signedOut' | 'failed'

export const useSessionStore = defineStore('session', () => {
  const status = ref<SessionStatus>('unknown')
  const settings = ref<UserSettings | null>(null)
  const failure = ref('')

  const isSignedIn = computed(() => status.value === 'signedIn')

  /** Asks the API who is signed in; its 401 means nobody. */
  async function load(): Promise<void> {
    status.value = 'loading'
    failure.value = ''
    try {
      settings.value = await api.get<UserSettings>('/api/user-settings')
      status.value = 'signedIn'
    } catch (error) {
      settings.value = null
      if (error instanceof UnauthorizedError) {
        status.value = 'signedOut'
        return
      }
      status.value = 'failed'
      failure.value = error instanceof Error ? error.message : 'Something went wrong.'
    }
  }

  /** Sets the Currency offered for new Accounts; the API answers with the settings as they now stand. */
  async function setCurrency(code: string): Promise<void> {
    settings.value = await api.put<UserSettings>('/api/user-settings/currency', { currency: code })
  }

  /** Ends the session on the API, then forgets the User here. */
  async function signOut(): Promise<void> {
    await api.post('/api/auth/sign-out')
    clear()
  }

  /** Forgets the User without asking the API, for when it already said the session is gone. */
  function clear(): void {
    settings.value = null
    status.value = 'signedOut'
  }

  return { status, settings, failure, isSignedIn, load, setCurrency, signOut, clear }
})
