<script setup lang="ts">
import { ElFormItem, ElInput, ElOption, ElSelect } from 'element-plus'
import type { CategoryOption } from '@/categoryRules'
import type { Account } from '@/stores/accounts'
import type { Payee } from '@/stores/payees'
import type { TransactionFilter } from '@/transactionRules'

const props = defineProps<{
  filter: TransactionFilter
  /** Every Account, closed or not, so a link to a Closed Account's Transactions shows it. */
  accounts: Account[]
  categories: CategoryOption[]
  payees: Payee[]
}>()
const emit = defineEmits<{ change: [filter: TransactionFilter] }>()

// A cleared select answers with something other than text.
function set(key: keyof TransactionFilter, value: unknown): void {
  emit('change', { ...props.filter, [key]: typeof value === 'string' ? value : '' })
}
</script>

<template>
  <form class="transaction-filters" aria-label="Filter Transactions" @submit.prevent>
    <ElFormItem label="Account">
      <ElSelect :model-value="filter.accountId" clearable placeholder="Any" @update:model-value="set('accountId', $event)">
        <ElOption
          v-for="account in accounts"
          :key="account.id"
          :value="account.id"
          :label="account.isClosed ? `${account.name} (closed)` : account.name"
        />
      </ElSelect>
    </ElFormItem>
    <ElFormItem label="Category">
      <ElSelect :model-value="filter.categoryId" clearable placeholder="Any" @update:model-value="set('categoryId', $event)">
        <ElOption v-for="category in categories" :key="category.id" :value="category.id" :label="category.label" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem label="Payee">
      <ElSelect :model-value="filter.payeeId" clearable filterable placeholder="Any" @update:model-value="set('payeeId', $event)">
        <ElOption v-for="payee in payees" :key="payee.id" :value="payee.id" :label="payee.name" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem label="From">
      <ElInput :model-value="filter.from" type="date" @update:model-value="set('from', $event)" />
    </ElFormItem>
    <ElFormItem label="To">
      <ElInput :model-value="filter.to" type="date" @update:model-value="set('to', $event)" />
    </ElFormItem>
  </form>
</template>

<style scoped>
.transaction-filters {
  display: flex;
  flex-wrap: wrap;
  gap: 0 1rem;
}

.transaction-filters .el-form-item {
  flex: 1 1 10rem;
  flex-direction: column;
  margin-bottom: 0.75rem;
}
</style>
