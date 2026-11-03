# Pilot module manifest

## Objective

Evaluate whether Grok Build can deliver one precise, source-compatible ARCWorks component without regenerating or taking ownership of the application.

## Selected Lego block

`KitchenOrderTicket`: one KDS ticket card, chosen because the waiter area has completed its current gate and Kitchen is the next UI area. The card has high visual value but a narrow integration boundary.

## Included inputs

- `PASTE_THIS_INTO_GROK.md` — the one prompt to paste.
- `INPUT/COMPONENT_CONTRACT.md` — exact public interface, behavior, states, edge cases, and exclusions.
- `INPUT/VISUAL_HANDOFF.md` — hierarchy, tokens, layout, responsive, motion, accessibility, and rationale.
- `INPUT/EXISTING_TYPES.cs` — exact existing application records and enum.
- `INPUT/THEME_TOKENS.css` — exact current theme variables.
- `INPUT/CURRENT_KITCHEN_EXCERPT.razor` — reduced, non-secret parent context.
- `INPUT/BASELINE.md` — authoritative repository checkpoint.
- `INPUT/KDS_REFERENCE.jpg` — visual direction only.
- `REVIEW_CHECKLIST.md` — Codex acceptance gate.

Reference image SHA-256: `E058E367EE69FB242D5B040748C8B032235CBB843D043C203271812CFE37C68C`.

## Explicitly excluded

Secrets, configuration, database access, service implementations, authentication, Docker, deployment, full Kitchen page, production data, and permission to modify the authoritative repository.

## Workflow

1. Upload this module or the generated ZIP to Grok Build.
2. Paste `PASTE_THIS_INTO_GROK.md` as the single prompt.
3. Download the returned `DELIVERY` unchanged.
4. Place it under `GROK_OUTPUT`.
5. Ask Codex to inspect it. No integration occurs until review and owner acceptance.
