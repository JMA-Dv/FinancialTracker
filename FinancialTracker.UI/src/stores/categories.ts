import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api } from '@/api/client'
import { flattenCategories, type CategoryOption } from '@/categoryRules'
import { useLoadedList } from '@/composables/useLoadedList'

export type CategoryKind = 'Income' | 'Expense'

/** A Category with the Categories under it, as `GET /api/categories` returns the tree. */
export interface CategoryNode {
  id: string
  name: string
  kind: CategoryKind
  isArchived: boolean
  children: CategoryNode[]
}

/** A child names its parent and takes its kind; a top-level Category names its kind. */
export interface CreateCategoryRequest {
  name: string
  kind?: CategoryKind
  parentId?: string
}

export const useCategoriesStore = defineStore('categories', () => {
  const includeArchived = ref(false)
  const {
    status,
    failure,
    items: tree,
    load,
    mutate,
  } = useLoadedList(() =>
    api.get<CategoryNode[]>(includeArchived.value ? '/api/categories?includeArchived=true' : '/api/categories'),
  )

  // Archived Categories, and what is under them, can't take new Transactions even when the tree lists them.
  const selectable = computed<CategoryOption[]>(() => flattenCategories(tree.value).filter((option) => !option.isArchived))

  /** The name of a Category in the loaded tree; null when it isn't there, e.g. an Archived one while the tree leaves them out. */
  function nameOf(id: string): string | null {
    const stack = [...tree.value]
    for (let node = stack.pop(); node !== undefined; node = stack.pop()) {
      if (node.id === id) {
        return node.name
      }
      stack.push(...node.children)
    }
    return null
  }

  /** Adds a Category. */
  function create(request: CreateCategoryRequest): Promise<void> {
    return mutate(() => api.post('/api/categories', request))
  }

  function rename(id: string, name: string): Promise<void> {
    return mutate(() => api.put(`/api/categories/${id}/name`, { name }))
  }

  /** Moves a Category under another top-level one, or to the top level when `parentId` is null. */
  function move(id: string, parentId: string | null): Promise<void> {
    return mutate(() => api.put(`/api/categories/${id}/parent`, { parentId }))
  }

  function setArchived(id: string, archived: boolean): Promise<void> {
    return mutate(() => api.post(`/api/categories/${id}/${archived ? 'archive' : 'unarchive'}`))
  }

  async function setIncludeArchived(value: boolean): Promise<void> {
    includeArchived.value = value
    await load()
  }

  return { status, failure, tree, includeArchived, selectable, nameOf, load, create, rename, move, setArchived, setIncludeArchived }
})
