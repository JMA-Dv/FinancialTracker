<script setup lang="ts">
import { formatMoney } from '@/money'
import type { DateRange } from '@/period'
import type { PayeeTotal } from '@/reports'

defineProps<{ payees: PayeeTotal[]; currency: string; range: DateRange | null }>()
</script>

<template>
  <div class="report-scroll">
    <table class="report-table">
      <caption class="report-table__caption">Spending by Payee</caption>
      <thead>
        <tr>
          <th scope="col">Payee</th>
          <th scope="col">Spent</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="payee in payees" :key="payee.payeeId ?? 'no-payee'">
          <td>
            <!-- Spending without a Payee has no id to filter the Transactions by. -->
            <RouterLink v-if="payee.payeeId !== null" :to="{ path: '/transactions', query: { payeeId: payee.payeeId, ...range } }">
              {{ payee.name }}
            </RouterLink>
            <span v-else>No Payee</span>
          </td>
          <td>{{ formatMoney(payee.spending, currency) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
