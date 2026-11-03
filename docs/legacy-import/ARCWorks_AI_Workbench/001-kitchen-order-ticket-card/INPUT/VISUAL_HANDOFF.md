# Visual handoff

## Intent

Create a tactile, glanceable kitchen ticket that can be read at distance under a busy service environment. The card should feel like a dedicated Kitchen Display System, not a generic back-office form.

The reference image is `KDS_REFERENCE.jpg`. Reproduce its hierarchy and operational clarity, not its sample data or unsupported features.

## Hierarchy

1. Table number and explicit state dominate the header.
2. Timer is the largest numeric element and remains immediately scannable.
3. Item quantities and names form a clean vertical rhythm.
4. Item notes are visually subordinate to the item name but impossible to miss.
5. The current permitted action anchors the footer.

## Card anatomy

- Outer card: dark raised surface, 16px radius, 1-2px state-aware border, restrained glow.
- Header: two-column layout. Left contains small `TABLE`, large table number, short order ID. Right contains state badge, waiter, revision.
- Timer band: inset dark panel, small purpose label, large monospaced timer, target/overdue line.
- Items: vertically stacked rows with strong quantity, name, and wrapped note.
- Footer: full-width or balanced actions based on the state matrix.

## Tokens

Use the existing variables without redefining `:root`:

- Background/surfaces: `--roms-bg`, `--roms-surface`, `--roms-surface-raised`, `--roms-surface-soft`.
- Borders/text: `--roms-border`, `--roms-border-strong`, `--roms-text`, `--roms-text-muted`, `--roms-text-disabled`.
- State: `--roms-focus` for New, `--roms-status-preparing`, `--roms-status-ready`, `--roms-danger`.
- Focus: `--roms-focus`.
- Typography: `--roms-font`; use a system monospace stack for timer digits.

## Typography and spacing

- Card base text: 16px minimum at desktop KDS sizes.
- Table number: `clamp(2.75rem, 5vw, 4.75rem)` or a visually equivalent scale constrained inside the card.
- Timer: `clamp(2.5rem, 5vw, 4.5rem)`.
- Item row: at least 1rem; quantity may be state-colored.
- Use an 8px spacing rhythm: 8, 16, 24, 32.
- Avoid tiny metadata below 12px.

## State styling

- New: cool blue/focus border and subtle glow.
- Preparing: amber border and timer emphasis.
- Ready: green border and waiting callout.
- Overdue: add danger text/border cue to the timer region while retaining the underlying order state.
- Do not fill the entire header with saturated color; use controlled accents so white text remains comfortable.

## Motion

- Optional subtle hover/focus elevation only for buttons, 120-180ms.
- No card pulsing, flashing, or timer animation.
- Remove transition effects under `prefers-reduced-motion: reduce`.

## Why

The kitchen reads state under time pressure. Large table/timer values reduce search time; stable card anatomy reduces cognitive load; explicit text plus color supports accessibility; constrained glows preserve the intended premium ARCWorks visual language without becoming decorative noise.
