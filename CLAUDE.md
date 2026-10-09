# Project: Finance Tracker

## Description:

This project is a finance tracker application that helps users manage their personal finances by tracking income, expenses, and budgeting, money borrowed, investments and interests.
It provides features such as expense categorization, financial reports, and visualizations to help users understand their spending habits and make informed financial decisions.

## Features:

- User authentication and authorization for secure access to personal financial data.
- Expense tracking with categorization and tagging for better organization.
- Budgeting tools to set financial goals and monitor progress.
- Financial reports and visualizations using Chart.js to provide insights into spending patterns and trends.

## Tech Stack

- Frontend: Vuejs 3 with Vite,typescript, Element plus, and Chart.js for data visualization, pinia.
- Backend : .Net 10 Api with Entity Framework Core for database management and RESTful API development.
- Testing: xUnit for unit testing and integration testing of the backend API.
- Database: NeonPostgres.
- Containerization: Docker for containerizing the application and its dependencies for easy deployment and scalability.

## Commands

- `pnpm dev`- start UI
- `pnpm i`- Install node modules

## TypeScript

- Strict mode enabled
- Prefer interfaces over types for objects
- Use explicit return types for public functions
- No `any` - use `unknown` if truly dynamic
-

## C# Style

- Follow PascalCase for component names, method names, and public members.
- Use camelCase for private fields and local variables.
- Prefix interface names with "I" (e.g., IUserService).
-
- Declare variables non-nullable, and check for `null` at entry points.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Demonstrate how to organize code using feature folders or domain-driven design principles.
- Show proper separation of concerns with models, services, and data access layers.
- Use Microsoft.Extensions.DependencyInjection patterns
- Use
- Register services with appropriate lifetimes (Singleton, Scoped, Transient)

## Communication

- Be concise - bullet points over paragraphs
- Skip the preamble - get to the point
- Don't ask for confirmation on routine tasks
- Challenge my assumptions when I'm wrong. Use **Grill me** to indicate when you are challenging my assumptions.
- Explain the Program.cs and configuration system in ASP.NET Core 10 including environment-specific settings.

## Don't

- Use `npm` to run commands, use `pnpm` instead.
- Install UI Libraries that are not listed in this CLAUDE.md.
- Add analytics or tracking without asking
- Add dependencies without justification
- Refactor unrelated code in feature PRs
- Leave console.logs in production code

## Git

- Do not stage changes
- Do not commit changes.

## Agent skills

### Issue tracker

Issues and specs are tracked in GitHub Issues (`JMA-Dv/FinancialTracker`) via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default canonical labels (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context (`CONTEXT.md` + `docs/adr/` at the repo root). See `docs/agents/domain.md`.
