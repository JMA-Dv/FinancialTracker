export interface DateRange {
  from: string
  to: string
}

export type PeriodPreset = 'this-month' | 'last-month' | 'last-30-days' | 'year-to-date'

export const periodPresets: { id: PeriodPreset; label: string }[] = [
  { id: 'this-month', label: 'This month' },
  { id: 'last-month', label: 'Last month' },
  { id: 'last-30-days', label: 'Last 30 days' },
  { id: 'year-to-date', label: 'Year to date' },
]

const isoDate = (date: Date): string => date.toISOString().slice(0, 10)

/** The dates a preset covers, counting `today` (a `yyyy-MM-dd` date) as its last day. */
export function rangeFor(preset: PeriodPreset, today: string): DateRange {
  const year = Number(today.slice(0, 4))
  const month = Number(today.slice(5, 7)) - 1
  switch (preset) {
    case 'this-month':
      return { from: isoDate(new Date(Date.UTC(year, month, 1))), to: today }
    case 'last-month':
      // Day 0 of a month is the last day of the one before.
      return { from: isoDate(new Date(Date.UTC(year, month - 1, 1))), to: isoDate(new Date(Date.UTC(year, month, 0))) }
    case 'last-30-days':
      return { from: isoDate(new Date(Date.UTC(year, month, Number(today.slice(8, 10)) - 29))), to: today }
    case 'year-to-date':
      return { from: `${year}-01-01`, to: today }
  }
}
