# Google is the only sign-in method

A User signs in only with their Google account; there is no email and password sign-in. With passwords, any sign-up or sign-in form can be used to test whether an email is registered (the "already taken" answer, or response time), and closing that properly needs email confirmation, which means running an email sender. Google already proves the person controls the account, so we store no passwords, send no email, and offer nothing an email can be fed into. We considered email and password with email confirmation, and email and password alongside Google with linking by verified email, and rejected both as more surface for a personal tracker than it needs.

## Consequences

- A User is identified by their Google account's permanent id (the `sub` claim), never by email. The email is kept only for display, isn't unique, and two Google accounts with the same email are two Users. A Google account whose email changes still opens the same finances.
- Losing access to the Google account (suspension, deletion, lost 2-Step Verification) means losing access to the finances. The tracker has no recovery path of its own.
- Everyone needs a Google account, and signing in depends on Google being available.
- Integration tests replace Google with a fake authentication scheme registered only in the test host. Running locally needs a Google Cloud OAuth client, with its id and secret in user secrets.
- Adding another sign-in provider later is an external login like Google. Adding passwords back would reopen the email-probing problem above.
