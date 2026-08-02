# ROMS AI Lab Security Boundary

Status: isolated laboratory; not connected to production command execution

## Runtime topology

```text
ephemeral evaluator
        |
  command network (internal)
        |
  command-gateway
        |
 inference network (internal)
        |
      ollama
```

MariaDB is attached only to the separate `backend` network. Ollama and the
command gateway are not attached to that network. ROMS is not yet attached to
the `command` network.

## Container controls

Ollama:

- API published only to Windows loopback at `127.0.0.1:11434` for controlled
  local benchmarking;
- no LAN, Cloudflare Tunnel, or public route to the Ollama API;
- cloud inference disabled;
- internal inference network for the command gateway;
- separate benchmark bridge used only for Windows loopback access and
  controlled model downloads;
- external model volume `ollama`;
- read-only root filesystem with bounded temporary storage;
- all Linux capabilities dropped;
- `no-new-privileges`;
- CPU, memory, and process limits;
- pinned local image digest.

Command gateway:

- no published host port;
- no database network or credentials;
- no host/project bind mounts;
- read-only root filesystem;
- all Linux capabilities dropped;
- `no-new-privileges`;
- CPU, memory, and process limits.

Neither container receives the Docker socket.

## Local benchmark access

The Ollama API can be reached from this Windows host at:

```text
http://127.0.0.1:11434
```

Examples:

```powershell
Invoke-RestMethod http://127.0.0.1:11434/api/tags
docker exec -it arcworks-resto-ollama-1 ollama list
docker exec -it arcworks-resto-ollama-1 ollama run qwen2.5:3b
```

The native Windows `ollama` command is not required for the containerized
instance. Tools that support an Ollama base URL should use the loopback URL
above. Do not change the binding to `0.0.0.0` and do not add an Ollama
Cloudflare route.

Benchmark resource limits are configurable through `OLLAMA_MEMORY_LIMIT` and
`OLLAMA_CPU_LIMIT`. The defaults are 16 GB and 12 logical CPUs, leaving
capacity for ROMS and MariaDB on the current host. Record these limits with
every result; comparisons made under different limits are not equivalent.

## Model installation

The inference network remains internal. The isolated AI-lab profile gives
Ollama a separate benchmark bridge so models can be downloaded without
attaching it to ROMS' backend, edge, or Docker socket. Model installation is a
controlled maintenance action:

1. Pull only an explicitly selected model through the containerized Ollama
   CLI or its loopback API.
2. Verify that the model persists in the named `ollama` volume.
3. Record the exact model tag, size, benchmark settings, and result.
4. Remove rejected models after the comparison so the portable model volume
   does not accumulate unused multi-gigabyte downloads.

Do not bind-mount the native Windows Ollama model directory into the container.

## Current limitations

- Container Ollama detects CPU only on this Windows/Docker Desktop setup.
- The provisional user-facing laboratory default is `qwen2.5:3b` because its
  Benchmark 3 result was the most balanced across factual, clarification,
  safety, and graceful-failure behavior.
- `qwen3:4b-instruct` is retained as a read-only factual/reporting challenger.
  It is not approved for direct actions and performed poorly on ambiguous
  clarification cases despite strong factual and failure-handling results.
- All rejected benchmark models were removed from the model volume after their
  benchmark records were preserved.
- No user interface, voice path, database query, inventory mutation, or
  production authorization path is connected to the AI lab.

## Stop and rollback

Stop the laboratory without affecting ROMS:

```powershell
docker compose --profile ai-lab stop command-gateway ollama
```

The production app, database, tunnel, and monitor do not depend on the AI lab.
