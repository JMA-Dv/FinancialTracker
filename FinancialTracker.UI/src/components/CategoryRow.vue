<script setup lang="ts">
import { ElButton, ElTag } from 'element-plus'
import { computed } from 'vue'
import { moveObstacle } from '@/categoryRules'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'
import type { CategoryAction } from './categoryAction'

const props = defineProps<{ node: CategoryNode; parent: CategoryNode | null }>()
const emit = defineEmits<{ action: [action: CategoryAction] }>()

const categories = useCategoriesStore()
const moveBlockedBecause = computed(() => moveObstacle(props.node, props.parent, categories.tree))
</script>

<template>
  <div class="category-row">
    <span :class="{ 'category-row__name--archived': node.isArchived }">{{ node.name }}</span>
    <ElTag v-if="node.isArchived" type="info" size="small" disable-transitions>Archived</ElTag>
    <span v-if="moveBlockedBecause" :id="`move-note-${node.id}`" class="category-row__note">{{ moveBlockedBecause }}</span>
    <span class="category-row__actions">
      <ElButton link :aria-label="`Rename ${node.name}`" @click="emit('action', { type: 'rename', node })">Rename</ElButton>
      <ElButton
        link
        :aria-label="`Move ${node.name}`"
        :aria-describedby="moveBlockedBecause ? `move-note-${node.id}` : undefined"
        :disabled="moveBlockedBecause !== null"
        @click="emit('action', { type: 'move', node, parent })"
      >
        Move
      </ElButton>
      <ElButton link :aria-label="`Archive ${node.name}`" v-if="!node.isArchived" @click="emit('action', { type: 'archive', node })">
        Archive
      </ElButton>
      <ElButton link :aria-label="`Unarchive ${node.name}`" v-else @click="emit('action', { type: 'unarchive', node })">
        Unarchive
      </ElButton>
    </span>
  </div>
</template>

<style scoped>
.category-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.25rem 0.5rem;
  padding: 0.375rem 0;
}

.category-row__name--archived {
  color: var(--el-text-color-secondary);
}

.category-row__note {
  color: var(--el-text-color-secondary);
  font-size: var(--el-font-size-small);
}

.category-row__actions {
  display: inline-flex;
  flex-wrap: wrap;
  margin-left: auto;
}
</style>
