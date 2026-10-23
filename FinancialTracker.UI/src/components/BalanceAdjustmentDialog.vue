<script setup lang="ts">
import { ElFormItem, ElInput } from 'element-plus'
import { computed, ref } from 'vue'
import { adjustmentName, type BalanceAdjustmentRequest, type HistoryEntry } from '@/composables/useAccountHistory'
import { useSubmit } from '@/composables/useSubmit'
import { todayUtc } from '@/dates'
import { amountProblem } from '@/money'
import type { Account } from '@/stores/accounts'
import FormDialog from './FormDialog.vue'

const props = defineProps<{
  account: Account
  /** The Balance Adjustment being edited; leave out to add one. */
  existing?: HistoryEntry
  /** Whether the Balance Adjustment is the Opening Balance, for the title. */
  opening?: boolean
  save: (request: BalanceAdjustmentRequest) => Promise<void>
}>()
const emit = defineEmits<{ close: []; saved: [] }>()

const today = todayUtc()

// The API keeps money owed negative, but a Credit Card's statement says how much is owed.
const asOwed = computed(() => props.account.isLiability)
const amountLabel = computed(() => (asOwed.value ? 'Amount owed' : 'Balance'))

function amountTextFor(stated: number | null | undefined): string {
  if (stated === null || stated === undefined) {
    return ''
  }
  return String(asOwed.value ? -stated : stated)
}

const amountText = ref(amountTextFor(props.existing?.statedBalance))
const date = ref(props.existing?.date ?? today)

const amountTrimmed = computed(() => amountText.value.trim())
const amountError = computed(() => amountProblem(amountTrimmed.value, props.account.currency))
const dateError = computed(() => {
  if (date.value === '') {
    return 'Choose the date this balance is as of.'
  }
  return date.value > today ? 'The date can\'t be after today.' : ''
})
const canSave = computed(() => amountTrimmed.value !== '' && amountError.value === '' && dateError.value === '')

const title = computed(() => `${props.existing ? 'Edit' : 'Add'} ${adjustmentName(props.opening === true)}`)

const { pending, error, submit } = useSubmit(() => {
  const entered = Number(amountTrimmed.value)
  return props.save({ date: date.value, balance: asOwed.value ? -entered : entered })
})

async function saveAdjustment(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="title"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="saveAdjustment"
  >
    <p>
      State what {{ account.name }} really held at the end of the date. The difference from what the tracker showed is
      never income or spending.
    </p>
    <ElFormItem :label="amountLabel">
      <ElInput v-model="amountText" inputmode="decimal" />
      <p v-if="asOwed" class="adjustment__note">Use a negative number if the Account is in credit.</p>
      <p v-if="amountError" role="status" class="adjustment__hint">{{ amountError }}</p>
    </ElFormItem>
    <ElFormItem label="As of">
      <ElInput v-model="date" type="date" :max="today" />
      <p v-if="dateError" role="status" class="adjustment__hint">{{ dateError }}</p>
    </ElFormItem>
  </FormDialog>
</template>

<style scoped>
.adjustment__note {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
}

.adjustment__hint {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  color: var(--el-color-danger);
  font-size: var(--el-font-size-small);
}
</style>
