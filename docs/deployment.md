# Deployment

Target: one Infomaniak VPS Lite in Switzerland (2 CPU, 4 GB RAM, 60 GB, Ubuntu 26.04 LTS), Docker Compose, images from `ghcr.io/blafa/latrovu-*` built by `.github/workflows/build.yml` on every push to `main`. Beta server: `200.234.68.209`, SSH alias `latrovu` (user `ubuntu`, key `~/.ssh/latrovu`).

| Container | Role | Public |
|---|---|---|
| caddy | TLS, `latrovu.com` (static site), `app.latrovu.com` → web | 80, 443 |
| web | Review pages, view links, renewal | no |
| intake | Mail polling | no |
| extraction | Event processing (Listings, Searches) | no |
| scheduler | Reminders, question forwarding, outbox cleanup, retention | no |
| db | PostgreSQL 17 + PostGIS + pgvector | no |
| rabbitmq | Messaging | no |

## Preparation (on your machine)

1. SSH key: `ssh-keygen -t ed25519 -f ~/.ssh/latrovu -C "latrovu-vps"`, public key imported when ordering the VPS.
2. Backup key, **in WSL, never on the server**: `age-keygen -o ~/latrovu-keys/latrovu-backup.key`. Keep the file in the password manager; only the public key (`age1…`) goes to the server.
3. GitHub token (classic) with `read:packages` only.
4. S3 buckets in the same Infomaniak project: `latrovu-prod-images`, `latrovu-prod-backup`.
5. DNS: A and AAAA records for `latrovu.com`, `www`, `app` to the VPS.
6. Infomaniak Manager → VPS → **Firewall**: incoming TCP `80,443` and UDP `443` from everywhere (SSH is open by default). Without this, Let's Encrypt cannot reach Caddy.

## Server setup

```bash
sudo apt update && sudo apt upgrade -y
```

### Encrypted Docker data

VPS Lite has a single disk, so Docker data lives in a LUKS container file. The password exists only in the password manager; after every reboot Docker waits until you unlock it.

```bash
sudo fallocate -l 40G /srv/docker-data.img
sudo chmod 600 /srv/docker-data.img
sudo cryptsetup luksFormat --type luks2 /srv/docker-data.img
sudo cryptsetup open /srv/docker-data.img dockerdata
sudo mkfs.ext4 -q -L dockerdata /dev/mapper/dockerdata
sudo mkdir -p /var/lib/docker
sudo mount /dev/mapper/dockerdata /var/lib/docker
```

Install Docker and age, then move containerd's data into the encrypted area and make both services wait for it:

```bash
curl -fsSL https://get.docker.com -o /tmp/get-docker.sh && sudo sh /tmp/get-docker.sh
sudo apt install -y age
sudo systemctl stop docker.socket docker.service containerd.service
sudo mkdir -p /var/lib/docker/containerd-root
sudo rm -rf /var/lib/containerd && sudo mkdir -p /var/lib/containerd
sudo mount --bind /var/lib/docker/containerd-root /var/lib/containerd
for svc in containerd docker; do
  sudo mkdir -p /etc/systemd/system/$svc.service.d
  printf '[Unit]\nConditionPathIsMountPoint=/var/lib/docker\nConditionPathIsMountPoint=/var/lib/containerd\n' | sudo tee /etc/systemd/system/$svc.service.d/encrypted-data.conf >/dev/null
done
sudo systemctl daemon-reload
```

Copy `docker/latrovu-unlock` to `/usr/local/sbin/latrovu-unlock` (`chmod 700`), then `sudo latrovu-unlock`.

### Access and logs

```bash
printf 'PasswordAuthentication no\nKbdInteractiveAuthentication no\nPermitRootLogin no\n' | sudo tee /etc/ssh/sshd_config.d/10-latrovu.conf >/dev/null
sudo sshd -t && sudo systemctl reload ssh
sudo apt install -y fail2ban ufw
sudo ufw default deny incoming && sudo ufw default allow outgoing
sudo ufw allow OpenSSH && sudo ufw allow 80/tcp && sudo ufw allow 443
sudo ufw --force enable
sudo mkdir -p /etc/systemd/journald.conf.d
printf '[Journal]\nMaxRetentionSec=14day\nSystemMaxUse=2G\n' | sudo tee /etc/systemd/journald.conf.d/latrovu.conf >/dev/null
sudo systemctl restart systemd-journald
```

Test a second `ssh latrovu` before closing the session.

## Configuration

```bash
sudo mkdir -p /opt/latrovu && sudo chown ubuntu:ubuntu /opt/latrovu
```

From WSL: `scp docker/compose.prod.yml docker/backup.sh docker/*.example latrovu:/opt/latrovu/`. On the server, strip Windows line endings (`sed -i 's/\r$//' …`), create `.env`, `app.env`, `backup.env` from the examples (`chmod 600`, `backup.sh` `chmod 700`).

- `.env`: generate `DB_PASSWORD` and `RABBITMQ_PASSWORD` with `openssl rand -hex 24`, the same values go into the connection strings in `app.env`. Beta access: `SITE_ACCESS=beta`, `BETA_USER`, `BETA_PASSWORD_HASH` (see below).
- `app.env`: `bash scripts/push-app-secrets.sh` in WSL copies AI, storage keys, mail and SMTP from the dev user secrets; set `Storage__Bucket=latrovu-prod-images` by hand. Add `;GSS Encryption Mode=Disable` to the connection string.
- `backup.env`: endpoint, region and keys as for storage, `BACKUP_BUCKET=latrovu-prod-backup`, `BACKUP_AGE_RECIPIENT=age1…`.

```bash
echo "<github token>" | sudo docker login ghcr.io -u blafa --password-stdin
```

## Beta access

Until launch, both domains ask for a username and password (`SITE_ACCESS=beta`) and tell search engines not to index anything. Hash the password without leaving it in the shell history:

```bash
read -rs -p "Beta-Passwort: " BETA_PW; echo
HASH=$(sudo docker run --rm caddy:2.10-alpine caddy hash-password --plaintext "$BETA_PW")
sed -i "s|^BETA_PASSWORD_HASH=.*|BETA_PASSWORD_HASH=${HASH//\$/\$\$}|" .env
unset BETA_PW HASH
```

Paste the `read` line on its own; pasted together with the rest, it reads the next line instead of your input. Go live: `SITE_ACCESS=public` in `.env`, then `sudo docker compose -f compose.prod.yml up -d caddy`.

## Start, update, logs

```bash
cd /opt/latrovu
sudo docker compose -f compose.prod.yml pull
sudo docker compose -f compose.prod.yml up -d
sudo docker compose -f compose.prod.yml ps --format "table {{.Service}}\t{{.Status}}"
sudo journalctl -t latrovu-web -f
```

`TAG=main` in `.env` deploys the latest build; `TAG=<commit sha>` pins a version.

## Automatic deployment

Every push to `main` runs tests, builds the images and then the `deploy` job: it connects as user `deploy`, whose key may only run `/usr/local/sbin/latrovu-deploy <sha>` (forced command, `restrict`). The script sets `TAG=<sha>`, pulls, starts, checks after 40 s that every container runs, and rolls back to the previous tag if not. The database image has its own pinned `DB_TAG` in `.env`, so deployments don't restart Postgres; change it by hand when `docker/postgres` changes. GitHub secrets: `DEPLOY_HOST`, `DEPLOY_SSH_KEY` (dedicated key without passphrase), `DEPLOY_KNOWN_HOSTS` (`ssh-keyscan -t ed25519 <host>`).

Server setup (once):

```bash
sudo adduser --disabled-password --gecos "" deploy
sudo install -m 755 -o root -g root latrovu-deploy /usr/local/sbin/latrovu-deploy
echo 'deploy ALL=(root) NOPASSWD: /usr/local/sbin/latrovu-deploy *' | sudo tee /etc/sudoers.d/latrovu-deploy
sudo chmod 440 /etc/sudoers.d/latrovu-deploy && sudo visudo -cf /etc/sudoers.d/latrovu-deploy
```

`/home/deploy/.ssh/authorized_keys` holds one line: `restrict,command="sudo /usr/local/sbin/latrovu-deploy \"$SSH_ORIGINAL_COMMAND\"" ssh-ed25519 …`. After a reboot, deployments fail with `DockerNotRunning` until `sudo latrovu-unlock`. History: `sudo journalctl -t latrovu-deploy`.

The local dev Intake and production must not poll the same mailbox folder at the same time: Intake deletes every mail after processing.

## After a reboot

```bash
ssh latrovu
sudo latrovu-unlock
```

Docker stays stopped until then, so nothing is written to the unencrypted disk.

## Backups

`backup.sh` dumps the database, encrypts it with age and uploads it with rclone; files older than 30 days are deleted. Nightly at 03:00:

```bash
echo '0 3 * * * root /opt/latrovu/backup.sh' | sudo tee /etc/cron.d/latrovu-backup
sudo journalctl -t latrovu-backup --since today
```

## Restore

Download the file from `latrovu-prod-backup/db/` (Infomaniak Manager or rclone), then in WSL:

```bash
age -d -i ~/latrovu-keys/latrovu-backup.key latrovu-<stamp>.dump.age > latrovu.dump
scp latrovu.dump latrovu:/tmp/
```

On the server:

```bash
sudo docker compose -f compose.prod.yml exec -T db pg_restore -U latrovu -d latrovu --clean --if-exists < /tmp/latrovu.dump
rm /tmp/latrovu.dump
```
