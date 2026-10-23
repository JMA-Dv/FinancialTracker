<script setup lang="ts">
import { ElButton, ElForm, ElFormItem, ElInput } from 'element-plus'
import { computed, ref, watch } from 'vue'
import { api } from '@/api/client'
import { useSubmit } from '@/composables/useSubmit'
import { todayUtc } from '@/dates'
import { formatMoney } from '@/money'
import type { Account } from '@/stores/accounts'
import ErrorAlert from './ErrorAlert.vue'

interface BalanceOnDateResponse {
  date: string
  balance: number
}

const props = defineProps<{ account: Account }>()

const today = todayUtc()
const date = ref(today)
const answer = ref<BalanceOnDateResponse | null>(null)

const dateError = computed(() => (date.value > today ? 'The date can\'t be after today.' : ''))
const canAsk = computed(() => date.value !== '' && dateError.value === '')

const { pending, error, submit } = useSubmit(async () => {
  answer.value = await api.get<BalanceOnDateResponse>(`/api/accounts/${props.account.id}/balance?date=${date.value}`)
})

// A new balance (a Balance Adjustment was saved) may change what an earlier answer said.
watch(
  () => props.account,
  () => {
    answer.value = null
  },
)

async function ask(): Promise<void> {
  answer.value = null
  await submit()
}
</script>

<template>
  <section aria-labelledby="balance-on-date-heading" class="balance-on-date">
    <h2 id="balance-on-date-heading">What did it hold on a date?</h2>
    <ElForm inline @submit.prevent="ask">
      <ElFormItem label="Date">
        <ElInput v-model="date" type="date" :max="today" />
        <p v-if="dateError" role="status" class="balance-on-date__hint">{{ dateError }}</p>
      </ElFormItem>
      <ElButton type="primary" native-type="submit" :loading="pending" :disabled="!canAsk">Show balance</ElButton>
    </ElForm>
    <ErrorAlert v-if="error" :title="error" />
    <p v-if="answer" role="status" data-testid="balance-on-date">
      {{ account.name }} held {{ formatMoney(answer.balance, account.currency) }} at the end of {{ answer.date }}.
    </p>
  </section>
</template>

<style scoped>
.balance-on-date__hint {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  color: var(--el-color-danger);
  font-size: var(--el-font-size-small);
}
</style>
