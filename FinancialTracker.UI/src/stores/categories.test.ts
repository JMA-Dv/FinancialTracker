import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { type FixtureCategory, groceries, salary, travel } from '@/testing/fixtures'
import { fakeApi, json } from '@/testing/harness'
import { useCategoriesStore } from './categories'

beforeEach(() => setActivePinia(createPinia()))

describe('Categories store', () => {
  it('offers the Categories a new Transaction may use, children labelled with their parent', async () => {
    fakeApi({ 'GET /api/categories': () => json(200, [salary, groceries]) })
    const categories = useCategoriesStore()

    await categories.load()

    expect(categories.selectable.map((category) => category.label)).toEqual([
      'Salary',
      'Groceries',
      'Groceries › Market',
    ])
    expect(categories.selectable.map((category) => category.kind)).toEqual(['Income', 'Expense', 'Expense'])
  })

  it('leaves Archived Categories and what is under them out of the pickers even when they are listed', async () => {
    fakeApi({ 'GET /api/categories?includeArchived=true': () => json(200, [salary, groceries, travel]) })
    const categories = useCategoriesStore()

    await categories.setIncludeArchived(true)

    expect(categories.selectable.map((category) => category.label)).toEqual([
      'Salary',
      'Groceries',
      'Groceries › Market',
    ])
  })

  it('refreshes what the pickers offer after a change', async () => {
    let tree: FixtureCategory[] = [salary]
    fakeApi({
      'GET /api/categories': () => json(200, tree),
      'POST /api/categories': () => {
        tree = [salary, groceries]
        return json(201, { id: 'groceries', name: 'Groceries', kind: 'Expense', parentId: null, isArchived: false })
      },
    })
    const categories = useCategoriesStore()
    await categories.load()
    expect(categories.selectable.map((category) => category.label)).toEqual(['Salary'])

    await categories.create({ name: 'Groceries', kind: 'Expense' })

    expect(categories.selectable.map((category) => category.label)).toEqual(['Salary', 'Groceries', 'Groceries › Market'])
  })
})
