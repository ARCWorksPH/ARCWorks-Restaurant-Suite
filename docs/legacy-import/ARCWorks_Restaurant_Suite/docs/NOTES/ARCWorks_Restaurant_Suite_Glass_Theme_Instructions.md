# ARCWorks Restaurant Suite — Glass Theme Visual Instructions

**Purpose**  
This document is the authoritative visual theme brief for the ARCWorks Restaurant Suite (ROMS).  
Any AI agent, designer, or implementer reading this file must produce a **restrained dark smoked-glass** interface.  
The goal is a polished, modern business application that feels premium to non-technical restaurant staff while remaining highly readable under real restaurant lighting conditions.

**Priority order (never reverse):**  
1. Readability & operational clarity  
2. Functional clarity (status, actions, ownership)  
3. Performance  
4. Visual polish  
5. Glass effects  

Do not sacrifice the first three items to increase glassiness.

---

## 1. Core Visual Concept

**Target look:**  
Dark smoked glass suspended over a quiet navy/charcoal aurora background.  
Think “modern command center for a restaurant”, not cyberpunk, not neon, not flat Material Design.

Key characteristics every generated image, mockup, or CSS implementation **must** show:

- Semi-transparent dark panels so the background softly shows through.
- Visible but restrained backdrop blur on major containers.
- Thin cyan-tinted borders.
- Very subtle white highlight on the top edge of glass panels.
- Soft ambient cyan glow (left / upper-left) and soft orange glow (right / lower-right).
- Existing brand gradient (purple → cyan) kept only for primary actions and active navigation.
- Status colors remain strong and high-contrast.
- Dense data (tables, order lines, KDS tickets) use higher opacity than summary cards.

**Forbidden:**  
Photographic backgrounds, visible grids, circuitry, stars, sharp polygons, bright neon patterns, heavy animated glows, busy decorative images, pure flat solid cards with no translucency.

---

## 2. Application Background (Mandatory)

```css
html, body {
    min-height: 100%;
}

body {
    background:
        radial-gradient(
            circle at 15% 20%,
            rgba(34, 211, 238, 0.16),
            transparent 32%
        ),
        radial-gradient(
            circle at 85% 75%,
            rgba(255, 107, 31, 0.12),
            transparent 30%
        ),
        radial-gradient(
            circle at 65% 15%,
            rgba(80, 100, 180, 0.08),
            transparent 35%
        ),
        linear-gradient(
            135deg,
            #0a1018 0%,
            #101823 45%,
            #090e15 100%
        );
    background-attachment: fixed;
    background-repeat: no-repeat;
    background-size: cover;
}
```

The cyan and orange gradients must behave like soft light sources behind the UI.  
They should be visible mostly **through** translucent panels and around page edges — never as obvious hard circles.

Optional soft haze (use only if the background feels too flat):

```css
body::before {
    content: "";
    position: fixed;
    inset: 0;
    pointer-events: none;
    z-index: -1;
    background:
        radial-gradient(ellipse at 0% 55%, rgba(0, 210, 225, 0.07), transparent 32%),
        radial-gradient(ellipse at 100% 65%, rgba(255, 90, 20, 0.06), transparent 30%);
    filter: blur(14px);
}
```

---

## 3. Glass Panel System

### 3.1 Primary Glass Panels (cards, sections, forms, chart containers)

```css
.glass-panel {
    background: rgba(28, 35, 47, 0.58);
    backdrop-filter: blur(18px) saturate(130%);
    -webkit-backdrop-filter: blur(18px) saturate(130%);
    border: 1px solid rgba(116, 220, 235, 0.20);
    border-radius: 16px;
    box-shadow:
        0 8px 30px rgba(0, 0, 0, 0.25),
        inset 0 1px 0 rgba(255, 255, 255, 0.04);
}
```

If a panel contains dense tables or critical forms, raise opacity:

```css
background: rgba(28, 35, 47, 0.68);
```

### 3.2 Secondary / Nested Surfaces (inputs, small cards, sub-panels)

```css
.glass-surface {
    background: rgba(15, 23, 34, 0.46);
    border: 1px solid rgba(150, 225, 235, 0.11);
    border-radius: 10px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.025);
}
```

### 3.3 Data Surfaces (tables, order lines, KDS ticket bodies)

These must stay more opaque for readability:

```css
.data-surface {
    background: rgba(13, 19, 28, 0.72);
}
```

### 3.4 Fallback (required)

```css
.glass-panel {
    background: rgba(28, 35, 47, 0.72);
}

@supports (backdrop-filter: blur(18px)) or (-webkit-backdrop-filter: blur(18px)) {
    .glass-panel {
        background: rgba(28, 35, 47, 0.58);
        backdrop-filter: blur(18px) saturate(130%);
        -webkit-backdrop-filter: blur(18px) saturate(130%);
    }
}
```

---

## 4. Header / Navigation

The top bar and side navigation must feel like a stable application frame (slightly darker and more opaque than content panels).

```css
.app-header {
    background: rgba(17, 24, 34, 0.78);
    backdrop-filter: blur(20px) saturate(125%);
    -webkit-backdrop-filter: blur(20px) saturate(125%);
    border-bottom: 1px solid rgba(116, 220, 235, 0.10);
    box-shadow:
        0 8px 24px rgba(0, 0, 0, 0.18),
        inset 0 -1px 0 rgba(255, 255, 255, 0.02);
}
```

### Navigation items

Default:
```css
background: rgba(255, 255, 255, 0.015);
border: 1px solid rgba(180, 220, 230, 0.18);
color: rgba(240, 246, 250, 0.82);
```

Hover:
```css
background: rgba(50, 200, 225, 0.08);
border-color: rgba(75, 215, 235, 0.28);
```

Active (use the brand gradient or soft cyan fill — keep restrained):
```css
background: linear-gradient(135deg, rgba(139, 92, 246, 0.25), rgba(45, 212, 191, 0.25));
border-color: rgba(90, 225, 245, 0.32);
box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.04);
```

---

## 5. Primary Action Buttons

Keep the existing brand gradient (purple → cyan) as the dominant action color.

```css
.btn-primary {
    background: linear-gradient(135deg, #8B5CF6 0%, #38BDF8 52%, #2DD4BF 100%);
    color: #ffffff;
    border: 1px solid rgba(255, 255, 255, 0.12);
    border-radius: 10px;
    box-shadow:
        0 5px 16px rgba(45, 212, 191, 0.18),
        inset 0 1px 0 rgba(255, 255, 255, 0.16);
}

.btn-primary:hover {
    filter: brightness(1.06);
    box-shadow:
        0 7px 20px rgba(45, 212, 191, 0.24),
        inset 0 1px 0 rgba(255, 255, 255, 0.18);
}
```

Keep glow restrained. This is restaurant software, not a game UI.

---

## 6. Status Colors (must remain high-contrast)

These colors are already in the product and must stay strong even on glass surfaces:

| State              | Color     | Usage                          |
|--------------------|-----------|--------------------------------|
| Available          | `#2DD4BF` | Table available, success       |
| Occupied / New     | `#60A5FA` | New orders                     |
| Preparing          | `#F59E0B` | Kitchen preparing              |
| Ready              | `#4ADE80` | Ready to serve                 |
| Pending payment    | `#A78BFA` | Awaiting payment               |
| Cancelled / Danger | `#F87171` | Errors, destructive actions    |
| Locked             | `#64748B` | Owned by another waiter        |

Status pills and table-card borders must use these colors with sufficient opacity so they remain immediately readable.

---

## 7. Form Inputs

```css
input, select, textarea {
    background: rgba(8, 15, 24, 0.56);
    color: #f4f7fa;
    border: 1px solid rgba(130, 210, 225, 0.26);
    border-radius: 8px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.02);
}

input:focus, select:focus, textarea:focus {
    outline: none;
    border-color: rgba(53, 216, 238, 0.60);
    box-shadow:
        0 0 0 3px rgba(53, 216, 238, 0.08),
        inset 0 1px 0 rgba(255, 255, 255, 0.03);
}
```

Never make inputs extremely transparent — form usability is more important than the glass effect.

---

## 8. Tables & Dense Data

```css
.data-table {
    background: rgba(13, 19, 28, 0.70);
    border: 1px solid rgba(120, 210, 225, 0.14);
    border-radius: 10px;
    overflow: hidden;
}

.data-table thead {
    background: rgba(95, 120, 145, 0.24);
}

.data-table tbody tr {
    background: rgba(255, 255, 255, 0.012);
}

.data-table tbody tr:nth-child(even) {
    background: rgba(255, 255, 255, 0.028);
}

.data-table tbody tr:hover {
    background: rgba(53, 216, 238, 0.055);
}
```

Text contrast must remain strong.

---

## 9. KPI / Metric Cards

```css
.kpi-card {
    background: rgba(34, 42, 55, 0.55);
    backdrop-filter: blur(16px) saturate(125%);
    -webkit-backdrop-filter: blur(16px) saturate(125%);
    border: 1px solid rgba(116, 220, 235, 0.17);
    border-radius: 14px;
    box-shadow:
        0 7px 20px rgba(0, 0, 0, 0.18),
        inset 0 1px 0 rgba(255, 255, 255, 0.035);
}
```

---

## 10. Kitchen Display (KDS) Special Rules

The Kitchen Display prioritizes distance readability (2–3 meters on 1920×1080).

- Tickets may use slightly higher opacity than normal cards.
- Status borders must stay thick and high-contrast.
- Timer / age text must remain large and clear.
- Avoid heavy blur on ticket bodies — keep the glass effect lighter here.
- Green “Ready” buttons must stay highly visible.

---

## 11. Typography

```css
body {
    color: #f2f5f8;
    font-family: Inter, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
}

.page-title, .panel-title {
    color: rgba(248, 250, 252, 0.96);
}

.secondary-text, .help-text, .panel-subtitle {
    color: rgba(205, 216, 226, 0.68);
}

.label-text {
    color: rgba(205, 216, 226, 0.72);
}
```

Do not reduce secondary-text contrast aggressively. This is an operational interface.

---

## 12. Border Radius Hierarchy

```css
--radius-panel: 16px;
--radius-card: 14px;
--radius-control: 8px;
--radius-button: 10px;
--radius-pill: 999px;
```

---

## 13. Theme Variables (recommended centralization)

```css
:root {
    --arc-bg-deep: #090e15;
    --arc-bg-mid: #101823;
    --arc-bg-soft: #0a1018;

    --arc-glass: rgba(28, 35, 47, 0.58);
    --arc-glass-dense: rgba(28, 35, 47, 0.68);
    --arc-surface: rgba(15, 23, 34, 0.46);
    --arc-table: rgba(13, 19, 28, 0.70);

    --arc-cyan: #35d8ee;
    --arc-cyan-soft: rgba(53, 216, 238, 0.18);
    --arc-cyan-border: rgba(116, 220, 235, 0.20);

    --arc-orange: #ff6b1f;
    --arc-orange-soft: rgba(255, 107, 31, 0.14);

    --arc-text-main: rgba(248, 250, 252, 0.96);
    --arc-text-secondary: rgba(205, 216, 226, 0.68);

    --arc-radius-panel: 16px;
    --arc-radius-card: 14px;
    --arc-radius-control: 8px;
    --arc-radius-button: 10px;

    /* Existing brand tokens kept for compatibility */
    --roms-primary: #2DD4BF;
    --roms-secondary: #8B5CF6;
    --roms-focus: #38BDF8;
    --roms-danger: #F87171;
}
```

---

## 14. Page-Specific Guidance

### Login
- Large centered glass panel.
- Soft ambient cyan glow behind the form.
- Primary gradient button remains prominent.

### Tables Overview
- Table cards use medium glass effect.
- Status border colors must stay strong (cyan = Available, orange = Preparing, etc.).
- “Open Draft / View Order” links remain clearly clickable.

### Order Editor
- Three-zone layout stays intact.
- Menu cards and order summary use glass panels.
- Current order panel can be slightly more opaque for clarity.

### Kitchen Display
- Higher opacity on tickets for distance readability.
- Large timers and status badges remain high-contrast.
- Green Ready buttons stay solid and bright.

### Manager / Inventory / Reports
- KPI cards: medium transparency.
- Data tables: higher opacity.
- Forms: medium-high opacity.

---

## 15. Implementation Rules for AI Agents

When generating images, mockups, or CSS:

1. **Always** render the background with the soft cyan + orange ambient glows.
2. **Always** make major panels semi-transparent so the background bleeds through.
3. **Always** include the thin cyan-tinted border and the subtle top-edge white highlight.
4. **Never** produce pure flat solid dark cards.
5. **Never** make every surface equally transparent — dense data areas stay more opaque.
6. **Never** add photographic backgrounds, neon grids, or heavy animated effects.
7. Preserve all existing status colors, brand gradient, and role-based navigation.
8. Keep text high-contrast. If the glass effect reduces readability, increase panel opacity.
9. Touch targets remain ≥ 48×48 px. Primary buttons ≥ 56 px height.
10. Respect the existing Soft Bento layout structure — this is a visual theme layer, not a layout redesign.

---

## 16. Final Visual Target

The finished ARCWorks Restaurant Suite should look like **dark smoked glass floating over a subdued cyan-and-orange aurora**.

The glass must be noticeable on first glance, then become quiet so staff can work without distraction.

When in doubt, choose readability over stronger glass effects.

---

**End of instructions.**  
Any agent producing UI, mockups, or CSS for this product must follow the rules above.
