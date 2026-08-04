# ARCWorks Restaurant Suite branding and compatibility policy

Effective 2026-08-04, the public product name is **ARCWorks Restaurant Suite**.
New user-facing UI, PWA metadata, deployment examples, operational labels, and
project introductions must use that name (or **ARCWorks** where a compact label
is required).

## Intentional legacy identifiers

`ROMS` remains the internal code name. It is intentionally retained in:

- .NET project, namespace, assembly, and solution names (`Roms.*`);
- database names, EF migration history, table mappings, and persisted role data;
- Docker service names, image names, compatibility environment-variable prefixes,
  and existing Compose instance/volume names;
- Data Protection's application name (`ROMS`) so existing cookies and key rings
  remain valid during a staged deployment;
- historical audit reports, work logs, benchmark evidence, and prior acceptance
  records.

These are not unfinished branding. Renaming them would be a migration or
compatibility operation and must be planned separately with a backup, a staging
cutover, and an explicit rollback path.

## Deployment rule

The canonical source may use the final public brand immediately. The running
legacy `arcworks-resto-*` stack is not renamed in place. A future production
cutover must record the selected commit, database migration level, key-ring
handling, image digests, tunnel routes, and backup identity before changing any
technical identifiers.
