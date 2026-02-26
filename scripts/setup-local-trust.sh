#!/usr/bin/env bash
set -euo pipefail

if [[ "${OSTYPE:-}" != darwin* ]]; then
  echo "This helper currently supports macOS only."
  echo "For other OSes, trust Caddy local CA from: /data/caddy/pki/authorities/local/root.crt"
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is not installed or not available in PATH."
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
server_dir="$(cd "${script_dir}/.." && pwd)"
cert_path="/tmp/caddy-local-root.crt"
container_name="caddy-prox"

echo "Ensuring Caddy container is running..."
(
  cd "${server_dir}"
  docker compose up -d caddy >/dev/null
)

if ! docker ps --format '{{.Names}}' | grep -qx "${container_name}"; then
  echo "Could not find running container '${container_name}'."
  echo "Start the stack first with: docker compose up -d --build"
  exit 1
fi

echo "Exporting Caddy local root certificate..."
docker cp "${container_name}:/data/caddy/pki/authorities/local/root.crt" "${cert_path}"

echo "Installing certificate into macOS System keychain (sudo required)..."
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain "${cert_path}"

echo "Certificate installed successfully."
echo "Restart your browser and open:"
echo "  https://api.localhost/health"
echo "  https://auth.localhost"
