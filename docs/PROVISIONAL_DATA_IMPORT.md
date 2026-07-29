# Provisional Restaurant Data Import

## Purpose

`Roms.ProvisionalImport` validates and imports the supplied restaurant-like JSON
into an isolated, empty sandbox. It is not a production seeding mechanism.

The importer creates:

- menu categories and menu items;
- inventory items and minimum-stock levels;
- recipe ingredient relationships;
- opening balances as auditable `Receipt` stock movements; and
- one audit record containing the source SHA-256 and imported counts.

Every opening balance is marked `UNVERIFIED`.

## Intentionally unmapped

The Phase 1 model does not import inventory category, unit cost, storage
location, serving size, contact details, employee permissions, scenario logs,
or the proposed negative-stock policy. Preview mode reports these exclusions.
They must not be silently interpreted as implemented product behavior.

## Preview

Preview reads the JSON and performs no database connection or write:

```powershell
dotnet run --project tools\Roms.ProvisionalImport -- preview `
  Resto_Data\bob_marlin_database_seed.json
```

Exit code `0` means validation passed. The JSON output includes the source hash,
record counts, errors, and warnings.

Validation covers required values, unique external IDs, supported canonical
units, database precision and length limits, exact count timestamps, recipe
foreign keys, recipe name/unit agreement, positive quantities, duplicate
menu/ingredient pairs, and recipe coverage for every menu item.

## Apply to a disposable sandbox

Apply is refused unless all of the following are true:

1. Preview validation passes.
2. `--confirm-empty-sandbox` is supplied.
3. `ROMS_PROVISIONAL_IMPORT_CONNECTION` is explicitly set.
4. The server is `localhost`, `127.0.0.1`, or `::1`.
5. The database name contains `sandbox`.
6. The database has no operational ROMS data.

Example using a deliberately local sandbox connection:

```powershell
$env:ROMS_PROVISIONAL_IMPORT_CONNECTION = `
  "Server=127.0.0.1;Port=<port>;Database=roms_sandbox;User=<user>;Password=<password>;SslMode=Disabled"

dotnet run --project tools\Roms.ProvisionalImport -- apply `
  Resto_Data\bob_marlin_database_seed.json `
  --confirm-empty-sandbox

Remove-Item Env:\ROMS_PROVISIONAL_IMPORT_CONNECTION
```

The import runs as one execution-strategy-managed database transaction. Any
validation error, existing operational data, or database failure prevents a
partial import.

## Production boundary

Do not point this utility at the production database, rename the production
database to bypass the sandbox guard, or enable inventory based on its output.
Production onboarding requires confirmed restaurant values, a backup, an
approved production migration procedure, reconciliation, and supervised
acceptance testing.
