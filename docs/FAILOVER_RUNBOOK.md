# ROMS manual failover runbook

Only the nominated incident administrator may promote a database. Record every command, UTC time, replication position, and decision in the incident log. Never allow both nodes to accept writes.

## Promote the cloud standby

1. Confirm the local primary is genuinely unavailable and record the last successful replication timestamp. If the local server still runs, stop the `app` and `db` containers and block its network access (fence it).
2. On the cloud standby, run `SHOW REPLICA STATUS\G`. Record `Seconds_Behind_Master`, both GTID positions, and any SQL/IO errors. If lag exceeds 60 seconds, announce the possible data-loss window before proceeding.
3. Stop replication with `STOP REPLICA; RESET REPLICA ALL;`, then set `SET GLOBAL read_only=OFF;` and `SET GLOBAL super_read_only=OFF;`.
4. Start the cloud application using the exact image digest deployed locally. Verify `/health`, staff login, table list, and one clearly identified test order.
5. Change the single ROMS hostname to the cloud endpoint. Keep DNS TTL at 30 seconds during the incident and verify resolution from a waiter device and kitchen display.
6. Announce cloud operation to staff. Monitor application errors, database writes, disk space, and latency continuously.

## Restore the local primary

1. Do not restart the old local database as writable. Rebuild it from a fresh encrypted cloud backup or replication seed.
2. Configure the rebuilt local node as a read-only replica of cloud. Verify GTID continuity, zero replication errors, and sustained zero lag.
3. Schedule a quiet cutback window. Stop restaurant writes, confirm both nodes have identical GTID positions, fence the cloud app, promote local, and switch the hostname back.
4. Reconfigure cloud as the read-only standby, submit and complete a test order locally, and document actual RPO/RTO.

## Quarterly restore test

Restore the newest encrypted backup into an isolated database, run migrations, compare table counts and recent completed orders, then record recovery duration and evidence. A backup is not considered successful until this test passes.
