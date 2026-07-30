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
| Start or amend preparation when recipe consumption would make any item negative | Block the operation and show the shortage. Do not change the order, ledger, or audit trail. |
| Administrator explicitly overrides a negative-stock block with a reason | Permit the operation, persist the manager and reason on the order, and append an `INVENTORY_DISCREPANCY_ALERT` audit entry. |
| Kitchen or Admin reports waste/spoilage | Create a Pending loss request. Do not change the stock ledger. |
| Admin approves waste/spoilage | Mark the request Approved and append one idempotent `Waste` or `Spoilage` stock movement. |
| Admin rejects waste/spoilage | Mark the request Rejected with a reason. Do not change the stock ledger. |

## Authorization and audit

- Cancelling a Preparing or Ready order requires an administrator.
- Removing an item while Preparing requires an administrator.
- Every cancellation or amendment requires a reason.
- Every post-preparation cancellation or removal requires an explicit inventory disposition.
- The disposition is persisted on the order or order item and included in the audit entry.
- Stock movements remain append-only. Reconciliation posts compensating `Reversal` movements; it never edits or deletes prior consumption.
- Negative-stock checks and the resulting stock movement are committed in one serializable database transaction.
- Only an administrator may override a negative-stock block; every override requires a reason and emits `INVENTORY_DISCREPANCY_ALERT`.
- Kitchen staff and administrators may report waste or spoilage. Only administrators may approve or reject a report.
- A rejected loss report requires a review reason.
- An approved physical loss is recorded even if it reveals negative stock; the system emits a discrepancy alert rather than hiding the loss.

## Deferred policies

- Ingredient/unit cost accounting and financial valuation remain a later gate.
- Alert acknowledgement/escalation beyond the append-only audit record remains a later gate.
- The provisional Bob Marlin dataset is sandbox-only until restaurant confirmation.
- Inventory must remain disabled in the active deployment until restaurant data is confirmed and live multi-user acceptance is authorized.
