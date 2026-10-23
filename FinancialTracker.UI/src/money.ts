/** An amount in its Account's Currency, such as "$1,250.50"; negative amounts keep their sign. */
export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount)
}

/** How many decimal places the Currency has: 2 for USD, 0 for JPY. */
export function minorUnits(currency: string): number {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).resolvedOptions().maximumFractionDigits ?? 2
}
