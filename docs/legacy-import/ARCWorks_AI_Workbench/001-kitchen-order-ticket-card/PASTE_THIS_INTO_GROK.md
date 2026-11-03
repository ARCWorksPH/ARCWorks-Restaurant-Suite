# Grok Build pilot: ARCWorks Kitchen Order Ticket

Build exactly one reusable Blazor component for an existing application. Do not build an application, page, dashboard, backend, database, authentication system, router, API, or project shell.

Read every file in the attached `INPUT` folder before producing output. The authoritative requirements are `INPUT/COMPONENT_CONTRACT.md` and `INPUT/VISUAL_HANDOFF.md`. The reference image is directional; the written contract wins where they differ.

## Required delivery

Return one folder named `DELIVERY` containing only:

1. `KitchenOrderTicket.razor`
2. `KitchenOrderTicket.razor.css`
3. `DELIVERY_MANIFEST.md`
4. `TEST_CASES.md`

You may create a disposable preview internally while designing, but do not put the preview shell, generated framework, package files, lock files, build output, screenshots, or unrelated files in `DELIVERY`.

## Non-negotiable boundaries

- Target: .NET 10 Blazor, Razor component plus scoped CSS.
- Use only the supplied component parameters, existing `OrderView`, `OrderItemView`, and `OrderStatus` types, and `EventCallback` outputs.
- No injected services, network calls, persistence, JavaScript, external packages, external fonts, external images, or CDN assets.
- No order-transition business logic. The parent page owns all mutations.
- Do not change the supplied model types or invent fields.
- Do not reproduce or rewrite `Kitchen.razor`.
- Do not use Bootstrap utility classes as the component's main styling system. Scoped component CSS must own its appearance.
- Keep status meaning available through text and semantics, not color alone.
- The component must compile when placed in `src/Roms.Web/Components/Kitchen/KitchenOrderTicket.razor`.
- If any requirement is impossible, disclose it in `DELIVERY_MANIFEST.md`; do not silently substitute a different architecture.

## Quality target

This is a small, integration-ready Lego block. It should visually approach the tactile, high-contrast Kitchen Display reference while remaining faithful to the current application contract. It must handle New, Preparing, and Ready states, long notes, long item names, multiple lines, overdue timers, disabled/busy actions, keyboard focus, and landscape display sizes.

Do not claim integration, runtime testing, or accessibility compliance unless you actually performed and documented that check.
