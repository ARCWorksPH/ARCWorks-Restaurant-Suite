# Gate 2 Waiter Dashboard — final review contract

**Status:** design review only; no operational Waiter UI implementation is authorized by this document.

## Review mockups

- [Final landscape composition](mockups/WAITER_DASHBOARD_LANDSCAPE_FINAL_REVIEW_V2_2026-08-14.png) — clocked-in state with Staff Hub drawer open.
- [Final Android portrait composition](mockups/WAITER_DASHBOARD_PORTRAIT_FINAL_REVIEW_V2_2026-08-14.png) — clocked-in state with compact Staff Hub launcher.
- The earlier V1 compositions remain in the folder as superseded design evidence; they are not implementation targets.

The mockups define composition and information hierarchy. Generated faces and the rendered Chef Doy mark are illustrative only; implementation must use the approved, exact restaurant-owned logo and staff portrait assets from the versioned restaurant package.

## Gate objective

Freeze the restaurant identity, authoritative time behavior, information boundaries, and responsive composition before backend or UI work begins. Implementation requires explicit project-owner acceptance of both final mockups.

## Central restaurant identity

- Display name: **Chef Doy's Gourmet Restaurant**.
- The restaurant identity is primary. ARCWorks is not repeated on the authenticated Waiter dashboard.
- Restaurant-controlled names, logos, imagery, colors, contact details, and operational labels come from one validated server-side configuration boundary; they are not duplicated as page literals.
- Branding configuration is presentation data only. It cannot change tenant, authentication, authorization, audit, or workflow identity.
- Missing or invalid optional assets use a neutral local fallback. External asset URLs and scripts are prohibited.

The complete replacement registry is in `MODULAR_RESTAURANT_ASSET_REGISTRY_2026-08-14.md`.

## Authoritative time contract

- The server clock is the sole authority for attendance, schedule boundaries, order timers, audit timestamps, expiry, and all persisted events.
- Persist instants in UTC. Convert server UTC to the configured display zone `Asia/Manila` for restaurant-facing dates and times.
- The browser/device clock is never read to make a decision and never corrects server state. The displayed clock is a presentation of server-issued time.
- Week summaries begin Monday at 00:00 Asia/Manila and end immediately before the following Monday.
- “Today” means the current Asia/Manila calendar date. Staff-on-duty membership is date-based only; it does not depend on whether each person's shift is currently active.

## Waiter information boundary

The dashboard is a personal, calm pre-shift space. It has no side navigation panel and is visually separate from the operational floor.

It contains:

1. Restaurant logo.
2. Time-appropriate greeting and the Waiter's display name.
3. Server date and clock, visibly marked as live/server time.
4. One stateful **Clock In / Clock Out** control beneath the clock.
5. Shift state and, when present, authoritative clock-in time.
6. **Today's Team** portrait carousel: portraits only, without names or roles; all staff scheduled anywhere on the current restaurant date are included.
7. Recent attendance records and calculated hours for the current Monday-based week. No payroll or compensation inference is shown.
8. Today's scheduled shift, assigned section if available, and the Manager note attached to that shift.
9. A secondary **Staff Hub** holds Shift Announcements, Manager Notes, Leave Requests, and My Journal. In landscape it is a closable right drawer; in portrait it opens as a full-height mobile sheet. It does not permanently consume primary dashboard space.
10. Notification severity is explicit and not color-only: Normal creates a quiet unread indicator; Important creates a labeled amber-priority indicator; Urgent / Requires acknowledgment opens an interruptive modal and records the user's server-side acknowledgment. Editing an acknowledged urgent message creates a new version that requires acknowledgment again.
11. Announcements can be dismissed for that user without deleting the Manager/Admin source record.
12. Leave requests support one or more future dates and a private request message sent to Manager/Admin. Approval handling is a later Manager gate.
13. **My Journal** remains inside Staff Hub. Journal content is server-stored, private to the staff author, protected by the application data boundary, absent from operational reports, and unreadable through ordinary Manager/Admin UI.
14. A compact profile control with portrait and a small expand button. Its drop-up provides editable personal information, Change Password, and **Log Out**. Logging out does not clock out.
15. **Enter the Floor — Tables & Orders** as the sole transition to operational work. It is disabled while the Waiter is clocked out.

Today's Team can be collapsed without hiding its heading or scheduled count. Expanded portrait rows remain swipeable and show portraits only, with no employee names or roles.

Explicitly excluded: Reservations, Messages, Team tab, Reports tab, payroll, future staff schedules in Today's Team, employee names beneath team portraits, and automatic clock-out merely because the browser session ends.

## Attendance safeguards

- Clock state survives ordinary logout and the 15-minute authenticated-session inactivity timeout.
- If an attendance record remains open 12 hours after the scheduled shift end, or 12 hours after clock-in when no shift end exists, the server automatically closes it and flags it for Manager review. It is never allowed to run indefinitely.
- Automatic closure is an audited safety limit, not a silent payroll correction.

## Responsive review targets

### Landscape

- Reference canvas: 1920 × 1080 at 100% browser zoom.
- No side panel. Use a spacious two-column hierarchy with greeting/team above, attendance and shift/announcements/leave below, and a clear floor-entry CTA.
- Essential actions and server time remain visible without horizontal scroll.

### Android portrait

- Design reference: 1080 × 2400; implementation acceptance also covers CSS widths 360, 390, 412, and 480 pixels.
- Single-column flow; no clipped text, horizontal page scrolling, or hover-only controls. Minimum touch target is 44 CSS pixels.
- Today's Team is a touch-scroll carousel with visible continuation affordance.
- Profile expansion is a touch-accessible drop-up/sheet, not a desktop hover menu.

## Acceptance gate

Before implementation, the owner explicitly accepts the information scope, identity, clock states, floor-entry gate, portrait-only team behavior, both responsive compositions, and replacement assets. After approval, backend work remains separate from the visual pass and must preserve Gate 1 security.
