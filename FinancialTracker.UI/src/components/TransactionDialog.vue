<script setup lang="ts">
import {
  ElAutocomplete,
  ElFormItem,
  ElInput,
  ElOption,
  ElRadioButton,
  ElRadioGroup,
  ElSelect,
} from 'element-plus'
import { computed, ref } from 'vue'
import type { CategoryOption } from '@/categoryRules'
import { useSubmit } from '@/composables/useSubmit'
import { todayUtc } from '@/dates'
import { positiveAmountProblem } from '@/money'
import type { Account } from '@/stores/accounts'
import { usePayeesStore } from '@/stores/payees'
import { categoryHint, type SaveTransactionRequest, type Transaction, type TransactionDirection } from '@/transactionRules'
import FormDialog from './FormDialog.vue'

const props = defineProps<{
  /** The Transaction being edited; leave out to record a new one. */
  existing?: Transaction
  /** Every Account, closed or not, so the Transaction's own Account is found. */
  accounts: Account[]
  /** Every Category, archived or not, for the same reason. */
  categories: CategoryOption[]
  save: (request: SaveTransactionRequest) => Promise<void>
}>()
const emit = defineEmits<{ close: []; saved: [] }>()

const payees = usePayeesStore()
const today = todayUtc()

const accountId = ref(props.existing?.accountId ?? '')
const date = ref(props.existing?.date ?? today)
const direction = ref<TransactionDirection>(props.existing?.direction ?? 'Out')
const amountText = ref(props.existing === undefined ? '' : String(props.existing.amount))
const categoryId = ref(props.existing?.categoryId ?? '')
const payeeName = ref(props.existing?.payeeId ? (payees.nameOf(props.existing.payeeId) ?? '') : '')
const note = ref(props.existing?.note ?? '')

// A save replaces every field, so a Payee whose name isn't known would be dropped from the Transaction.
const payeeUnknown = Boolean(props.existing?.payeeId) && payeeName.value === ''
// Whether the Category came from the Payee rather than the User, so the next Payee may change it.
const categoryFromPayee = ref(false)

// The API only moves a Transaction to an Account in the same Currency.
const currentCurrency = props.accounts.find((account) => account.id === props.existing?.accountId)?.currency
const accountOptions = computed(() =>
  props.accounts.filter((account) => !account.isClosed && (currentCurrency === undefined || account.currency === currentCurrency)),
)

// An Archived Category stays on the Transactions that have it, but can't be picked for another.
const categoryOptions = computed(() =>
  props.categories.filter((category) => !category.isArchived || category.id === props.existing?.categoryId),
)
const category = computed(() => props.categories.find((candidate) => candidate.id === categoryId.value))
const hint = computed(() => (category.value === undefined ? null : categoryHint(direction.value, category.value)))

const currency = computed(() => props.accounts.find((account) => account.id === accountId.value)?.currency ?? '')
const amountTrimmed = computed(() => amountText.value.trim())
const amountError = computed(() => positiveAmountProblem(amountTrimmed.value, currency.value))
const dateError = computed(() => {
  if (date.value === '') {
    return 'Choose the date of the Transaction.'
  }
  return date.value > today ? 'The date can\'t be after today.' : ''
})
const canSave = computed(
  () =>
    !payeeUnknown &&
    accountId.value !== '' &&
    categoryId.value !== '' &&
    amountTrimmed.value !== '' &&
    amountError.value === '' &&
    dateError.value === '',
)

function suggestPayees(query: string, respond: (suggestions: { value: string }[]) => void): void {
  respond(payees.suggest(query).map((payee) => ({ value: payee.name })))
}

// Only a new Transaction takes the Category last used with the Payee, and never over the User's own choice.
function suggestCategory(name: string): void {
  if (props.existing !== undefined || (categoryId.value !== '' && !categoryFromPayee.value)) {
    return
  }
  const suggested = payees.suggestedCategoryId(name)
  if (suggested !== null && categoryOptions.value.some((option) => option.id === suggested)) {
    categoryId.value = suggested
    categoryFromPayee.value = true
  }
}

function payeePicked(item: Record<string, unknown>): void {
  suggestCategory(String(item.value))
}

function payeeChanged(value: string | number): void {
  suggestCategory(String(value))
}

const { pending, error, submit } = useSubmit(() => {
  const payee = payeeName.value.trim()
  const text = note.value.trim()
  return props.save({
    accountId: accountId.value,
    date: date.value,
    direction: direction.value,
    amount: Number(amountTrimmed.value),
    categoryId: categoryId.value,
    ...(payee === '' ? {} : { payeeName: payee }),
    ...(text === '' ? {} : { note: text }),
  })
})

async function saveTransaction(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="existing ? 'Edit transaction' : 'Record transaction'"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="saveTransaction"
  >
    <ElFormItem label="Account">
      <ElSelect v-model="accountId" placeholder="Choose an Account">
        <ElOption v-for="account in accountOptions" :key="account.id" :value="account.id" :label="account.name" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem label="Date">
      <ElInput v-model="date" type="date" :max="today" />
      <p v-if="dateError" role="status" class="transaction-form__problem">{{ dateError }}</p>
    </ElFormItem>
    <ElFormItem label="Direction">
      <ElRadioGroup v-model="direction">
        <ElRadioButton value="Out">Out</ElRadioButton>
        <ElRadioButton value="In">In</ElRadioButton>
      </ElRadioGroup>
    </ElFormItem>
    <ElFormItem label="Amount">
      <ElInput v-model="amountText" inputmode="decimal">
        <template v-if="currency" #append>{{ currency }}</template>
      </ElInput>
      <p v-if="amountError" role="status" class="transaction-form__problem">{{ amountError }}</p>
    </ElFormItem>
    <ElFormItem label="Category">
      <ElSelect v-model="categoryId" placeholder="Choose a Category" @change="categoryFromPayee = false">
        <ElOption v-for="option in categoryOptions" :key="option.id" :value="option.id" :label="option.label" />
      </ElSelect>
      <p v-if="hint" role="status" class="transaction-hint">{{ hint }}</p>
    </ElFormItem>
    <p v-if="payeeUnknown" role="status" class="transaction-form__problem">
      Its Payee could not be loaded, so it can't be saved without losing it. Close this and try again.
    </p>
    <ElFormItem label="Payee">
      <ElAutocomplete
        v-model="payeeName"
        :fetch-suggestions="suggestPayees"
        :debounce="0"
        trigger-on-focus
        maxlength="100"
        placeholder="Optional; a new name creates the Payee"
        @select="payeePicked"
        @change="payeeChanged"
      />
    </ElFormItem>
    <ElFormItem label="Note">
      <ElInput v-model="note" maxlength="500" />
    </ElFormItem>
  </FormDialog>
</template>

<style scoped>
.transaction-form__problem {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  color: var(--el-color-danger);
  font-size: var(--el-font-size-small);
}

.transaction-hint {
  flex-basis: 100%;
  margin: 0.25rem 0 0;
  color: var(--el-color-info);
  font-size: var(--el-font-size-small);
}
</style>
