#!/usr/bin/env bash
set -euo pipefail

STAC=https://data.geo.admin.ch/api/stac/v0.9/collections/ch.swisstopo.swissnames3d
WORK=/tmp/latrovu-places/names
NETWORK=latrovu_default
GDAL_IMAGE=ghcr.io/osgeo/gdal:ubuntu-small-3.10.0
PG="PG:host=latrovu-db port=5432 dbname=latrovu user=latrovu password=${LATROVU_DB_PASSWORD:-devonly}"

mkdir -p "$WORK"

url=$(curl -sf "$STAC/items?limit=100" | jq -r '.features[].assets[].href' | grep -E '_2056.*\.shp\.zip$' | sort | tail -1)
echo "Event=Download Source=$url"
curl -sfL -o "$WORK/names.zip" "$url"
unzip -oq "$WORK/names.zip" -d "$WORK/extracted"

while IFS= read -r shapefile; do
  table="names_$(basename "$shapefile" .shp | tr '[:upper:]' '[:lower:]')"
  echo "Event=Load File=$shapefile Table=$table"
  docker run --rm --network "$NETWORK" -v "$WORK:/data" "$GDAL_IMAGE" \
    ogr2ogr -f PostgreSQL -lco SCHEMA=geo_raw -lco OVERWRITE=YES -nln "$table" -t_srs EPSG:2056 -overwrite \
    "$PG" "/data/${shapefile#"$WORK"/}"
done < <(find "$WORK/extracted" -iname '*.shp')

docker exec latrovu-db psql -U latrovu -d latrovu -c "
  SELECT table_name, string_agg(column_name, ', ' ORDER BY ordinal_position) AS columns
  FROM information_schema.columns WHERE table_schema = 'geo_raw' AND table_name LIKE 'names_%' GROUP BY table_name;"
