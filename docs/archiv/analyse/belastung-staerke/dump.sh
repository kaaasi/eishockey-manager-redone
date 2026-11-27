#!/bin/bash
# Dump der Spielstandblöcke nach $OUT/<datei>.blk (keine Originaldaten im Repo).
set -e
REPO=$(cd "$(dirname "$0")/../../.." && pwd); OUT=${OUT:-/tmp/belastung-staerke}; mkdir -p "$OUT"
for f in "$@"; do
  dotnet run --no-build -c Release --project "$REPO/src/EishockeyManager.Tools" -- hok dump \
    ~/referenz/original/spielstaende/$f.HOK 5DC3 far:9657 far:8253 > "$OUT/$f.blk"
done
