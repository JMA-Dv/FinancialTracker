<script setup lang="ts">
import { ElFormItem, ElInput } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { useAccountsStore, type Account } from '@/stores/accounts'
import FormDialog from './FormDialog.vue'

const props = defineProps<{ account: Account }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const accounts = useAccountsStore()
const name = ref(props.account.name)
const canSave = computed(() => name.value.trim() !== '')

const { pending, error, submit } = useSubmit(() => accounts.rename(props.account.id, name.value))

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="`Rename ${account.name}`"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="save"
  >
    <ElFormItem label="Name">
      <ElInput v-model="name" maxlength="100" />
    </ElFormItem>
  </FormDialog>
</template>
