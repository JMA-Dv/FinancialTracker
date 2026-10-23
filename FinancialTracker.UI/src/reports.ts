import type { CategoryKind } from '@/stores/categories'
import type { DateRange } from './period'

export interface CurrencyTotal {
  currency: string
  amount: number
}

export interface NetWorth {
  date: string
  totals: CurrencyTotal[]
}

export interface IncomeSpendingTotal {
  currency: string
  income: number
  spending: number
}

export interface IncomeSpending extends DateRange {
  totals: IncomeSpendingTotal[]
}

/** A Category in the breakdown; a parent's `total` includes its children, `ownAmount` leaves them out. */
export interface CategoryTotal {
  id: string
  name: string
  kind: CategoryKind
  isArchived: boolean
  total: number
  ownAmount: number
  children: CategoryTotal[]
}

export interface CategoryBreakdown extends DateRange {
  currencies: { currency: string; categories: CategoryTotal[] }[]
}

/** Spending with one Payee; `payeeId` and `name` are null for spending that names none. */
export interface PayeeTotal {
  payeeId: string | null
  name: string | null
  spending: number
}

export interface PayeeSpending extends DateRange {
  currencies: { currency: string; payees: PayeeTotal[] }[]
}

export function netWorthPath(asOf: string): string {
  return asOf === '' ? '/api/reports/net-worth' : `/api/reports/net-worth?date=${asOf}`
}

export function rangePath(report: 'income-spending' | 'categories' | 'payees', range: DateRange): string {
  return `/api/reports/${report}?from=${range.from}&to=${range.to}`
}

/** Everything the reports say about one Currency. Totals in different Currencies are never combined. */
export interface CurrencyReport {
  currency: string
  netWorth: number | null
  incomeSpending: IncomeSpendingTotal | null
  categories: CategoryTotal[]
  /** The period the categories and payees were worked out for, so links name it even while another period loads. */
  categoriesRange: DateRange | null
  payees: PayeeTotal[]
  payeesRange: DateRange | null
}

/** The reports regrouped by Currency, in code order. A report that has not arrived contributes nothing. */
export function byCurrency(
  netWorth: NetWorth | null,
  incomeSpending: IncomeSpending | null,
  categories: CategoryBreakdown | null,
  payees: PayeeSpending | null,
): CurrencyReport[] {
  const codes = new Set([
    ...(netWorth?.totals ?? []).map((total) => total.currency),
    ...(incomeSpending?.totals ?? []).map((total) => total.currency),
    ...(categories?.currencies ?? []).map((entry) => entry.currency),
    ...(payees?.currencies ?? []).map((entry) => entry.currency),
  ])
  return [...codes].sort().map((currency) => ({
    currency,
    netWorth: netWorth?.totals.find((total) => total.currency === currency)?.amount ?? null,
    incomeSpending: incomeSpending?.totals.find((total) => total.currency === currency) ?? null,
    categories: categories?.currencies.find((entry) => entry.currency === currency)?.categories ?? [],
    categoriesRange: categories === null ? null : { from: categories.from, to: categories.to },
    payees: payees?.currencies.find((entry) => entry.currency === currency)?.payees ?? [],
    payeesRange: payees === null ? null : { from: payees.from, to: payees.to },
  }))
}
