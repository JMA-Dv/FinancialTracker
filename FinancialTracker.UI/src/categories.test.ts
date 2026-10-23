import { flushPromises } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { type FixtureCategory, groceries, market, salary, travel } from './testing/fixtures'
import { documentBody, fakeApi, json, pickOption, press, signedIn, visit } from './testing/harness'

describe('Categories screen', () => {
  it('shows Income and Expense in separate sections, with children under their parent', async () => {
    fakeApi({ ...signedIn, 'GET /api/categories': () => json(200, [salary, groceries]) })

    const { page } = await visit('/categories')

    const income = page.get('section[aria-labelledby="income-heading"]')
    expect(income.get('h2').text()).toBe('Income')
    expect(income.text()).toContain('Salary')
    expect(income.text()).not.toContain('Groceries')

    const expense = page.get('section[aria-labelledby="expense-heading"]')
    expect(expense.get('h2').text()).toBe('Expense')
    const groceriesRow = expense.get('li')
    expect(groceriesRow.text()).toContain('Groceries')
    expect(groceriesRow.get('ul').text()).toContain('Market')
  })

  it('says so when a section has no Categories', async () => {
    fakeApi({ ...signedIn, 'GET /api/categories': () => json(200, [salary]) })

    const { page } = await visit('/categories')

    expect(page.get('section[aria-labelledby="expense-heading"]').text()).toContain('No Expense Categories yet.')
  })

  it('shows the API failure with a way to try again', async () => {
    let healthy = false
    fakeApi({
      ...signedIn,
      'GET /api/categories': () =>
        healthy ? json(200, [salary]) : json(500, { title: 'Oops', detail: 'Database unavailable.' }),
    })
    const { page } = await visit('/categories')

    expect(page.get('[role="alert"]').text()).toContain('Database unavailable.')

    healthy = true
    await press(page, 'Try again')

    expect(page.find('[role="alert"]').exists()).toBe(false)
    expect(page.text()).toContain('Salary')
  })

  it('hides Archived Categories until the toggle is on, then marks them', async () => {
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [salary]),
      'GET /api/categories?includeArchived=true': () => json(200, [salary, travel]),
    })
    const { page } = await visit('/categories')
    expect(page.text()).not.toContain('Travel')

    await page.get('[role="switch"]').trigger('click')
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/api/categories?includeArchived=true', expect.anything())
    const travelRow = page.get('section[aria-labelledby="expense-heading"] li')
    expect(travelRow.text()).toContain('Travel')
    expect(travelRow.text()).toContain('Archived')

    await page.get('[role="switch"]').trigger('click')
    await flushPromises()
    expect(page.text()).not.toContain('Travel')
  })
})

describe('creating a Category', () => {
  it('puts a new Category under the chosen parent, without asking for a kind', async () => {
    let tree: FixtureCategory[] = [salary, groceries]
    const bakery: FixtureCategory = { id: 'bakery', name: 'Bakery', kind: 'Expense', isArchived: false, children: [] }
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, tree),
      'POST /api/categories': () => {
        tree = [salary, { ...groceries, children: [...groceries.children, bakery] }]
        return json(201, { id: 'bakery', name: 'Bakery', kind: 'Expense', parentId: 'groceries', isArchived: false })
      },
    })
    const { page } = await visit('/categories')

    await press(page, 'New category')
    const dialog = documentBody().get('[role="dialog"]')
    expect(dialog.text()).toContain('Kind')
    await dialog.get('input.el-input__inner').setValue('Bakery')
    await pickOption(dialog, 'Groceries')
    expect(dialog.text()).not.toContain('Kind')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/categories',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ name: 'Bakery', parentId: 'groceries' }) }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
    expect(page.get('section[aria-labelledby="expense-heading"] li ul').text()).toContain('Bakery')
  })

  it('asks for a kind when the Category is top level', async () => {
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [salary]),
      'POST /api/categories': () => json(201, { id: 'x', name: 'Gifts', kind: 'Income', parentId: null, isArchived: false }),
    })
    const { page } = await visit('/categories')

    await press(page, 'New category')
    const dialog = documentBody().get('[role="dialog"]')
    await dialog.get('input.el-input__inner').setValue('Gifts')
    expect(dialog.get('button.is-disabled, button[disabled]').text()).toBe('Save')
    await dialog.get('input[type="radio"][value="Income"]').setValue(true)
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/categories',
      expect.objectContaining({ body: JSON.stringify({ name: 'Gifts', kind: 'Income' }) }),
    )
  })

  it('shows the API\'s message and keeps the dialog open when it refuses', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [salary]),
      'POST /api/categories': () =>
        json(409, { title: 'Category exists', detail: 'You already have a Category named Gifts.' }),
    })
    const { page } = await visit('/categories')

    await press(page, 'New category')
    const dialog = documentBody().get('[role="dialog"]')
    await dialog.get('input.el-input__inner').setValue('Gifts')
    await dialog.get('input[type="radio"][value="Income"]').setValue(true)
    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('You already have a Category named Gifts.')
    expect((dialog.get('input.el-input__inner').element as HTMLInputElement).value).toBe('Gifts')
  })
})

describe('archiving a Category', () => {
  it('archives a Category, which then leaves the list', async () => {
    let tree: FixtureCategory[] = [salary, groceries]
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, tree),
      'POST /api/categories/salary/archive': () => {
        tree = [groceries]
        return json(200, { id: 'salary', name: 'Salary', kind: 'Income', parentId: null, isArchived: true })
      },
    })
    const { page } = await visit('/categories')

    await press(page, 'Archive Salary')

    expect(fetchMock).toHaveBeenCalledWith('/api/categories/salary/archive', expect.objectContaining({ method: 'POST' }))
    expect(page.text()).not.toContain('Salary')
  })

  it('unarchives an Archived Category, which loses its mark', async () => {
    let archivedTravel: FixtureCategory = { ...travel, children: [] }
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [salary]),
      'GET /api/categories?includeArchived=true': () => json(200, [salary, archivedTravel]),
      'POST /api/categories/travel/unarchive': () => {
        archivedTravel = { ...archivedTravel, isArchived: false }
        return json(200, { id: 'travel', name: 'Travel', kind: 'Expense', parentId: null, isArchived: false })
      },
    })
    const { page } = await visit('/categories')
    await page.get('[role="switch"]').trigger('click')
    await flushPromises()

    await press(page, 'Unarchive Travel')

    expect(fetchMock).toHaveBeenCalledWith('/api/categories/travel/unarchive', expect.objectContaining({ method: 'POST' }))
    const row = page.get('section[aria-labelledby="expense-heading"] li')
    expect(row.text()).toContain('Travel')
    expect(row.text()).not.toContain('Archived')
  })

  it('shows what the API says when it refuses', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [salary]),
      'POST /api/categories/salary/archive': () => json(404, { title: 'Not found', detail: 'That Category no longer exists.' }),
    })
    const { page } = await visit('/categories')

    await press(page, 'Archive Salary')

    expect(page.get('[role="alert"]').text()).toContain('That Category no longer exists.')
    expect(page.text()).toContain('Salary')
  })
})

describe('renaming a Category', () => {
  it('starts from the current name and shows the new one after saving', async () => {
    let name = 'Groceries'
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [{ ...groceries, name }]),
      'PUT /api/categories/groceries/name': () => {
        name = 'Food'
        return json(200, { id: 'groceries', name, kind: 'Expense', parentId: null, isArchived: false })
      },
    })
    const { page } = await visit('/categories')

    await press(page, 'Rename Groceries')
    const dialog = documentBody().get('[role="dialog"]')
    const input = dialog.get('input.el-input__inner')
    expect((input.element as HTMLInputElement).value).toBe('Groceries')
    await input.setValue('Food')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/categories/groceries/name',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ name: 'Food' }) }),
    )
    expect(documentBody().find('[role="dialog"]').exists()).toBe(false)
    expect(page.get('section[aria-labelledby="expense-heading"]').text()).toContain('Food')
  })

  it('shows the field message when the name is refused', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [groceries]),
      'PUT /api/categories/groceries/name': () =>
        json(400, { title: 'Invalid', errors: { Name: ['The field Name must be a string with a maximum length of 100.'] } }),
    })
    const { page } = await visit('/categories')

    await press(page, 'Rename Groceries')
    const dialog = documentBody().get('[role="dialog"]')
    await dialog.get('input.el-input__inner').setValue('x')
    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('maximum length of 100')
  })
})

describe('moving a Category', () => {
  const transport: FixtureCategory = { id: 'transport', name: 'Transport', kind: 'Expense', isArchived: false, children: [] }
  const movedResponse = { id: 'x', name: 'x', kind: 'Expense', parentId: null, isArchived: false }

  it('moves a child to the top level', async () => {
    let tree: FixtureCategory[] = [salary, groceries]
        const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, tree),
      'PUT /api/categories/market/parent': () => {
        tree = [salary, { ...groceries, children: [] }, market]
        return json(200, movedResponse)
      },
    })
    const { page } = await visit('/categories')

    await press(page, 'Move Market')
    const dialog = documentBody().get('[role="dialog"]')
    await pickOption(dialog, 'Top level')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/categories/market/parent',
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ parentId: null }) }),
    )
    const topLevel = page.findAll('section[aria-labelledby="expense-heading"] > ul > li')
    expect(topLevel.map((row) => row.get('.category-row span').text())).toEqual(['Groceries', 'Market'])
  })

  it('moves a Category under another of the same kind, and offers no other kind', async () => {
    let tree: FixtureCategory[] = [salary, groceries, transport]
    const fetchMock = fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, tree),
      'PUT /api/categories/transport/parent': () => {
        tree = [salary, { ...groceries, children: [...groceries.children, transport] }]
        return json(200, movedResponse)
      },
    })
    const { page } = await visit('/categories')

    await press(page, 'Move Transport')
    const dialog = documentBody().get('[role="dialog"]')
    await dialog.get('.el-select__wrapper').trigger('click')
    await flushPromises()
    const offered = Array.from(document.body.querySelectorAll('.el-select-dropdown__item')).map((option) => option.textContent?.trim())
    expect(offered).toEqual(['Groceries (Expense)'])
    await pickOption(dialog, 'Groceries')
    await press(dialog, 'Save')

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/categories/transport/parent',
      expect.objectContaining({ body: JSON.stringify({ parentId: 'groceries' }) }),
    )
    expect(page.get('section[aria-labelledby="expense-heading"] li ul').text()).toContain('Transport')
  })

  it('disables the move of a Category that has children, and says why', async () => {
    fakeApi({ ...signedIn, 'GET /api/categories': () => json(200, [salary, groceries, transport]) })
    const { page } = await visit('/categories')

    const move = page.get('button[aria-label="Move Groceries"]')

    expect(move.attributes('disabled')).toBeDefined()
    expect(page.text()).toContain('Groceries has Categories under it, so it can\'t be moved under another.')
  })

  it('shows the API\'s message when it refuses a move the screen allowed', async () => {
    fakeApi({
      ...signedIn,
      'GET /api/categories': () => json(200, [groceries, transport]),
      'PUT /api/categories/transport/parent': () =>
        json(400, {
          title: 'Category has children',
          detail: 'Transport has Categories under it, so moving it under another would make a third level.',
        }),
    })
    const { page } = await visit('/categories')

    await press(page, 'Move Transport')
    const dialog = documentBody().get('[role="dialog"]')
    await pickOption(dialog, 'Groceries')
    await press(dialog, 'Save')

    expect(dialog.get('[role="alert"]').text()).toContain('so moving it under another would make a third level.')
    expect(documentBody().find('[role="dialog"]').exists()).toBe(true)
  })
})
