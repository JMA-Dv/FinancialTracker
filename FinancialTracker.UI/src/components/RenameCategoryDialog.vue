<script setup lang="ts">
import { ElAlert, ElButton, ElDialog, ElForm, ElFormItem, ElInput } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'

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
  <ElDialog :model-value="true" :title="`Rename ${node.name}`" width="min(28rem, 92vw)" @close="emit('close')">
    <ElForm label-position="top" @submit.prevent="save">
      <ElAlert v-if="error" type="error" show-icon :closable="false" :title="error" class="dialog-error" />
      <ElFormItem label="Name">
        <ElInput v-model="name" maxlength="100" />
      </ElFormItem>
      <ElButton type="primary" native-type="submit" :loading="pending" :disabled="!canSave">Save</ElButton>
      <ElButton @click="emit('close')">Cancel</ElButton>
    </ElForm>
  </ElDialog>
</template>

<style scoped>
.dialog-error {
  margin-bottom: 1rem;
}
</style>
