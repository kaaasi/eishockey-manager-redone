#!/bin/bash
# writers.sh – Teil 1c (#730): Schreiber der Reihenwahl in em.exe und Reihenmodus/Minuten-Vorgabe der Spielstände.
# Ausgabe nur Adressen und Bytes der Taktikblöcke (keine Originaldaten ins Repo). Braucht ndisasm, gamedata/ und ~/referenz.
REPO=$(cd "$(dirname "$0")/../../.." && pwd); OUT=${OUT:-/tmp/belastung-staerke}; mkdir -p "$OUT"
EXE="$REPO/gamedata/em.exe"; ndisasm -b16 "$EXE" > "$OUT/nd.asm"
func() { # Funktionskopf (disasm.py) vor einer Adresse
  local a=$((16#$1)); python3 "$REPO/tools/reverse-engineering/disasm.py" "$EXE" $(printf "0x%X" $((a > 0x800 ? a - 0x800 : 0))) \
    $(printf "0x%X" $((a + 1))) 2>/dev/null | grep "^;=====" | tail -1 | awk '{print $2}'; }
echo "== Vereinsfelder 0x2E/0x2F/0x30/0x31 (Reihe/Minuten), 0x2C (Modus, Bits) – schreibende Befehle"
grep -E "^\S+\s+\S+\s+(mov|or|and) (byte |word )?\[es:bx\+0x(2c|2e|2f|30|31)\]," "$OUT/nd.asm" | while read off _ ins; do
  echo "Datei 0x$off  $(func $off)  $ins"; done
echo "== Kaderplatz +0x15 (Reihe des Spielers) – schreibende Befehle"
grep -E "^\S+\s+\S+\s+(mov|or|and) (byte |word )?\[es:(bx|si|di)(\+(si|di))?\+0x15\]," "$OUT/nd.asm" | while read off _ ins; do
  echo "Datei 0x$off  $(func $off)  $ins"; done
echo "== Reihenwechsel-Modus A701 (0 = Automatisch … 4 = Minuten-Vorgabe, 5 = ganz manuell) und Minuten-Vorgabe CC43 je Manager"
for f in ${FILES:-FILE02 FILE03 FILE16A FILE17 FILE38 FILE44}; do
  printf "%-8s " $f
  dotnet run --no-build -c Release --project "$REPO/src/EishockeyManager.Tools" -- hok dump ~/referenz/original/spielstaende/$f.HOK A701 CC43 2>/dev/null \
    | awk '/^Block /{b=$2} /^00[0-9a-f]0  /{sub(/^00[0-9a-f]0  /,""); l=substr($0,1,48); gsub(/  +/," ",l); printf "%s[%s] ", b, l} END{print ""}'
done
