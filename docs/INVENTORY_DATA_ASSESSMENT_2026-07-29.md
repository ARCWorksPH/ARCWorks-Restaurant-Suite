# Inventory Data Assessment — 2026-07-29

## Decision

The `Resto_Data` package is suitable for sandbox imports, automated tests, and
field-mapping design. It is **not approved for production opening balances or
automatic stock deductions**.

The supplied report describes the material as scraped, analyzed, sample, and
generated. Therefore, item quantities, costs, thresholds, recipes, staff
identities, operational scenarios, and policy decisions must be treated as
unverified until an authorized restaurant representative confirms them.

The raw package should remain outside Git because it includes contact and staff
information and may later be replaced with confidential restaurant data.

## Structural verification

- 35 inventory items, 24 menu items, and 75 recipe ingredient rows.
- All inventory, menu, and recipe IDs are unique.
- Every recipe references an existing inventory item and menu item.
- Every menu item has at least one recipe ingredient.
- Recipe names and units match their referenced records.
- Canonical units are consistently restricted to `piece`, `g`, and `ml`.
- Opening quantities, minimum levels, costs, and recipe quantities are numeric
  and non-negative; recipe quantities are greater than zero.
- The JSON section counts and schemas match the seven CSV tables.
- The file named `.xls` is XML Spreadsheet content rather than a binary Excel
  workbook. The CSV and JSON files are the safer machine-readable sources.

## Production confirmation required

An authorized restaurant representative must confirm or replace:

1. Actual inventory item names and the exact unit used for physical counts.
2. A witnessed opening count, count date/time, and responsible employee.
3. Minimum-stock thresholds and current unit costs.
4. The active menu, selling prices, serving sizes, and availability.
5. Recipe quantities measured from actual kitchen preparation.
6. Which roles may receive, adjust, override, and approve stock discrepancies.
7. The negative-stock policy and the operational response to discrepancies.
8. Waste, spoilage, complimentary-item, void, and staff-meal workflows.

## Policy gap

The proposed dataset policy calls for zero-stock blocking, manager override,
discrepancy alerts, and daily reconciliation. ROMS does not yet implement that
complete policy. Enabling automatic inventory deductions before those controls
and the real data are approved would create an unacceptable operational gap.

`INVENTORY_ENABLED=false` therefore remains the correct production setting.

## Safe onboarding sequence

1. Import the supplied package into an isolated sandbox only.
2. Produce a mapping and validation preview; do not write production data.
3. Replace assumptions with restaurant-confirmed values.
4. Run supervised receiving, order, amendment, cancellation, waste, and count
   reconciliation scenarios.
5. Obtain written sign-off on balances, recipes, units, and policy.
6. Back up production, perform the controlled import, and reconcile totals.
7. Enable inventory only after all acceptance gates pass.
