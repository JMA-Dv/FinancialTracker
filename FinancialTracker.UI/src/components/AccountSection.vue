<script setup lang="ts">
import type { Account } from '@/stores/accounts'
import type { AccountAction } from './accountAction'
import AccountRow from './AccountRow.vue'

defineProps<{ id: string; title: string; emptyText: string; accounts: Account[] }>()
const emit = defineEmits<{ action: [action: AccountAction] }>()
</script>

<template>
  <section :aria-labelledby="id" class="account-section">
    <h2 :id="id">{{ title }}</h2>
    <p v-if="accounts.length === 0">{{ emptyText }}</p>
    <ul v-else class="account-list">
      <li v-for="account in accounts" :key="account.id">
        <AccountRow :account="account" @action="emit('action', $event)" />
      </li>
    </ul>
  </section>
</template>

<style scoped>
.account-list {
  list-style: none;
  margin: 0;
  padding: 0;
}
</style>
