<script setup lang="ts">
import { ElFormItem, ElInput, ElOption, ElRadio, ElRadioGroup, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryKind } from '@/stores/categories'
import FormDialog from './FormDialog.vue'

const emit = defineEmits<{ close: []; saved: [] }>()

const categories = useCategoriesStore()
const name = ref('')
const parentId = ref('')
const kind = ref<CategoryKind>()

// Only top-level, unarchived Categories can take children: there are two levels at most.
const parents = computed(() => categories.tree.filter((node) => !node.isArchived))

const canSave = computed(() => name.value.trim() !== '' && (parentId.value !== '' || kind.value !== undefined))

// A child takes its parent's kind, so only a top-level Category sends one.
const { pending, error, submit } = useSubmit(() =>
  categories.create(
    parentId.value === '' ? { name: name.value, kind: kind.value } : { name: name.value, parentId: parentId.value },
  ),
)

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <FormDialog
    title="New category"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="save"
  >
    <ElFormItem label="Name">
      <ElInput v-model="name" maxlength="100" />
    </ElFormItem>
    <ElFormItem label="Parent">
      <ElSelect v-model="parentId" clearable placeholder="None, a top-level Category">
        <ElOption v-for="parent in parents" :key="parent.id" :value="parent.id" :label="`${parent.name} (${parent.kind})`" />
      </ElSelect>
    </ElFormItem>
    <ElFormItem v-if="parentId === ''" label="Kind">
      <ElRadioGroup v-model="kind">
        <ElRadio value="Income">Income</ElRadio>
        <ElRadio value="Expense">Expense</ElRadio>
      </ElRadioGroup>
    </ElFormItem>
  </FormDialog>
</template>
