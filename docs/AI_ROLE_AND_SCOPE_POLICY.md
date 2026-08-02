# AI ROLE AND SCOPE POLICY

Status: approved for implementation on 2026-08-02. Recipe functionality,
menu-to-ingredient mappings, automatic consumption, and recipe-related AI
functions are removed from the current product—not merely hidden by a feature
flag. Historical benchmark and audit evidence remains preserved.

## Purpose

This document defines the approved role, responsibilities, and boundaries of the AI component in the restaurant ordering system.

The goal is to keep the AI useful, reliable, and safe without allowing it to become a source of operational truth or unnecessary system complexity.

The AI should act primarily as an intelligent interface to verified restaurant information.

**Core principle:**

> Code and database records determine facts. The AI explains, summarizes, and communicates those facts.

The AI must not invent, infer, or silently modify operational data.

---

# 1. APPROVED AI FUNCTIONS

The current AI implementation should focus on four approved areas:

1. Menu information
2. Inventory information
3. Order information
4. Permitted operational summaries

These functions should remain read-focused unless a future feature is explicitly approved and separately implemented with proper authorization.

---

# 2. MENU INFORMATION

The AI may retrieve and explain current menu information stored in the application.

Approved information may include:

- Menu item name
- Current selling price
- Category
- Description
- Size or variant
- Modifier options
- Availability
- Active promotions already recorded by the system
- Other restaurant-approved menu metadata

Example requests:

- "How much is the large pepperoni pizza?"
- "What drinks are available?"
- "Which burgers are currently unavailable?"
- "What are the available add-ons for this item?"

## Restrictions

The AI must not:

- Invent prices
- Apply unauthorized discounts
- Change menu prices
- Create promotions
- Assume that an expired promotion is still active
- Mark an item available or unavailable on its own
- Override database information

If the AI cannot retrieve a confirmed answer, it should clearly state that the information cannot currently be verified.

The database and application logic remain authoritative.

---

# 3. INVENTORY INFORMATION

Inventory should remain intentionally simple for the initial version of the application.

The current inventory system should track independent stock items or ingredients without requiring recipes or menu-to-ingredient mappings.

Typical inventory fields may include:

- Item name
- Current quantity
- Unit
- Low-stock threshold
- Stock status
- Optional notes or category

Example:

- Chicken Breast — 14 kg
- Cooking Oil — 8 L
- Tomatoes — 32 kg
- Cheese — 11 packs

The AI may answer questions such as:

- "What ingredients are low in stock?"
- "How much cooking oil is left?"
- "Are there any inventory problems?"
- "Which items are out of stock?"

## Low-Stock Logic

The AI must not decide whether an item is low in stock by reasoning on its own.

The application should determine this using deterministic logic, for example:

```text
quantity <= low_stock_threshold
```

The AI may then explain the result in natural language.

Example:

> Cooking Oil is low in stock. Current quantity: 2 L. Low-stock threshold: 3 L.

## Restrictions

The AI must not:

- Modify stock balances
- Perform stock adjustments
- Invent missing quantities
- Assume consumption
- Estimate usage unless a future approved feature explicitly provides that calculation
- Deduct inventory based on menu sales
- Infer recipe consumption

---

# 4. RECIPE MANAGEMENT IS OUT OF SCOPE

## Important

**Recipe management must NOT be included in the current implementation.**

This includes, but is not limited to:

- Recipe definitions
- Ingredient-to-menu mappings
- Automatic ingredient deduction from sales
- Portion-size calculations
- Yield calculations
- Recipe costing
- Waste calculations
- Recipe substitutions
- Serving conversions
- Recipe-based forecasting
- Automatic inventory depletion based on orders

This is a deliberate product and architectural decision.

It is not a missing feature and should not be treated as unfinished work.

---

# 5. WHY RECIPES ARE DEFERRED

A recipe-based inventory system would significantly increase complexity.

To implement it correctly, each restaurant would need to define and maintain information such as:

- Every menu item
- Every ingredient used by each item
- Exact ingredient quantities
- Portion sizes
- Batch sizes
- Expected yields
- Preparation loss
- Waste
- Ingredient substitutions
- Variations between branches
- Changes in recipes over time
- Different serving sizes
- Different preparation methods

This would make onboarding much more difficult.

The system is intended to be adaptable to many restaurants.

If recipes were required from the beginning, every new restaurant would need extensive setup before inventory tracking could even become useful.

That would make the starting application unnecessarily complex and expensive to implement and maintain.

---

# 6. FUNDING AND DEVELOPMENT PRIORITY

Recipe and consumption management is considered a potentially valuable future feature.

However, it should not be implemented until there is a clear business justification.

The current project has already spent a large portion of its development time and compute resources on the AI component.

In fact, the AI work has consumed more effort and compute than much of the rest of the application.

Because of that, future AI expansion must be controlled carefully.

Recipe automation would require substantial additional development, testing, restaurant-specific configuration, validation, and likely ongoing support.

This level of work should not be undertaken before the project has actual paying restaurant customers or sufficient funding to justify it.

For now:

> Do not spend development time, AI usage, or compute budget on recipe-based inventory.

---

# 7. FUTURE OPTIONAL EXPANSION

Recipe functionality may be reconsidered later as an optional advanced module.

Possible future capabilities may include:

- Recipe definitions
- Ingredient mappings
- Yield calculations
- Portion tracking
- Automatic stock deduction
- Waste tracking
- Recipe costing
- Ingredient forecasting
- Consumption analytics

Any future implementation should be modular.

Basic inventory must continue to work without recipes.

A possible future structure is:

## Basic Inventory

```text
Manual stock updates
        ↓
Stock balance
        ↓
Low-stock thresholds
        ↓
Notifications
```

## Advanced Inventory

```text
Recipes
   ↓
Ingredient mappings
   ↓
Sales / orders
   ↓
Calculated consumption
   ↓
Inventory deduction
   ↓
Forecasting and analytics
```

The advanced system should extend the basic inventory system, not replace it.

Restaurants that do not need recipe management should never be forced to configure it.

---

# 8. ORDER INFORMATION

The AI may retrieve and explain order information that the requesting user is authorized to access.

Approved information may include:

- Order number
- Order status
- Ordered items
- Order timestamps
- Preparation stage
- Payment state where permitted
- Completion state
- Cancellation state
- Other approved operational fields

Example order states may include:

```text
received
confirmed
preparing
ready
completed
awaiting_payment
cancelled
rejected
refunded
```

Example response:

> Order #R1048 is currently preparing. It entered preparation at 7:42 PM.

## Restrictions

The AI should describe order status, not control it.

The AI must not automatically:

- Mark an order completed
- Mark an order paid
- Cancel an order
- Refund an order
- Change preparation status
- Reassign an order
- Change customer details
- Promise completion times not provided by the system

Any future write action must be implemented as a separate authenticated command function with explicit authorization.

Natural-language understanding alone must never be treated as sufficient authorization.

---

# 9. OPERATIONAL SUMMARIES

The AI may generate summaries from operational data already available to the requesting user.

Approved summaries may include:

- Current orders
- Pending orders
- Completed orders
- Order counts
- Sales quantity by menu item
- Low-stock items
- Out-of-stock items
- Unavailable menu items
- Kitchen workload
- Preparation times
- Cancellations
- Daily totals
- Shift totals

Example:

> Dinner Shift Summary<br>
> 126 orders received<br>
> 118 completed<br>
> 5 preparing<br>
> 3 cancelled<br>
> Chicken Alfredo was the highest-selling item with 34 orders.<br>
> Two inventory items are below their configured low-stock thresholds.

The AI must only summarize information the requesting user is already authorized to access.

---

# 10. ROLE-BASED ACCESS CONTROL

AI permissions must never exceed the permissions of the requesting user.

Example:

If a cashier is not allowed to view ingredient cost, the cashier must not be able to retrieve that information by asking the AI.

The AI must not become a bypass around application permissions.

Authorization must be enforced before data is provided to the AI.

Sensitive fields should preferably be removed at the API or service layer rather than relying only on prompt instructions.

---

# 11. GLOBAL AI RULES

The following rules apply to all approved AI functions.

## 11.1 Read-Only by Default

The AI should primarily retrieve, explain, and summarize data.

It should not modify operational data unless a future write function is explicitly designed, approved, authenticated, and audited.

## 11.2 Database Over Model Knowledge

For restaurant-specific facts, the AI must use system data.

It must not rely on model memory for:

- Prices
- Inventory
- Order status
- Availability
- Promotions
- Restaurant policies
- Operational totals

## 11.3 No Guessing

If data is missing, unavailable, contradictory, or uncertain, the AI must say that it cannot confirm the answer.

It must not choose a value because it "looks reasonable."

## 11.4 Deterministic Calculations

Where practical, calculations should be performed by normal application code.

Examples:

- Totals
- Taxes
- Low-stock checks
- Counts
- Payment calculations
- Status transitions
- Permission checks

The AI may explain the result.

The AI should not be the system responsible for calculating authoritative operational values.

## 11.5 Logging

AI requests involving restaurant data should be logged where practical.

Useful fields include:

- Requesting user
- User role
- AI function called
- Parameters
- Timestamp
- Result status
- Any refused or unauthorized request

## 11.6 No Silent Changes

The AI must never silently alter production data.

If write capabilities are introduced later, they should require explicit actions and appropriate authorization.

---

# 12. RECOMMENDED CURRENT AI FUNCTION SET

The current implementation can be represented conceptually with functions such as:

```text
get_menu_information()
get_menu_price()
get_inventory_balance()
get_low_stock_items()
get_order_status()
get_order_information()
generate_operational_summary()
```

Recipe-related functions should not be implemented at this stage.

Do not add functions such as:

```text
get_recipe()
calculate_recipe_yield()
calculate_ingredient_consumption()
deduct_inventory_from_order()
calculate_recipe_cost()
forecast_recipe_usage()
```

These belong to a future optional module only.

---

# 13. ARCHITECTURAL PRINCIPLE

The intended relationship between the system and the AI is:

```text
Database / Application Logic
            ↓
     Verified Result
            ↓
           AI
            ↓
 Natural-language explanation
```

Not:

```text
User question
     ↓
    AI guess
     ↓
Operational decision
```

The AI is a communication and interpretation layer.

It is not the source of truth.

---

# 14. FINAL IMPLEMENTATION DIRECTION

For the current release:

- Keep menu lookup.
- Keep price lookup.
- Keep simple inventory balances.
- Keep configurable low-stock thresholds and notifications.
- Keep order-status lookup.
- Keep approved operational summaries.
- Keep role-based access control.
- Keep AI functions read-only by default.
- Keep deterministic application logic responsible for authoritative values.
- Keep AI activity auditable where practical.
- Do not implement recipes.
- Do not implement automatic ingredient consumption.
- Do not implement recipe yields.
- Do not implement menu-to-ingredient inventory deduction.

Recipe management is reserved for future evaluation after the product has paying restaurant customers, clear demand for the feature, and sufficient funding and development capacity.

Until then, recipe functionality is explicitly outside project scope.
