import type { AccountKind } from '@/stores/accounts'

export interface AccountKindOption {
  value: AccountKind
  label: string
}

export const accountKinds: AccountKindOption[] = [
  { value: 'Checking', label: 'Checking' },
  { value: 'Savings', label: 'Savings' },
  { value: 'Cash', label: 'Cash' },
  { value: 'CreditCard', label: 'Credit card' },
]
