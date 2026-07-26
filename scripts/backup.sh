#!/usr/bin/env bash
set -euo pipefail

: "${BACKUP_RECIPIENT:?Set BACKUP_RECIPIENT to an age public key}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$BACKUP_DIR/daily" "$BACKUP_DIR/monthly"

docker compose exec -T db mariadb-dump --single-transaction --routines --events \
  -uroot -p"${DB_ROOT_PASSWORD:?Set DB_ROOT_PASSWORD}" "${DB_NAME:-roms}" \
  | gzip -9 | age -r "$BACKUP_RECIPIENT" -o "$BACKUP_DIR/daily/roms-$STAMP.sql.gz.age"

find "$BACKUP_DIR/daily" -type f -name '*.age' -mtime +30 -delete
if [ "$(date -u +%d)" = "01" ]; then
  cp "$BACKUP_DIR/daily/roms-$STAMP.sql.gz.age" "$BACKUP_DIR/monthly/"
  find "$BACKUP_DIR/monthly" -type f -name '*.age' -mtime +366 -delete
fi
