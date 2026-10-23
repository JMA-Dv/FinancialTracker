import { computed, ref, watch, type ComputedRef, type Ref } from 'vue'
import { api } from '@/api/client'
import { useLoaded, type Loaded } from '@/composables/useLoaded'
import { todayUtc } from '@/dates'
import { rangeFor, type DateRange, type PeriodPreset } from '@/period'
import {
  byCurrency,
  netWorthPath,
  rangePath,
  type CategoryBreakdown,
  type CurrencyReport,
  type IncomeSpending,
  type NetWorth,
  type PayeeSpending,
} from '@/reports'

export type Period = PeriodPreset | 'custom'

export interface Dashboard {
  period: Ref<Period>
  /** The dates of a custom period, as typed. */
  custom: Ref<DateRange>
  /** The date net worth is as of; empty means today. */
  asOf: Ref<string>
  /** What is wrong with the custom range or the net worth date, or empty. No report is asked for while it is wrong. */
  periodProblem: ComputedRef<string>
  asOfProblem: ComputedRef<string>
  netWorth: Loaded<NetWorth | null>
  incomeSpending: Loaded<IncomeSpending | null>
  categories: Loaded<CategoryBreakdown | null>
  payees: Loaded<PayeeSpending | null>
  currencies: ComputedRef<CurrencyReport[]>
  load: () => Promise<void>
}

/**
 * The reports behind the dashboard. Each loads on its own, so one failing leaves the rest showing, and a change of
 * period or date fetches only the reports that depend on it.
 */
export function useDashboard(): Dashboard {
  const today = todayUtc()
  const period = ref<Period>('this-month')
  const custom = ref<DateRange>({ from: '', to: '' })
  const asOf = ref('')

  const periodProblem = computed(() => {
    if (period.value !== 'custom' || custom.value.from === '' || custom.value.to === '') {
      return ''
    }
    return custom.value.from > custom.value.to ? 'The start can\'t be after the end.' : ''
  })
  const asOfProblem = computed(() => (asOf.value > today ? 'The date can\'t be after today.' : ''))

  // The range that was last asked for; a custom range that is incomplete or wrong asks for nothing new.
  const range = ref<DateRange>(rangeFor('this-month', today))
  const requested = computed<DateRange | null>(() => {
    if (period.value !== 'custom') {
      return rangeFor(period.value, today)
    }
    const { from, to } = custom.value
    return from !== '' && to !== '' && periodProblem.value === '' ? { from, to } : null
  })

  const netWorth = useLoaded<NetWorth | null>(() => api.get<NetWorth>(netWorthPath(asOf.value)), null)
  const incomeSpending = useLoaded<IncomeSpending | null>(
    () => api.get<IncomeSpending>(rangePath('income-spending', range.value)),
    null,
  )
  const categories = useLoaded<CategoryBreakdown | null>(
    () => api.get<CategoryBreakdown>(rangePath('categories', range.value)),
    null,
  )
  const payees = useLoaded<PayeeSpending | null>(() => api.get<PayeeSpending>(rangePath('payees', range.value)), null)

  const currencies = computed(() =>
    byCurrency(netWorth.data.value, incomeSpending.data.value, categories.data.value, payees.data.value),
  )

  async function load(): Promise<void> {
    await Promise.all([netWorth, incomeSpending, categories, payees].map((report) => report.load()))
  }

  watch(requested, (next) => {
    if (next !== null) {
      range.value = next
      void Promise.all([incomeSpending, categories, payees].map((report) => report.load()))
    }
  })
  watch(asOf, () => {
    if (asOfProblem.value === '') {
      void netWorth.load()
    }
  })

  return { period, custom, asOf, periodProblem, asOfProblem, netWorth, incomeSpending, categories, payees, currencies, load }
}
