<script setup lang="ts">
import { computed } from 'vue'
import { currencyName, formatMoney } from '@/money'
import type { CurrencyReport } from '@/reports'
import BarChart from './BarChart.vue'
import ReportCategories from './ReportCategories.vue'
import ReportPayees from './ReportPayees.vue'

const props = defineProps<{
  report: CurrencyReport
  /** The date net worth is as of. */
  netWorthDate: string | null
  /** Whether the period's reports have arrived, so a Currency with nothing in them can say so. */
  periodReady: boolean
}>()

// A chart of everything would be unreadable; the table beside it has every row.
const CHART_ROWS = 8

const id = computed(() => `currency-${props.report.currency}`)
const spending = computed(() => props.report.categories.filter((category) => category.kind === 'Expense'))
const income = computed(() => props.report.categories.filter((category) => category.kind === 'Income'))
const topSpending = computed(() => [...spending.value].sort((a, b) => b.total - a.total).slice(0, CHART_ROWS))
const topPayees = computed(() => props.report.payees.slice(0, CHART_ROWS))

const noTransactions = computed(
  () =>
    props.periodReady &&
    props.report.incomeSpending === null &&
    props.report.categories.length === 0 &&
    props.report.payees.length === 0,
)
</script>

<template>
  <section :aria-labelledby="id" class="currency-section">
    <h2 :id="id" class="currency-section__heading">
      <span class="currency-section__chip" aria-hidden="true">{{ report.currency }}</span>
      {{ currencyName(report.currency) }}
    </h2>

    <div class="currency-section__cards">
      <article v-if="report.netWorth !== null" class="card">
        <h3 class="card__label">Net worth</h3>
        <p class="card__figure">{{ formatMoney(report.netWorth, report.currency) }}</p>
        <p class="card__note">as of {{ netWorthDate }}</p>
      </article>
      <article v-if="report.incomeSpending" class="card">
        <h3 class="card__label">Income and spending</h3>
        <dl class="card__pair">
          <div>
            <dt>Income</dt>
            <dd class="card__figure">{{ formatMoney(report.incomeSpending.income, report.currency) }}</dd>
          </div>
          <div>
            <dt>Spending</dt>
            <dd class="card__figure">{{ formatMoney(report.incomeSpending.spending, report.currency) }}</dd>
          </div>
        </dl>
        <p class="card__note">Transfers and Balance Adjustments are not counted. A Refund reduces spending.</p>
      </article>
    </div>

    <p v-if="noTransactions">No Transactions in this period.</p>

    <section v-if="spending.length > 0" :aria-labelledby="`${id}-spending`" class="card">
      <h3 :id="`${id}-spending`" class="card__label">Spending by Category</h3>
      <BarChart
        :labels="topSpending.map((category) => category.name)"
        :values="topSpending.map((category) => category.total)"
        :currency="report.currency"
        :label="`Spending by Category in ${report.currency}`"
      />
      <ReportCategories caption="Spending by Category" :categories="spending" :currency="report.currency" :range="report.categoriesRange" />
    </section>

    <section v-if="income.length > 0" :aria-labelledby="`${id}-income`" class="card">
      <h3 :id="`${id}-income`" class="card__label">Income by Category</h3>
      <ReportCategories caption="Income by Category" :categories="income" :currency="report.currency" :range="report.categoriesRange" />
    </section>

    <section v-if="report.payees.length > 0" :aria-labelledby="`${id}-payees`" class="card">
      <h3 :id="`${id}-payees`" class="card__label">Spending by Payee</h3>
      <BarChart
        :labels="topPayees.map((payee) => payee.name ?? 'No Payee')"
        :values="topPayees.map((payee) => payee.spending)"
        :currency="report.currency"
        :label="`Spending by Payee in ${report.currency}`"
      />
      <ReportPayees :payees="report.payees" :currency="report.currency" :range="report.payeesRange" />
    </section>
  </section>
</template>

<style>
/* The dashboard's look: soft white cards, a purple-to-blue accent, small muted labels. Not scoped, so the tables of
   its child components share it. */
.currency-section {
  --accent-from: #7c6cf0;
  --accent-to: #4fb4f5;
  --accent-link: #5b4de0;
  /* Darker than Element Plus's secondary grey, which is too faint for 12px labels on these cards. */
  --muted: #5d6272;
  --ink: #1f1b4d;
  margin-bottom: 2.5rem;
}

.currency-section__heading {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: var(--ink);
}

.currency-section__chip {
  display: inline-grid;
  place-items: center;
  width: 3.25rem;
  height: 2.75rem;
  border-radius: 0.875rem;
  background: linear-gradient(135deg, var(--accent-from), var(--accent-to));
  box-shadow: 0 8px 18px rgb(124 108 240 / 35%);
  color: #fff;
  font-size: 0.8rem;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.currency-section__cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr));
  gap: 1.25rem;
  margin-bottom: 1.25rem;
}

.currency-section .card {
  margin-bottom: 1.25rem;
  padding: 1.25rem 1.5rem;
  border-radius: 1.25rem;
  background: #fff;
  box-shadow: 0 12px 32px rgb(99 102 241 / 10%);
}

.currency-section__cards .card {
  margin-bottom: 0;
}

.currency-section .card__label {
  margin: 0 0 0.75rem;
  color: var(--muted);
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.currency-section .card__figure {
  margin: 0;
  color: var(--ink);
  font-size: 1.75rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}

.currency-section .card__note {
  margin: 0.5rem 0 0;
  color: var(--muted);
  font-size: var(--el-font-size-small);
}

.currency-section .card__pair {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 2rem;
  margin: 0;
}

.currency-section .card__pair dt {
  color: var(--muted);
  font-size: var(--el-font-size-small);
}

.currency-section .card__pair dd {
  margin: 0;
}

.currency-section a {
  color: var(--accent-link);
  text-decoration: underline;
  text-underline-offset: 0.2em;
}

.currency-section .report-scroll {
  overflow-x: auto;
}

.currency-section .report-table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

/* The heading above names the table; the caption is for screen readers. */
.currency-section .report-table__caption {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip-path: inset(50%);
}

.currency-section .report-table th,
.currency-section .report-table td {
  padding: 0.5rem 0.75rem 0.5rem 0;
  border-bottom: 1px solid var(--el-border-color-extra-light);
  font-variant-numeric: tabular-nums;
}

.currency-section .report-table th {
  color: var(--muted);
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.currency-section .report-table__child td:first-child {
  padding-left: 2rem;
}
</style>
