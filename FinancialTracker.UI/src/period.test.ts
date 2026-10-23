import { describe, expect, it } from 'vitest'
import { rangeFor } from './period'

describe('period presets', () => {
  it('runs this month and the year up to today', () => {
    expect(rangeFor('this-month', '2026-10-23')).toEqual({ from: '2026-10-01', to: '2026-10-23' })
    expect(rangeFor('year-to-date', '2026-10-23')).toEqual({ from: '2026-01-01', to: '2026-10-23' })
  })

  it('runs last month over its whole length, across a year end and in a leap year', () => {
    expect(rangeFor('last-month', '2026-10-23')).toEqual({ from: '2026-09-01', to: '2026-09-30' })
    expect(rangeFor('last-month', '2026-01-05')).toEqual({ from: '2025-12-01', to: '2025-12-31' })
    expect(rangeFor('last-month', '2028-03-31')).toEqual({ from: '2028-02-01', to: '2028-02-29' })
  })

  it('counts today as one of the last 30 days, even across a year end', () => {
    expect(rangeFor('last-30-days', '2026-10-23')).toEqual({ from: '2026-09-24', to: '2026-10-23' })
    expect(rangeFor('last-30-days', '2026-01-10')).toEqual({ from: '2025-12-12', to: '2026-01-10' })
  })
})
