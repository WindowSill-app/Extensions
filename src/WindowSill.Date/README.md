# WindowSill.Date — Developer Guide

## OAuth Secrets Setup

The Date extension connects to Outlook, Google Calendar, and iCloud. The OAuth client credentials for Outlook and Google are **not checked into source control**. They are injected at build time.

### How it works

The MSBuild target in `OAuthSecrets.targets` generates `Core/OAuthSecretsValues.g.cs` before compilation. It reads credentials from:

1. **Environment variables** (CI) — `OUTLOOK_CLIENT_ID`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`
2. **`OAuthSecrets.json`** (local dev) — a gitignored file in this directory
3. **Empty strings** (fallback) — the project always builds, but OAuth sign-in won't work

### Local development setup

1. Copy the template:

   ```
   copy OAuthSecrets.template.json OAuthSecrets.json
   ```

2. Fill in your credentials in `OAuthSecrets.json`:

   ```json
   {
     "OutlookClientId": "your-azure-ad-client-id",
     "GoogleClientId": "your-google-client-id.apps.googleusercontent.com",
     "GoogleClientSecret": "your-google-client-secret"
   }
   ```

3. Build the project — credentials are embedded automatically.

> **`OAuthSecrets.json` is gitignored.** Never commit it.

### Getting your own credentials

#### Google Calendar

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project (or select an existing one)
3. **Enable the Google Calendar API**: APIs & Services → Library → search "Google Calendar API" → Enable
4. **Create OAuth credentials**: APIs & Services → Credentials → Create Credentials → OAuth client ID
   - Application type: **Desktop app**
   - Copy the **Client ID** and **Client Secret**
5. **Configure the consent screen**: OAuth consent screen → Add your Gmail as a test user
6. Paste the values into `OAuthSecrets.json`

#### Microsoft Outlook

1. Go to [Azure Portal — App registrations](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
2. Click **New registration**
   - Name: `WindowSill Date` (or any name)
   - Supported account types: **Accounts in any organizational directory and personal Microsoft accounts**
   - Redirect URI: select **Public client/native (mobile & desktop)** and enter `http://localhost`
3. Copy the **Application (client) ID**
4. Under **API permissions**, add:
   - `Calendars.Read`
   - `Calendars.ReadWrite`
   - `User.Read`
5. Under **Authentication**, enable **Allow public client flows** → Yes
6. Paste the Client ID into `OAuthSecrets.json`

#### iCloud / CalDAV

iCloud and generic CalDAV use username/password authentication — no OAuth credentials needed.

### Verifying your setup

After setting up credentials, build the project and check that `Core/OAuthSecretsValues.g.cs` contains your values (not empty strings):

```
type src\WindowSill.Date\Core\OAuthSecretsValues.g.cs
```

If you see empty strings, double-check that `OAuthSecrets.json` exists and has the correct format.
