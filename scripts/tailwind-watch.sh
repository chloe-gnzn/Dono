#!/usr/bin/env bash
# Rebuilds wwwroot/css/app.css whenever a .razor/.cs/.css file changes.
# Run in a second terminal next to `dotnet watch`.
set -euo pipefail
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/src/BloodMatch.Web"
exec ./tools/tailwindcss -i Styles/app.css -o wwwroot/css/app.css --watch
