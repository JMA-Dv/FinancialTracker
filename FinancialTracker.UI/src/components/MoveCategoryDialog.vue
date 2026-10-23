<script setup lang="ts">
import { ElAlert, ElButton, ElDialog, ElForm, ElFormItem, ElOption, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { moveTargets } from '@/categoryRules'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'

const props = defineProps<{ node: CategoryNode; parent: CategoryNode | null }>()
const emit = defineEmits<{ close: []; saved: [] }>()

// Stands for "no parent" in the select, which can't hold null.
const TOP_LEVEL = 'top-level'

const categories = useCategoriesStore()
const targets = computed(() => moveTargets(props.node, categories.tree))
const target = ref(props.parent?.id ?? '')
const canSave = computed(() => target.value !== '' && target.value !== (props.parent?.id ?? ''))

const { pending, error, submit } = useSubmit(() =>
  categories.move(props.node.id, target.value === TOP_LEVEL ? null : target.value),
)

async function save(): Promise<void> {
  if (await submit()) {
    emit('saved')
  }
}
</script>

<template>
  <ElDialog :model-value="true" :title="`Move ${node.name}`" width="min(28rem, 92vw)" @close="emit('close')">
    <ElForm label-position="top" @submit.prevent="save">
      <ElAlert v-if="error" type="error" show-icon :closable="false" :title="error" class="dialog-error" />
      <ElFormItem label="Move under">
        <ElSelect v-model="target" placeholder="Choose a Category">
          <ElOption v-if="parent" :value="TOP_LEVEL" label="Top level" />
          <ElOption v-for="candidate in targets" :key="candidate.id" :value="candidate.id" :label="`${candidate.name} (${candidate.kind})`" />
        </ElSelect>
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
