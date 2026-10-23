/** An amount in its Account's Currency, such as "$1,250.50"; negative amounts keep their sign. */
export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount)
}

/** How many decimal places the Currency has: 2 for USD, 0 for JPY. */
export function minorUnits(currency: string): number {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency }).resolvedOptions().maximumFractionDigits ?? 2
}

/** How an entry changed a balance, always signed, such as "+$50.00" or "-$400.00". */
export function formatEffect(amount: number, currency: string): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency, signDisplay: 'exceptZero' }).format(amount)
}

// Plain digits with an optional decimal part. Anything else (a comma, an exponent) is refused rather than
// guessed at, because a number field would quietly read it as empty and state a balance of zero.
const AMOUNT = /^-?\d+(\.\d+)?$/

/** What is wrong with an amount typed for a Currency, or empty when it is fine or blank. The API stays the authority. */
export function amountProblem(text: string, currency: string): string {
  if (text === '') {
    return ''
  }
  if (!AMOUNT.test(text)) {
    return 'Enter a number such as 1250.50.'
  }
  const decimals = text.split('.')[1]?.length ?? 0
  const allowed = currency === '' ? 2 : minorUnits(currency)
  return decimals > allowed ? `${currency} has ${allowed} decimal places.` : ''
}
