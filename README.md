# MailGrabber

MailGrabber is a .NET console app for Linux, Windows, and macOS. It reads incoming emails from Outlook and optionally Gmail, extracts sender data, groups senders by full domain, detects newsletter-like senders, and exports results to CSV and structured JSON.

It also generates a local HTML viewer so you can browse clusters and inspect sender details quickly.

## AI Notice

This project contains source code that was generated with AI assistance and then refined manually.

## What It Does

- Reads messages from Outlook Inbox via Microsoft Graph.
- Reads messages from Gmail Inbox via Gmail API.
- Combines both data sources into one report.
- Clusters senders by full domain (for example: amazon.de, hetzner.com).
- Moves newsletter-like senders into a shared cluster named newsletter.
- Exports CSV, structured JSON, and a standalone HTML viewer.

## Requirements

- .NET 10 SDK (or compatible runtime)
- Microsoft Entra app registration (if Outlook is enabled)
- Google OAuth Desktop credentials (if Gmail is enabled)

## Quick Start

1. Create your local config from the example file.

```bash
cp appsettings.example.json appsettings.json
```

2. Edit appsettings.json and set your values.

3. Run the app.

```bash
dotnet restore
dotnet run -- --config appsettings.json
```

4. Open the generated viewer.

- output/cluster-viewer.html

## Outlook Setup

1. Create an app in Microsoft Entra.
2. Use account type Personal Microsoft accounts only (or multi-tenant with personal accounts).
3. Enable Public client / Mobile and desktop flows.
4. Add Microsoft Graph delegated permissions: Mail.Read and User.Read.
5. Copy Application (client) ID into appsettings.json.

## Gmail Setup

1. Create or open a Google Cloud project.
2. Enable Gmail API.
3. Create OAuth credentials of type Desktop app.
4. Download the JSON file and place it as google-client-secret.json in the project root (or set a custom path in config).
5. Set EnableGmail to true.

## Configuration

Main local configuration file: appsettings.json

Important fields:

- EnableOutlook / EnableGmail: turn providers on or off.
- ClientId / TenantId: Outlook credentials.
- GmailClientSecretsPath: path to Google OAuth client file.
- OutputPath / JsonOutputPath / HtmlOutputPath: report output files.
- MaxMessages: max messages fetched per enabled provider.

Environment variable overrides are supported, including:

- MAILGRABBER_ENABLE_OUTLOOK
- MAILGRABBER_ENABLE_GMAIL
- MAILGRABBER_CLIENT_ID
- MAILGRABBER_TENANT_ID
- MAILGRABBER_GMAIL_CLIENT_SECRETS_PATH
- MAILGRABBER_GMAIL_TOKEN_DIRECTORY
- MAILGRABBER_OUTPUT_PATH
- MAILGRABBER_JSON_OUTPUT_PATH
- MAILGRABBER_HTML_OUTPUT_PATH
- MAILGRABBER_WRITE_CSV
- MAILGRABBER_WRITE_JSON
- MAILGRABBER_WRITE_HTML_VIEWER
- MAILGRABBER_AUTH_RECORD_PATH
- MAILGRABBER_TOKEN_CACHE_NAME
- MAILGRABBER_ALLOW_UNENCRYPTED_TOKEN_CACHE
- MAILGRABBER_MAX_MESSAGES
- MAILGRABBER_PAGE_SIZE

## Output Formats

CSV columns:

- Cluster
- Tld
- Domain
- SenderAddress
- SenderName
- Providers
- SourceAccounts
- MessageCount
- FirstSeenUtc
- LastSeenUtc
- IsNewsletter
- SampleSubjects

JSON structure:

- Clusters: array of cluster objects.
- Each cluster has SenderAddresses (array).
- In each sender entry, Providers, SourceAccounts, and SampleSubjects are arrays.

Default files:

- output/sender-clusters.csv
- output/sender-clusters.json
- output/cluster-viewer.html

## Authentication Behavior

- Outlook uses Device Code flow with persistent token cache.
- Gmail uses OAuth with local token persistence.
- First run requires browser authentication. Later runs should usually be silent until tokens are revoked/expired.

## Security and Safe Push Checklist

Before pushing, verify these points:

1. Do not commit appsettings.json.
2. Do not commit google-client-secret.json.
3. Do not commit output files (personal mailbox metadata).
4. Keep only appsettings.example.json in Git.

Run this check:

```bash
git status --short
git grep -nEI "(api[_-]?key|secret|token|password|private[_-]?key|BEGIN (RSA|EC|OPENSSH|PRIVATE)|ghp_[A-Za-z0-9]{20,}|AIza[0-9A-Za-z_-]{35}|ya29\.[0-9A-Za-z_-]+)" -- .
```

## Notes

- Newsletter detection is heuristic and configurable.
- Only metadata is exported, not email bodies.
- Linux fallback can use unencrypted local token cache if secure key storage is unavailable.