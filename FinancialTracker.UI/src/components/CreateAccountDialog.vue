<script setup lang="ts">
import { ElFormItem, ElInput, ElOption, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { accountKinds } from '@/accountKinds'
import { useSubmit } from '@/composables/useSubmit'
import { minorUnits } from '@/money'
import { useAccountsStore, type AccountKind, type CreateAccountRequest } from '@/stores/accounts'
import { useSessionStore } from '@/stores/session'
import CurrencySelect from './CurrencySelect.vue'
import FormDialog from './FormDialog.vue'

const emit = defineEmits<{ close: []; saved: [] }>()

const accounts = useAccountsStore()
const session = useSessionStore()

// Date-only, in UTC, like the API's "not after today" rule.
const today = new Date().toISOString().slice(0, 10)

const name = ref('')
const kind = ref<AccountKind>()
const currency = ref(session.settings?.currency ?? '')
const amountText = ref('')
const date = ref(today)

const isCreditCard = computed(() => kind.value === 'CreditCard')
const amountLabel = computed(() => (isCreditCard.value ? 'Amount owed' : 'Opening balance'))

// Plain digits with an optional decimal part. Anything else (a comma, an exponent) is refused rather than
// guessed at, because a number field would quietly read it as empty and start the Account at zero.
const AMOUNT = /^-?\d+(\.\d+)?$/
const AMOUNT_OWED = /^\d+(\.\d+)?$/

const amountTrimmed = computed(() => amountText.value.trim())
const hasAmount = computed(() => amountTrimmed.value !== '')
const allowedDecimals = computed(() => (currency.value === '' ? 2 : minorUnits(currency.value)))

// The API stays the authority; these hints only save a round trip.
const amountError = computed(() => {
  if (!hasAmount.value) {
    return ''
  }
  if (isCreditCard.value && !AMOUNT_OWED.test(amountTrimmed.value)) {
    return 'Enter the amount owed as a positive number.'
  }
  if (!AMOUNT.test(amountTrimmed.value)) {
    return 'Enter a number such as 1250.50.'
  }
  const decimals = amountTrimmed.value.split('.')[1]?.length ?? 0
  return decimals > allowedDecimals.value ? `${currency.value} has ${allowedDecimals.value} decimal places.` : ''
})

const dateError = computed(() => {
  if (!hasAmount.value) {
    return ''
  }
  if (date.value === '') {
    return 'Choose the date this balance is as of.'
  }
  return date.value > today ? 'The date can\'t be after today.' : ''
})

const canSave = computed(
  () =>
    name.value.trim() !== '' &&
    kind.value !== undefined &&
    currency.value !== '' &&
    amountError.value === '' &&
    dateError.value === '',
)

const { pending, error, submit } = useSubmit(() => {
  if (kind.value === undefined) {
    return Promise.resolve()
  }
  const request: CreateAccountRequest = { name: name.value, kind: kind.value, currency: currency.value }
  if (hasAmount.value) {
    // The form asks a Credit Card's balance as the amount owed; the API keeps money owed negative.
    const entered = Number(amountTrimmed.value)
    request.openingBalance = { amount: isCreditCard.value ? -entered : entered, date: date.value }
  }
  return accounts.create(request)
})

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    title="New account"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="save"
  >
    <ElFormItem label="Name">
      <ElInput v-model="name" maxlength="100" />
    </ElFormItem>
    <ElFormItem label="Kind">
      <ElSelect v-model="kind" placeholder="Choose a kind">
        <ElOption v-for="option in accountKinds" :key="option.value" :value="option.value" :label="option.label" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem label="Currency">
      <CurrencySelect v-model="currency" />
    </ElFormItem>
    <ElFormItem :label="amountLabel">
      <ElInput v-model="amountText" inputmode="decimal" placeholder="Optional" />
      <p v-if="amountError" role="status" class="create-account__hint">{{ amountError }}</p>
    </ElFormItem>
    <ElFormItem label="As of">
      <ElInput v-model="date" type="date" :max="today" />
      <p v-if="dateError" role="status" class="create-account__hint">{{ dateError }}</p>
    </ElFormItem>
  </FormDialog>
</template>

<style scoped>
.create-account__hint {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  color: var(--el-color-danger);
  font-size: var(--el-font-size-small);
}
</style>
