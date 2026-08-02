# Gemini UI Design and Implementation Brief for ROMS

## 1. Authority and objective

This file is the authoritative visual-design brief for the ROMS staff-side
restaurant ordering application.

Gemini is responsible for visual exploration, role-specific mockups, design
tokens, component states, and the visual handoff. Do not infer new product
features from reference images. The existing ROMS backend, authorization rules,
order workflow, audit behavior, and safety controls remain authoritative.

The approved direction is:

> **Soft Bento operational shell + restrained Neo-Glass status accents +
> high-contrast Tactile Kitchen Display.**

Use Concept 2, “Soft Neo-Bento,” as the shared application foundation. Borrow
the table-state clarity from Concept 1, “Neo-Glass.” Use the information density
and distance readability of Concept 3, “Tactile Sci-Fi,” for the Kitchen
Display only.

Do not copy any concept literally. The concept images are visual references,
not functional specifications.

## 2. Required working sequence

### Phase A — design only

1. Inspect the current Razor pages and `roms.css`.
2. Create role-accurate wireframes and polished mockups.
3. Produce the design-token and component-state specifications in this file's
   format.
4. Identify any desired information or action that the backend does not
   currently provide under a separate **Future backend proposal** heading.
5. Stop and obtain approval before changing application source.

### Phase B — visual implementation after approval

1. Preserve all existing service calls, authorization policies, state
   transitions, idempotency behavior, validation, SignalR updates, and audit
   controls.
2. Prefer CSS and presentation-only Razor restructuring.
3. Do not edit domain, application, infrastructure, migration, database,
   authentication, or container code for a visual requirement.
4. If a visual request truly requires backend work, stop and document the
   requirement instead of simulating it in the UI.
5. Implement one operational surface at a time and provide a rendered
   screenshot for approval before continuing.

Recommended implementation order:

1. Tables
2. Order Editor
3. Kitchen Display
4. Inventory
5. Shared navigation and remaining admin pages

## 3. Current ROMS product facts

ROMS is a .NET 10 Blazor Interactive Server PWA using ASP.NET Identity,
MariaDB, EF Core, SignalR, and role-based authorization.

Primary roles and surfaces:

| Role | Primary surfaces |
| --- | --- |
| Waiter | Attendance, Tables, Order Editor |
| Kitchen | Attendance, Kitchen Display |
| Admin | All operational surfaces, catalog, users, attendance administration, payments, reports, inventory |

Current order lifecycle:

```text
Draft → New → Preparing → Ready → Completed
   └──────────────→ Cancelled, when permitted
```

Current table states:

```text
Available
Occupied
Preparing
Ready to serve
Pending payment
```

The following behaviors already matter and must remain visible:

- A waiter selects a table and owns its active order.
- A non-admin waiter cannot open another waiter's active order.
- Draft orders can be edited directly.
- Submitted `New` and `Preparing` orders require an amendment reason.
- Removing an item during `Preparing` requires an administrator.
- Orders carry a visible revision and receive real-time updates.
- Kitchen actions operate at the order level.
- Waiters mark `Ready` orders as served.
- Only an administrator confirms payment.
- Inventory automatic deduction may be disabled while setup remains accessible.
- Low stock must show the item, balance, unit, threshold state, and clear
  warning text.

## 4. Features that must not be invented

Do not show these as working controls or authoritative data:

- waiter-side `Confirm & Pay`;
- taxes, discounts, tips, refunds, or split payments;
- `Hold`, `Bump`, `Reprint`, or printer-routing actions;
- per-item kitchen states such as Waiting, Prepping, or Plated;
- kitchen stations such as Grill, Sauté, Pastry, or Expo;
- promised completion time or ETA;
- persisted preparation timers beyond elapsed order age;
- customer feedback;
- menu photographs unless a real image field and asset workflow are approved;
- stock purchase orders, supplier ledgers, or costing reports;
- manager PIN override or negative-stock approval;
- AI write actions without proposal, deterministic validation, and explicit
  confirmation.

These may appear only in a clearly labeled future-concept annotation outside
the operational screen.

## 5. Shared visual system

### 5.1 Base theme

Use a matte, warm graphite interface. Do not use a photographic page
background.

| Token | Value | Usage |
| --- | --- | --- |
| `--roms-bg` | `#0F141B` | Application canvas |
| `--roms-surface` | `#171E27` | Main panels and cards |
| `--roms-surface-raised` | `#1E2733` | Active or elevated cards |
| `--roms-surface-soft` | `#253140` | Inputs and secondary controls |
| `--roms-border` | `#344153` | Standard borders |
| `--roms-border-strong` | `#526174` | High-emphasis boundaries |
| `--roms-text` | `#F4F7FB` | Primary text |
| `--roms-text-muted` | `#A8B3C2` | Supporting text |
| `--roms-text-disabled` | `#718096` | Disabled text |
| `--roms-primary` | `#2DD4BF` | Primary action and available accent |
| `--roms-secondary` | `#8B5CF6` | Secondary emphasis |
| `--roms-focus` | `#38BDF8` | Keyboard focus and live connection |
| `--roms-danger` | `#F87171` | Destructive action and critical error |

### 5.2 Operational status colors

Status colors must be consistent everywhere.

| State | Color | Required label |
| --- | --- | --- |
| Available | `#2DD4BF` | `Available` |
| Occupied / New | `#60A5FA` | `Occupied` or `New` |
| Preparing | `#F59E0B` | `Preparing` |
| Ready / Ready to serve | `#4ADE80` | `Ready` or `Ready to serve` |
| Pending payment | `#A78BFA` | `Pending payment` |
| Cancelled / destructive | `#F87171` | `Cancelled` or explicit action label |
| Disabled / unavailable | `#64748B` | `Unavailable`, `Disabled`, or reason |

Never use color as the only status cue. Every state requires:

- readable text;
- a distinctive border or icon;
- sufficient contrast against its surface; and
- identical meaning across waiter, kitchen, and admin pages.

### 5.3 Gradients and glow

Use gradients only for:

- the primary call-to-action;
- the selected top-level navigation item; or
- a small brand accent.

Approved primary gradient:

```css
linear-gradient(135deg, #8B5CF6 0%, #38BDF8 52%, #2DD4BF 100%)
```

Glow must be restrained:

```css
box-shadow: 0 0 16px rgba(45, 212, 191, 0.16);
```

Do not place glow around every card. Do not use animated neon effects.
`backdrop-filter` may be used only in the fixed header at a maximum blur of
`8px`; operational cards must remain opaque and readable.

### 5.4 Typography

Use a locally available system stack:

```css
font-family: Inter, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
```

Do not introduce a remote font dependency.

| Element | Desktop/KDS | Tablet/mobile |
| --- | --- | --- |
| Page title | `32px / 1.15`, weight `750` | `28px / 1.2` |
| Section title | `22px / 1.25`, weight `700` | `20px / 1.25` |
| Card title | `18px / 1.25`, weight `700` | `17px / 1.25` |
| Body | `16px / 1.45` | `16px / 1.45` |
| Supporting text | minimum `14px / 1.4` | minimum `14px / 1.4` |
| KDS table/timer | minimum `24px`, weight `750` | not applicable |
| KDS item line | minimum `18px / 1.4` | not applicable |

Avoid decorative condensed sci-fi fonts for operational content.

### 5.5 Spacing, shape, and elevation

Use a 4px spacing scale:

```text
4, 8, 12, 16, 20, 24, 32, 40, 48
```

| Element | Radius |
| --- | --- |
| Main bento panel | `18px` |
| Standard card | `14px` |
| KDS ticket | `10px` |
| Input/button | `10px` |
| Status pill | `999px` |

Standard card elevation:

```css
box-shadow: 0 12px 30px rgba(0, 0, 0, 0.28);
```

Use borders and spacing to establish hierarchy. Do not decorate every element.

### 5.6 Touch, keyboard, and motion

- Minimum interactive size: `48 × 48px`.
- Primary workflow buttons: minimum height `56px`.
- Minimum spacing between destructive and primary actions: `12px`.
- Visible keyboard focus: `3px` outline using `--roms-focus`, with `2px`
  offset.
- Hover must never be the only indication of an available action.
- Interaction transitions: `120ms–180ms`.
- Respect `prefers-reduced-motion`.
- No looping animation, parallax, pulsing status, or moving background.

### 5.7 Accessibility

- Meet WCAG AA: `4.5:1` normal text and `3:1` large text/UI boundaries.
- Preserve browser zoom up to 200 percent.
- Do not encode meaning using color alone.
- Use descriptive button labels rather than icon-only critical actions.
- Show form validation beside the relevant input and in an accessible summary
  when appropriate.
- Loading, empty, error, success, disabled, locked, and reconnecting states
  require explicit designs.
- Kitchen content must remain legible from approximately 2–3 meters on a
  1920×1080 display.

## 6. Responsive targets

Create and verify all core designs at:

| Target | Viewport | Intended use |
| --- | --- | --- |
| Mobile | `390 × 844` | Emergency/limited waiter access |
| Tablet | `1280 × 800` | Primary waiter and admin touch device |
| Desktop | `1440 × 900` | Admin and general operations |
| Kitchen display | `1920 × 1080` | Wall-mounted KDS |

Rules:

- Do not horizontally scroll a primary workflow.
- On tablet, keep current order and menu visible together when space permits.
- Below `800px`, stack the order summary before the menu so current state and
  primary action remain visible.
- Do not require hover.
- Navigation must collapse without hiding the current role or connection state.
- Kitchen tickets may use responsive columns but must never shrink below
  `300px`.

## 7. Shared application shell

Use the Soft Bento shell across the app:

- matte graphite background;
- compact top bar showing ROMS, live/reconnecting status, current user, role,
  and current time;
- role-filtered navigation;
- one selected-navigation gradient;
- high-contrast content canvas;
- consistent error/success notification region.

Do not display navigation destinations the current user cannot access.

Keep these distinct:

- `Clock Out + Log Out` changes attendance and session state.
- `Log Out Only` ends the session without changing attendance.

Never merge these into one ambiguous icon.

## 8. Required page designs

### 8.1 Tables — Waiter and Admin

Visual direction:

- Concept 1 table-state clarity inside the shared Soft Bento shell.
- Use functional rectangular table cards, not literal furniture illustrations.
- Optimize for rapid scanning and touch.

Each table card may show only real current data:

- `Table <number>`;
- state label;
- waiter name when an active order exists;
- current total when greater than zero;
- `Locked` when another waiter owns the order.

Required states:

- Available;
- Occupied;
- Preparing;
- Ready to serve;
- Pending payment;
- Locked;
- loading;
- no configured tables;
- error/reconnecting.

Do not show revenue, feedback, guest counts, or notification totals on this
screen unless the backend later supplies them.

### 8.2 Order Editor — Waiter and Admin

Visual direction:

- Concept 2 three-zone Bento layout.

Desktop/tablet zones:

1. menu category navigation;
2. menu item grid;
3. current order summary and allowed actions.

The current order panel must show:

- table number;
- order status;
- waiter name;
- revision;
- active order lines;
- quantities, notes, line totals, and order total;
- validation and workflow messages;
- only the action allowed by current status and role.

Required primary actions:

| State | Primary action |
| --- | --- |
| Draft | `Send to kitchen` |
| New | Amendment controls with required reason |
| Preparing | Amendment controls; removal is Admin-only |
| Ready | `Mark served` |
| Completed, unpaid | Show `Pending payment`; no waiter payment button |
| Completed, paid | Show completed confirmation |

Cancellation must remain visually separate, require a reason, and never sit
directly beside the main positive action without spacing.

Do not add taxes, discounts, Hold, Clear All, or Confirm & Pay.

Menu item cards currently support:

- name;
- price in Philippine pesos (`₱`);
- description; and
- add action.

Do not require menu photography.

### 8.3 Kitchen Display — Kitchen and Admin

Visual direction:

- Concept 3 Tactile Sci-Fi information density;
- shared color tokens and typography;
- matte cards with crisp status borders;
- no glass blur.

The ticket grid must remain oldest-first.

Each ticket may show:

- table number;
- waiter name;
- short order identifier;
- revision;
- elapsed age from submission;
- order-level status;
- active item quantities, names, and notes;
- one valid order-level action.

Valid actions:

| State | Action |
| --- | --- |
| New | `Start preparing` |
| Preparing | `Ready` |
| Ready | non-interactive `Waiting for waiter` callout |

Do not show per-item preparation states, station assignment, ETA, Hold, Bump,
or Reprint.

Age may influence visual urgency, but it must be labeled as elapsed age rather
than a promised completion time. Proposed warning thresholds must be presented
for product approval before implementation.

### 8.4 Inventory — Admin

Visual direction:

- Concept 2 Bento layout with denser, table-like balance presentation.
- Prioritize numeric alignment and audit clarity over decorative cards.

The page must visibly show when automatic stock deduction is paused.

Required operational sections:

- add inventory item;
- post stock adjustment;
- configure recipe ingredient;
- current balances.

Balance rows must show:

- item name;
- current stock;
- canonical unit;
- low-stock label when applicable.

Form inputs must preserve:

- canonical units `piece`, `g`, or `ml`;
- three-decimal quantity precision;
- required adjustment reason;
- positive recipe quantity.

Do not visually claim that unit cost, supplier, purchase orders, receiving,
waste, approval limits, or negative-stock override are implemented.

Opening balances imported from provisional data must remain visibly
`UNVERIFIED` in any future import-review design.

## 9. LLM assistant visual reservation

The LLM assistant is not a chat-first replacement for operational pages.

Reserve an optional collapsible right-side panel on tablet/desktop. It must not
obscure the current order, kitchen ticket, or inventory balance.

Future panel stages:

```text
User request
→ Proposed interpretation
→ Deterministic validation result
→ Explicit confirmation
→ Authoritative operation result
```

Use these labels exactly:

- `Proposal only`;
- `Validation passed` or `Validation blocked`;
- `Review changes`;
- `Confirm`;
- `Cancel`;
- `Reload authoritative state`.

Do not show autonomous execution, database access, shell access, or a generic
“AI handled it” success message.

## 10. Required component-state sheet

Provide a component sheet containing:

- primary, secondary, quiet, and destructive buttons;
- default, hover, focus, pressed, busy, and disabled button states;
- text, number, select, and reason-required inputs;
- inline validation;
- status pills for every operational state;
- table cards for every state including Locked;
- menu cards;
- order lines including notes and removed-history treatment;
- KDS tickets for New, Preparing, Ready, amended revision, and urgent age;
- inventory balance rows including normal and Low stock;
- application alerts: information, success, warning, error, offline, and
  reconnecting;
- loading skeletons;
- empty states;
- confirmation dialog for destructive actions.

## 11. Deliverables

For Phase A, provide:

1. A style tile with the exact approved tokens.
2. High-fidelity mockups for Tables, Order Editor, Kitchen, and Inventory.
3. Tablet and desktop variants for Tables, Order Editor, and Inventory.
4. A 1920×1080 Kitchen Display.
5. A 390×844 responsive Order Editor example.
6. The complete component-state sheet.
7. A short annotation beside every control explaining the real backend action
   or labeling it as future-only.
8. A list of any proposed backend additions, separated from the approved UI.
9. An asset manifest identifying every icon or image and its license/source.

Preferred visual outputs:

- lossless PNG for review;
- editable source when available;
- SVG for original icons;
- no important labels rasterized into background artwork.

For Phase B, provide after approval:

1. The exact files changed.
2. Before-and-after screenshots.
3. Screenshots at all required viewport targets.
4. A contrast and focus-state check.
5. Confirmation that no backend behavior or authorization rule changed.
6. A list of remaining visual limitations.

## 12. Acceptance checklist

The design is ready for implementation only when all are true:

- [ ] Concept 2 is visibly the shared foundation.
- [ ] Concept 1 influence is limited to restrained state emphasis.
- [ ] Concept 3 influence is concentrated in the Kitchen Display.
- [ ] All controls correspond to real backend actions or are marked future-only.
- [ ] Waiter payment is not introduced.
- [ ] Per-item kitchen workflow is not implied.
- [ ] Role-based navigation is preserved.
- [ ] Philippine peso formatting is used.
- [ ] Inventory-disabled and low-stock states are explicit.
- [ ] Touch targets and responsive layouts meet this brief.
- [ ] Status is understandable without relying on color.
- [ ] Error, loading, empty, offline, and reconnecting states are designed.
- [ ] There is no photographic application background or excessive glow.
- [ ] Phase A has been approved before application code is changed.

## 13. Current implementation references

Inspect these files before designing or implementing:

```text
src/Roms.Web/wwwroot/roms.css
src/Roms.Web/Components/Layout/MainLayout.razor
src/Roms.Web/Components/Layout/NavMenu.razor
src/Roms.Web/Components/Pages/Tables.razor
src/Roms.Web/Components/Pages/OrderEditor.razor
src/Roms.Web/Components/Pages/Kitchen.razor
src/Roms.Web/Components/Pages/Inventory.razor
src/Roms.Application/Contracts.cs
src/Roms.Domain/Entities.cs
```

When a concept conflicts with these workflows, the real ROMS workflow wins.
