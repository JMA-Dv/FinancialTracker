<script setup lang="ts">
import { ElFormItem } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { useAccountsStore, type Account } from '@/stores/accounts'
import CurrencySelect from './CurrencySelect.vue'
import FormDialog from './FormDialog.vue'

const props = defineProps<{ account: Account }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const accounts = useAccountsStore()
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
      <CurrencySelect v-model="currency" />
    </ElFormItem>
  </FormDialog>
</template>
