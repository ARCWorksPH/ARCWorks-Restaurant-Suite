# AI feature hold

**Status:** held and removed from the active product path as of 2026-08-06.

The current release focuses on the deterministic restaurant workflows:

- waiter → kitchen → management
- management → independent-item inventory
- management → staff schedule and reports

The AI assistant and command gateway remain in the repository as future-version
source, contracts, benchmark evidence, and an isolated `ai-lab` Compose profile.
They are not part of the current application release.

## What the hold guarantees

- The Assistant navigation item is hidden.
- `/assistant` renders a not-found response while the hold is active.
- The web app does not register `ICommandGatewayClient` and has no active HTTP
  connection to the command gateway.
- The app is no longer attached to the private `command` Docker network.
- AI service registrations are not activated while the hold is active.
- `Ai:Hold` defaults to `true`; a stale `AI_ENABLED=true` value cannot bypass
  the hold.
- Ollama and `command-gateway` remain available only under the opt-in
  `ai-lab` profile for offline benchmarks and future development. They are not
  required by the production application.

## Future re-enable gate

Do not remove the hold as a routine configuration change. A future version must
first provide a written scope, a reviewed threat model, updated acceptance
tests, and an explicit deployment decision. Then, in a controlled branch:

1. Set `Ai:Hold` to `false` and `Ai:Enabled` to `true` only in the intended
   environment.
2. Restore an explicit `Ai:CommandGatewayBaseUrl`; the application now refuses
   to start without it when the hold is released.
3. Re-attach the app to the `command` network and start the `ai-lab` gateway and
   model services as documented in the Compose files.
4. Run the AI unit, integration, browser, timeout, authorization, and
   adversarial tests, plus a fresh security review.
5. Keep all write operations, unrestricted SQL, recipes, yields, forecasting,
   and autonomous actions out of scope unless separately approved.

The hold is intentionally reversible, but the default remains disconnected so
the waiter, kitchen, management, inventory, schedule, and reports workflows
can be completed and accepted independently.
