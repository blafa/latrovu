#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
source ./backup.env

retention_days=30
stamp=$(date -u +%Y%m%dT%H%M%SZ)
file="/tmp/latrovu-${stamp}.dump.age"

aws_s3() {
    docker run --rm \
        -e AWS_ACCESS_KEY_ID="$BACKUP_ACCESS_KEY" \
        -e AWS_SECRET_ACCESS_KEY="$BACKUP_SECRET_KEY" \
        -e AWS_DEFAULT_REGION="$BACKUP_REGION" \
        -v /tmp:/tmp \
        amazon/aws-cli:2.22.0 --endpoint-url "$BACKUP_ENDPOINT" "$@"
}

docker compose -f compose.prod.yml exec -T db pg_dump -U latrovu -d latrovu --format=custom \
    | age -r "$BACKUP_AGE_RECIPIENT" > "$file"
aws_s3 s3 cp "$file" "s3://${BACKUP_BUCKET}/db/"
rm -f "$file"

cutoff=$(date -u -d "-${retention_days} days" +%Y-%m-%dT%H:%M:%SZ)
aws_s3 s3api list-objects-v2 --bucket "$BACKUP_BUCKET" --prefix db/ \
        --query "Contents[?LastModified<'${cutoff}'].Key" --output text \
    | tr '\t' '\n' \
    | grep -v -e '^None$' -e '^$' \
    | while read -r key; do aws_s3 s3 rm "s3://${BACKUP_BUCKET}/${key}"; done

logger -t latrovu-backup "Event=BackupUploaded Stamp=${stamp} RetentionDays=${retention_days}"
