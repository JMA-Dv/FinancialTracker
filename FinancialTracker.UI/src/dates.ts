/** Today as a date-only `yyyy-MM-dd` string, in UTC like the API's "not after today" rule. */
export function todayUtc(): string {
  return new Date().toISOString().slice(0, 10)
}
