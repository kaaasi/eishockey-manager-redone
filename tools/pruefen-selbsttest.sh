#!/usr/bin/env bash
# Selbsttest für tools/pruefen.sh mit einem dotnet-Testdouble (kein SDK nötig).
# Prüft, dass das Skript fehlschlägt, wenn der Pflichtlauf ohne Daten abstürzt oder grün ist (Review 2026-10-28, P2).
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMP="$(mktemp -d)"; mkdir -p "$TMP/bin"
fehler=0

lauf() {  # $1 = Verhalten des Pflichtlaufs: absturz | gruen
  cat > "$TMP/bin/dotnet" <<DOUBLE
#!/usr/bin/env bash
case "\$1" in
  build) exit 0 ;;
  --version) echo double; exit 0 ;;
  test)
    if [ "\${EM_SKIP_GAMEDATA:-}" = 1 ] && [ "\${EM_REQUIRE_GAMEDATA:-}" = 1 ]; then
      [ "$1" = absturz ] && { echo "Testhost crashed"; exit 1; }
    fi
    echo "Passed!  - Failed:     0, Passed:     7, Skipped:     2, Total:     9, Duration: 1 s - X.dll (net10.0)"; exit 0 ;;
esac
DOUBLE
  chmod +x "$TMP/bin/dotnet"
  if PATH="$TMP/bin:$PATH" "$ROOT/tools/pruefen.sh" > "$TMP/$1.txt" 2>&1; then
    echo "FEHLER: pruefen.sh meldet bestanden, obwohl der Pflichtlauf '$1' ist"; fehler=1
  else
    echo "ok: Pflichtlauf '$1' wird abgelehnt"
  fi
}

lauf absturz
lauf gruen
rm -rf "$TMP"
exit $fehler
