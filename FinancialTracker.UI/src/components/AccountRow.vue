<script setup lang="ts">
import { ElButton, ElTag } from 'element-plus'
import { formatMoney } from '@/money'
import type { Account } from '@/stores/accounts'
import type { AccountAction } from './accountAction'

defineProps<{ account: Account }>()
const emit = defineEmits<{ action: [action: AccountAction] }>()
</script>

<template>
  <div class="account-row">
    <RouterLink
      :to="{ name: 'account', params: { id: account.id } }"
      class="account-row__name"
      :class="{ 'account-row__name--closed': account.isClosed }"
    >
      {{ account.name }}
    </RouterLink>
    <ElTag v-if="account.isClosed" type="info" size="small" disable-transitions>Closed</ElTag>
    <span class="account-row__balance">{{ formatMoney(account.balance, account.currency) }}</span>
    <span class="account-row__actions">
      <template v-if="!account.isClosed">
        <ElButton link :aria-label="`Rename ${account.name}`" @click="emit('action', { type: 'rename', account })">
          Rename
        </ElButton>
        <ElButton
          link
          :aria-label="`Change currency of ${account.name}`"
          @click="emit('action', { type: 'change-currency', account })"
        >
          Change currency
        </ElButton>
        <ElButton link :aria-label="`Close ${account.name}`" @click="emit('action', { type: 'close', account })">
          Close
        </ElButton>
      </template>
      <ElButton v-else link :aria-label="`Reopen ${account.name}`" @click="emit('action', { type: 'reopen', account })">
        Reopen
      </ElButton>
    </span>
  </div>
</template>

<style scoped>
.account-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.25rem 0.75rem;
  padding: 0.5rem 0;
}

.account-row__name--closed {
  color: var(--el-text-color-secondary);
}

.account-row__balance {
  margin-left: auto;
  font-variant-numeric: tabular-nums;
}

.account-row__actions {
  display: inline-flex;
  flex-wrap: wrap;
}
</style>
