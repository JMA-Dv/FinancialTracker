<script setup lang="ts">
import { ElButton, ElSwitch } from 'element-plus'
import { computed, onMounted, ref } from 'vue'
import CategorySection from '@/components/CategorySection.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import type { CategoryAction } from '@/components/categoryAction'
import CreateCategoryDialog from '@/components/CreateCategoryDialog.vue'
import MoveCategoryDialog from '@/components/MoveCategoryDialog.vue'
import RenameDialog from '@/components/RenameDialog.vue'
import { useSubmit } from '@/composables/useSubmit'
import { useCategoriesStore, type CategoryNode } from '@/stores/categories'

const categories = useCategoriesStore()
const creating = ref(false)
const renaming = ref<CategoryNode | null>(null)
const moving = ref<{ node: CategoryNode; parent: CategoryNode | null } | null>(null)

// A failed refresh keeps the tree already shown, and says so above it.
const showLists = computed(() => categories.tree.length > 0 || categories.status === 'ready')

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

function toggleArchived(value: string | number | boolean): void {
  void categories.setIncludeArchived(value === true)
}

onMounted(() => categories.load())
</script>

<template>
  <section>
    <h1>Categories</h1>

    <div class="categories__toolbar">
      <ElButton type="primary" @click="creating = true">New category</ElButton>
      <label class="categories__archived">
        <ElSwitch :model-value="categories.includeArchived" @change="toggleArchived" />
        Show archived
      </label>
    </div>

    <ErrorAlert v-if="archiving.error.value" :title="archiving.error.value" class="categories__alert" />

    <div v-if="categories.status === 'failed'" class="categories__alert">
      <ErrorAlert title="Could not load your Categories" :description="categories.failure" />
      <ElButton class="categories__retry" @click="categories.load()">Try again</ElButton>
    </div>

    <p v-if="categories.status === 'idle' || (categories.status === 'loading' && !showLists)" role="status">
      Loading Categories…
    </p>
    <template v-else-if="showLists">
      <CategorySection kind="Income" :nodes="income" @action="perform" />
      <CategorySection kind="Expense" :nodes="expense" @action="perform" />
    </template>

    <CreateCategoryDialog v-if="creating" @close="creating = false" @saved="creating = false" />
    <MoveCategoryDialog v-if="moving" :node="moving.node" :parent="moving.parent" @close="moving = null" @saved="moving = null" />
    <RenameDialog v-if="renaming" :target="renaming" :rename="categories.rename" @close="renaming = null" @saved="renaming = null" />
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
