# Component contract: `KitchenOrderTicket`

## Purpose

Render one operational kitchen ticket inside the existing ARCWorks Restaurant Suite Kitchen Display. The component is presentational and interaction-emitting only. The existing parent page remains responsible for loading orders, timer updates, return-reason validation, service calls, authorization, and state transitions.

## Integration target

- Destination folder after acceptance: `src/Roms.Web/Components/Kitchen/`
- Framework: .NET 10 Blazor
- Namespace through folder convention: `Roms.Web.Components.Kitchen`
- Existing global tokens: see `THEME_TOKENS.css`
- Existing application types: see `EXISTING_TYPES.cs`
- Reference parent behavior: see `CURRENT_KITCHEN_EXCERPT.razor`

## Required parameters

Use these exact public parameters. Do not add service or repository parameters.

```csharp
[Parameter, EditorRequired] public OrderView Order { get; set; } = default!;
[Parameter, EditorRequired] public string TimerText { get; set; } = "00:00";
[Parameter] public bool IsOverdue { get; set; }
[Parameter] public bool Busy { get; set; }
[Parameter] public string ReturnReason { get; set; } = string.Empty;
[Parameter] public EventCallback<string> ReturnReasonChanged { get; set; }
[Parameter] public EventCallback<Guid> StartPreparing { get; set; }
[Parameter] public EventCallback<Guid> MarkReady { get; set; }
[Parameter] public EventCallback<Guid> ReturnToWaiter { get; set; }
```

Implement `ReturnReason` as a two-way bindable parameter by invoking `ReturnReasonChanged` on textarea input/change. Do not validate or mutate the order inside the component.

## Data rules

- Table title: `Table {Order.TableNumber}`.
- Short order identifier: first 8 characters of `Order.Id` using safe formatting; never assume a non-empty string slice without guarding it.
- Show `Order.WaiterName` and `Order.Revision` as secondary metadata.
- Render only `Order.Items` where `IsRemoved == false`.
- Each line shows `Quantity x Name`.
- Notes remain immediately attached to their item line and must be visually prominent.
- Show `TimerText` exactly as passed by the parent. A leading minus sign means overdue.
- For Preparing only, show `Order.PreparationTargetMinutes` when present.
- Do not display price, total, payment, inventory, recipe, or customer data.

## State matrix

### New

- Visible text state: `NEW` and `Awaiting kitchen acceptance`.
- Blue state treatment.
- Primary action: `Start preparing`, emits `StartPreparing(Order.Id)`.
- Return-reason textarea is visible.
- Secondary destructive action: `Return to waiter`, emits `ReturnToWaiter(Order.Id)`.
- The component does not decide whether an empty reason is valid.

### Preparing

- Visible text state: `PREPARING` and `In preparation`.
- Amber state treatment.
- Primary action: `Ready`, emits `MarkReady(Order.Id)`.
- No return textarea or return action.

### Ready

- Visible text state: `READY` and `Waiting for waiter`.
- Green state treatment.
- No mutable action.
- Show a non-button ready callout: `Waiting for waiter to serve...`.

### Unsupported status

- Render the ticket safely with a neutral state label derived from `Order.Status`.
- Do not show mutation actions.
- Do not throw.

### Busy

- Disable every visible form control and button.
- Preserve labels and layout.
- Add `aria-busy="true"` at the article level.

### Overdue

- Timer area includes visible text `OVERDUE` in addition to its color treatment.
- Do not change or invent the timer value.

## Interaction constraints

- Buttons must be real `<button type="button">` elements.
- Minimum action target: 48px high.
- A click emits exactly one relevant callback.
- The card does not call services, navigate, refresh, or alter `Order`.
- Event handlers must tolerate an unassigned callback without throwing.

## Layout constraints

- The parent grid controls card count and columns. The component must fill its allocated width without defining the page grid.
- Target card width: usable from 320px to 560px.
- Target landscape surfaces: 1920x1080 primary; 1366x768 and 1024x768 secondary.
- Minimum card height should be content-driven; do not clip a long order.
- Keep the footer visually anchored after the item list without absolute positioning.
- Do not create a portrait/mobile page layout; this is a landscape-only Kitchen Display component, but it must not overflow its card width.

## Content and edge cases

Handle all of these without overlap, clipping, or horizontal scroll:

- Table number: `VIP-12A`.
- Waiter name: `Alexandria de la Cruz-Santos`.
- 12 active item lines.
- Quantity: 99.
- Item name: 80 characters.
- Notes: 240 characters with natural wrapping.
- No active item lines: show `No active items on this ticket.`.
- Missing preparation target.
- Timer strings: `14:32`, `00:00`, `-125:09`.
- Revision at least three digits.

## Accessibility requirements

- Use an `<article>` with an accessible label containing table and status.
- One logical heading for the ticket.
- State must be conveyed with text, not color alone.
- Notes must not depend only on an icon.
- Use `aria-live="polite"` for the timer/status region, not the entire ticket.
- All interactive controls require a visible `:focus-visible` treatment using `--roms-focus`.
- Disabled controls must be visibly disabled and use the native `disabled` attribute.
- Maintain readable contrast against the dark card.
- Respect `prefers-reduced-motion`; no essential animation.

## Prohibited additions

- Acceptance-extension controls or counters.
- Station routing.
- Per-item preparation timers or check-off controls.
- Audio, printing, notifications, drag-and-drop, or swipe behavior.
- Customer feedback, guest count, recipe, inventory, or payment information.
- New enums, DTOs, JavaScript files, or NuGet/npm dependencies.
