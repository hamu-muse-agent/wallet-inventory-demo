# Wallet Inventory — detection core demo

A small demonstration of the detection engine proposed for the
"Crypto Wallet Analyzer (Local PC Inventory Tool)" project on LaborX.

It lists crypto wallet software **installed** on a Windows PC:

- Desktop wallet applications (via registry uninstall entries)
- Browser wallet extensions (via browser profile folders, matched
  against a catalog of known extension IDs)

For each item it shows only: wallet name, type, browser, browser
profile, and installation date.

## Safety scope — what this tool deliberately does NOT do

- Never opens wallet software
- Never reads wallet data stores (no Local Extension Settings, no
  IndexedDB contents, no wallet files) — for extensions it looks at
  directory **names** (extension IDs) only, never their contents
- Never accesses seed phrases, private keys, or balances
- No network calls, no telemetry — local console output only

## Running the demo

```
dotnet run
```

Requires the .NET 8 SDK on Windows. The full project delivers the
same engine behind a WPF interface, packaged as a self-contained
single-file application, with the wallet catalog as an editable file
so new wallets can be added without code changes.

## Roadmap to the full tool

1. This detection core + WPF main window (stage 1)
2. Full catalog coverage, Firefox profiles, install-date refinement (stage 2)
3. Polished UI, single-file packaging, documentation (stage 3)
