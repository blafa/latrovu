#!/usr/bin/env bash
set -euo pipefail

STAC=https://data.geo.admin.ch/api/stac/v0.9/collections
WORK=/tmp/latrovu-places
NETWORK=latrovu_default
GDAL_IMAGE=ghcr.io/osgeo/gdal:ubuntu-small-3.10.0
PG="PG:host=latrovu-db port=5432 dbname=latrovu user=latrovu password=${LATROVU_DB_PASSWORD:-devonly}"
ZURICH_WFS=https://www.ogd.stadt-zuerich.ch/wfs/geoportal

command -v jq > /dev/null || { echo "jq is missing: sudo apt install -y jq"; exit 1; }
mkdir -p "$WORK"
docker exec latrovu-db psql -U latrovu -d latrovu -q -c "CREATE EXTENSION IF NOT EXISTS postgis; CREATE SCHEMA IF NOT EXISTS geo_raw;"

latest_asset() {
  curl -sf "$STAC/$1/items?limit=100" | jq -r '.features[].assets[].href' | grep -E "$2" | sort | tail -1
}

download() {
  echo "Event=Download Source=$2"
  curl -sfL -o "$WORK/$1" "$2"
}

load() {
  local file=$1 table=$2
  shift 2
  echo "Event=Load File=$file Table=$table"
  docker run --rm --network "$NETWORK" -v "$WORK:/data" "$GDAL_IMAGE" \
    ogr2ogr -f PostgreSQL -lco SCHEMA=geo_raw -lco OVERWRITE=YES -nln "$table" -t_srs EPSG:2056 -nlt PROMOTE_TO_MULTI -overwrite \
    "$PG" "/data/$file" "$@"
}

download plz.zip "$(latest_asset ch.swisstopo-vd.ortschaftenverzeichnis_plz '_2056\.shp\.zip$')"
unzip -oq "$WORK/plz.zip" -d "$WORK/plz"
while IFS= read -r shapefile; do
  table="plz_$(basename "$shapefile" .shp | tr '[:upper:]' '[:lower:]')"
  load "${shapefile#"$WORK"/}" "$table"
done < <(find "$WORK/plz" -iname '*.shp')

download boundaries.zip "$(latest_asset ch.swisstopo.swissboundaries3d '_2056_.*\.gpkg\.zip$')"
unzip -oq "$WORK/boundaries.zip" -d "$WORK/boundaries"
load "boundaries/$(cd "$WORK/boundaries" && ls *.gpkg | head -1)" municipalities tlm_hoheitsgebiet

load_wfs() {
  echo "Event=LoadWfs Source=$1"
  docker run --rm --network "$NETWORK" "$GDAL_IMAGE" \
    ogr2ogr -f PostgreSQL -lco SCHEMA=geo_raw -lco OVERWRITE=YES -s_srs EPSG:2056 -t_srs EPSG:2056 -nlt PROMOTE_TO_MULTI -overwrite "$PG" "WFS:$1"
}

load_wfs "$ZURICH_WFS/Stadtkreise"
load_wfs "$ZURICH_WFS/Statistische_Quartiere"

docker exec latrovu-db psql -U latrovu -d latrovu -c "
  SELECT table_name, string_agg(column_name, ', ' ORDER BY ordinal_position) AS columns
  FROM information_schema.columns WHERE table_schema = 'geo_raw' GROUP BY table_name;"
docker exec latrovu-db psql -U latrovu -d latrovu -c "
  SELECT relname AS table_name, n_live_tup AS rows FROM pg_stat_user_tables WHERE schemaname = 'geo_raw' ORDER BY relname;"
