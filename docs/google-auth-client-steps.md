# Creating the Google OAuth client

Google is the only way to sign in to Finance Tracker (see [ADR 0002](adr/0002-google-is-the-only-sign-in-method.md)). Running the API, locally or deployed, needs a Google OAuth client. These steps create one.

Menu names follow the Google Cloud Console layout where OAuth settings live under **Google Auth Platform**. If Google has moved things, look for the same names nearby.

## 1. Create a project

1. Open <https://console.cloud.google.com>.
2. Open the project picker at the top and choose **New project**. Name it, for example, `finance-tracker`, then create it.
3. Make sure the new project is selected.

## 2. Set up the consent screen

1. Go to **Menu → Google Auth Platform → Get started**.
2. **App information:** app name `Finance Tracker`, with your email as the user support email.
3. **Audience:** **External**.
4. **Contact information:** your email. Accept the policy, then click **Create**.

## 3. Add test users

1. Go to **Audience**. Leave the publishing status as **Testing**.
2. Under **Test users**, click **Add users** and add every Google account that should be able to sign in, starting with your own.

While the app is in Testing, a Google account not on this list is blocked from signing in.

## 4. Scopes (optional)

In **Data access** you can add `openid`, `.../auth/userinfo.email` and `.../auth/userinfo.profile`. The API asks for these anyway. They are non-sensitive, so Google won't ask for a review.

## 5. Create the OAuth client

1. Go to **Clients → Create client**.
2. **Application type:** Web application. **Name:** `Finance Tracker (local)`.
3. **Authorized JavaScript origins:** leave empty. The sign-in flow runs on the server.
4. **Authorized redirect URIs:** add `https://localhost:7204/signin-google`. It must match exactly: `https`, port `7204`, no trailing slash.
5. Click **Create**. Copy the **Client ID** and **Client secret**, or download the JSON.

> **Google shows the client secret only once, when the client is created.** If you lose it, add a new secret to the client.

Changes can take a few minutes to take effect.

## 6. Store the credentials

Keep the client id and secret in user secrets, never in `appsettings.json`:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client id>" --project FinancialTracker.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client secret>" --project FinancialTracker.Api
```

The API stops at startup if either one is missing.

## 7. Try it locally

1. Give the API a database and apply the migrations:

   ```bash
   dotnet user-secrets set "ConnectionStrings:FinanceDb" "<Neon connection string>" --project FinancialTracker.Api
   dotnet ef database update --project FinancialTracker.Infrastructure --startup-project FinancialTracker.Api
   ```

2. Trust the HTTPS development certificate:

   ```bash
   dotnet dev-certs https --trust
   ```

   On Linux, some browsers still need the certificate imported by hand.

3. Start the API with the `https` profile. Google sign-in uses a cookie that browsers only accept over HTTPS, so the `http` profile won't work:

   ```bash
   dotnet run --project FinancialTracker.Api --launch-profile https
   ```

4. In a browser, open:

   ```
   https://localhost:7204/api/auth/google/sign-in?returnUrl=/api/user-settings
   ```

   After you choose your Google account, the page shows `{"email":"<your email>"}`.

## 8. Deployment

- Create a separate client for production, with the redirect URI `https://<your-domain>/signin-google`.
- Give the deployed API its credentials as environment variables:
  - `Authentication__Google__ClientId`
  - `Authentication__Google__ClientSecret`
- To let Google accounts outside the test-user list sign in, publish the app under **Audience → Publish app**.

## Troubleshooting

| What you see | Cause | Fix |
|---|---|---|
| `redirect_uri_mismatch` | The redirect URI in step 5 doesn't exactly match the URL the API runs on | Check the scheme, port and path match exactly |
| "Access blocked" or "app has not completed verification" | The Google account isn't a test user | Add it in step 3 |
| "Correlation failed" after returning from Google | The API is running on `http` | Use the `https` launch profile |
| The API stops at startup with `Value cannot be null. (Parameter 'ClientId')` | The client id or secret isn't configured | Repeat step 6 |
