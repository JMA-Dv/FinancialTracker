<script setup lang="ts">
import { ElFormItem, ElInput, ElOption, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import type { SaveTransferRequest, Transfer } from '@/composables/useTransfers'
import { todayUtc } from '@/dates'
import { positiveAmountProblem } from '@/money'
import type { Account } from '@/stores/accounts'
import FormDialog from './FormDialog.vue'

const props = defineProps<{
  /** The Transfer being edited; leave out to record a new one. */
  existing?: Transfer
  /** Every Account, closed or not, so the Transfer's own Accounts are found. */
  accounts: Account[]
  save: (request: SaveTransferRequest) => Promise<void>
}>()
const emit = defineEmits<{ close: []; saved: [] }>()

const today = todayUtc()

const fromId = ref(props.existing?.fromAccountId ?? '')
const toId = ref(props.existing?.toAccountId ?? '')
const date = ref(props.existing?.date ?? today)
const amountText = ref(props.existing === undefined ? '' : String(props.existing.amount))
const receivedText = ref(props.existing === undefined ? '' : String(props.existing.receivedAmount))
const note = ref(props.existing?.note ?? '')

const openAccounts = computed(() => props.accounts.filter((account) => !account.isClosed))
const from = computed(() => props.accounts.find((account) => account.id === fromId.value))
const to = computed(() => props.accounts.find((account) => account.id === toId.value))

// Nothing is converted, so between Currencies the User says how much arrived.
const crossCurrency = computed(() => from.value !== undefined && to.value !== undefined && from.value.currency !== to.value.currency)

const sameAccount = computed(() => fromId.value !== '' && fromId.value === toId.value)
const amountTrimmed = computed(() => amountText.value.trim())
const receivedTrimmed = computed(() => receivedText.value.trim())
const amountError = computed(() => positiveAmountProblem(amountTrimmed.value, from.value?.currency ?? ''))
const receivedError = computed(() => positiveAmountProblem(receivedTrimmed.value, to.value?.currency ?? ''))
const dateError = computed(() => {
  if (date.value === '') {
    return 'Choose the date of the Transfer.'
  }
  return date.value > today ? 'The date can\'t be after today.' : ''
})

const canSave = computed(
  () =>
    from.value !== undefined &&
    to.value !== undefined &&
    !sameAccount.value &&
    amountTrimmed.value !== '' &&
    amountError.value === '' &&
    (!crossCurrency.value || (receivedTrimmed.value !== '' && receivedError.value === '')) &&
    dateError.value === '',
)

const { pending, error, submit } = useSubmit(() => {
  const text = note.value.trim()
  return props.save({
    fromAccountId: fromId.value,
    toAccountId: toId.value,
    date: date.value,
    amount: Number(amountTrimmed.value),
    ...(crossCurrency.value ? { receivedAmount: Number(receivedTrimmed.value) } : {}),
    ...(text === '' ? {} : { note: text }),
  })
})

async function saveTransfer(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="existing ? 'Edit transfer' : 'Record transfer'"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="saveTransfer"
  >
    <p>
      A Transfer moves money between your own Accounts and is never income or spending. Paying off a Credit Card is a
      Transfer to it from the Account you paid with: it is not spending.
    </p>
    <ElFormItem label="From">
      <ElSelect v-model="fromId" placeholder="Choose an Account">
        <ElOption v-for="account in openAccounts" :key="account.id" :value="account.id" :label="account.name" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem label="To">
      <ElSelect v-model="toId" placeholder="Choose an Account">
        <ElOption v-for="account in openAccounts" :key="account.id" :value="account.id" :label="account.name" />
      </ElSelect>
      <p v-if="sameAccount" role="status" class="transfer-form__problem">Choose two different Accounts.</p>
    </ElFormItem>
    <ElFormItem label="Date">
      <ElInput v-model="date" type="date" :max="today" />
      <p v-if="dateError" role="status" class="transfer-form__problem">{{ dateError }}</p>
    </ElFormItem>
    <ElFormItem label="Amount sent">
      <ElInput v-model="amountText" inputmode="decimal">
        <template v-if="from" #append>{{ from.currency }}</template>
      </ElInput>
      <p v-if="amountError" role="status" class="transfer-form__problem">{{ amountError }}</p>
    </ElFormItem>
    <ElFormItem v-if="crossCurrency && to" :label="`Amount received in ${to.currency}`">
      <ElInput v-model="receivedText" inputmode="decimal">
        <template #append>{{ to.currency }}</template>
      </ElInput>
      <p v-if="receivedError" role="status" class="transfer-form__problem">{{ receivedError }}</p>
      <p class="transfer-form__note">
        Nothing is converted: the rate is implied by the two amounts and never stored.
      </p>
    </ElFormItem>
    <ElFormItem label="Note">
      <ElInput v-model="note" maxlength="500" />
    </ElFormItem>
  </FormDialog>
</template>

<style scoped>
.transfer-form__problem,
.transfer-form__note {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  font-size: var(--el-font-size-small);
}

.transfer-form__problem {
  color: var(--el-color-danger);
}

.transfer-form__note {
  color: var(--el-text-color-secondary);
}
</style>
