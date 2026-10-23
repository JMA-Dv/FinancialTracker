<script setup lang="ts">
import { ElFormItem, ElInput } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'
import FormDialog from './FormDialog.vue'

const props = defineProps<{ node: CategoryNode }>()
const emit = defineEmits<{ close: []; saved: [] }>()

const categories = useCategoriesStore()
const name = ref(props.node.name)
const canSave = computed(() => name.value.trim() !== '')

const { pending, error, submit } = useSubmit(() => categories.rename(props.node.id, name.value))

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    :title="`Rename ${node.name}`"
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
