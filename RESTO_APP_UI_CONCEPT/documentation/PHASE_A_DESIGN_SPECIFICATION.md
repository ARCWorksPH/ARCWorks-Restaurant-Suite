# ROMS Phase A — Design and Specification Document

**Selected Direction:** Concept 1 "Neo-Glass & Glow" accents embedded within Concept 2 "Soft Neo-Bento" operational shell + Concept 3 "Tactile Sci-Fi" KDS.

---

## 1. Master Style Tile & Design Tokens

### 1.1 Canvas & Surfaces
```css
:root {
  /* Surface Tokens */
  --roms-bg: #0F141B;             /* Base application dark slate canvas */
  --roms-surface: #171E27;        /* Standard bento panels and card containers */
  --roms-surface-raised: #1E2733; /* Elevated/focused cards */
  --roms-surface-soft: #253140;   /* Inputs, secondary buttons, soft containers */
  
  /* Borders */
  --roms-border: #344153;        /* Standard subtle border */
  --roms-border-strong: #526174; /* High-contrast section boundaries */
  
  /* Typography */
  --roms-text: #F4F7FB;          /* High contrast primary text */
  --roms-text-muted: #A8B3C2;    /* Secondary/supporting label text */
  --roms-text-disabled: #718096; /* Disabled text */
  
  /* Accent Tokens */
  --roms-primary: #2DD4BF;       /* Luminous Cyan-Teal (Primary CTA / Available status) */
  --roms-secondary: #8B5CF6;     /* Vivid Purple accent */
  --roms-focus: #38BDF8;         /* Accessible blue focus ring */
  --roms-danger: #F87171;        /* Red for cancellations & low stock warnings */
  
  /* Shared Primary Gradient */
  --roms-gradient-primary: linear-gradient(135deg, #8B5CF6 0%, #38BDF8 52%, #2DD4BF 100%);
  
  /* Elevation & Glow */
  --roms-shadow-card: 0 12px 30px rgba(0, 0, 0, 0.28);
  --roms-glow-primary: 0 0 16px rgba(45, 212, 191, 0.16);
}
```

### 1.2 Status Color Mapping

| Status State | Color Token | Hex Code | Accessible Text Label |
| :--- | :--- | :--- | :--- |
| **Available** | `--roms-status-available` | `#2DD4BF` | `Available` |
| **Occupied / New** | `--roms-status-occupied` | `#60A5FA` | `Occupied` or `New` |
| **Preparing** | `--roms-status-preparing` | `#F59E0B` | `Preparing` |
| **Ready / Ready to serve** | `--roms-status-ready` | `#4ADE80` | `Ready` or `Ready to serve` |
| **Pending Payment** | `--roms-status-pending-payment` | `#A78BFA` | `Pending payment` |
| **Cancelled / Destructive** | `--roms-status-cancelled` | `#F87171` | `Cancelled` |
| **Disabled / Locked** | `--roms-status-locked` | `#64748B` | `Locked` / `Unavailable` |

---

## 2. Operational Surface Mockups Summary

The following high-fidelity mockup renders have been produced and archived in `RESTO_APP_UI_CONCEPT/mockups/`:

1. **Tables View (`resto_phase_a_tables_mockup`)**:
   - Soft Bento layout displaying rectangular table cards with status-colored LED pill indicators and waiter ownership tags.
   - Shows Available, Occupied, Preparing, Ready to Serve, Pending Payment, and Locked states.

2. **Order Editor (`resto_phase_a_order_editor_mockup`)**:
   - 3-zone Bento structure (Category nav, Menu Item Grid with Philippine Pesos `₱`, Active Order Cart Sidebar).
   - Touch-first buttons with min 48px targets and primary gradient CTA button ("Send to Kitchen").

3. **Kitchen Display System (`resto_phase_a_kds_1080p_mockup`)**:
   - 1920x1080 wall-mounted Tactile KDS layout with high-contrast tickets, clear table numbers, order item quantities, notes, and elapsed age timers.

4. **Inventory Page (`resto_phase_a_inventory_mockup`)**:
   - Bento panels for Add Item, Stock Adjustment with mandatory reason field, Recipe Ingredient mapping, and Current Balances with Low Stock badges.

5. **Component State Sheet (`resto_phase_a_component_sheet_mockup`)**:
   - Specifications for buttons (Default, Hover, Focus, Busy, Disabled), inputs, status badges, order lines, and alert banners.

---

## 3. Future Backend Proposals

The following features were identified during visual planning as potential future enhancements, but are **explicitly excluded** from visual implementation in Phase B to protect current backend contracts:

1. **Per-Item Preparation Timestamps / Kitchen Station Routing**: Ability to track item-level cooking progress (e.g. Grill vs Drinks) rather than order-level transitions.
2. **Customer Feedback / Guest Count Snapshots**: Displaying guest seat counts on table cards.
3. **Printed Kitchen Ticket Receipts / Audio Chimes**: Automatic printer routing upon `SubmitAsync`.
4. **Supplier PO / Inventory Unit Costing**: Full supplier purchase orders and unit cost accounting.

---

## 4. Asset Manifest

- **Font Family**: Inter, Segoe UI, Roboto, Helvetica, Arial, sans-serif (System fallback stack, zero external web-font dependencies).
- **Icons**: Inline SVG / Unicode status symbols (⌂ Dashboard, ◷ Attendance, ▦ Tables, ▤ Kitchen, ▥ Inventory).
- **Mockup Artifacts**: Generated losslessly into `RESTO_APP_UI_CONCEPT/mockups/`.
