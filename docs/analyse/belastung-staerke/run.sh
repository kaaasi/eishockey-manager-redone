#!/bin/bash
# run.sh A B tag Saaten… – nur mit lokal angewendetem trace.patch; Ausgabe $OUT/tr/<tag>_<saat>.txt
REPO=$(cd "$(dirname "$0")/../../.." && pwd); OUT=${OUT:-/tmp/belastung-staerke}; mkdir -p "$OUT/tr"
A=$1; B=$2; tag=$3; shift 3
for s in "$@"; do
  f="$OUT/tr/${tag}_$s.txt"; rm -f "$f"
  EM_TRACE="$f" dotnet run --no-build -c Release --project "$REPO/src/EishockeyManager.Tools" -- abgleich \
    ~/referenz/original/spielstaende/$A.HOK ~/referenz/original/spielstaende/$B.HOK --seed $s > /dev/null 2>&1
done
