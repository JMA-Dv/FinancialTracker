<script setup lang="ts">
import { ElFormItem, ElOption, ElSelect } from 'element-plus'
import { computed, ref } from 'vue'
import { moveTargets } from '@/categoryRules'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'
import FormDialog from './FormDialog.vue'

const props = defineProps<{ node: CategoryNode; parent: CategoryNode | null }>()
const emit = defineEmits<{ close: []; saved: [] }>()

// Stands for "no parent" in the select, which can't hold null.
const TOP_LEVEL = 'top-level'

const categories = useCategoriesStore()
const targets = computed(() => moveTargets(props.node, categories.tree))

// An Archived parent is not a place to move to, so a child under one starts with no choice made.
const currentParentId = props.parent !== null && !props.parent.isArchived ? props.parent.id : ''
const target = ref(currentParentId)
const canSave = computed(() => target.value !== '' && target.value !== currentParentId)

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
  <FormDialog
    :title="`Move ${node.name}`"
    :pending="pending"
    :can-save="canSave"
    :error="error"
    @close="emit('close')"
    @submit="save"
  >
    <ElFormItem label="Move under">
      <ElSelect v-model="target" placeholder="Choose a Category">
        <ElOption v-if="parent" :value="TOP_LEVEL" label="Top level" />
        <ElOption
          v-for="candidate in targets"
          :key="candidate.id"
          :value="candidate.id"
          :label="`${candidate.name} (${candidate.kind})`"
        />
      </ElSelect>
    </ElFormItem>
  </FormDialog>
</template>
