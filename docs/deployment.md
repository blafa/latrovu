# Deployment

Target: one Infomaniak VM in Switzerland, Docker Compose, images from `ghcr.io/blafa/latrovu-*` built by `.github/workflows/build.yml` on every push to `main`.

| Container | Role | Public |
|---|---|---|
| caddy | TLS, `latrovu.com` (static site), `app.latrovu.com` → web | 80, 443 |
| web | Review pages, view links, renewal | no |
| intake | Mail polling | no |
| extraction | Event processing (Listings, Searches) | no |
| scheduler | Reminders, question forwarding, outbox cleanup, retention | no |
| db | PostgreSQL 17 + PostGIS + pgvector | no |
| rabbitmq | Messaging | no |

## One-time preparation

1. **VM:** Infomaniak Public Cloud, Ubuntu 24.04, at least 2 vCPU and 4 GB RAM, a separate data volume for Docker.
2. **Firewall (security group):** 22 only from your own IP, 80 and 443 from everywhere.
3. **DNS:** A records (and AAAA if IPv6) for `latrovu.com`, `www.latrovu.com` and `app.latrovu.com` to the VM.
4. **S3:** one bucket for the app (images), one separate bucket for backups.
5. **Backup key:** on your own machine `age-keygen -o latrovu-backup.key`. Keep the key file offline; only the public key (`age1…`) goes to the server.
6. **GitHub token:** personal access token (classic) with `read:packages` only, for pulling images.

## Server setup

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y ca-certificates curl age unattended-upgrades
curl -fsSL https://get.docker.com | sudo sh
sudo dpkg-reconfigure -plow unattended-upgrades
```

Disk encryption: put `/var/lib/docker` on the data volume encrypted with LUKS (`cryptsetup luksFormat`, unlock at boot with a key file stored outside the volume). Do this before the first start, because the database volume lives there.

Log retention (14 days, see `docs/data-inventory.md`):

```bash
sudo mkdir -p /etc/systemd/journald.conf.d
printf '[Journal]\nMaxRetentionSec=14day\nSystemMaxUse=2G\n' | sudo tee /etc/systemd/journald.conf.d/latrovu.conf
sudo systemctl restart systemd-journald
```

## First start

```bash
sudo mkdir -p /opt/latrovu && cd /opt/latrovu
```

Copy `docker/compose.prod.yml` and `docker/backup.sh` from the repository into `/opt/latrovu`, then create the three configuration files from the `.example` files and fill in the values:

```bash
sudo chmod 600 .env app.env backup.env
sudo chmod 700 backup.sh
echo "<github token>" | sudo docker login ghcr.io -u blafa --password-stdin
sudo docker compose -f compose.prod.yml pull
sudo docker compose -f compose.prod.yml up -d
sudo docker compose -f compose.prod.yml ps
```

Check: all containers `running` (db and rabbitmq `healthy`), `https://latrovu.com` shows the site, `https://app.latrovu.com` answers "Latrovu Web".

Nightly backup at 03:00:

```bash
echo '0 3 * * * root /opt/latrovu/backup.sh' | sudo tee /etc/cron.d/latrovu-backup
```

## Update

Every push to `main` builds new images tagged with the commit SHA and `main`. To deploy a specific version, set `TAG=<sha>` in `.env`; to deploy the latest, keep `TAG=main`.

```bash
cd /opt/latrovu
sudo docker compose -f compose.prod.yml pull
sudo docker compose -f compose.prod.yml up -d
```

## Logs

```bash
sudo journalctl -t latrovu-web -f
sudo journalctl -t latrovu-backup --since today
```

## Restore

```bash
age -d -i latrovu-backup.key latrovu-<stamp>.dump.age > latrovu.dump
sudo docker compose -f compose.prod.yml exec -T db pg_restore -U latrovu -d latrovu --clean --if-exists < latrovu.dump
```

Run the restore on your own machine or copy the key to the server only for the duration of the restore.
