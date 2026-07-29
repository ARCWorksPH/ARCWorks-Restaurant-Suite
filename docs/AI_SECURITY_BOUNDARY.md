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

- no published host port;
- cloud inference disabled;
- internal inference network only;
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

## Model installation

The inference network is intentionally internal. Model installation is a
controlled maintenance action:

1. Temporarily attach the Ollama container to Docker's bridge network.
2. Pull the explicitly selected model.
3. Disconnect the bridge network immediately.
4. Restart Ollama and verify that the model persists in its named volume.
5. Confirm that only the internal inference network remains.

Do not bind-mount the native Windows Ollama model directory into the container.

## Current limitations

- Container Ollama detects CPU only on this Windows/Docker Desktop setup.
- Native Windows Ollama currently runs TinyLlama on the AMD GPU and remains
  installed as a benchmark and rollback.
- TinyLlama has already demonstrated material semantic errors. It is not an
  approved model.
- No user interface, voice path, database query, inventory mutation, or
  production authorization path is connected to the AI lab.

## Stop and rollback

Stop the laboratory without affecting ROMS:

```powershell
docker compose --profile ai-lab stop command-gateway ollama
```

The production app, database, tunnel, and monitor do not depend on the AI lab.
