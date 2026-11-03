# Codex integration review checklist

## Scope gate

- [ ] `DELIVERY` contains exactly the four permitted files.
- [ ] No full app, generated shell, package manager, build artifacts, binary assets, or unrelated files.
- [ ] No injected services, persistence, authentication, routing, JavaScript, or external dependencies.
- [ ] Existing application contracts were not redefined or replaced.

## Compile contract

- [ ] Exact required parameters are present.
- [ ] Two-way `ReturnReason` binding invokes `ReturnReasonChanged`.
- [ ] Event callbacks emit only the current `Order.Id`.
- [ ] New, Preparing, Ready, unsupported, Busy, and Overdue states render safely.
- [ ] Safe short-ID formatting and active-item filtering.

## Visual and responsive review

- [ ] Table, state, timer, items, notes, and CTA hierarchy match the handoff.
- [ ] Tested at card widths 320, 400, 480, and 560px.
- [ ] Tested within 1920x1080, 1366x768, and 1024x768 landscape previews.
- [ ] Long names, notes, 12 lines, large quantities, and `-125:09` do not clip or overflow.
- [ ] No entire-header saturation that reduces readability.

## Accessibility review

- [ ] Article label includes table and status.
- [ ] State and overdue meaning are textual, not color-only.
- [ ] Native buttons/textarea, native disabled state, 48px targets.
- [ ] Keyboard focus is visible.
- [ ] Timer/status live region is polite and narrowly scoped.
- [ ] Reduced-motion rule exists.

## Verification before integration

- [ ] Review every delivered line.
- [ ] Copy into a disposable branch only.
- [ ] Build the full solution.
- [ ] Run focused automated tests.
- [ ] Render with synthetic New, Preparing, Ready, overdue, long-content, empty-items, and busy fixtures.
- [ ] Capture desktop screenshots and compare with `KDS_REFERENCE.jpg`.
- [ ] Do not claim production acceptance until the owner visually approves the integrated runtime.
