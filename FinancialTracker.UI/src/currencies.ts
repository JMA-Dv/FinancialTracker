export interface CurrencyOption {
  code: string
  name: string
  label: string
}

/** The ISO 4217 codes this browser knows, with English names; the API has no endpoint for them. */
export function currencyOptions(): CurrencyOption[] {
  const names = new Intl.DisplayNames(['en'], { type: 'currency' })
  return Intl.supportedValuesOf('currency').map((code) => {
    const name = names.of(code) ?? code
    return { code, name, label: `${code} – ${name}` }
  })
}
