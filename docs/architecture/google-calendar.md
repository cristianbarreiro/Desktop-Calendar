# Google Calendar connection

Google integration is read-only in its initial delivery. The user connects an account from Settings, discovers calendars, enables the calendars to import, and starts synchronization manually. Disabled calendars are not synchronized. Local calendar use does not depend on Google availability.

## OAuth client configuration

Create an OAuth client with application type **Desktop app** in Google Cloud Console and enable the Google Calendar API. Set `DESKTOP_CALENDAR_GOOGLE_CLIENT_ID` to that public client ID before starting Desktop Calendar. A desktop client secret is neither required nor used. The application opens the system browser, listens on a temporary IPv4 loopback redirect, validates OAuth state, and uses PKCE with `S256`.

Requested scopes are `openid`, `email`, and `https://www.googleapis.com/auth/calendar.readonly`. Refresh credentials are saved in the current Windows user's Credential Manager; they are not included in application backups or the database.

## Synchronization

Google event lists are paged through to the final `nextSyncToken`; that opaque provider token is persisted per calendar. Later syncs send the token back to Google and represent cancelled events as deletions. If Google returns `410 Gone`, the provider discards the expired cursor for that request and returns a new full snapshot. The synchronization service applies changes and stores the replacement token after processing succeeds.

Google event writes and background synchronization are not enabled in this phase. Synchronization is initiated from Settings and imported events are mapped to their Google event identities to prevent repeated imports.
