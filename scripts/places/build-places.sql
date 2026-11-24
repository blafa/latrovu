CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

DROP TABLE IF EXISTS places;
CREATE TABLE places (
    id          bigserial PRIMARY KEY,
    kind        text NOT NULL,
    name        text NOT NULL,
    search_name text NOT NULL,
    context     text,
    geom        geometry(MultiPolygon, 2056) NOT NULL
);

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'canton', name, lower(unaccent(name)), NULL, ST_Multi(ST_Union(ST_Force2D(geom)))
FROM geo_raw.cantons
WHERE name IS NOT NULL
GROUP BY name;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'region', name, lower(unaccent(name)), 'Kanton ' || kantonsnummer, ST_Multi(ST_Union(ST_Force2D(geom)))
FROM geo_raw.regions
WHERE name IS NOT NULL
GROUP BY name, kantonsnummer;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'municipality', name, lower(unaccent(name)), 'Kanton ' || kantonsnummer, ST_Multi(ST_Union(ST_Force2D(geom)))
FROM geo_raw.municipalities
WHERE name IS NOT NULL
GROUP BY name, kantonsnummer;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'locality', name, lower(unaccent(name)), NULL, ST_Multi(ST_Union(ST_Force2D(wkb_geometry)))
FROM geo_raw.plz_amtovz_locality
WHERE name IS NOT NULL
GROUP BY name;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'zip', z.zip4::text, z.zip4::text, l.name, ST_Multi(ST_Union(ST_Force2D(z.wkb_geometry)))
FROM geo_raw.plz_amtovz_zip z
JOIN geo_raw.plz_amtovz_locality l ON l.localityid = z.fk_localit
GROUP BY z.zip4, l.name;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'district', 'Kreis ' || name, 'kreis ' || name, 'Zürich',
       ST_Multi(ST_CollectionExtract(ST_CurveToLine(ST_Force2D(geometry)), 3))
FROM geo_raw.adm_stadtkreise_a;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'quarter', qname, lower(unaccent(qname)), 'Zürich, ' || kname,
       ST_Multi(ST_CollectionExtract(ST_CurveToLine(ST_Force2D(geometry)), 3))
FROM geo_raw.adm_statistische_quartiere_v;

INSERT INTO places (kind, name, search_name, context, geom)
SELECT 'alias', alias.name, lower(unaccent(alias.name)), 'Zürich', ST_Multi(ST_Union(q.geom))
FROM (VALUES
        ('Wiedikon', ARRAY['Alt-Wiedikon', 'Friesenberg', 'Sihlfeld']),
        ('Aussersihl', ARRAY['Langstrasse', 'Werd']),
        ('Altstadt', ARRAY['Rathaus', 'Hochschulen', 'Lindenhof', 'City'])
     ) AS alias(name, quarters)
JOIN places q ON q.kind = 'quarter' AND q.name = ANY (alias.quarters)
GROUP BY alias.name;

CREATE INDEX ix_places_geom ON places USING gist (geom);
CREATE INDEX ix_places_search_name ON places USING gin (search_name gin_trgm_ops);

SELECT kind, count(*) FROM places GROUP BY kind ORDER BY kind;

SELECT kind, name, context, round(similarity(search_name, 'wiedikon')::numeric, 2) AS score
FROM places WHERE search_name % 'wiedikon' ORDER BY score DESC LIMIT 5;

SELECT kind, name, context FROM places WHERE search_name IN ('kreis 3', 'oerlikon', '8003', 'bern', 'horgen', 'lugano') ORDER BY kind;
