# Data inventory

Last updated: 2026-11-26. Source for the privacy statement. Update it with every schema or retention change.

## Processors

| Processor | Location | Used for |
|---|---|---|
| Infomaniak | Switzerland | VM hosting (planned), S3 object storage (`gva2`), AI API (Mistral Small via `api.infomaniak.com`), mail (IMAP intake, SMTP sending) |

PostgreSQL and RabbitMQ run on our own VM. No transfer abroad. To confirm in Infomaniak's data processing terms: data stays in Switzerland, AI inputs are not used for training.

## Data subjects

- **Seekers:** e-mail address, search wishes in their own words, criteria, chat questions, viewing bookings, language.
- **Providers (agents, private owners):** e-mail address, exposé with contact details, answers to questions, viewing times.
- **Third parties named in exposés:** e.g. contact persons, sometimes current tenants. Not collected from them directly.
- **People unlocking a shared link:** e-mail address.

## Inventory

| # | Data | Where | Purpose | Kept | Deleted by |
|---|---|---|---|---|---|
| 1 | Listing: sender address, subject, file name, extracted data, field evidence, full page text, location | `listings` | Publish, match, answer questions | Until withdrawn, at most ~3 months + 1 day without renewal | Withdrawal or `RenewalReminderService` → `ListingRemover` (cascade) |
| 2 | Listing images | S3, keys in `listing_images` | Show the listing | As 1 | `ListingRemover` deletes the objects |
| 3 | Raw mail attachments | S3 (temporary) | Processing | Until processed | `SubmissionReceivedHandler` |
| 4 | Incoming mails (exposés, search requests, replies) | Mailbox | Processing | Until processed (deleted right after) | `ImapInbox` |
| 5 | Review access: tokens, verification codes (hashed), sessions, renewal tokens | `listing_access_tokens`, `listing_verification_codes`, `listing_review_sessions`, `listing_renewal_tokens` | Provider access without account | Until expiry (hours to days), removed within a day after | `ListingsRetentionTask` (daily), cascade |
| 6 | View links: token hash, search, seeker address, view count | `listing_view_links` | Personal and shared links | Valid 30 days (shared 7), removed 7 days after expiry | `ListingsRetentionTask`, cascade |
| 7 | Chat: questions (with original wording), answers, forwarded questions | `listing_chat_messages` | Answer seekers, forward to provider | Until its view link is removed or the listing is deleted | `ListingsRetentionTask`, cascade |
| 8 | Provider answers | `listing_knowledge` | Reuse for all seekers | Until the listing is deleted | Cascade |
| 9 | Viewing slots and bookings (token hash; address via view link) | `listing_viewing_slots`, `listing_viewing_bookings` | Book viewings, inform provider | Bookings until their view link is removed; slots until the listing is deleted | `ListingsRetentionTask`, cascade |
| 10 | Unlock requests: address | `listing_access_requests` | Rate limit (1 h window, 10 per link) | 24 hours | `ListingsRetentionTask` |
| 11 | Processed intake ids (Message-Ids) | `processed_submissions` | Avoid double processing | 7 days | `ListingsRetentionTask` |
| 12 | Searches: address, name, language, criteria, area, status | `searches` | Find and send matches | Active and paused: open-ended; stopped: 7 days | `SearchesRetentionTask` (cascade to 13, 14) |
| 13 | Alerts sent per search | `search_alerts` | Avoid repeats | With the search; removed when the listing is withdrawn | Cascade, `ListingWithdrawnHandler` |
| 14 | Message-Ids of mails sent to a seeker | `search_sent_messages` | Link replies to their search | With the search | Cascade |
| 15 | Processed search request ids | `processed_search_requests` | Avoid double processing | 7 days | `SearchesRetentionTask` |
| 16 | Listing facts for matching (no personal data) | `matchable_listings` | Matching | Until withdrawn | `ListingWithdrawnHandler` |
| 17 | Event payloads (addresses, titles, times) | `outbox` | Reliable delivery between modules | 7 days after delivery; undelivered rows stay | `OutboxCleanupWorker` |
| 18 | Messages in transit | RabbitMQ queues | Delivery | Until consumed; failed ones stay in the error queue | — |
| 19 | AI answers keyed by prompt hash | `llm_response_cache` | Development only (`AI:CacheResponses = true`) | Forever while enabled | Must stay disabled in production |
| 20 | Application logs (ids, counts; addresses to be checked) | Dev: `/tmp/latrovu/*.log`; production: to define | Operation, debugging | Dev: until restart; production: to define | — |
| 21 | Backups | To define at deployment | Recovery | To define | Rotation |

Daily jobs run in `Latrovu.Hosts.Scheduler` via `RetentionWorker` (once at start, then every 24 h), which runs every registered `IRetentionTask`.

## Open

| # | Topic | Proposal |
|---|---|---|
| O1 | Active or paused searches are kept as long as they exist | Reminder after 12 months without activity, then delete (like listings). |
| O2 | Failed messages (error queue, undelivered outbox rows) | Monitoring and manual handling; maximum 30 days. |
| O3 | Production logs | Retention 14 days; check that no addresses or message texts are logged. |
| O4 | Backups | Encrypted, retention 30 days; deleted data disappears from backups with rotation. |
| O5 | Mails already in the old `Latrovu-Bot/Erledigt` folder | Delete once by hand; new mails are no longer moved there. |
