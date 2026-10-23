import type { CategoryKind, CategoryNode } from '@/stores/categories'

/** A Category in a picker. */
export interface CategoryOption {
  id: string
  name: string
  kind: CategoryKind
  /** The name, with the parent's in front for a child: "Groceries › Market". */
  label: string
  /** Archived itself or under an Archived Category; either way it can't take new Transactions. */
  isArchived: boolean
}

/** The tree as a flat list, each parent followed by its children. */
export function flattenCategories(tree: CategoryNode[]): CategoryOption[] {
  return tree.flatMap((parent) => [
    { id: parent.id, name: parent.name, kind: parent.kind, label: parent.name, isArchived: parent.isArchived },
    ...parent.children.map((child) => ({
      id: child.id,
      name: child.name,
      kind: child.kind,
      label: `${parent.name} › ${child.name}`,
      isArchived: parent.isArchived || child.isArchived,
    })),
  ])
}

/** The top-level Categories `node` may sit under: unarchived, of its kind, and not itself. */
export function moveTargets(node: CategoryNode, tree: CategoryNode[]): CategoryNode[] {
  return tree.filter((candidate) => candidate.kind === node.kind && !candidate.isArchived && candidate.id !== node.id)
}

/**
 * Why `node` can't be moved, or null when it can. The API enforces these rules too; checking here
 * lets the screen explain instead of offering a move that is certain to be refused.
 */
export function moveObstacle(node: CategoryNode, parent: CategoryNode | null, tree: CategoryNode[]): string | null {
  if (node.children.length > 0) {
    return `${node.name} has Categories under it, so it can't be moved under another.`
  }
  // A child can always go to the top level; a top-level Category can only go under another one.
  if (parent === null && moveTargets(node, tree).length === 0) {
    return `There is no other ${node.kind} Category to move ${node.name} under.`
  }
  return null
}
