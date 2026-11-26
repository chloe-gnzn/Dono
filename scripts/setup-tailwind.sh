#!/usr/bin/env bash
# Downloads the Tailwind CSS standalone CLI into src/BloodMatch.Web/tools/.
# Run once after cloning:  ./scripts/setup-tailwind.sh
set -euo pipefail

VERSION="v3.4.17"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST="$ROOT/src/BloodMatch.Web/tools/tailwindcss"

case "$(uname -s)" in
  Linux)  OS="linux" ;;
  Darwin) OS="macos" ;;
  *) echo "Unsupported OS: $(uname -s). Use scripts/setup-tailwind.ps1 on Windows." >&2; exit 1 ;;
esac

case "$(uname -m)" in
  x86_64|amd64)  ARCH="x64" ;;
  arm64|aarch64) ARCH="arm64" ;;
  *) echo "Unsupported CPU: $(uname -m)" >&2; exit 1 ;;
esac

URL="https://github.com/tailwindlabs/tailwindcss/releases/download/$VERSION/tailwindcss-$OS-$ARCH"
mkdir -p "$(dirname "$DEST")"
echo "Downloading Tailwind $VERSION ($OS-$ARCH)..."
curl -fsSL "$URL" -o "$DEST"
chmod +x "$DEST"
echo "Done: $DEST"
"$DEST" --help | head -n 1 || true
