# VectorFlow — VPS Deployment Guide

**Domain:** vflow.bkokumu.com  
**Stack:** Docker + docker compose + Nginx (host) + Let's Encrypt

---

## Prerequisites on your VPS

```bash
# Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # log out and back in after this

# Nginx + Certbot
sudo apt install -y nginx certbot python3-certbot-nginx
```

---

## 1. Get your code onto the server

```bash
git clone https://github.com/KOKUMUbooker/vector-flow.git
cd vector-flow
```

---

## 2. Create your .env file

```bash
cp .env.example .env
nano .env          # fill in all values
```

Generate a strong JWT key:

```bash
node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
# or use your existing tools/GenerateKey
```

---

## 3. Point your domain at the VPS

In your DNS provider, add an A record:

```
vflow.bkokumu.com  →  <VPS IP>
```

Wait for propagation (usually 1–5 minutes with modern providers).

---

## 4. Install the Nginx config

```bash
sudo cp nginx.conf /etc/nginx/sites-available/vflow.bkokumu.com
sudo ln -s /etc/nginx/sites-available/vflow.bkokumu.com \
           /etc/nginx/sites-enabled/vflow.bkokumu.com

# Remove the default site if it's there
sudo rm -f /etc/nginx/sites-enabled/default

sudo nginx -t          # must say: syntax is ok
sudo systemctl reload nginx
```

---

## 5. Get the TLS certificate

```bash
sudo certbot --nginx -d vflow.bkokumu.com
```

Certbot rewrites the ssl_certificate lines in your nginx config automatically.  
Auto-renewal is set up by the Certbot package — verify it with:

```bash
sudo systemctl status certbot.timer
```

---

## 6. Build and start the containers

```bash
# Build the Docker image (takes 2–4 minutes first time)
docker compose build

# Start everything (app + postgres) in the background
docker compose up -d

# Watch logs
docker compose logs -f app
```

## 7. Verify everything works

```bash
# App is healthy
curl -I https://vflow.bkokumu.com

# Container status
docker compose ps
```

---

## Ongoing operations

### Deploy a new version

```bash
git pull
docker compose build
docker compose up -d   # rolling restart; compose recreates only changed containers
```

### View logs

```bash
docker compose logs -f app      # application logs
docker compose logs -f db       # postgres logs
sudo tail -f /var/log/nginx/vflow.access.log
```

### Connect to the database directly

```bash
docker compose exec db psql -U vectorflow -d vectorflow
```

### Stop everything

```bash
docker compose down              # stops containers, keeps volumes
docker compose down -v           # ⚠ also deletes the postgres volume (all data)
```

---

## File layout on the server

```
~/vector-flow/
├── Dockerfile
├── docker-compose.yml
├── .env
├── nginx.conf            ← copied to /etc/nginx/sites-available/
└── ...source files...
```

---
