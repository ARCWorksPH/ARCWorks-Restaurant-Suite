# ROMS Code Proof Card

Generate a tiny, non-reconstructable engineering evidence file:

```powershell
pwsh -File .\scripts\New-CodeProofCard.ps1
```

The result is written to the ignored local file
`.artifacts/ROMS_CODE_PROOF_CARD.md`. It is capped at 12 KB and contains only:

- a short commit identifier and subject;
- selected public type names and three small allowlisted code excerpts;
- selected test names;
- CI evidence wording; and
- a clear statement of what was withheld.

It contains no source bodies, configuration values, credentials, database
content, private paths, or infrastructure addresses. The script fails if its
output matches common credential, email, local-path, private-key, or IP-address
patterns.
