#!/bin/bash
# strain.sh A B Verein Saaten… → je Zeile "Saat Spieler Original Remake" (Belastung, emtool abgleich --alle)
# Namen stehen hier mit Umlaut (KÜHN), im Dump (hok.py) im DOS-Zeichensatz (K]HN) – hok.name() gleicht das an (Runde 19).
REPO=$(cd "$(dirname "$0")/../../.." && pwd)
A=$1; B=$2; C=$3; shift 3
for s in "$@"; do
  dotnet run --no-build -c Release --project "$REPO/src/EishockeyManager.Tools" -- abgleich \
    ~/referenz/original/spielstaende/$A.HOK ~/referenz/original/spielstaende/$B.HOK --seed $s --alle 2>&1 \
    | grep "$C\].Players\[.*\]\.Strain " | sed -E "s/.*Players\[(.*)\]\.Strain +([0-9]+) +([0-9]+).*/$s \1 \2 \3/"
done
