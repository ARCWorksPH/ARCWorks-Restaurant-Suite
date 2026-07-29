# ROMS UI Concept & Implementation Change Log

All modifications, visual assets, design tokens, documentation, and remediation passes produced for the ROMS UI redesign are tracked below.

---

## [Documentation & Contributor Guide] - 2026-07-30

### Added
- **Contributor & Developer UI Guide**: Created [CONTRIBUTOR_UI_GUIDE.md](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/CONTRIBUTOR_UI_GUIDE.md) detailing the hybrid Concept 1 Neo-Glass + Concept 2 Soft Bento design system, Blazor CSS isolation rules (`.razor.css` vs `roms.css`), master CSS tokens, touch target ergonomics, 1920x1080 KDS layout rules, and pre-commit verification workflows.
- **Updated Work Log**: Documented contributor build guides and documentation workflows in [docs/WORK_LOG.md](file:///d:/ARCWorks_Restaurant%20Suite/docs/WORK_LOG.md).

---

## [Remediation Pass] - 2026-07-30

### Fixed & Remedied
- **Blazor CSS Isolation Overrides**: Corrected `MainLayout.razor.css` and `NavMenu.razor.css` at their source to remove legacy blue-purple sidebar gradients and bright white top bar, applying the approved dark matte graphite theme (`#0F141B`/`#171E27`).
- **Dedicated Kitchen Display Mode (1920x1080)**: Added route-aware `kds-mode` styling to collapse the 250px sidebar on `/kitchen`, expanding ticket canvas with 24px+ table headers, 18px+ item lines, and red `#F87171` notes readable at 2-3 meters.
- **Mobile Navigation (`390x844`)**: Updated mobile header to preserve ROMS brand, live connection indicator, and user/role badges when collapsed, providing 48px aria-labeled toggle buttons.
- **Inventory Canonical Units**: Constrained `Inventory.razor` unit inputs strictly to `<select>` with `piece`, `g`, `ml` choices and added empty state notices for stock adjustment and recipe mapping.
- **Accessibility & Theme Continuity**: Added global `:focus-visible` rings (`3px solid #38BDF8`), `prefers-reduced-motion` media queries, and dark theme variables to `ReconnectModal.razor.css` and `#blazor-error-ui`.

---

## [Phase A — Design & Specification] - 2026-07-29

### Added
- **UI Concept Explorations**:
  - Concept 1: "Futuristic Neo-Glass & Glow"
  - Concept 2: "Soft Neo-Bento & Gradient"
  - Concept 3: "Tactile Sci-Fi Graphite"
- **User Concept Approval**: Adopted Concept 1 "Neo-Glass & Glow" accents integrated within Concept 2 "Soft Neo-Bento" operational shell + Concept 3 "Tactile Sci-Fi" KDS.
- **Directory Structure Created**:
  - `RESTO_APP_UI_CONCEPT/mockups/`
  - `RESTO_APP_UI_CONCEPT/documentation/`
  - `RESTO_APP_UI_CONCEPT/specifications/`
- **High-Fidelity Mockups Generated & Saved to `RESTO_APP_UI_CONCEPT/mockups/`**:
  - `resto_phase_a_tables_mockup`
  - `resto_phase_a_order_editor_mockup`
  - `resto_phase_a_kds_1080p_mockup`
  - `resto_phase_a_inventory_mockup`
  - `resto_phase_a_component_sheet_mockup`
- **Design Tokens Specified**: Defined master CSS variables (`--roms-bg`, `--roms-surface`, `--roms-primary`, `--roms-secondary`, status state mapping) in [PHASE_A_DESIGN_SPECIFICATION.md](file:///d:/ARCWorks_Restaurant%20Suite/RESTO_APP_UI_CONCEPT/documentation/PHASE_A_DESIGN_SPECIFICATION.md).
- **Future Backend Proposals Documented**: Explicitly separated out non-existent backend features (per-item kitchen states, supplier POs, guest feedback) to preserve existing C# backend contracts.

### Milestones
- [x] Phase A (Design & Specification) Complete.
- [x] Phase B (CSS Visual Implementation & Scoped Remediation) Complete.
- [x] Contributor UI Guide Created (`CONTRIBUTOR_UI_GUIDE.md`).
- [x] Automated Tests: 36/36 Passed.
