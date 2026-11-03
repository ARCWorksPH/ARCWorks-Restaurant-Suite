# Single Active Staff Session Policy

## Purpose

Each ARCWorks Restaurant Suite staff account is permitted one active application
session at a time. This prevents one identity from being used concurrently on
multiple waiter, kitchen, manager, or administrator devices.

## Enforced behavior

- A successful login claims a cryptographically generated session identifier in
  the application database.
- A second login for the same account is rejected while that claimed session is
  active. The rejected device receives no information about the first device.
- Explicit **Log out** and **Clock out + Log out** release the session claim.
- If the user is inactive for 15 minutes, the browser submits a logout and the
  session claim is released.
- Normal interaction refreshes the activity record. The active session is
  revalidated against the server at most one minute later, so a stale, replaced,
  or missing session claim is not trusted merely because a browser tab remains
  open.

## Security boundary

The database-backed session claim is the enforcement mechanism. Hiding pages,
navigation buttons, or controls is only a presentation concern and is not used
as the access-control decision. Session identifiers are random 256-bit values
stored as authentication claims; they are not displayed in the interface or
written to normal application logs.

The normal cookie lifetime is bounded at 20 minutes with sliding expiration
disabled. The 15-minute inactivity limit remains authoritative because it is
checked against the server-side activity record.

## Deployment consequence

The migration `20260811190000_AddSingleActiveStaffSession` adds the session
fields and index to `AspNetUsers`. Existing browser cookies issued before this
deployment do not have the required session claim and are invalidated on the
next authentication revalidation. Users then sign in normally and obtain a
new, single active session.

## Required human/browser acceptance checks

These are deliberately not claimed by the container health check:

1. Sign in to an account in Browser/Profile A.
2. Attempt the same account in Browser/Profile B; confirm the second login is
   denied while Profile A remains active.
3. Log out from Profile A; confirm Profile B can then sign in.
4. Sign in, leave the application idle for 15 minutes, and confirm automatic
   logout.
5. Confirm a normally active session remains usable through more than one
   minute of routine interaction.

No landing page, role navigation, workflow, or visual redesign behavior is
changed by this policy.
