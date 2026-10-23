/** A Category as `GET /api/categories` returns it; `kind` is a string because the test types it loosely. */
export interface FixtureCategory {
  id: string
  name: string
  kind: string
  isArchived: boolean
  children: FixtureCategory[]
}

export const salary: FixtureCategory = { id: 'salary', name: 'Salary', kind: 'Income', isArchived: false, children: [] }

export const market: FixtureCategory = { id: 'market', name: 'Market', kind: 'Expense', isArchived: false, children: [] }

export const groceries: FixtureCategory = {
  id: 'groceries',
  name: 'Groceries',
  kind: 'Expense',
  isArchived: false,
  children: [market],
}

export const flights: FixtureCategory = { id: 'flights', name: 'Flights', kind: 'Expense', isArchived: false, children: [] }

/** Archived, with a child that is not archived itself. */
export const travel: FixtureCategory = { id: 'travel', name: 'Travel', kind: 'Expense', isArchived: true, children: [flights] }
