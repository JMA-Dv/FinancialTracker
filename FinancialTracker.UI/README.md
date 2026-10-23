# Finance Tracker UI

Vue 3 single-page app for the Finance Tracker API. Stack: Vite, TypeScript (strict), Element Plus, Pinia, vue-router, Vitest.

## Scripts

Use `pnpm` (Node 24 LTS).

| Command | What it does |
|---|---|
| `pnpm i` | Install dependencies |
| `pnpm dev` | Start the dev server |
| `pnpm build` | Typecheck, then build to `dist/` |
| `pnpm typecheck` | `vue-tsc --build` |
| `pnpm test` | Run the Vitest suite once |

## Running against the API

The dev server proxies `/api` and `/signin-google` to the API, so the app and the API share one origin: no CORS, and the API's cookie works.

| Setting | Default | Meaning |
|---|---|---|
| `VITE_API_TARGET` | `https://localhost:7204` | Where the API runs (its `https` launch profile) |
| `VITE_HTTPS_CERT`, `VITE_HTTPS_KEY` | unset | PEM certificate and key to serve the dev server over https |

Set them in `FinancialTracker.UI/.env.local` (git-ignored); `.env.example` lists them.

### Signing in with Google

The API tells Google to send the browser back to the origin it was called on, which is this dev server, and Google only accepts an https redirect for it. So the dev server must run over https:

1. Export the ASP.NET development certificate (trusted already if you followed [the Google steps](../docs/google-auth-client-steps.md)) next to the app. `certs/` is git-ignored:

   ```bash
   dotnet dev-certs https --export-path ./certs/localhost.pem --format Pem --no-password
   ```

2. Put this in `.env.local`:

   ```
   VITE_HTTPS_CERT=./certs/localhost.pem
   VITE_HTTPS_KEY=./certs/localhost.key
   ```

3. Register `https://localhost:5173/signin-google` as an authorized redirect URI in the Google OAuth client ([step 5](../docs/google-auth-client-steps.md#5-create-the-oauth-client)). If you change the dev server port, register that port.
4. Start the API (`dotnet run --project FinancialTracker.Api --launch-profile https`), then `pnpm dev`, and open `https://localhost:5173`.

Without the certificate the app still runs, but the Google redirect has nowhere to land.

## Conventions

- All API calls go through `src/api/client.ts`. It sends the session cookie and turns failures into `UnauthorizedError` (401), `ValidationError` (400 with `errors`) and `ProblemError` (other 400, 404, 409 and unexpected failures). Show a `ProblemError`'s message as-is: the API words it for people.
- A 401 from any call clears the session and returns to sign-in (wired in `src/app.ts`).
- Tests replace `fetch` and drive the whole app (`src/app.test.ts`) or the client (`src/api/client.test.ts`).
