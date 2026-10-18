# Single Currency per User, no conversion

Superseded by ADR 0003 (a Currency per Account).

Each User keeps all their finances in one Currency, chosen at setup, so no amount stores a currency of its own and every total is a single number. We considered a Currency per Account (with totals shown per Currency) and conversion to a home Currency, and rejected both: exchange-rate history, rounding, and "which day's rate" questions are a whole subdomain a personal tracker doesn't need yet.

## Consequences

- Changing a User's Currency relabels existing amounts rather than converting them ($50 becomes 50 MXN). The User is warned before confirming. This is meant for fixing a setup mistake, not for moving to a new Currency.
- Supporting several Currencies later means adding a currency to every stored amount and reworking every total, so it should be its own planned piece of work.
