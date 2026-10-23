<script setup lang="ts">
import { ElButton, ElTag } from 'element-plus'
import { computed } from 'vue'
import type { Transfer } from '@/composables/useTransfers'
import { formatMoney } from '@/money'
import type { Account } from '@/stores/accounts'

const props = defineProps<{
  transfers: Transfer[]
  /** Every Account, closed or not. */
  accounts: Account[]
}>()
const emit = defineEmits<{ edit: [transfer: Transfer]; delete: [transfer: Transfer] }>()

interface Row {
  transfer: Transfer
  from: Account | undefined
  to: Account | undefined
  /** What names the Transfer in the names of its buttons. */
  description: string
  /** Money can't move on a Closed Account, so a Transfer touching one is read-only. */
  locked: boolean
}

const rows = computed<Row[]>(() =>
  props.transfers.map((transfer) => {
    const from = props.accounts.find((account) => account.id === transfer.fromAccountId)
    const to = props.accounts.find((account) => account.id === transfer.toAccountId)
    return {
      transfer,
      from,
      to,
      description: `transfer from ${from?.name ?? 'an Account'} to ${to?.name ?? 'an Account'} on ${transfer.date}`,
      locked: from?.isClosed === true || to?.isClosed === true,
    }
  }),
)

const anyLocked = computed(() => rows.value.some((row) => row.locked))
</script>

<template>
  <p v-if="anyLocked" id="transfers-locked-reason">
    Transfers touching a Closed Account can't be changed. Reopen the Account first.
  </p>
  <div class="transfers-scroll">
    <table class="transfers">
      <caption class="transfers__caption">Transfers, newest first</caption>
      <thead>
        <tr>
          <th scope="col">Date</th>
          <th scope="col">From</th>
          <th scope="col">To</th>
          <th scope="col">Sent</th>
          <th scope="col">Received</th>
          <th scope="col">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.transfer.id">
          <td>{{ row.transfer.date }}</td>
          <td>
            {{ row.from?.name ?? 'an Account' }}
            <ElTag v-if="row.from?.isClosed" type="info" size="small" disable-transitions>Closed</ElTag>
          </td>
          <td>
            {{ row.to?.name ?? 'an Account' }}
            <ElTag v-if="row.to?.isClosed" type="info" size="small" disable-transitions>Closed</ElTag>
            <span v-if="row.transfer.note" class="transfers__note">{{ row.transfer.note }}</span>
          </td>
          <td>{{ row.from ? formatMoney(row.transfer.amount, row.from.currency) : row.transfer.amount }}</td>
          <td>{{ row.to ? formatMoney(row.transfer.receivedAmount, row.to.currency) : row.transfer.receivedAmount }}</td>
          <td>
            <ElButton
              link
              :disabled="row.locked"
              :aria-describedby="row.locked ? 'transfers-locked-reason' : undefined"
              :aria-label="`Edit ${row.description}`"
              @click="emit('edit', row.transfer)"
            >
              Edit
            </ElButton>
            <ElButton
              link
              :disabled="row.locked"
              :aria-describedby="row.locked ? 'transfers-locked-reason' : undefined"
              :aria-label="`Delete ${row.description}`"
              @click="emit('delete', row.transfer)"
            >
              Delete
            </ElButton>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.transfers-scroll {
  overflow-x: auto;
}

.transfers {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.transfers__caption {
  text-align: left;
  font-weight: 600;
  padding-bottom: 0.5rem;
}

.transfers th,
.transfers td {
  padding: 0.5rem 0.75rem 0.5rem 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-variant-numeric: tabular-nums;
}

.transfers__note {
  display: block;
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
}
</style>
