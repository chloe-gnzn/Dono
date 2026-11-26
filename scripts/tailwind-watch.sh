#!/usr/bin/env bash
set -euo pipefail
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/src/BloodMatch.Web"
exec ./tools/tailwindcss -i Styles/app.css -o wwwroot/css/app.css --watch
