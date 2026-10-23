<script setup lang="ts">
import { ElAlert, ElButton, ElSwitch } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import CategorySection from '@/components/CategorySection.vue'
import type { CategoryAction } from '@/components/categoryAction'
import CreateCategoryDialog from '@/components/CreateCategoryDialog.vue'
import MoveCategoryDialog from '@/components/MoveCategoryDialog.vue'
import RenameCategoryDialog from '@/components/RenameCategoryDialog.vue'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'

const categories = useCategoriesStore()
const creating = ref(false)
const renaming = ref<CategoryNode | null>(null)
const moving = ref<{ node: CategoryNode; parent: CategoryNode | null } | null>(null)

const income = computed(() => categories.tree.filter((node) => node.kind === 'Income'))
const expense = computed(() => categories.tree.filter((node) => node.kind === 'Expense'))

const archiving = useSubmit((id: string, archived: boolean) => categories.setArchived(id, archived))

async function perform(action: CategoryAction): Promise<void> {
  if (action.type === 'rename') {
    renaming.value = action.node
  } else if (action.type === 'move') {
    moving.value = { node: action.node, parent: action.parent }
  } else if (action.type === 'archive' || action.type === 'unarchive') {
    await archiving.submit(action.node.id, action.type === 'archive')
  }
}

onMounted(() => categories.load())
</script>

<template>
  <section>
    <h1>Categories</h1>

    <div class="categories__toolbar">
      <ElButton type="primary" @click="creating = true">New category</ElButton>
    <label class="categories__archived">
      <ElSwitch :model-value="categories.includeArchived" @change="categories.setIncludeArchived(Boolean($event))" />
      Show archived
    </label>
    </div>

    <ElAlert v-if="archiving.error.value" type="error" show-icon :closable="false" :title="archiving.error.value" class="categories__alert" />

    <p v-if="categories.status === 'idle' || (categories.status === 'loading' && categories.tree.length === 0)" role="status">
      Loading Categories…
    </p>
    <template v-else-if="categories.status === 'failed'">
      <ElAlert type="error" show-icon :closable="false" title="Could not load your Categories" :description="categories.failure" />
      <ElButton class="categories__retry" @click="categories.load()">Try again</ElButton>
    </template>
    <template v-else>
      <CategorySection kind="Income" :nodes="income" @action="perform" />
      <CategorySection kind="Expense" :nodes="expense" @action="perform" />
    </template>
    <CreateCategoryDialog v-if="creating" @close="creating = false" @saved="creating = false" />
    <MoveCategoryDialog v-if="moving" :node="moving.node" :parent="moving.parent" @close="moving = null" @saved="moving = null" />
    <RenameCategoryDialog v-if="renaming" :node="renaming" @close="renaming = null" @saved="renaming = null" />
  </section>
</template>

<style scoped>
.categories__toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem 1.5rem;
  margin-bottom: 1rem;
}

.categories__archived {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.categories__alert {
  margin-bottom: 1rem;
}

.categories__retry {
  margin-top: 1rem;
}
</style>
