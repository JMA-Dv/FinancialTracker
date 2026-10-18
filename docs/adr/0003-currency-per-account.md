# A Currency per Account, no conversion

Supersedes ADR 0001 (single Currency per User).

Each Account keeps its money in one Currency, chosen when the Account is created, so a User can track a USD savings account next to an MXN checking account. Amounts still store no currency of their own: an amount is in its Account's Currency. Nothing is ever converted. We considered keeping one Currency per User (ADR 0001), which can't describe money really held in two currencies, and converting everything to a home Currency, which needs exchange-rate history and "which day's rate" rules, a whole subdomain a personal tracker doesn't need yet.

## Consequences

- Creating an Account requires a Currency. The User's Currency (set in user settings) is only the default offered for new Accounts.
- An Account's Currency can be changed only while it has no Transactions or Transfers. After that, a change is refused with 409, so existing amounts are never relabelled. The Opening Balance doesn't count, so a mistake made at setup can still be fixed.
- Amounts are checked against their Account's Currency's minor units (2 for USD and MXN, 0 for JPY).
- Every total that spans Accounts (net worth, income and spending, Category breakdowns, spending by Payee) is reported per Currency, never added across Currencies.
- A Transfer between Accounts in the same Currency has one amount. A Transfer between Accounts in different Currencies records both the amount that left and the amount that arrived. The rate between them is implied, never stored or reused.
- Converting to a home Currency later means adding exchange rates and reworking every total, so it should be its own planned piece of work.
