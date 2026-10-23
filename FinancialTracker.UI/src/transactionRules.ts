import type { LocationQuery } from 'vue-router'
import type { CategoryKind } from '@/stores/categories'

export type TransactionDirection = 'In' | 'Out'

/** A Transaction as `GET /api/transactions` returns it. */
export interface Transaction {
  id: string
  accountId: string
  date: string
  direction: TransactionDirection
  /** Positive, in the Account's Currency; `direction` says which way it moves. */
  amount: number
  categoryId: string
  payeeId: string | null
  note: string | null
}

/** What the API replaces every field with on an edit. */
export interface SaveTransactionRequest {
  accountId: string
  date: string
  direction: TransactionDirection
  amount: number
  categoryId: string
  /** An unknown name creates the Payee; leave it out for none. */
  payeeName?: string
  note?: string
}

/** Which Transactions to list; an empty string means no restriction. The same fields the URL query carries. */
export interface TransactionFilter {
  accountId: string
  categoryId: string
  payeeId: string
  from: string
  to: string
}

// The order the API path and the URL list the filters in.
const filterKeys: (keyof TransactionFilter)[] = ['accountId', 'categoryId', 'payeeId', 'from', 'to']

function firstValue(value: LocationQuery[string] | undefined): string {
  const first = Array.isArray(value) ? value[0] : value
  return first ?? ''
}

/** The filters a URL query asks for; anything else in it is ignored. */
export function filterFromQuery(query: LocationQuery): TransactionFilter {
  return {
    accountId: firstValue(query.accountId),
    categoryId: firstValue(query.categoryId),
    payeeId: firstValue(query.payeeId),
    from: firstValue(query.from),
    to: firstValue(query.to),
  }
}

/** The URL query for a filter, leaving out what is not restricted. */
export function queryFromFilter(filter: TransactionFilter): Record<string, string> {
  return Object.fromEntries(filterKeys.filter((key) => filter[key] !== '').map((key) => [key, filter[key]]))
}

export function isFiltered(filter: TransactionFilter): boolean {
  return filterKeys.some((key) => filter[key] !== '')
}

/** The API path listing the Transactions that match. */
export function transactionsPath(filter: TransactionFilter): string {
  const query = new URLSearchParams(queryFromFilter(filter)).toString()
  return query === '' ? '/api/transactions' : `/api/transactions?${query}`
}

/** The amount as it changes the balance: negative for Out. */
export function signedAmount(transaction: Pick<Transaction, 'direction' | 'amount'>): number {
  return transaction.direction === 'In' ? transaction.amount : -transaction.amount
}

/** A Refund is money coming back in an Expense Category. */
export function isRefund(direction: TransactionDirection, kind: CategoryKind): boolean {
  return direction === 'In' && kind === 'Expense'
}

/** What the User should know about the direction and Category they picked, or null when it is ordinary. */
export function categoryHint(direction: TransactionDirection, category: { name: string; kind: CategoryKind }): string | null {
  if (isRefund(direction, category.kind)) {
    return `This is a Refund: it reduces spending in ${category.name} and is not income.`
  }
  return direction === 'Out' && category.kind === 'Income' ? `This reduces income in ${category.name}.` : null
}
