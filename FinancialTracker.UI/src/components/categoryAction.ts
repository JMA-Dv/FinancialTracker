import type { CategoryNode } from '@/stores/categories'

/** What the User asked to do to a Category in the list; `parent` is the Category it sits under, if any. */
export type CategoryAction =
  | { type: 'rename'; node: CategoryNode }
  | { type: 'move'; node: CategoryNode; parent: CategoryNode | null }
  | { type: 'archive'; node: CategoryNode }
  | { type: 'unarchive'; node: CategoryNode }
