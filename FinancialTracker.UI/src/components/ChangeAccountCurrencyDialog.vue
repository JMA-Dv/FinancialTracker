<script setup lang="ts">
import { ElFormItem, ElOption, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { currencyOptions } from '@/currencies'
import { useAccountsStore, type Account } from '@/stores/accounts'
import FormDialog from './FormDialog.vue'

const props = defineProps<{ account: Account }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const accounts = useAccountsStore()
const currencies = currencyOptions()
const currency = ref(props.account.currency)
const canSave = computed(() => currency.value !== props.account.currency)

const { pending, error, submit } = useSubmit(() => accounts.changeCurrency(props.account.id, currency.value))

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="`Change currency of ${account.name}`"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="save"
  >
    <p>Only possible until the Account has Transactions or Transfers. Nothing is converted.</p>
    <ElFormItem label="Currency">
      <ElSelect v-model="currency" filterable placeholder="Search by code or name">
        <ElOption v-for="option in currencies" :key="option.code" :value="option.code" :label="option.label" />
      </ElSelect>
    </ElFormItem>
  </FormDialog>
</template>
