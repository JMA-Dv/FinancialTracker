<script setup lang="ts">
import { ElFormItem, ElInput, ElOption, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { accountKinds } from '@/accountKinds'
import { useSubmit } from '@/composables/useSubmit'
import { currencyOptions } from '@/currencies'
import { minorUnits } from '@/money'
import { useAccountsStore, type AccountKind, type CreateAccountRequest } from '@/stores/accounts'
import { useSessionStore } from '@/stores/session'
import FormDialog from './FormDialog.vue'

const emit = defineEmits<{ close: []; saved: [] }>()

const accounts = useAccountsStore()
const session = useSessionStore()
const currencies = currencyOptions()

// Date-only, in UTC, like the API's "not after today" rule.
const today = new Date().toISOString().slice(0, 10)

const name = ref('')
const kind = ref<AccountKind>()
const currency = ref(session.settings?.currency ?? '')
const amountText = ref('')
const date = ref(today)

const isCreditCard = computed(() => kind.value === 'CreditCard')
const amountLabel = computed(() => (isCreditCard.value ? 'Amount owed' : 'Opening balance'))

const hasAmount = computed(() => amountText.value.trim() !== '')
const amount = computed(() => Number(amountText.value))
const decimals = computed(() => amountText.value.split('.')[1]?.length ?? 0)
const allowedDecimals = computed(() => (currency.value === '' ? 2 : minorUnits(currency.value)))
const amountError = computed(() =>
  decimals.value > allowedDecimals.value ? `${currency.value} has ${allowedDecimals.value} decimal places.` : '',
)

const canSave = computed(
  () =>
    name.value.trim() !== '' &&
    kind.value !== undefined &&
    currency.value !== '' &&
    amountError.value === '' &&
    (!hasAmount.value || (Number.isFinite(amount.value) && date.value !== '')),
)

const { pending, error, submit } = useSubmit(() => {
  if (kind.value === undefined) {
    return Promise.resolve()
  }
  const request: CreateAccountRequest = { name: name.value, kind: kind.value, currency: currency.value }
  if (hasAmount.value) {
    // The form asks a Credit Card's balance as the amount owed; the API keeps money owed negative.
    const signed = isCreditCard.value ? -Math.abs(amount.value) : amount.value
    request.openingBalance = { amount: signed, date: date.value }
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
      <ElSelect v-model="currency" filterable placeholder="Search by code or name">
        <ElOption v-for="option in currencies" :key="option.code" :value="option.code" :label="option.label" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem :label="amountLabel">
      <ElInput v-model="amountText" type="number" :step="10 ** -allowedDecimals" placeholder="Optional" />
      <p v-if="amountError" role="status" class="create-account__hint">{{ amountError }}</p>
    </ElFormItem>
    <ElFormItem label="As of">
      <ElInput v-model="date" type="date" :max="today" />
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
