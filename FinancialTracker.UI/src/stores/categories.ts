import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api, describeFailure } from '@/api/client'

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

export type CategoriesStatus = 'idle' | 'loading' | 'ready' | 'failed'

export const useCategoriesStore = defineStore('categories', () => {
  const status = ref<CategoriesStatus>('idle')
  const failure = ref('')
  const tree = ref<CategoryNode[]>([])
  const includeArchived = ref(false)

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

  /** Fetches the tree again, with or without the Archived Categories. */
  async function load(): Promise<void> {
    status.value = 'loading'
    failure.value = ''
    try {
      const path = includeArchived.value ? '/api/categories?includeArchived=true' : '/api/categories'
      tree.value = await api.get<CategoryNode[]>(path)
      status.value = 'ready'
    } catch (error) {
      status.value = 'failed'
      failure.value = describeFailure(error)
    }
  }

  /** Adds a Category, then refreshes the tree so every screen sees it. */
  async function create(request: CreateCategoryRequest): Promise<void> {
    await api.post('/api/categories', request)
    await load()
  }

  async function rename(id: string, name: string): Promise<void> {
    await api.put(`/api/categories/${id}/name`, { name })
    await load()
  }

  /** Moves a Category under another top-level one, or to the top level when `parentId` is null. */
  async function move(id: string, parentId: string | null): Promise<void> {
    await api.put(`/api/categories/${id}/parent`, { parentId })
    await load()
  }

  async function setArchived(id: string, archived: boolean): Promise<void> {
    await api.post(`/api/categories/${id}/${archived ? 'archive' : 'unarchive'}`)
    await load()
  }

  async function setIncludeArchived(value: boolean): Promise<void> {
    includeArchived.value = value
    await load()
  }

  return { status, failure, tree, includeArchived, selectable, load, create, rename, move, setArchived, setIncludeArchived }
})
