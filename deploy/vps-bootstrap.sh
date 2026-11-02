#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${1:-/opt/wathiq}"

echo "==> Creating $APP_DIR"
sudo mkdir -p "$APP_DIR"
sudo chown "$USER":"$USER" "$APP_DIR"

if [[ ! -f "$APP_DIR/docker-compose.yml" ]]; then
  echo "Copy docker-compose.yml and .env.example into $APP_DIR first."
  exit 1
fi

cd "$APP_DIR"

if [[ ! -f .env ]]; then
  cp .env.example .env
  echo "Created .env — edit passwords before continuing."
  exit 1
fi

docker compose pull || true
docker compose up -d --build
docker compose ps

echo "Done. App should be on port defined by WEB_PORT (default 8080)."
