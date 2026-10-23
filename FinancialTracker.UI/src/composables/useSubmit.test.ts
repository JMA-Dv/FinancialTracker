import { describe, expect, it } from 'vitest'
import { ProblemError } from '@/api/client'
import { useSubmit } from './useSubmit'

describe('useSubmit', () => {
  it('keeps what the API said so it can be shown', async () => {
    const { error, submit } = useSubmit(() => Promise.reject(new ProblemError(409, 'Exists', 'That name is taken.')))

    expect(await submit()).toBe(false)
    expect(error.value).toBe('That name is taken.')
  })

  it('tells the User something went wrong when the failure is not the API\'s, and still raises it', async () => {
    const { error, submit } = useSubmit(() => Promise.reject(new RangeError('a bug')))

    await expect(submit()).rejects.toThrow('a bug')
    expect(error.value).toBe('Something went wrong.')
  })

  it('clears the message on the next attempt', async () => {
    let fail = true
    const { error, submit } = useSubmit(() =>
      fail ? Promise.reject(new ProblemError(400, 'No', 'Not allowed.')) : Promise.resolve(),
    )
    await submit()
    fail = false

    expect(await submit()).toBe(true)
    expect(error.value).toBe('')
  })
})
