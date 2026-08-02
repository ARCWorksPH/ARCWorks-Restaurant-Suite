# ROMS Natural-Language Command Protocol

Status: implemented laboratory protocol; feature disabled by default

Schema version: 3

## Governing rule

The database owns the truth. The model only proposes one structured read-only
function. Model output is untrusted and is never executed directly.

The authoritative function behavior and role matrix are in
`docs/AI_FUNCTIONS.md`.

## Approved proposals

Schema 3 recognizes these proposal names:

- `GetMenuItem`
- `ListMenu`
- `GetInventoryBalance`
- `ListInventoryBalances`
- `ListLowStockItems`
- `GetOrderStatus`
- `ListOrdersByStatus`
- `GetDailyOrderSummary`
- `GetOrderStatusSummary`
- `GetLowStockSummary`
- `GetMenuAvailabilitySummary`
- `GetOperationalSummary`
- `Unknown` for safe refusal

The only proposal fields are item, category, availability, order ID, table
number, exact ROMS order status, and one business date. Quantity, unit, SQL,
role, actor, price, totals, arbitrary filters, write values, and database facts
are not accepted model fields.

## Request limits and grounding

- User text is limited to 500 characters.
- Catalog context is limited to 500 entries and is supplied by ROMS.
- Proposed item names, category names, table numbers, and statuses must match a
  bounded current catalog exactly.
- The original user text must contain the proposed identifying value; a model
  cannot introduce a hidden item, table, order ID, status, or date.
- Relative `today` requests leave the date empty so ROMS determines the Manila
  business date. Unsupported or ambiguous relative dates require clarification.
- Every proposal is revalidated before conversion to an `AiFunctionRequest`.

## Gateway outcomes

- `Recognized`: the proposal passed deterministic field and catalog validation.
  ROMS must still apply current authentication, authorization, and data rules.
- `ClarificationRequired`: intent or an identifying argument is missing,
  ambiguous, conflicting, or unsupported.
- `Unsupported`: the request or proposed command is outside schema 3.
- `InterpreterError`: timeout, unavailable model, malformed response, or other
  safe interpreter failure.

Only `Recognized` can reach the ROMS function dispatcher. All other outcomes
return without a database function query.

## Trust boundary

The command gateway:

- has no database provider, connection string, credentials, or backend network;
- accepts only a bounded catalog supplied for the current request;
- returns a validated application DTO, never a model-authored factual answer;
- rejects every write, recipe, arbitrary SQL, payroll, discount, refund,
  approval, receiving, adjustment, and deletion request;
- logs request identifiers and outcomes, not credentials or raw database data.

ROMS performs the database read and formats the deterministic factual response
after the proposal passes the gateway and the signed-in user passes the
function's authorization policy.

## Evaluation gates

Passing JSON is not acceptance. The locked corpus and runtime acceptance must
cover:

1. exact function and argument interpretation;
2. zero unauthorized or unsupported execution;
3. multilingual ambiguity and nonsense-input clarification;
4. prompt injection and invented-catalog rejection;
5. model timeout, malformed output, and offline fallback;
6. stale-catalog revalidation by current ROMS state;
7. concurrent staff requests without cross-user data leakage.

Historical benchmark protocols and rejected-model evidence remain preserved in
`docs/AI Model Benchmark`; they are not the production schema.
