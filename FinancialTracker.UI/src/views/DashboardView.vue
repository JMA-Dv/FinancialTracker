<script setup lang="ts">
import { ElFormItem, ElInput } from 'element-plus'
import { computed, onMounted } from 'vue'
import CurrencySection from '@/components/CurrencySection.vue'
import LoadFailure from '@/components/LoadFailure.vue'
import PeriodPicker from '@/components/PeriodPicker.vue'
import { useDashboard } from '@/composables/useDashboard'
import { todayUtc } from '@/dates'

const dashboard = useDashboard()
const today = todayUtc()

const reports = [
  { name: 'Net worth', report: dashboard.netWorth },
  { name: 'Income and spending', report: dashboard.incomeSpending },
  { name: 'Spending by Category', report: dashboard.categories },
  { name: 'Spending by Payee', report: dashboard.payees },
]

// Until something has arrived there is nothing to say about a Currency, and nothing to tell there are none.
const nothingYet = computed(() => dashboard.currencies.value.length === 0 && reports.every(({ report }) => report.status.value === 'ready'))

// A Currency can only be said to have nothing in the period once every report of the period has arrived.
const periodReady = computed(() =>
  [dashboard.incomeSpending, dashboard.categories, dashboard.payees].every((report) => report.data.value !== null),
)

// Figures from the previous period stay up while the new ones load.
const updating = computed(() => reports.some(({ report }) => report.status.value === 'loading' && report.data.value !== null))

onMounted(() => dashboard.load())
</script>

<template>
  <section class="dashboard">
    <h1>Dashboard</h1>

    <div class="dashboard__toolbar">
      <PeriodPicker v-model:period="dashboard.period.value" v-model:custom="dashboard.custom.value" :problem="dashboard.periodProblem.value" />
      <ElFormItem label="Net worth as of" class="dashboard__as-of">
        <ElInput v-model="dashboard.asOf.value" type="date" :max="today" />
        <p v-if="dashboard.asOfProblem.value" role="status" class="dashboard__problem">{{ dashboard.asOfProblem.value }}</p>
      </ElFormItem>
    </div>

    <template v-for="{ name, report } in reports" :key="name">
      <LoadFailure
        v-if="report.status.value === 'failed'"
        :title="`Could not load ${name}`"
        :description="report.failure.value"
        @retry="report.load()"
      />
      <p v-else-if="report.status.value === 'idle' || (report.status.value === 'loading' && report.data.value === null)" role="status">
        Loading {{ name }}…
      </p>
    </template>

    <p v-if="updating" role="status" class="dashboard__updating">Updating…</p>

    <p v-if="nothingYet">Nothing to show yet. Open an Account and record Transactions to see where your money goes.</p>

    <CurrencySection
      v-for="currency in dashboard.currencies.value"
      :key="currency.currency"
      :report="currency"
      :net-worth-date="dashboard.netWorth.data.value?.date ?? null"
      :period-ready="periodReady"
    />
  </section>
</template>

<style scoped>
/* A soft tinted backdrop so the white cards lift off the page. */
.dashboard {
  padding: 1.5rem;
  border-radius: 1.75rem;
  background: linear-gradient(160deg, #f3f1ff 0%, #f4faff 55%, #fff4f8 100%);
  font-family: var(--el-font-family);
}

.dashboard h1 {
  margin-top: 0;
  color: #1f1b4d;
}

.dashboard__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 0 2rem;
  margin-bottom: 1rem;
}

.dashboard__as-of {
  flex-direction: column;
  margin-bottom: 0.5rem;
}

.dashboard__updating {
  margin: 0 0 1rem;
  color: #5d6272;
}

.dashboard__problem {
  margin: 0.25rem 0 0;
  color: var(--el-color-danger);
  font-size: var(--el-font-size-small);
}
</style>
