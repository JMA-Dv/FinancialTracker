<script setup lang="ts">
import { ElFormItem, ElInput } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import FormDialog from './FormDialog.vue'

/** What can be renamed: a Category, an Account or a Payee. */
interface Renamable {
  id: string
  name: string
}

const props = defineProps<{ target: Renamable; rename: (id: string, name: string) => Promise<void> }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const name = ref(props.target.name)
const canSave = computed(() => name.value.trim() !== '')

const { pending, error, submit } = useSubmit(() => props.rename(props.target.id, name.value))

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="`Rename ${target.name}`"
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
