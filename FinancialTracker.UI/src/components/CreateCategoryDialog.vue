<script setup lang="ts">
import { ElAlert, ElButton, ElDialog, ElForm, ElFormItem, ElInput, ElOption, ElRadio, ElRadioGroup, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryKind, type CreateCategoryRequest } from '@/stores/categories'

const emit = defineEmits<{ close: []; saved: [] }>()

const categories = useCategoriesStore()
const name = ref('')
const parentId = ref('')
const kind = ref<CategoryKind | ''>('')

// Only top-level, unarchived Categories can take children: there are two levels at most.
const parents = computed(() => categories.tree.filter((node) => !node.isArchived))

const canSave = computed(() => name.value.trim() !== '' && (parentId.value !== '' || kind.value !== ''))

const { pending, error, submit } = useSubmit(async () => {
  const request: CreateCategoryRequest =
    parentId.value === '' ? { name: name.value, kind: kind.value as CategoryKind } : { name: name.value, parentId: parentId.value }
  await categories.create(request)
})

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <ElDialog :model-value="true" title="New category" width="min(28rem, 92vw)" @close="emit('close')">
    <ElForm label-position="top" @submit.prevent="save">
      <ElAlert v-if="error" type="error" show-icon :closable="false" :title="error" class="dialog-error" />
      <ElFormItem label="Name">
        <ElInput v-model="name" maxlength="100" />
      </ElFormItem>
      <ElFormItem label="Parent">
        <ElSelect v-model="parentId" clearable placeholder="None, a top-level Category">
          <ElOption v-for="parent in parents" :key="parent.id" :value="parent.id" :label="`${parent.name} (${parent.kind})`" />
        </ElSelect>
      </ElFormItem>
      <!-- A child takes its parent's kind, so only a top-level Category asks. -->
      <ElFormItem v-if="parentId === ''" label="Kind">
        <ElRadioGroup v-model="kind">
          <ElRadio value="Income">Income</ElRadio>
          <ElRadio value="Expense">Expense</ElRadio>
        </ElRadioGroup>
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
