# ROMS Inventory Reversal Rules

Status: implementation baseline for inventory-readiness testing. Inventory remains disabled in the active deployment.

## Required invariants

| Workflow | Required inventory result |
| --- | --- |
| Cancel a Draft or New order | No stock movement. Preparation has not started. |
| Cancel a Preparing or Ready order and choose **Return to stock** | Reverse the order's net recipe consumption back to zero. |
| Cancel a Preparing or Ready order and choose **Consumed as waste/staff meal** | Preserve recipe consumption. Do not create a false restock. |
| Add an item while Preparing | Consume the added item's recipe quantities. |
| Remove an item while Preparing and choose **Return to stock** | Reverse only the removed item's recipe quantities. |
| Remove an item while Preparing and choose **Consumed as waste/staff meal** | Preserve the removed item's consumption, including after later amendments. |

## Authorization and audit

- Cancelling a Preparing or Ready order requires an administrator.
- Removing an item while Preparing requires an administrator.
- Every cancellation or amendment requires a reason.
- Every post-preparation cancellation or removal requires an explicit inventory disposition.
- The disposition is persisted on the order or order item and included in the audit entry.
- Stock movements remain append-only. Reconciliation posts compensating `Reversal` movements; it never edits or deletes prior consumption.

## Deferred policies

- Negative-stock blocking and manager override remain a later gate.
- Waste/spoilage approvals and cost accounting remain a later gate.
- The provisional Bob Marlin dataset is sandbox-only until restaurant confirmation.
- Inventory must remain disabled in the active deployment until these rules pass real-MariaDB, concurrency, browser, and multi-user acceptance.
