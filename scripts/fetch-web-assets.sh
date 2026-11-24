#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

ASSETS=src/Hosts/Latrovu.Hosts.Web/wwwroot/assets
HTMX_URL=https://unpkg.com/htmx.org@2.0.4/dist/htmx.min.js
FONT_BASE=https://cdn.jsdelivr.net/npm/@fontsource/outfit@5/files

mkdir -p "$ASSETS/fonts"

cp branding/favicon.svg branding/favicon-32.png branding/favicon-180.png branding/latrovu-logo.svg "$ASSETS/"
echo "Event=Copied Files=branding"

curl -sfL -o "$ASSETS/htmx.min.js" "$HTMX_URL"
echo "Event=Downloaded File=htmx.min.js"

for weight in 400 500 600 700; do
  curl -sfL -o "$ASSETS/fonts/outfit-latin-$weight-normal.woff2" "$FONT_BASE/outfit-latin-$weight-normal.woff2"
  echo "Event=Downloaded File=outfit-latin-$weight-normal.woff2"
done

ls -la "$ASSETS" "$ASSETS/fonts"
