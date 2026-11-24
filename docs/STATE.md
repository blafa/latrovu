# Latrovu – project state (handover)

Last updated: 2026-11-25. Read this first in a new session.

## Product in one paragraph
Latrovu ("Te la trovo.") matches flat seekers in Switzerland (start: Zurich) with listings. Agencies send an exposé PDF to `inserat@latrovu.com`; AI extracts the data; the agent checks it on a review page (gated by a 6-digit email code) and approves. Seekers describe what they want in their own words and language (email now; WhatsApp, Telegram, Threema, Signal later) and receive alerts with a **personal, expiring view link** per listing. There are **no public listing pages** (anti-scraping). Contact details of the agent are hidden unless the agent opts in; inquiries go through Latrovu (basis for the later paid applicant handling).

## Key decisions
- Modular monolith, .NET 10, C#. Modules never reference each other; cross-module contracts live in `src/Core`.
- Messaging: **RabbitMQ + Rebus**, with our own **transactional outbox** in Postgres (`outbox` table, `OutboxForwarder`). At-least-once delivery, consumers must be idempotent.
- Storage: one **private** S3 bucket `ad-images` (Infomaniak, endpoint `https://s3.gva2.infomaniak.cloud`, region `gva2`). Own SigV4 signer, no AWS SDK. Bucket `latrovu-public` is no longer used (delete it).
- AI: Infomaniak AI Tools, model `mistralai/Mistral-Small-4-119B-2603`, v2 endpoint, always with `response_format: json_schema` (schemas sealed with `additionalProperties:false`).
- Evidence: per field we store a text snippet (page, before, match, after) from the exposé; PDFs themselves are not stored. Submission files live in S3 `submissions/…` only until processed.
- Review page: A+B design (fact sheet left, evidence/editor pane right), value sources marked: found / derived / AI-written / confirmed by agent / open.
- View links `/i/{token}`: hashed token, 30 days, view count, only for approved listings, `noindex`, 120 req/min per IP. Withdrawing a listing invalidates all links.
- Search side (in progress): search module; channel-neutral input; multiple searches per seeker named by place ("Albisrieden"), updates by naming the place or replying; confirmation contains a "possible refinement" box instead of blocking questions; AI detects language, all messages in the seeker's language (de/fr/it/en hand-written, others AI-translated and cached; admin tool later). Places as polygons with **PostGIS** (swisstopo postal code/municipality polygons, Stadt Zürich districts/quarters); listings geocoded to points; matching = point in area. Soft wishes later via pgvector.

## Solution layout
```
src/Core                                   cross-module contracts (Submissions, Listings events)
src/BuildingBlocks/Latrovu.BuildingBlocks  interfaces: Ai, Email, Images, Messaging, Pdf, Storage
src/BuildingBlocks/...Infrastructure       Infomaniak LLM, SMTP (MailKit), S3, PdfPig, Postgres, Rebus/outbox
src/Modules/Intake (+Infrastructure)       IMAP polling, routing, SubmissionForwarder (S3 + outbox)
src/Modules/Listings (+Infrastructure)     Ingestion, Classification, Extraction, Images, Review, Access, Viewing
src/Hosts/Latrovu.Hosts.Web        :5082   review pages, view links, dev view-link endpoint
src/Hosts/Latrovu.Hosts.Intake     :5083   mail polling, dev import endpoint
src/Hosts/Latrovu.Hosts.Extraction        consumes SubmissionReceived (queue latrovu-extraction)
tests/*                                    xUnit per project, fakes in tests/Latrovu.Listings.Tests/Fakes
docker/postgres/Dockerfile                 pgvector image + PostGIS (being built)
scripts/dev-run.sh                         builds, starts 3 hosts in tmux, logs in /tmp/latrovu/*.log
```
Setup extensions per area: `AddPostgres`, `AddAiExtraction`, `AddObjectStorage`, `AddEmailSending`, `AddPdfReading`, `AddMessagePublishing`, `AddMessageConsuming`, `AddIntakeModule`, `AddListingsReview`, `AddListingsViewing`, `AddListingsProcessing`.

## Events
`SubmissionReceived` (Intake → Listings), `ListingApproved`, `ListingWithdrawn` (Listings → future Search/Notifications).

## Main tables
`listings` (data, extracted_data, field_scores, field_evidence, status draft/approved/withdrawn, publish_contact), `listing_images`, `listing_access_tokens`, `listing_verification_codes`, `listing_review_sessions`, `listing_view_links`, `processed_submissions`, `outbox`.

## Conventions (from the developer)
No comments (exceptions: algorithm references, workarounds, license headers, unclear DTO names). Braces on new lines, explicit types, named constants, methods ≤ 20 lines, file per class, SOLID, structured logging with durations. German UI texts with Swiss spelling ("ss"). Developer runs all git commits. Keep chat answers short.

## Dev workflow
- Infra: `docker compose -f docker-compose.dev.yml up -d` (Postgres `latrovu-db`, RabbitMQ `latrovu-rabbitmq`, UI http://localhost:15672, latrovu/devonly).
- Run: `bash scripts/dev-run.sh`; stop: `tmux kill-session -t latrovu`.
- Test: `set -o pipefail; dotnet test --nologo 2>&1 | grep -E 'error|Passed|Failed'`.
- Import a test exposé: `curl -s -X POST "http://localhost:5083/dev/import?pattern=single_02*.pdf"`; review link: `grep -o 'review at /review/[^ ]*' /tmp/latrovu/extraction.log | tail -1`.
- View link for an approved listing: `curl -s -X POST "http://localhost:5082/dev/view-links/<listingId>"`.
- Re-import a processed file: `DELETE FROM processed_submissions WHERE source_id LIKE 'dev-import:%'`.
- Test exposés in `tests/exposes` (git-ignored, contain personal data).
- `Alerts:ViewBaseUrl` (e.g. http://localhost:5082) for links in alert mails. Test seeker flow: `POST http://localhost:5083/dev/search` with `{"text":"..."}`; replies go to `DevImport:Sender`.
- User secrets (shared id for all hosts): `ConnectionStrings:Db`, `Mail:*`, `Smtp:*`, `AI:*`, `Storage:*`, `Messaging:RabbitMqConnectionString`, `DevImport:Sender`, `Viewing:InquiryAddress`.
- `AI:CacheResponses = true` (dev only): model answers cached in `llm_response_cache` by hash of model, prompt, schema and text, so re-imports cost no AI credits. Changing prompt or schema creates new calls automatically.

## Current status
Done: mail intake, classification, schema-based extraction with evidence, images to S3, review page with code gate, editing, value sources, contact opt-in, approve/withdraw with outbox events, view links with searcher page, three hosts, RabbitMQ/Rebus/outbox, dev LLM response cache, Postgres image `latrovu/postgres:17` with PostGIS 3.7 and pgvector 0.8.7.
In progress: search side. Step 1 done (Searches module, `SearchRequestReceived` from Intake, AI interpretation with language, multiple searches by place name, multilingual HTML replies with refinement box, `POST /dev/search` on Intake). Place catalog built (~67k areas, national): table `places` (kind canton/region/municipality/locality/zip/settlement/quarter/quarter_part/district/lake/alias, name, search_name with pg_trgm, context = municipality, geom MultiPolygon EPSG:2056) from `geo_raw` (swisstopo postal codes via STAC, swissBOUNDARIES cantons/Bezirke/municipalities, swissNAMES3D polygons incl. exonyms like Genf/Ginevra, Zürich districts/quarters via city WFS). Rebuild: `bash scripts/places/import-raw.sh`, `bash scripts/places/import-names.sh`, then `docker exec -i latrovu-db psql -U latrovu -d latrovu < scripts/places/build-places.sql`.

## Next steps (search side)
1. Done: place resolver (`PostgresPlaceResolver`): AI returns structured places (name, within, kind, nearKm) → exact then trigram lookup in `places`, kind hint retried without filter, ambiguity by municipality → question in refinement box, unknown → question; area = union with buffers (lakes 1 km shore) stored in `searches.area`, refs in `searches.place_refs`.
2. Done: geocoding on approval (`ListingApprovedHandler` in Extraction host, `SwisstopoGeocoder` via api3.geo.admin.ch SearchServer, address then postal code fallback) → `listings.location` (Point 2056) + `location_precision`, and `ListingReadyForMatching` (point, offer type, rooms, price, area, availability) via outbox.
3. Done: matching (`ListingMatchingHandler` in Searches on `ListingReadyForMatching`): active searches with `ST_Covers(area, point)`, offer type, rooms min/max, price ≤ max × 1.05, min living area, not yet alerted (`search_alerts`). Per match: token created in Searches, only its hash sent to Listings (`ViewLinkRequested` via outbox, stored by `ViewLinkRequestedHandler`).
4. Done: alert mail (`SeekerAlertMail`, texts de/fr/it/en, neutral "Angebot"), link `{Alerts:ViewBaseUrl}/i/{token}`. Existing listings: Searches keeps `matchable_listings` (filled from `ListingReadyForMatching`, removed on `ListingWithdrawn`); a new or updated active search is matched immediately (max 5 newest). Shared criteria SQL in `SearchCriteria`, alert logic in `SearchAlerter`. Re-announce all approved listings: insert `ListingApproved` rows into `outbox` (see chat history) or withdraw/approve in the UI.
5. Pause/stop by reply (interpretation exists; verify end to end).

## Backlog
- Extraction quality: golden set; object type confused by building names ("Haus First"); wrong ZIP (Wattwil "630"); multi-unit documents (one listing per unit); image classification (photo vs plan vs map vs document; hide land register extracts; photos first); some PDF image formats yield no images.
- Delete listing + data retention; review page HTML tests.
- View link validity (30 days) is hardcoded in `ListingMatchingHandler.LinkValidity` and in the alert texts; make it a setting (probably shorter). Remove dev view-link endpoint (real alerts create links now).
- Deployment: Dockerfiles per host, compose for hosts, optional VS `.dcproj`, Caddy, forwarded headers (cookie `Secure` flag behind proxy), Infomaniak VM.
- Cleanup: delete bucket `latrovu-public`, RabbitMQ queue `latrovu-listings`, any leftover `src/Api` folder.
- Business: Einzelfirma registration → Meta business verification for WhatsApp; Swiss lawyer check before launch; trademark check.
- Other countries (after CH): FR and LU easy (open boundaries, open geocoding), AT easy to medium, DE and IT medium (no open postal code areas, OSM fallback). Prepare: `places.country`, geocoder per country behind `IGeocoder`.
