# Ideal Glass UI Implementation Plan

**Status:** Phase 0 shared token work completed and container-smoke-checked; screen rebuilds have not started.
**Authoritative visual source:** `ARCWorks_ROMS_Ideal_Glass_Design_Spec3.md`
**Applies to:** `D:\ARCWorks_Restaurant_Suite` only.

## 1. Purpose and scope

This plan replaces the previous broad "glass pass" approach with a
component-first visual rebuild. The objective is to make the running interface
match the approved ideal mockups while preserving the already approved staff
workflow, data model, role boundaries, responsive behavior, and audit trails.

The existing smoked-glass styling remains a temporary foundation. It is not
visual acceptance evidence and must not be described as the completed ideal
design.

## 2. Fixed product boundaries

The visual work must not:

- change order states, timers, status ownership, or role permissions;
- add recipe configuration, automatic stock deduction, supplier management,
  unit-cost fields, or other rejected inventory scope;
- expose deletion, reporting, payment, or administrative controls to roles
  that are not already authorized;
- replace operational controls with decorative-only UI;
- claim visual comparison is functional, browser, multi-role, or beta
  acceptance.

The inventory mockup is a visual reference only. Recipe-related controls in it
remain intentionally excluded.

## 3. Design system foundation (Phase 0)

Create shared, reusable tokens and component classes before rebuilding an
individual screen.

| Element | Required implementation |
| --- | --- |
| Base background | Quiet `#0B0F14` navy/charcoal with restrained cyan and violet ambient gradients only. |
| Glass card | `rgba(22, 28, 38, 0.72)`, 20px blur/saturation, 16px radius, white inset highlight, soft depth shadow. |
| Status glass | Per-status fill, border, text color, and soft outer glow exactly as defined in the authoritative specification. |
| Status pill | Rounded, translucent, status-matched pill; icon only where it does not harm touch targets or readability. |
| Buttons | Purple-to-cyan-to-teal gradient only for primary action; glass secondary/quiet actions; solid or bordered red danger actions. |
| Inputs and alerts | Dark translucent controls, clear accessible cyan focus, explicit valid/invalid states, and color-matched alert callouts. |
| Motion | Short, restrained hover/focus transitions only; honor reduced-motion settings. |

### Phase 0 acceptance

- No generic late CSS rule can flatten a status card into an evenly dark panel.
- Each status has one canonical token set; values are not duplicated ad hoc in
  page styles.
- Keyboard focus is visible on controls but never creates a decorative focus
  outline around passive headings or containers.
- Desktop, tablet, and phone layouts remain usable.

### Phase 0 result (2026-08-11)

- Centralized base glass, neutral background, status fill/border/text/glow, and
  status-pill rules in `src/Roms.Web/wwwroot/roms.css`.
- Replaced the broad cyan/orange background treatment with the restrained
  dark navy, cyan, and violet ambient treatment specified by the authoritative
  design brief.
- Replaced the remaining table-card final overrides with the approved soft
  colored-fill, matching-border, and restrained-glow pattern.
- Rebuilt the `app` Docker image. The container reported `healthy` and
  `http://127.0.0.1:7070/health` returned HTTP 200.
- This is build/runtime smoke evidence only. Tables and KDS still require
  their dedicated visual composition phases and owner visual acceptance.

## 4. Screen implementation order

### Phase 1 — Tables Overview

Rebuild the current table cards first because they define the status-glass
language used by the rest of the application.

- Recompose each card around a top status pill, table identity, relevant
  operational facts, optional menu trigger, and one clear action.
- Use status-colored glass for Available, Occupied/New, Preparing, Ready to
  Serve, Pending Payment, and Locked states.
- Preserve all existing table actions and responsive grid behavior.
- Do not invent data such as seat counts or order counts unless those values
  are available from the current model; omit unavailable fields gracefully.

### Phase 2 — Kitchen Display

Treat the KDS as a dedicated landscape-first display rather than an admin
page.

- Present tickets oldest first with large table number, large elapsed timer,
  high-contrast item list, status badge, and clear bottom action.
- Use higher-opacity status glass than ordinary cards.
- Bind timer color and ticket emphasis to the existing order state without
  changing timer calculations.
- Retain portrait usability, but preserve the intentional landscape-first KDS
  composition and its existing 86-item controls.

### Phase 3 — Waiter Order Editor

Refine the existing three-pane workflow rather than replacing it.

- Keep category rail, food-photo menu grid, live read-only customer summary,
  quantity controls, notes, and submission workflow.
- Normalize food imagery with an undistorted `object-fit: contain` or a padded
  image frame; do not crop dishes merely to fill a fixed rectangle.
- Make the order summary slightly more opaque than the catalog and make the
  primary workflow action visually dominant.
- Preserve the special rule that identical items with different notes remain
  separate order lines.

### Phase 4 — Manager and Inventory

Improve hierarchy and readability without turning operational forms into a
showcase.

- Use calm glass sections, compact metric cards, clean data tables, and
  status-aware low-stock alerts.
- Keep controls and text readable during extended shifts.
- Do not reintroduce recipe functionality or visual placeholders for it.

### Phase 5 — Supporting screens and responsive polish

Apply the shared component system to attendance, users, staff scheduling,
reports, pending payments, authentication, confirmation dialogs, and alerts.

- Side navigation must remain usable and intentionally collapsible; do not
  change its functional behavior during a purely visual pass.
- Review 320px mobile, phone landscape, tablet portrait, 1366px desktop, and
  1920px KDS landscape.

## 5. Safe delivery sequence

For each phase:

1. Record target files and a before screenshot/reference.
2. Implement only the planned component and screen changes.
3. Build the `app` image and verify container health plus `/health`.
4. Perform a visual comparison at the target viewport(s).
5. Verify no role, order, inventory, timer, or deletion behavior changed.
6. Record accepted, rejected, and deferred differences in the work log.
7. Commit only a coherent, independently reviewable phase after user visual
   sign-off.

If a phase causes a behavioral regression, revert that phase before working on
the next screen. Do not compensate for a layout problem by weakening workflow
rules.

## 6. Visual acceptance matrix

| Area | Must visibly match | Must remain true |
| --- | --- | --- |
| Tables | Colored fill, border, soft glow, status pill, strong hierarchy | Existing table filters/actions and responsive grid work. |
| KDS | Large readable timer, ticket status identity, prominent action | Kitchen-only actions and current order lifecycle remain unchanged. |
| Waiter | Appetizing image grid, clear summary, dominant primary action | Draft/edit/resubmit and note separation remain unchanged. |
| Manager | Calm dashboard hierarchy, readable data and controls | Read-only operations boundary remains intact. |
| Inventory | Practical glass forms/tables and legible stock alerts | Independent-item inventory only; no recipes. |

## 7. Completion definition

The ideal-glass visual pass is complete only when every target screen has been
reviewed against its relevant approved mockup at its intended viewport,
functional smoke checks pass, accessibility basics remain intact, and the
result is explicitly accepted by the project owner. It remains separate from
supervised four-role acceptance and beta-readiness gates.
