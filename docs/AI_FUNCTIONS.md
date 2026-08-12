# ROMS AI Read-Only Function Contract

Status: implemented behind a disabled-by-default feature flag on 2026-08-02

Protocol version: 1

Natural-language command schema: 3

Authoritative restaurant time zone: `Asia/Manila`

## Governing boundary

The signed-in ROMS user supplies the identity. ROMS code applies authorization,
queries MariaDB, calculates all counts and money values, and formats the factual
answer. The local model may only translate one natural-language question into
one proposed function call. Model output is untrusted and never becomes SQL,
authorization, a database value, or a user-facing financial calculation.

The assistant is read-only. It cannot create, update, approve, cancel, pay,
refund, receive, adjust, reconcile, or delete anything.

## Approved functions

| Function | Arguments | Admin | Waiter | Kitchen |
|---|---|---:|---:|---:|
| `GetMenuItem` | exact item name | Full | Full | Price hidden |
| `ListMenu` | exact category and/or availability | Full | Full | Prices hidden |
| `GetInventoryBalance` | exact item name | Yes | No | Yes |
| `ListInventoryBalances` | none | Yes | No | Yes |
| `ListLowStockItems` | none | Yes | No | Yes |
| `GetOrderStatus` | exactly one order ID or table number | Any order | Own order | Active kitchen queue only |
| `ListOrdersByStatus` | exact ROMS status | Any status | Own permitted orders | `New`, `Preparing`, `Ready` only |
| `GetDailyOrderSummary` | one optional business date | Yes | No | No |
| `GetOrderStatusSummary` | none | Yes | No | No |
| `GetLowStockSummary` | none | Yes | No | Yes |
| `GetMenuAvailabilitySummary` | none | Yes | Yes | Yes |
| `GetOperationalSummary` | one optional business date | Yes | No | No |

All list functions return at most 100 records. Dates are single restaurant
business dates, not model-invented ranges. When no date is supplied, ROMS uses
the current date in `Asia/Manila`.

## Exact semantics

- Menu lookups require an exact active database name. Category filters require
  an exact active category. The application reports stored PHP prices and
  availability; the model does not calculate either.
- Inventory balances are the sum of the append-only stock-movement ledger.
  Low stock means `current balance <= minimum stock` and is calculated by ROMS.
- Table lookup considers only the active order or a completed order still
  awaiting payment. An order ID never bypasses authorization.
- Waiters may inspect only their own orders. They cannot list cancelled-order
  history. Their completed-order list contains only orders awaiting payment.
- Kitchen users may inspect only `New`, `Preparing`, and `Ready` orders. Prices
  and totals are omitted from kitchen-only responses.
- Completed-order count and value include only orders whose payment was
  confirmed during the selected Manila business day.
- Cancellation count comes from persisted status history, not inference.
- Recipe definitions, yields, menu-to-ingredient mappings, automatic
  consumption, recipe costing, and recipe advice are not functions.

## Safe outcomes

Every request produces one of these deterministic outcomes:

- `Success`: authorized database result returned.
- `NotFound`: no exact current record exists.
- `Ambiguous`: more than one exact eligible record exists.
- `Unauthorized`: the signed-in role or ownership boundary denies the read.
- `InvalidRequest`: required arguments are missing, conflicting, or invalid.
- `Unsupported`: the requested function is outside this contract.

The natural-language gateway may instead require clarification, reject an
unsupported request, or return an interpreter-unavailable fallback. None of
those outcomes executes a ROMS function.

## Audit and privacy

Every executed ROMS function writes an `AiRead:<FunctionName>` audit entry with
the actor, function, sanitized arguments, result status, and timestamp. Raw
prompts, model reasoning, credentials, connection strings, and database result
payloads are not written to that audit entry.

## Runtime topology

```text
authenticated browser
        |
     ROMS app ---- backend ---- MariaDB
        |
  internal command network
        |
 command gateway ---- internal inference network ---- Ollama
```

The app is the only component spanning the command and database boundaries.
The command gateway and Ollama have no database network, database credentials,
host bind mount, Docker socket, or public port. Ollama's benchmark API remains
bound to Windows loopback only.

## Feature and acceptance state

`Ai:Hold` / `AI_HOLD` defaults to `true`, and `Ai:Enabled` / `AI_ENABLED`
defaults to `false`. While the hold is active, the Assistant navigation and
page are unavailable and the application has no gateway connection. Enabling
the flag alone is not production approval; follow `docs/AI_HOLD.md` for the
future-version release gate.
Required remaining evidence includes actual container-model interpretation,
browser rendering, timeout/fallback behavior, prompt-injection and multilingual
ambiguity acceptance, concurrent use, rollback, and external review.

Automated evidence currently includes deterministic validator tests,
permission/function tests, and disposable MariaDB 11.4 translation tests.

## 2026-08-02 contained runtime checkpoint

- The rebuilt gateway reported schema 3 healthy over the internal command
  network.
- The first exact-item attempt exposed an overly terse model prompt. The
  validator refused the inconsistent proposal; ROMS executed nothing.
- After the function/argument matrix and examples were made explicit,
  `How much Cooking oil is left?` produced a validated
  `GetInventoryBalance(Cooking oil)` proposal.
- `Which items are low in stock?` produced `ListLowStockItems` in three
  consecutive repeat runs.
- A deletion request returned `Unsupported`; prompt injection and the
  untranslated `牛肉饭` versus `Beef Pares` mismatch returned safe
  clarification outcomes. No rejected case reached a ROMS function.
- The authenticated Assistant page passed an isolated Chromium render and
  interaction smoke test. The feature flag was returned to `false` after the
  checkpoint.

This is a spot-check, not the locked adversarial acceptance corpus or guarded
pilot approval.
