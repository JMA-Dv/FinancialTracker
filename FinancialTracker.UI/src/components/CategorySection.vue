<script setup lang="ts">
import type { CategoryKind, CategoryNode } from '@/stores/categories'
import CategoryRow from './CategoryRow.vue'
import type { CategoryAction } from './categoryAction'

defineProps<{ kind: CategoryKind; nodes: CategoryNode[] }>()
const emit = defineEmits<{ action: [action: CategoryAction] }>()
</script>

<template>
  <section :aria-labelledby="`${kind.toLowerCase()}-heading`" class="category-section">
    <h2 :id="`${kind.toLowerCase()}-heading`">{{ kind }}</h2>
    <p v-if="nodes.length === 0">No {{ kind }} Categories yet.</p>
    <ul v-else class="category-list">
      <li v-for="parent in nodes" :key="parent.id">
        <CategoryRow :node="parent" :parent="null" @action="emit('action', $event)" />
        <ul v-if="parent.children.length > 0" class="category-list category-list--children">
          <li v-for="child in parent.children" :key="child.id">
            <CategoryRow :node="child" :parent="parent" @action="emit('action', $event)" />
          </li>
        </ul>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.category-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.category-list--children {
  padding-left: 1.5rem;
}
</style>
