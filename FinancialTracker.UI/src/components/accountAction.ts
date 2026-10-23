import type { Account } from '@/stores/accounts'

/** What the User asked to do to an Account in the list. */
export interface AccountAction {
  type: 'rename' | 'change-currency' | 'close' | 'reopen'
  account: Account
}
