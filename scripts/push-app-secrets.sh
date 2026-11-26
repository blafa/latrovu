#!/usr/bin/env bash
set -euo pipefail

readonly PROJECT="src/Hosts/Latrovu.Hosts.Web"
readonly REMOTE="latrovu"
readonly REMOTE_FILE="/opt/latrovu/app.env"
readonly KEYS=(
    AI:ProductId
    AI:Token
    Storage:Endpoint
    Storage:Region
    Storage:AccessKey
    Storage:SecretKey
    Mail:User
    Mail:Password
    Smtp:Host
    Smtp:User
    Smtp:Password
    Smtp:FromAddress
    Viewing:InquiryAddress
)

cd "$(dirname "$0")/.."
secrets=$(dotnet user-secrets list --project "$PROJECT")

updates=""
for key in "${KEYS[@]}"; do
    value=$(printf '%s\n' "$secrets" | grep -m1 "^${key} = " | sed "s/^${key} = //" || true)
    if [ -z "$value" ]; then
        echo "Event=SecretMissing Key=${key}" >&2
        continue
    fi
    updates+="${key//:/__}=${value}"$'\n'
done

printf '%s' "$updates" | ssh "$REMOTE" "
set -euo pipefail
file=${REMOTE_FILE}
while IFS= read -r line; do
    name=\${line%%=*}
    tmp=\$(mktemp)
    grep -v \"^\${name}=\" \"\$file\" > \"\$tmp\" || true
    printf '%s\n' \"\$line\" >> \"\$tmp\"
    cat \"\$tmp\" > \"\$file\"
    rm -f \"\$tmp\"
done
echo \"Event=AppEnvUpdated File=\${file} Lines=\$(grep -c . \"\$file\")\"
"
echo "Event=SecretsCopied Count=$(printf '%s' "$updates" | grep -c .)"
