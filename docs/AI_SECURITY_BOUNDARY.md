# ROMS AI Security Boundary

Status: integrated laboratory path; disabled by default

## Runtime topology

```text
browser -> authenticated ROMS app -> backend -> MariaDB
                    |
             command network (internal)
                    |
             command-gateway
                    |
            inference network (internal)
                    |
                  Ollama
```

ROMS is attached to `backend`, `edge`, and the internal `command` network. This
is intentional: ROMS supplies bounded catalogs to the gateway and is the only
component allowed to turn a validated proposal into a permission-checked
database read. The gateway and Ollama are not attached to `backend`.

## Component controls

Ollama:

- API published only to Windows loopback at `127.0.0.1:11434` for controlled
  maintenance and benchmarking;
- no LAN or Cloudflare route;
- no cloud inference;
- external model volume `ollama`;
- read-only root filesystem, bounded temporary storage, dropped capabilities,
  `no-new-privileges`, and CPU/memory/process limits.

Command gateway:

- no published host port;
- no backend network, database provider, database credentials, host/project
  bind mount, or Docker socket;
- read-only root filesystem, bounded temporary storage, dropped capabilities,
  `no-new-privileges`, and CPU/memory/process limits;
- deterministic validation of bounded model output.

ROMS application:

- authenticates the user and loads current roles/ownership from MariaDB;
- supplies only bounded item/category/table catalogs needed for interpretation;
- converts only a recognized, validated proposal to an approved function;
- performs parameterized EF Core queries, not model-generated SQL;
- formats database facts itself and audits every executed AI read;
- exposes no AI write function.

## Feature control

`AI_ENABLED` defaults to `false`. The Assistant link and page are hidden when
disabled. Enabling the flag requires the private `ai-lab` services, but does
not constitute production approval. Disable the flag or stop the AI services
to remove the path without affecting ordering, inventory, MariaDB, tunnel, or
monitoring.

## Local maintenance access

The containerized Ollama API is reachable only from this Windows host:

```text
http://127.0.0.1:11434
```

```powershell
Invoke-RestMethod http://127.0.0.1:11434/api/tags
docker exec -it arcworks-resto-ollama-1 ollama list
```

Do not bind it to `0.0.0.0`, create a tunnel route, or mount a native Windows
Ollama model directory into the container.

## Resource and model boundary

Limits are configurable through `OLLAMA_MEMORY_LIMIT` and `OLLAMA_CPU_LIMIT`.
Record effective limits with benchmark evidence. Model benchmark performance
does not grant access or execution authority; deterministic validation and ROMS
authorization remain mandatory for every model.

## Stop and rollback

1. Set `AI_ENABLED=false` and recreate only the app if the feature had been
   enabled.
2. Stop the laboratory services:

```powershell
docker compose --profile ai-lab stop command-gateway ollama
```

The core ROMS application does not depend on the model to process restaurant
operations.
