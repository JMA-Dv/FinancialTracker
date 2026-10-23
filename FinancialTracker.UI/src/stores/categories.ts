import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api } from '@/api/client'
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

/** A Category a new Transaction may use, for pickers. */
export interface SelectableCategory {
  id: string
  name: string
  kind: CategoryKind
  /** The name, with the parent's in front for a child: "Groceries › Market". */
  label: string
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
  } = useLoadedList(() =>
    api.get<CategoryNode[]>(includeArchived.value ? '/api/categories?includeArchived=true' : '/api/categories'),
  )

  // Archived Categories, and what is under them, can't take new Transactions even when the tree lists them.
  const selectable = computed<SelectableCategory[]>(() =>
    tree.value
      .filter((parent) => !parent.isArchived)
      .flatMap((parent) => [
        { id: parent.id, name: parent.name, kind: parent.kind, label: parent.name },
        ...parent.children
          .filter((child) => !child.isArchived)
          .map((child) => ({
            id: child.id,
            name: child.name,
            kind: child.kind,
            label: `${parent.name} › ${child.name}`,
          })),
      ]),
  )

  /** Runs a change, then refreshes the tree so every screen sees it. */
  async function mutate(change: () => Promise<unknown>): Promise<void> {
    await change()
    await load()
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

  return { status, failure, tree, includeArchived, selectable, load, create, rename, move, setArchived, setIncludeArchived }
})
