# Gate 2E — Announcements and Manager notes evidence

**Date:** 2026-08-23
**Branch:** `agent/gate2e-announcements-manager-notes`
**Status:** implementation and focused verification complete; pull-request CI
and merge are the repository closeout steps.

## Scope completed

Gate 2E adds the server-side communication contract required by the later
Waiter Staff Hub. It does not implement the final modal, unread badge, Staff Hub
visual design, or any live-instance deployment.

### Announcement lifecycle

- `StaffAnnouncement` stores a bounded title and body, Normal/Important/Urgent
  priority, optional role audience, UTC publication and expiry, active state,
  author/update metadata, and an incrementing content version.
- Manager and Admin identities may create, revise, activate, or deactivate an
  announcement. An authenticated active Waiter may read only currently
  published, unexpired, active announcements addressed to Waiters or all staff.
- Revising an announcement creates a new version. Previous per-employee
  acknowledgment or dismissal records do not satisfy or hide the revised
  version.
- Normal and Important announcements may be dismissed per employee. Urgent
  announcements require a server-recorded acknowledgment before dismissal.
- Dismissal never deletes or deactivates the source announcement and does not
  affect another employee.
- Create, edit, state change, acknowledgment, and dismissal operations write
  audit entries.

### Manager note boundary

The Staff Hub read model uses the existing `StaffSchedule.Notes` value from the
authenticated Waiter's schedule overlapping the current Asia/Manila calendar
date. Gate 2E does not create a competing Manager-note table or copy the note
into announcement storage.

## Persistence

Migration `20260822174358_AddStaffAnnouncements` creates the announcement and
receipt tables. A unique index on announcement, employee, and announcement
version prevents duplicate receipt rows at the database boundary. The generated
idempotent migration script completed successfully, and EF Core reported no
pending model changes after generation.

## Focused verification

The five `StaffCommunicationTests` prove:

1. current-day Manager Note selection and live/audience announcement filtering;
2. per-employee dismissal while retaining the source and other employees' view;
3. mandatory urgent acknowledgment and fresh acknowledgment after an edit;
4. Manager/Admin authoring, Waiter-only read/receipt identity, UTC/audience
   validation, and audit events; and
5. rejection of invalid acknowledgment and inactive Waiter access.

Local results:

- solution build — **passed**, 0 errors;
- Gate 2E focused integration tests — **5 passed, 0 failed**;
- real application/browser E2E tests — **4 passed, 0 failed** after the
  readiness correction;
- EF migration model check — **passed**, no pending changes;
- idempotent migration script generation — **passed**.

The first broad local run exposed a pre-existing E2E harness assumption rather
than a Gate 2E assertion failure: clean MariaDB containers and the growing
migration chain required more than the hard-coded 60-second application startup
allowance on the HDD-backed workstation. The harness allowance was raised to
180 seconds; this changes only test readiness timing, not application behavior.
The complete E2E project then passed 4/4 in 5 minutes 18 seconds.

The unfiltered Integration project was also attempted. As in Gate 2D, its
long-running disposable-database process remained alive but emitted no result
for a bounded ten-minute window, so it was stopped without touching the
application or retained data. Pull-request CI remains the independent
authoritative broad-suite check and is recorded during repository closeout.

## Preserved boundaries

- No live or preview container, database, tunnel, credential, or restaurant
  record was changed.
- No announcement-management or Staff Hub browser UI is claimed complete.
- No employee journal, leave-request, or later-gate functionality was added.
- The current Manager Note continues to come from the schedule record.
- Visual priority treatments and the urgent interruptive modal remain part of
  the later Waiter Dashboard UI acceptance gate.
