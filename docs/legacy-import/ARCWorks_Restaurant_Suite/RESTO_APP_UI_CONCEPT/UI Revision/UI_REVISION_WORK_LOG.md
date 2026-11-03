# ROMS UI Mockup vs. Current Implementation Work Log

**Date:** August 6, 2026  
**Author:** Antigravity AI  
**Folder Path:** `D:\ARCWorks_Restaurant Suite\RESTO_APP_UI_CONCEPT\UI Revision`  
**Scope:** Scanning, Comparison, and Adoption Plan for UI Redesign (Strictly Read-Only Analysis)

---

## 1. Executive Summary & Verification Policies

This document represents the technical audit and UI comparison work log for the **Restaurant Ordering & Management System (ROMS)**. As requested, no source code or database changes have been performed during this task. The repository is treated on a strictly **read-only basis** with the exception of this documentation file.

This log is organized into the following sections:
1. **Scanning the Current UI Design:** Overview of the architecture, layout system, CSS configurations, and page structures currently implemented in the codebase.
2. **Scanning the Uploaded Mockup Images:** Detailed review of the five visual concepts provided by the user.
3. **UI Design Comparison:** High-level comparative analysis of the layout paradigms.
4. **Detailed Differences & Discrepancies:** Section-by-section breakdown of visual, structural, and behavioral mismatches.
5. **Detailed Mockup Adoption Plan:** Step-by-step roadmap to implement the mockup design within the Blazor application while respecting backend boundaries.

> [!WARNING]
> **Functional Inconsistencies Detected:** Several elements in the user's mockup designs (such as *Recipe Ingredient Configuration*, *Supplier*, *Unit Cost* fields in Inventory, and *Menu photographs* in the Order Editor) represent features that do not exist or were explicitly deprecated/removed in the backend domain (e.g., the `RemoveRecipeFunctionality` EF Core migration executed on `2026-08-02` which dropped the `RecipeIngredients` table). Adopting these fields fully requires backend database schema extensions or UI-level mocks/placeholders.

---

## 2. Scanning the Current UI Design (Step 1)

### 2.1 Project Architecture & CSS Setup
The project is built as a **C# ASP.NET Core Blazor Web App** using a hybrid of **Bootstrap 5** and custom styling rules.
- **Main stylesheet:** [`roms.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/wwwroot/roms.css) defines the color tokens, bento card variables, transitions, and status-pill utility classes.
- **CSS Isolation:** Scoped styles exist for the main page frame ([`MainLayout.razor.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Layout/MainLayout.razor.css)) and navigation ([`NavMenu.razor.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Layout/NavMenu.razor.css)). These files currently control sidebar borders, top-row headers, and collapsibility behavior.

### 2.2 Current Theme and Colors
The current CSS uses a deep charcoal theme defined by CSS custom properties in [`roms.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/wwwroot/roms.css#L1-L28):
- Background (`--roms-bg`): `#0F141B`
- Card surfaces (`--roms-surface`): `#171E27`
- Raised surfaces (`--roms-surface-raised`): `#1E2733`
- Borders (`--roms-border`): `#344153`
- Primary gradient (`--roms-gradient-primary`): Purple to cyan (`linear-gradient(135deg, #8B5CF6 0%, #38BDF8 52%, #2DD4BF 100%)`)
- Status colors: Available (teal), Occupied (blue), Preparing (orange), Ready (green), Pending Payment (purple), Cancelled (red), Locked (grey).

### 2.3 Current Pages Scan
- **Inventory Page ([`Inventory.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/Inventory.razor)):** A single-column vertical feed containing panels for technical readiness checklist, Add inventory item, Receive stock, Physical count reconciliation, Stock adjustment, Loss reporting, Waste/spoilage approvals, Current balances list, and logs of recent counts/activity.
- **Tables Page ([`Tables.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/Tables.razor)):** A grid of status-colored button cards (`status-available`, `status-occupied`, etc.) indicating table numbers, waiter names, lock status, and active order totals.
- **Kitchen Page ([`Kitchen.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/Kitchen.razor)):** A KDS layout featuring columns of order tickets. Uses a SignalR event bus to refresh order lists. Sinks into a compact 72px sidebar mode.
- **Order Editor Page ([`OrderEditor.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/OrderEditor.razor)):** A split two-column layout. The left column shows the cart (current item list, total, cancel panels, and "Send to kitchen" submit button). The right column shows input options (notes, quantity spinner) followed by a simple text-based list of menu items grouped by category under `<h3>` headers.

---

## 3. Scanning the Uploaded Mockup Images (Step 2)

The user uploaded 5 mockup images located in [`RESTO_APP_UI_CONCEPT\GEMINI LOGS\mockups`](file:///D:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/GEMINI%20LOGS/mockups).

### 3.1 Mockup 1: Inventory Management Dashboard
- **Layout:** Bento grid (2x2) configuration.
  - **Top-Left:** Add New Inventory Item (Form card).
  - **Top-Right:** Recipe Ingredient Configuration (Ingredient breakdown card).
  - **Bottom-Left:** Stock Adjustment (Form card).
  - **Bottom-Right:** Current Balances (Tabular data table).
- **Navigation context:** A horizontal top bar with the brand logo/title, an alert banner "Automatic stock deduction is paused", and right-hand utility actions (Refresh, Notifications, Profile Avatar).
- **Inputs & Fields:** Includes Supplier dropdowns, Initial Stock, Unit Cost, Barcodes, and Recipe ingredient details (e.g. Signature Pasta: Pasta 200g, Tomato Sauce 150g, etc.) not presently available in the functional code.

### 3.2 Mockup 2: UI Component State Sheet
- **Buttons:** 4 major categories (Primary Gradient, Secondary, Quiet, Danger) shown across 5 states: Default, Hover, Focus Ring (blue outline), Busy (with loading spinner), and Disabled.
- **Input Fields:** Text Input, Number, and Search. Inline validation states: Default, Hover, Focus (blue border), Active/Typing, Busy (loading spinner inside input), Invalid (red border/icon/error message), and Valid (green border/icon).
- **KDS Ticket Card:** Three cards detailing "New" (blue accent), "In Preparation" (orange accent), and "Delayed" (red accent) states with layout elements.
- **Status Pills, Table Cards, Menu Cards, and Alert Callouts:** Specifies exact styles for available/occupied badges, Reserved cards (with assign buttons), Cheeseburger menu cards (with "Added to cart" capsules), and warning callout styles.

### 3.3 Mockup 3: Tables Overview
- **Layout:** Horizontal top navigation menu containing links to Dashboard, Tables (Active), Orders, Menu, Reports, Staff. Displays a profile dropdown ("Waiter Alex") and "Clock Out" button on the right.
- **Sub-header:** "Tables Overview" title on the left and filter buttons ("All", "Available", "Occupied", "Reserved") on the right.
- **Cards Grid:** Symmetrical 2x3 grid of glassmorphic, glowing cards representing tables.
- **Card Metadata:** Large bold table numbers (e.g., "Table 1") with header capsules representing states. Detailed metrics: Seats count, Waiter name, active Orders, Item counts, Elapsed time (e.g., "Time 45m"), Total values, and quick action buttons.

### 3.4 Mockup 4: Kitchen Display System (KDS)
- **Top Header:** Black minimalist layout displaying brand logo, live digital clock (24h style), oldest incoming status, active orders count, and refresh latency tracking.
- **Columns:** Grid showing ticket panels with colored header stripes (Blue for New, Orange for Preparing, Green for Waiting).
- **Ticket Elements:** Large Table Number, Order ID, "Received X min ago" badge, a dominant MM:SS count-up elapsed timer, quantity list of items, bracketed modifications/instructions (e.g., `[M/R] [NO ONION]`), and a bottom outline action button ("START" or "READY").

### 3.5 Mockup 5: Order Editor
- **Layout:** Three-pane split screen.
  - **Left Pane:** Categorized vertical sidebar (Mains - active, Drinks, Desserts).
  - **Center Pane:** Card grid of food menu items with pictures (Adobo, Sinigang, Halo-Halo) and "Add to Order" action buttons. Includes customer name and table status at the top.
  - **Right Pane:** Table Order Summary. A drawer lists selected items, notes text areas, item quantity incrementers (`[-] 1 [+]`), and a purple/cyan gradient action button "Send to Kitchen".

---

## 4. UI Design Comparison (Step 3 & 4)

### 4.1 Structural Differences

| UI Section | Current Implementation in Codebase | Mockup Concept Design |
| :--- | :--- | :--- |
| **Global Navigation** | Vertical sidebar via [`NavMenu.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Layout/NavMenu.razor) for general pages, collapsing to a 72px rail in KDS. | Horizontal top header navigation. Vertical nav is restricted to page-level tabs (e.g. Order Editor category lists). |
| **Inventory Layout** | Single-column stacked feed of 9 vertical panels. | 2x2 Bento grid card layout (Add, Recipes, Adjust, Balances). |
| **Inventory Data** | Simple inputs (Name, Unit, Min Stock). Current balances is a list. | Expanded form fields (Category, Supplier, Initial Stock, Unit Cost, Barcode). Balances is an interactive table with Total Value & Status. |
| **Recipe Management** | **Completely absent.** Schema was dropped on 2026-08-02. | Active "Recipe Ingredient Configuration" panel with edit/delete controls. |
| **Tables Grid** | Grid of basic buttons changing background based on status. | Glassmorphism card grid showing seats, waiter name, active metrics (Orders/Items), elapsed timers, and table actions. |
| **KDS Tickets** | Borders reflect state. Text-based labels. Standard yellow/green action buttons. | Colored header stripes. Huge table numbers. Prominent central MM:SS elapsed timers. Clear outline START/READY button styles. |
| **Order Editor layout** | Two-column layout (Cart left, menu items vertical group lists right). | Three-pane design (Left: menu categories; Center: item picture cards; Right: summary drawer). |
| **Order Item Options** | Quantity and special instructions must be selected *before* adding the item. | Inline quantity adjustments (`+/-` buttons) and notes text fields *in the cart/summary drawer itself*. |

### 4.2 Visual Styling & Aesthetics

- **Glassmorphism & Shadows:** The mockup features soft drop-shadows, blurred backdrop panels (glassmorphism), and neon-like color glow borders (e.g., cyan glow for available tables, orange for preparing). The codebase uses solid background surfaces (`#171E27`) and flat borders (`#344153`).
- **Typography:** The mockup utilizes clean, semi-condensed modern fonts with heavier weight contrasts (e.g., massive table numbers, thin labels, prominent timers). The codebase uses standard `Inter` with basic font-weight bounds.
- **Controls & Form inputs:** The mockup utilizes custom validation styling (success checks or warning indicators *inside* text boxes) and custom incrementers. The codebase falls back to standard Bootstrap `.form-control` outlines.

---

## 5. Detailed Mockup Adoption Plan (Step 5)

This plan outlines the required steps to adopt the mockup designs into the current code structure without modifying the core domain logic or security boundaries.

### 5.1 Stage 1: Global Variables & Navigation Alignment
1. **CSS variables extension:** Update [`roms.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/wwwroot/roms.css) to add neon-glow shadow definitions and update status color hexes to align with the mockup sheet.
2. **Top-row horizontal header:**
   - Refactor [`MainLayout.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Layout/MainLayout.razor) and [`NavMenu.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Layout/NavMenu.razor) to convert the sidebar into a horizontal navigation bar.
   - Embed the "Online Status Indicator" and Waiter Avatar dropdown into the top header.
   - Keep the vertical 72px rail only when KDS mode is activated to optimize vertical cooking canvas.

### 5.2 Stage 2: Implementing the Inventory Bento Dashboard
1. **Grid layout refactoring:** Modify [`Inventory.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/Inventory.razor) from a stacked feed to a `d-grid` Bento card layout (using CSS Grid properties).
2. **Form enhancements:**
   - Add frontend input elements for *Category*, *Supplier*, *Unit Cost*, *Initial Stock*, and *Barcode* in "Add Item".
   - *Since these fields are absent in the database, either mock their presentation or add migration support to extend the `InventoryItem` model with these properties.*
3. **Recipe Configuration placeholder:**
   - Re-introduce a presentation-level mock or DB schema matching the former `RecipeIngredients` model to render the recipe list (Pasta, Caesar Salad) so users can click "Manage Recipes" and "Save Changes".
4. **Current balances table:**
   - Replace the linear loop with a `<table>` structured grid.
   - Implement the "Low Stock" pill rendering.

### 5.3 Stage 3: Revamping the Tables Grid
1. **Glassmorphism UI:** Update `.table-card` styles in [`roms.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/wwwroot/roms.css) using `backdrop-filter: blur(12px)` and neon glows corresponding to states.
2. **Card layout extension:**
   - Update [`Tables.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/Tables.razor) to display seats count and order metrics (waiter name, orders count, elapsed time).
   - Add status filtering tab buttons (All, Available, Occupied, Reserved) at the top of the grid to filter the `tables` list reactively.

### 5.4 Stage 4: KDS Ticket Revise
1. **Card styling:** Adjust `.kds-ticket` in [`roms.css`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/wwwroot/roms.css) to support colored top bars (headers) instead of a solid border.
2. **Elapsed timer integration:**
   - Write a client-side JavaScript or Blazor timer task to update the MM:SS timer relative to the `SubmittedUtc` timestamp. Renders in large text.
3. **Footer buttons:** Style the transition buttons as outlined pills with massive labels ("START" or "READY").

### 5.5 Stage 5: Order Editor Multi-Pane Layout
1. **Layout split:** Refactor [`OrderEditor.razor`](file:///D:/ARCWorks_Restaurant%20Suite/src/Roms.Web/Components/Pages/OrderEditor.razor) to split the view into three containers (Categories sidebar, food grid, summary drawer).
2. **Cart notes & quantity update:**
   - Remove the general instruction box. Move instructions and quantity controls *directly* inside each item row in the cart using inline `[-]` and `[+]` button adjusters.
3. **Photos placeholder:** Introduce static photo routes (e.g. using local placeholder shapes or SVG assets) inside the menu card loop to match the visuals of Adobo, Lumpia, and Halo-Halo.

---

## 6. Worklog Checklist (Step 1 to 5 Details)

- [x] **Step 1:** Scanned project directory `D:\ARCWorks_Restaurant Suite` and examined CSS/pages.
- [x] **Step 2:** Scanned all 5 uploaded images in `RESTO_APP_UI_CONCEPT\GEMINI LOGS\mockups`.
- [x] **Step 3:** Compared the visual and logical layouts of the codebase vs. mockup concepts.
- [x] **Step 4:** Documented detailed visual, database, and structural differences.
- [x] **Step 5:** Formulated a structured plan to adopt the design mockup safely.

---

## 7. UI Redesign Implementation & Verification (August 7, 2026)

Following user approval, the planned visual and structural improvements have been fully implemented across all target pages. All modifications preserve the underlying business logic, database structure, and security constraints.

### 7.1 Implemented Changes By Component

1. **Global CSS Layouts & Accents ([`roms.css`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/wwwroot/roms.css)):**
   - Added class `.glass-card` for transparent, backdrop-filtered (glassmorphism) panel backgrounds.
   - Added active glow borders (`.status-available`, `.status-occupied`, etc.) matching mockup statuses.
   - Styled `.tables-filter-bar` and horizontal/vertical flex properties.
   - Set three-pane responsive split bounds (`.order-layout-three-pane`).

2. **Horizontal Header Navigation:**
   - Modified [`MainLayout.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Layout/MainLayout.razor) and [`NavMenu.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Layout/NavMenu.razor) to support the horizontal navbar mode for common pages.
   - Preserved vertical collapsed 72px rail specifically for `/kitchen` mode to maximize task list space.

3. **Tables Grid Revamp ([`Tables.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Pages/Tables.razor)):**
   - Implemented responsive glassmorphism button cards showing seats, waiter name, active total amounts, and elapsed timers.
   - Added status-filtering tabs (All, Available, Occupied, Reserved) at the top of the grid.
   - Retained accessibility elements (`aria-label`) to ensure Playwright end-to-end locator stability.

4. **Order Editor Three-Pane Layout ([`OrderEditor.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Pages/OrderEditor.razor)):**
   - Configured three-pane desktop split (left: Category sidebar, center: Menu catalog grid, right: Cart summary drawer).
   - Displayed food menu cards with inline details, price tags, and fallback plate SVGs inside a visual image-box.
   - Moved quantity selectors (`[-] 1 [+]`) and instruction labels directly inside cart rows.
   - Integrated live order-entry countdown timer with expired late alert banner and a pop-up modal to request timing extensions.

5. **KDS Ticket Display ([`Kitchen.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Pages/Kitchen.razor)):**
   - Styled tickets as glass cards with status-themed header stripes and large table numbers.
   - Integrated active preparation/acceptance timers showing count-down status and negative elapsed late timers if deadline is passed.
   - Implemented outline buttons for state transitions (e.g. "Start preparing", "Ready").

6. **Manager Operations Dashboard ([`Manager.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Pages/Manager.razor)):**
   - Created a new operations hub displaying current shift workloads, live order counts, and target due times.
   - Added timing configuration sliders for Order-Entry and Kitchen-Acceptance parameters.
   - Rendered a read-only list of clocked-in staff presence and low-stock inventory balance notifications.

7. **Inventory Bento Dashboard ([`Inventory.razor`](file:///D:/ARCWorks_Restaurant_Suite/src/Roms.Web/Components/Pages/Inventory.razor)):**
   - Re-arranged forms and tables into a 2x2 Bento grid layout.
   - Top-Left: Add New Inventory Item (Form card).
   - Top-Right: Recipe Ingredient Configuration (Disabled/Out of Scope card explaining `RemoveRecipeFunctionality` migration context).
   - Bottom-Left: Stock Operations (Compact, tabbed card supporting stock receipts, count reconciliations, adjustments, and loss reporting).
   - Bottom-Right: Current Balances (Tabular data showing current stock, safety limits, and status badge with Low-Stock indicator).

### 7.2 Verification & Test Evidence
- Ran database translation and concurrency checks for the implemented workflow paths.
- The available targeted authorization checks and Playwright E2E smoke tests passed.
- A full solution test run is not treated as a release gate here because some
  long-running integration suites require separate runtime/database setup.

### 7.3 Corrective pass — 2026-08-07

- Corrected availability permissions: Kitchen can only apply `86`; Manager
  and Admin can apply `86` or restore with `68`. Enforcement is in the
  application service as well as the Inventory controls.
- Replaced the Manager attendance payload with a dedicated current-operation
  read model containing active clocked-in staff only. The Admin schedule,
  historical attendance, and correction view remains isolated to Admin.
- Verification for this pass: solution build passed with 0 warnings/errors;
  targeted Manager authorization and role-matrix tests passed. Full visual
  acceptance remains pending the user's supervised test.
