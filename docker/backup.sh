#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
source ./backup.env

readonly retention_days=30
stamp=$(date -u +%Y%m%dT%H%M%SZ)
workdir=$(mktemp -d)
trap 'rm -rf "$workdir"' EXIT
file_name="latrovu-${stamp}.dump.age"

run_rclone() {
    docker run --rm \
        -e RCLONE_CONFIG_BACKUP_TYPE=s3 \
        -e RCLONE_CONFIG_BACKUP_PROVIDER=Other \
        -e RCLONE_CONFIG_BACKUP_ENDPOINT="$BACKUP_ENDPOINT" \
        -e RCLONE_CONFIG_BACKUP_REGION="$BACKUP_REGION" \
        -e RCLONE_CONFIG_BACKUP_ACCESS_KEY_ID="$BACKUP_ACCESS_KEY" \
        -e RCLONE_CONFIG_BACKUP_SECRET_ACCESS_KEY="$BACKUP_SECRET_KEY" \
        -v "$workdir:/work" \
        rclone/rclone:1.68.2 "$@"
}

docker compose -f compose.prod.yml exec -T db pg_dump -U latrovu -d latrovu --format=custom \
    | age -r "$BACKUP_AGE_RECIPIENT" > "${workdir}/${file_name}"
run_rclone copy "/work/${file_name}" "backup:${BACKUP_BUCKET}/db/"
run_rclone delete --min-age "${retention_days}d" "backup:${BACKUP_BUCKET}/db/"

logger -t latrovu-backup "Event=BackupUploaded Stamp=${stamp} RetentionDays=${retention_days}"
