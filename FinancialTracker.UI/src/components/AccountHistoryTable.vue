<script setup lang="ts">
import { ElButton } from 'element-plus'
import { formatEffect, formatMoney } from '@/money'
import { adjustmentName, type HistoryEntry } from '@/composables/useAccountHistory'

const props = defineProps<{
  entries: HistoryEntry[]
  currency: string
  /** The id of the Opening Balance, the earliest Balance Adjustment. */
  openingId: string | null
  /** A Closed Account keeps its history but accepts no change. */
  locked: boolean
}>()
const emit = defineEmits<{ edit: [entry: HistoryEntry]; delete: [entry: HistoryEntry] }>()

/** What the entry is called, in the table and in the names of its buttons. */
function label(entry: HistoryEntry): string {
  if (entry.kind !== 'BalanceAdjustment') {
    return entry.kind
  }
  return adjustmentName(entry.id === props.openingId)
}
</script>

<template>
  <p>A Balance Adjustment states what the Account really held. The difference it makes is never income or spending.</p>
  <div class="history-scroll">
    <table class="history">
      <caption class="history__caption">History, newest first</caption>
      <thead>
        <tr>
          <th scope="col">Date</th>
          <th scope="col">What</th>
          <th scope="col">Effect</th>
          <th scope="col">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="entry in entries" :key="`${entry.kind}-${entry.id}`">
          <td>{{ entry.date }}</td>
          <td>
            {{ label(entry) }}
            <span v-if="entry.statedBalance !== null" class="history__stated">
              States {{ formatMoney(entry.statedBalance, currency) }}
            </span>
          </td>
          <td>{{ formatEffect(entry.amount, currency) }}</td>
          <td>
            <template v-if="entry.kind === 'BalanceAdjustment'">
              <ElButton
                link
                :disabled="locked"
                aria-describedby="account-closed-reason"
                :aria-label="`Edit ${label(entry)} of ${entry.date}`"
                @click="emit('edit', entry)"
              >
                Edit
              </ElButton>
              <ElButton
                link
                :disabled="locked"
                aria-describedby="account-closed-reason"
                :aria-label="`Delete ${label(entry)} of ${entry.date}`"
                @click="emit('delete', entry)"
              >
                Delete
              </ElButton>
            </template>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.history-scroll {
  overflow-x: auto;
}

.history {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.history__caption {
  text-align: left;
  font-weight: 600;
  padding-bottom: 0.5rem;
}

.history th,
.history td {
  padding: 0.5rem 0.75rem 0.5rem 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-variant-numeric: tabular-nums;
}

.history__stated {
  display: block;
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
}
</style>
