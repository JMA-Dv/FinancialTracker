/** A Category as `GET /api/categories` returns it; `kind` is a string because the test types it loosely. */
export interface FixtureCategory {
  id: string
  name: string
  kind: string
  isArchived: boolean
  children: FixtureCategory[]
}

export const salary: FixtureCategory = { id: 'salary', name: 'Salary', kind: 'Income', isArchived: false, children: [] }

export const market: FixtureCategory = { id: 'market', name: 'Market', kind: 'Expense', isArchived: false, children: [] }

export const groceries: FixtureCategory = {
  id: 'groceries',
  name: 'Groceries',
  kind: 'Expense',
  isArchived: false,
  children: [market],
}

export const flights: FixtureCategory = { id: 'flights', name: 'Flights', kind: 'Expense', isArchived: false, children: [] }

/** Archived, with a child that is not archived itself. */
export const travel: FixtureCategory = { id: 'travel', name: 'Travel', kind: 'Expense', isArchived: true, children: [flights] }

/** An Account as `GET /api/accounts` returns it. */
export interface FixtureAccount {
  id: string
  name: string
  kind: string
  currency: string
  balance: number
  isLiability: boolean
  isClosed: boolean
  openingBalance: { amount: number; date: string } | null
}

export const checking: FixtureAccount = {
  id: 'checking',
  name: 'Everyday',
  kind: 'Checking',
  currency: 'USD',
  balance: 1250.5,
  isLiability: false,
  isClosed: false,
  openingBalance: null,
}

export const wallet: FixtureAccount = {
  id: 'wallet',
  name: 'Wallet',
  kind: 'Cash',
  currency: 'MXN',
  balance: 3000,
  isLiability: false,
  isClosed: false,
  openingBalance: null,
}

export const visa: FixtureAccount = {
  id: 'visa',
  name: 'Visa',
  kind: 'CreditCard',
  currency: 'USD',
  balance: -300,
  isLiability: true,
  isClosed: false,
  openingBalance: null,
}

export const oldSavings: FixtureAccount = {
  id: 'old-savings',
  name: 'Old savings',
  kind: 'Savings',
  currency: 'USD',
  balance: 0,
  isLiability: false,
  isClosed: true,
  openingBalance: null,
}

/** An entry of `GET /api/accounts/{id}/history`; `kind` is a string because the test types it loosely. */
export interface FixtureHistoryEntry {
  kind: string
  id: string
  date: string
  amount: number
  statedBalance: number | null
}

/** Newest first, as the API returns them. */
export const checkingHistory: FixtureHistoryEntry[] = [
  { kind: 'Transaction', id: 'rent', date: '2026-10-05', amount: -400, statedBalance: null },
  { kind: 'BalanceAdjustment', id: 'recount', date: '2026-10-01', amount: 20, statedBalance: 90 },
  { kind: 'Transfer', id: 'to-savings', date: '2026-09-20', amount: 50, statedBalance: null },
  { kind: 'BalanceAdjustment', id: 'opening', date: '2026-09-01', amount: 70, statedBalance: 70 },
]
