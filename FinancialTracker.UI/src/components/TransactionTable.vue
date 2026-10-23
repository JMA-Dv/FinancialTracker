<script setup lang="ts">
import { ElButton, ElTag } from 'element-plus'
import { computed } from 'vue'
import type { CategoryOption } from '@/categoryRules'
import { formatEffect } from '@/money'
import type { Account } from '@/stores/accounts'
import { usePayeesStore } from '@/stores/payees'
import { isRefund, signedAmount, type Transaction } from '@/transactionRules'

const props = defineProps<{
  transactions: Transaction[]
  /** Every Account, closed or not. */
  accounts: Account[]
  categories: CategoryOption[]
}>()
const emit = defineEmits<{ edit: [transaction: Transaction]; delete: [transaction: Transaction] }>()

const payees = usePayeesStore()

interface Row {
  transaction: Transaction
  account: Account | undefined
  category: CategoryOption | undefined
  payee: string | null
  /** What names the Transaction in the names of its buttons. */
  description: string
  refund: boolean
}

const rows = computed<Row[]>(() =>
  props.transactions.map((transaction) => {
    const category = props.categories.find((candidate) => candidate.id === transaction.categoryId)
    const payee = transaction.payeeId === null ? null : payees.nameOf(transaction.payeeId)
    return {
      transaction,
      account: props.accounts.find((candidate) => candidate.id === transaction.accountId),
      category,
      payee,
      description: `${payee ?? category?.label ?? 'Transaction'} on ${transaction.date}`,
      refund: category !== undefined && isRefund(transaction.direction, category.kind),
    }
  }),
)

// Money can't move on a Closed Account, so its Transactions are read-only.
const anyLocked = computed(() => rows.value.some((row) => row.account?.isClosed === true))
</script>

<template>
  <p v-if="anyLocked" id="transactions-locked-reason">
    Transactions on a Closed Account can't be changed. Reopen the Account first.
  </p>
  <div class="transactions-scroll">
    <table class="transactions">
      <caption class="transactions__caption">Transactions, newest first</caption>
      <thead>
        <tr>
          <th scope="col">Date</th>
          <th scope="col">Payee</th>
          <th scope="col">Category</th>
          <th scope="col">Account</th>
          <th scope="col">Amount</th>
          <th scope="col">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.transaction.id">
          <td>{{ row.transaction.date }}</td>
          <td>
            {{ row.payee }}
            <span v-if="row.transaction.note" class="transactions__note">{{ row.transaction.note }}</span>
          </td>
          <td>
            {{ row.category?.label }}
            <ElTag v-if="row.refund" type="success" size="small" disable-transitions>Refund</ElTag>
          </td>
          <td>
            {{ row.account?.name }}
            <ElTag v-if="row.account?.isClosed" type="info" size="small" disable-transitions>Closed</ElTag>
          </td>
          <td>
            <span class="transactions__amount">
              {{ row.account ? formatEffect(signedAmount(row.transaction), row.account.currency) : row.transaction.amount }}
            </span>
            <span class="transactions__direction">{{ row.transaction.direction }}</span>
          </td>
          <td>
            <ElButton
              link
              :disabled="row.account?.isClosed"
              :aria-describedby="row.account?.isClosed ? 'transactions-locked-reason' : undefined"
              :aria-label="`Edit ${row.description}`"
              @click="emit('edit', row.transaction)"
            >
              Edit
            </ElButton>
            <ElButton
              link
              :disabled="row.account?.isClosed"
              :aria-describedby="row.account?.isClosed ? 'transactions-locked-reason' : undefined"
              :aria-label="`Delete ${row.description}`"
              @click="emit('delete', row.transaction)"
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
.transactions-scroll {
  overflow-x: auto;
}

.transactions {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
}

.transactions__caption {
  text-align: left;
  font-weight: 600;
  padding-bottom: 0.5rem;
}

.transactions th,
.transactions td {
  padding: 0.5rem 0.75rem 0.5rem 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-variant-numeric: tabular-nums;
}

.transactions__note,
.transactions__direction {
  display: block;
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
}
</style>
