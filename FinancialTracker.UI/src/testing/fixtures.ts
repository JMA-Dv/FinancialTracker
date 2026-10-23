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

/** A Payee as `GET /api/payees` returns it. */
export interface FixturePayee {
  id: string
  name: string
  suggestedCategoryId: string | null
}

export const corner: FixturePayee = { id: 'corner', name: 'Corner Market', suggestedCategoryId: 'groceries' }

export const landlord: FixturePayee = { id: 'landlord', name: 'Landlord', suggestedCategoryId: null }

/** A Transaction as `GET /api/transactions` returns it; `direction` is a string because the test types it loosely. */
export interface FixtureTransaction {
  id: string
  accountId: string
  date: string
  direction: string
  amount: number
  categoryId: string
  payeeId: string | null
  note: string | null
}

export const shopping: FixtureTransaction = {
  id: 'shopping',
  accountId: 'checking',
  date: '2026-10-05',
  direction: 'Out',
  amount: 40.5,
  categoryId: 'groceries',
  payeeId: 'corner',
  note: 'Weekly shop',
}

/** In, in an Expense Category: a Refund. */
export const refund: FixtureTransaction = {
  id: 'refund',
  accountId: 'checking',
  date: '2026-10-03',
  direction: 'In',
  amount: 12,
  categoryId: 'groceries',
  payeeId: 'corner',
  note: null,
}

export const pay: FixtureTransaction = {
  id: 'pay',
  accountId: 'checking',
  date: '2026-10-01',
  direction: 'In',
  amount: 2000,
  categoryId: 'salary',
  payeeId: null,
  note: null,
}

/** On an Account that is closed. */
export const oldPurchase: FixtureTransaction = {
  id: 'old-purchase',
  accountId: 'old-savings',
  date: '2026-08-01',
  direction: 'Out',
  amount: 5,
  categoryId: 'groceries',
  payeeId: null,
  note: null,
}

/** A Transfer as `GET /api/transfers` returns it. */
export interface FixtureTransfer {
  id: string
  fromAccountId: string
  toAccountId: string
  date: string
  amount: number
  receivedAmount: number
  note: string | null
}

/** Paying off the Credit Card: both Accounts in USD. */
export const cardPayment: FixtureTransfer = {
  id: 'card-payment',
  fromAccountId: 'checking',
  toAccountId: 'visa',
  date: '2026-10-05',
  amount: 100,
  receivedAmount: 100,
  note: 'October bill',
}

/** USD into a MXN Account. */
export const toWallet: FixtureTransfer = {
  id: 'to-wallet',
  fromAccountId: 'checking',
  toAccountId: 'wallet',
  date: '2026-10-03',
  amount: 50,
  receivedAmount: 900,
  note: null,
}

/** Out of an Account that is closed. */
export const fromOldSavings: FixtureTransfer = {
  id: 'from-old-savings',
  fromAccountId: 'old-savings',
  toAccountId: 'checking',
  date: '2026-08-01',
  amount: 5,
  receivedAmount: 5,
  note: null,
}

/** A Category in `GET /api/reports/categories`. */
export interface FixtureCategoryTotal {
  id: string
  name: string
  kind: string
  isArchived: boolean
  total: number
  ownAmount: number
  children: FixtureCategoryTotal[]
}

export const salaryTotal: FixtureCategoryTotal = {
  id: 'salary',
  name: 'Salary',
  kind: 'Income',
  isArchived: false,
  total: 2000,
  ownAmount: 2000,
  children: [],
}

export const marketTotal: FixtureCategoryTotal = {
  id: 'market',
  name: 'Market',
  kind: 'Expense',
  isArchived: false,
  total: 200,
  ownAmount: 200,
  children: [],
}

export const groceriesTotal: FixtureCategoryTotal = {
  id: 'groceries',
  name: 'Groceries',
  kind: 'Expense',
  isArchived: false,
  total: 300,
  ownAmount: 100,
  children: [marketTotal],
}

export const travelTotal: FixtureCategoryTotal = {
  id: 'travel',
  name: 'Travel',
  kind: 'Expense',
  isArchived: true,
  total: 150.25,
  ownAmount: 150.25,
  children: [],
}
