#!/usr/bin/env bash
# Einheitliche Prüfung vor jedem PR – für alle Agenten gleich (siehe AGENTS.md, Abschnitt „Tests“).
#
#   tools/pruefen.sh            Build + drei Testläufe (Integration, Test-Infrastruktur), Tabelle für den PR
#   tools/pruefen.sh --schnell  Build + ein Lauf: mit Originaldaten, falls vorhanden, sonst ohne (normaler PR)
#
# Läufe:
#   1. mit Originaldaten   EM_REQUIRE_GAMEDATA=1 – scheitert, wenn gamedata/ fehlt (nur wenn vorhanden)
#   2. ohne Originaldaten  EM_SKIP_GAMEDATA=1   – wie die CI: Originaldaten-Tests sichtbar übersprungen
#   3. Pflichtlauf ohne Daten (beide Variablen) – MUSS scheitern; sonst laufen Originaldaten-Tests still leer
# Exit-Code 0 nur, wenn Build ohne Warnungen, Lauf 1 (falls Daten da) und Lauf 2 grün sind und Lauf 3 scheitert.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
SLN=EishockeyManager.slnx
QUICK=0; [ "${1:-}" = "--schnell" ] && QUICK=1
LOG="$(mktemp -d)"
STATUS=0

# Summiert die Zeilen „Passed!/Failed! - Failed: x, Passed: y, Skipped: z“ aller Testprojekte.
summe() {
  grep -E "^(Passed|Failed)! " "$1" | sed -E 's/.*Failed: *([0-9]+), Passed: *([0-9]+), Skipped: *([0-9]+).*/\1 \2 \3/' |
    awk '{f+=$1; p+=$2; s+=$3} END {printf "%d %d %d", p, f, s}'
}

echo "== Build (Release, Warnungen = Fehler)"
if ! dotnet build "$SLN" -c Release -warnaserror -nologo -v q > "$LOG/build.txt" 2>&1; then
  tail -30 "$LOG/build.txt"; echo "Build fehlgeschlagen."; exit 1
fi

HAVE_DATA=0; [ -d gamedata/kb ] && HAVE_DATA=1
ROWS=()

if [ $HAVE_DATA = 1 ]; then
  echo "== Tests mit Originaldaten"
  EM_REQUIRE_GAMEDATA=1 dotnet test "$SLN" -c Release --no-build -nologo -v q > "$LOG/mit.txt" 2>&1
  rc=$?; read -r p f s <<< "$(summe "$LOG/mit.txt")"
  ROWS+=("| Mit Originaldaten (\`EM_REQUIRE_GAMEDATA=1\`) | $p | $f | $s |")
  [ $rc -ne 0 ] && STATUS=1
  # Übersprungene Tests im Lauf mit Daten sind erklärungsbedürftig (bekannte offene Befunde).
  [ "$s" != "0" ] && echo "Hinweis: $s Tests trotz Originaldaten übersprungen – im PR begründen."
else
  echo "Keine Originaldaten (gamedata/kb fehlt) – Lauf mit Originaldaten entfällt. Im PR angeben!"
  ROWS+=("| Mit Originaldaten | – | – | – (keine Daten vorhanden) |")
fi

if [ $QUICK = 0 ] || [ $HAVE_DATA = 0 ]; then
  echo "== Tests ohne Originaldaten"
  EM_SKIP_GAMEDATA=1 dotnet test "$SLN" -c Release --no-build -nologo -v q > "$LOG/ohne.txt" 2>&1
  rc=$?; read -r p f s <<< "$(summe "$LOG/ohne.txt")"
  ROWS+=("| Ohne Originaldaten (\`EM_SKIP_GAMEDATA=1\`) | $p | $f | $s |")
  [ $rc -ne 0 ] && STATUS=1
fi

if [ $QUICK = 0 ]; then
  echo "== Pflichtlauf ohne Daten (muss scheitern)"
  EM_SKIP_GAMEDATA=1 EM_REQUIRE_GAMEDATA=1 dotnet test "$SLN" -c Release --no-build -nologo -v q > "$LOG/pflicht.txt" 2>&1
  rc=$?; read -r p f s <<< "$(summe "$LOG/pflicht.txt")"
  if [ $rc -eq 0 ]; then
    ROWS+=("| Pflichtlauf ohne Daten | $p | $f | $s – **FEHLER: hätte scheitern müssen** |"); STATUS=1
  else
    ROWS+=("| Pflichtlauf ohne Daten (scheitert wie gewollt) | $p | $f | $s |")
  fi
fi

echo
echo "| Lauf | bestanden | fehlgeschlagen | übersprungen |"
echo "|---|---:|---:|---:|"
printf '%s\n' "${ROWS[@]}"
echo
echo "Commit: $(git rev-parse HEAD)$(git diff --quiet && git diff --cached --quiet || echo ' (mit nicht eingecheckten Änderungen!)')"
echo "SDK: $(dotnet --version)"
[ $STATUS -ne 0 ] && echo "Ergebnis: NICHT bestanden (Protokolle in $LOG)" || echo "Ergebnis: bestanden"
exit $STATUS
