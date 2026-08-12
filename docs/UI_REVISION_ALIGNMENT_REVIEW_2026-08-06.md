# UI Revision Alignment Review — 2026-08-06

**Source reviewed:** `C:\Users\GBServerPH\Desktop\UI_REVISION_WORK_LOG.md`
and the five supplied Phase A mockups.

## Decision

The proposed visual direction is compatible with ARCWorks Restaurant Suite,
provided the mockups remain a presentation layer over the approved workflow
and do not reintroduce retired domain features.

The following visual direction is approved in principle:

- dark charcoal/glass surfaces with restrained status-color glows;
- reusable buttons, inputs, status pills, cards, KDS tickets, and alert
  callouts from the component sheet;
- tables overview cards with live order status and elapsed activity;
- KDS cards with prominent timers and clear Start/Ready/Return actions;
- three-pane waiter order editor with category navigation, menu cards, and a
  cart summary;
- responsive layouts that preserve touch targets and readable text.

## Required corrections to the proposed plan

### Inventory mockup

The recipe panel must not be implemented. Recipe definitions, ingredient
relationships, yields, costing, and automatic deduction were deliberately
removed from the active product scope.

The following mockup fields are not currently part of the approved inventory
model and must not be presented as working features without a separate product
decision and schema design:

- Recipe Ingredient Configuration
- Supplier
- Unit Cost
- Barcode
- Total inventory value
- Automatic stock deduction

The inventory bento layout may still be adopted using the supported functions:

- Add/edit independent inventory item
- Unit and minimum-stock threshold
- Receive stock
- Physical count reconciliation
- Manual adjustment and Admin/Owner negative-stock override
- Current balance and low-stock status
- Movement and count history

The banner should say that automatic deduction is unavailable or out of scope,
not imply that recipes can be configured.

### Role-aware navigation

The horizontal navigation concept may replace the current visual sidebar, but
route visibility and server-side authorization must remain intact:

- Waiter sees waiter/table/order actions only.
- Kitchen sees KDS and operational availability actions only.
- Manager sees live/current-shift dashboards and configuration actions, not
  actionable waiter or kitchen interfaces or unrestricted historical records.
- Admin/Owner sees system administration, historical data, payments,
  corrections, and protected overrides.

The UI must never rely on hiding a button as the security boundary.

### Timers and live status

The tables and KDS mockups align with the approved timer direction, but the
display must use authoritative persisted values:

- one standard Manager-configured waiter order-entry timer;
- one standard Manager-configured kitchen acceptance timer;
- Admin-configured per-item preparation minutes multiplied by quantity;
- retained extension and return/resubmission history.

SignalR may refresh the display, but the database remains authoritative after a
stale or concurrent update.

### Menu imagery

Food photographs are optional presentation assets. If used, they must be local
versioned assets with a neutral fallback when an image is missing. They must
not change menu identity, price, availability, or order behavior.

## Recommended implementation order

1. Finish the approved workflow foundation: Manager role, return/resubmission,
   timer persistence, extension handling, and per-item preparation targets.
2. Apply component-sheet tokens and state styles without changing behavior.
3. Revise the tables overview while preserving existing status semantics.
4. Revise the KDS with authoritative timers and explicit return controls.
5. Revise the waiter order editor with category navigation and cart controls.
6. Apply the inventory bento layout using only the independent-item ledger.
7. Run role-based browser acceptance and responsive visual checks.

## Acceptance boundary

The supplied mockups are design references, not evidence that the corresponding
features already exist. A visual control is accepted only when its underlying
authorization, persistence, audit behavior, and negative/error state have also
passed testing.
