# Glass theme implementation - 2026-08-11

## Scope

Applied the authoritative smoked-glass theme brief as a presentation-only
change. No workflow, authorization, data, timer, or persistence behavior was
changed.

## Root cause corrected

The earlier UI had a glass helper class, but the application background was a
flat solid and later page-specific rules made most panels 94-98% opaque. That
combination prevents visible background bleed-through and makes `backdrop-filter`
appear ineffective.

## Changes

- Added the subdued cyan/orange aurora application background.
- Made standard panels, table cards, menu cards, summaries, navigation, and KPI
  cards genuinely translucent with cyan-tinted borders and top-edge highlights.
- Kept the kitchen tickets and dense operational data more opaque for distance
  readability.
- Retained the existing status palette and purple-to-cyan primary action
  gradient.
- Added browser fallbacks: each glass surface retains a readable opaque-enough
  background when `backdrop-filter` is unavailable.

## Verification required

Visual inspection must be performed on Tables, Order Editor, Kitchen Display,
Manager, and Inventory in both desktop and mobile viewports. The acceptance
criterion is visibly restrained glass and ambient background bleed-through
without reducing readability of controls, tables, or KDS timers.
