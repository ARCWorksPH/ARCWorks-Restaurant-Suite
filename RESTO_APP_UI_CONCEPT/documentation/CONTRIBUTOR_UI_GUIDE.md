# ROMS UI Contributor & Developer Guide

Welcome to the **ROMS (Restaurant Order Management System)** visual frontend and UI contributor guide. This document provides clear architectural rules, design token references, and component guidelines for developers working on the ROMS user interface.

---

## 1. Design System Philosophy

ROMS uses a hybrid dark-theme operational design system:
- **Operational Shell:** Concept 2 "Soft Neo-Bento" matte dark graphite canvas (`#0F141B`) with elevated surface cards (`#171E27`) and rounded radii (`14px`–`18px`).
- **Status Accents:** Concept 1 "Neo-Glass & Glow" luminous state badges, LED pill indicators, and subtle translucent border accents.
- **Kitchen Display (KDS):** Concept 3 "Tactile Sci-Fi" high-contrast tickets optimized for distance readability (2–3 meters) on wall-mounted 1920×1080 displays.

---

## 2. Blazor CSS Isolation & Styling Architecture

ROMS uses .NET 10 Blazor Interactive Server. Styling is structured into two layers:

### 2.1 Master Global Tokens (`roms.css`)
Location: [src/Roms.Web/wwwroot/roms.css](file:///d:/ARCWorks_Restaurant%20Suite/src/Roms.Web/wwwroot/roms.css)
- Contains master CSS variables (`:root`), global typography, utility classes, input field styles, alert callouts, global `:focus-visible` focus rings, and `@media (prefers-reduced-motion: reduce)` accessibility rules.

### 2.2 Component-Scoped CSS Files
- `src/Roms.Web/Components/Layout/MainLayout.razor.css`
- `src/Roms.Web/Components/Layout/NavMenu.razor.css`
- `src/Roms.Web/Components/Layout/ReconnectModal.razor.css`

> [!IMPORTANT]
> **Blazor CSS Isolation Rule:** Blazor generates `Roms.Web.styles.css` from scoped `.razor.css` files and loads it **after** `roms.css`. Therefore, scoped component CSS rules override global rules. When modifying shell components (`MainLayout`, `NavMenu`, `ReconnectModal`), always update their corresponding `.razor.css` file directly instead of adding `!important` rules to `roms.css`.

---

## 3. Design Tokens Reference

When creating or modifying components, use these standard CSS variables:

```css
:root {
  /* Surfaces */
  --roms-bg: #0F141B;             /* Base dark slate canvas */
  --roms-surface: #171E27;        /* Bento card & panel container */
  --roms-surface-raised: #1E2733; /* Elevated/focused cards */
  --roms-surface-soft: #253140;   /* Inputs, select fields, soft containers */
  
  /* Borders & Dividers */
  --roms-border: #344153;        /* Standard border */
  --roms-border-strong: #526174; /* High-emphasis card boundary */
  
  /* Text & Labels */
  --roms-text: #F4F7FB;          /* Primary high-contrast text */
  --roms-text-muted: #A8B3C2;    /* Secondary/supporting label text */
  --roms-text-disabled: #718096; /* Disabled control text */
  
  /* Core Brand Accents */
  --roms-primary: #2DD4BF;       /* Luminous Cyan-Teal (Primary CTA) */
  --roms-secondary: #8B5CF6;     /* Vivid Purple */
  --roms-focus: #38BDF8;         /* Accessible blue focus outline ring */
  --roms-danger: #F87171;        /* Red for cancellations & low stock */
  
  /* Shared Gradients & Shadows */
  --roms-gradient-primary: linear-gradient(135deg, #8B5CF6 0%, #38BDF8 52%, #2DD4BF 100%);
  --roms-shadow-card: 0 12px 30px rgba(0, 0, 0, 0.35);
}
```

### Operational Status Colors

| Operational State | Hex Code | Class / Token | Standard Text Label |
| :--- | :--- | :--- | :--- |
| **Available** | `#2DD4BF` | `.status-available` | `Available` |
| **Occupied / New** | `#60A5FA` | `.status-occupied`, `.status-new` | `Occupied` or `New` |
| **Preparing** | `#F59E0B` | `.status-preparing` | `Preparing` |
| **Ready / Ready to serve** | `#4ADE80` | `.status-ready` | `Ready` or `Ready to serve` |
| **Pending Payment** | `#A78BFA` | `.status-pendingpayment` | `Pending payment` |
| **Cancelled** | `#F87171` | `.status-cancelled` | `Cancelled` |
| **Locked** | `#64748B` | `.status-locked` | `Locked` / `Unavailable` |

---

## 4. UI Patterns & Ergonomic Guidelines

1. **Touch Ergonomics:**
   - Interactive buttons and inputs must be at least **48×48px**.
   - Primary workflow CTA buttons (e.g., "Send to Kitchen", "Start Preparing") should be **56px** high (`.touch-button`, `.btn-lg`).
2. **Keyboard Focus & Accessibility:**
   - Never use `outline: none` without providing an alternative focus ring.
   - All interactive controls use `:focus-visible { outline: 3px solid var(--roms-focus); outline-offset: 2px; }`.
3. **Dedicated KDS Mode (`/kitchen`):**
   - Route-aware `kds-mode` collapses the 250px navigation rail on `/kitchen` to a compact 60px rail, maximizing screen real estate.
   - KDS tickets must have a minimum width of `300px`. Table headers and elapsed timers must be at least `24px` (`1.5rem`), item text `18px` (`1.15rem`), and item notes highlighted in red (`#F87171`).
4. **Form Controls:**
   - Form inputs (`.form-control`, `.form-select`) use `--roms-surface-soft` (`#253140`) with `--roms-border` (`#344153`).
   - For unit selections (such as Inventory item units), **always** constrain choices to canonical values (`piece`, `g`, `ml`) via a `<select>` dropdown.

---

## 5. Backend Protection Rules

When modifying or adding UI components:
- **Do NOT edit domain, entity, application service, migration, or authorization code** for a visual requirement.
- **Do NOT invent non-existent backend features** in Razor templates (e.g., waiter-side payment buttons, per-item kitchen states, supplier POs).
- If a visual request requires a new backend field or API, document it under **Future Backend Proposals** in `PHASE_A_DESIGN_SPECIFICATION.md`.

---

## 6. Pre-Commit Verification Workflow

Before pushing UI changes to Git/GitHub, every contributor must run this exact verification sequence:

```powershell
# 1. Inspect seed passwords
pwsh tools/Test-NoCommittedSeedPasswords.ps1

# 2. Check whitespace and git diff errors
git diff --check

# 3. Build release configuration
dotnet build Roms.slnx --configuration Release -m:1

# 4. Run automated test suite
dotnet test Roms.slnx --configuration Release --no-build

# 5. Update Change Log
# Add an entry to RESTO_APP_UI_CONCEPT/documentation/CHANGE_LOG.md and docs/WORK_LOG.md
```

---

## 7. Change Log Protocol

> [!IMPORTANT]
> **Mandatory Change Logging:** Every time a change is made to the codebase or UI assets:
> 1. Add a dated entry to [`RESTO_APP_UI_CONCEPT/documentation/CHANGE_LOG.md`](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/CHANGE_LOG.md) summarizing added features, bug fixes, or visual refinements.
> 2. Update [`docs/WORK_LOG.md`](file:///d:/ARCWorks_Restaurant%20Suite/docs/WORK_LOG.md) to record high-level architectural progress.
