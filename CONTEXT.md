# Finance Tracker

A personal finance tracker where one person records where their money sits and how it moves, so they can see their spending, budgets, debts, and investments.

## Language

### People

**User**:
The person who logs in and owns one private set of finances. Finances are never shared between Users.
_Avoid_: Account, member, customer

### Money

**Currency**:
The single unit of money a User's finances are kept in, chosen at setup. Every amount a User records is in their Currency.
_Avoid_: Money type, denomination

**Account**:
A place where a User's money sits, such as a checking account, a savings account, a cash wallet, or a credit card.
_Avoid_: Wallet, bank, fund

**Closed Account**:
An Account the User no longer uses. It holds a zero balance, accepts nothing new, and keeps its history.
_Avoid_: Deleted account, inactive account

**Liability Account**:
An Account whose balance is normally money the User owes, such as a credit card. Every other Account is an asset.
_Avoid_: Debt account, negative account

**Balance Adjustment**:
A statement that an Account's real balance was a given amount at the end of a given date. The difference from what the tracker would otherwise show is never income or spending, and it has no Category.
_Avoid_: Correction, reconciliation, fix

**Opening Balance**:
The first Balance Adjustment on an Account, stating what it held when the User started tracking it.
_Avoid_: Initial balance, starting amount

**Transaction**:
One amount of money into or out of one Account, on a date, in one Category. A Transfer is not a Transaction.
_Avoid_: Entry, record, movement

**Payee**:
The person or business a User paid or was paid by, kept as a reusable name across Transactions.
_Avoid_: Merchant, vendor, counterparty

**Schedule**:
A plan for a future income or expense, happening once or repeating. A Schedule is not a Transaction.
_Avoid_: Recurring transaction, reminder, future transaction

**Refund**:
A Transaction bringing money back in an Expense Category, which reduces spending in that Category. A Refund is never income.
_Avoid_: Return, reversal

**Transfer**:
A movement of an amount from one of a User's Accounts to another. A Transfer is never income or spending.
_Avoid_: Movement, internal transaction

### Organising

**Category**:
The single label that says what money was for or where it came from. Every Category is either an Income Category or an Expense Category, and may sit under one parent Category.
_Avoid_: Type, group, bucket

**Archived Category**:
A Category hidden from new Transactions but kept on the Transactions that already use it.
_Avoid_: Deleted category, inactive category

**Tag**:
A free-form label that groups money across Categories, such as a trip or a project. A Transaction can carry any number of Tags.
_Avoid_: Label, category
