# ROMS Natural-Language Command Protocol

Status: experimental, non-production
Schema version: 2

## Governing rule

The database owns the truth. The model only proposes a structured command.
Model output is untrusted and must never be executed directly.

## Version 2 commands

### `InventoryLookup`

- Purpose: request the current balance of one exact inventory item.
- Required model fields: catalog item name or alias.
- Forbidden model fields: quantity and unit.
- Execution class: read-only.
- Production confirmation: not required.

### `Unknown`

- Purpose: reject unsupported requests.
- It never produces an executable proposal.

## Gateway outcomes

- `Recognized`: model output passed deterministic catalog and field validation.
  This is still only a proposal.
- `ClarificationRequired`: the model selected an ambiguous or invalid item,
  quantity, or unit.
- `Unsupported`: the model explicitly rejected the request or proposed a command
  outside this schema version.
- `InterpreterError`: timeout, malformed response, unavailable model, or other
  safe failure.

## Trust boundary

The command gateway:

- has no database provider, connection string, credentials, or backend network;
- cannot execute inventory operations;
- accepts a bounded catalog supplied for the request;
- requires exact deterministic catalog and unit matching;
- requires the original text to explicitly name the proposed catalog item;
- rejects every write request, including inventory receiving;
- returns application DTOs rather than model-authored user messages;
- logs request identifiers and outcomes, not database data or credentials.

ROMS must revalidate every future recognized proposal using current database
state, authorization, and command-specific policies.

## Evaluation gates

A candidate model must be measured on a locked corpus. Two metrics are separate:

1. Exact interpretation: correct status, command, item, quantity, and unit.
2. Safety: no unsupported, ambiguous, or incorrect input produces a recognized
   executable proposal.

Valid JSON alone is not a passing result. The initial 20-case corpus is a
baseline and must grow to include real item names, spelling variations, Taglish,
voice transcripts, prompt injection, boundary quantities, and ambiguous units.

## TinyLlama baseline

The first valid container evaluation produced:

- exact interpretation: 6/20;
- safely refused or correct: 12/20;
- unsafe recognized proposals: 8.

After adding deterministic original-text evidence requirements for writes:

- exact interpretation: 8/20;
- safely refused or correct: 20/20;
- unsafe recognized proposals: 0;
- average CPU-only response time: 5.224 seconds.

This proves the fail-closed boundary for the current corpus. It does not approve
TinyLlama: 8/20 exact accuracy is unacceptable for user integration.
