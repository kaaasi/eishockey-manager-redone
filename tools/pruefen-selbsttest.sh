#!/usr/bin/env bash
# Selbsttest für tools/pruefen.sh mit einem dotnet-Testdouble (kein SDK nötig).
# Der Pflichtlauf ohne Daten darf nur dann als „scheitert wie gewollt“ gelten, wenn ALLE Testprojekte Ergebnisse
# liefern und jeder Fehlschlag die fehlenden Originaldaten meldet (Reviews 2026-10-28: PROZ-02).
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TMP="$(mktemp -d)"; mkdir -p "$TMP/bin"
PROJEKTE=$(ls "$ROOT"/tests/*/*.csproj | wc -l)
fehler=0

cat > "$TMP/bin/dotnet" <<'DOUBLE'
#!/usr/bin/env python3
import os, pathlib, sys
args = sys.argv[1:]
if args[0] in ("build", "--version"):
    print("double"); sys.exit(0)
if args[0] == "test" and os.getenv("DOUBLE_ARGS"):
    with open(os.environ["DOUBLE_ARGS"], "a") as f: f.write(" ".join(args) + "\n")
n = int(os.environ["DOUBLE_PROJEKTE"]); mode = os.environ["DOUBLE_MODUS"]
def block(outcome, failed, passed, skipped, total, i):
    # Ergebnisblock des Konsolenloggers (verbosity=normal), je Testprojekt
    print(f"Test Run {outcome}.\nTotal tests: {total}\n     Passed: {passed}")
    if failed: print(f"     Failed: {failed}")
    if skipped: print(f"    Skipped: {skipped}")
    print(" Total time: 1 Seconds")
pflicht = os.getenv("EM_REQUIRE_GAMEDATA") == "1" and os.getenv("EM_SKIP_GAMEDATA") == "1"
if not pflicht:
    # Zahlen je Lauf verschieden, damit der Selbsttest die Summen der Tabelle prüft: mit Daten 7/0/0, ohne Daten 5/0/2 je Projekt.
    skip = os.getenv("EM_SKIP_GAMEDATA") == "1"
    for i in range(n):
        if mode == "haenger" and i == n - 1:
            print("Test Run Aborted."); continue   # letztes Projekt ohne Ergebnisblock (Hänger)
        block("Successful", 0, 5 if skip else 7, 2 if skip else 0, 7, i)
    sys.exit(1 if mode == "haenger" else 0)
ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
def trx(path, outcome, msg):
    path.write_text(f'<TestRun xmlns="{ns}"><Results><UnitTestResult outcome="Failed"><Output><ErrorInfo>'
                    f'<Message>{msg}</Message></ErrorInfo></Output></UnitTestResult></Results>'
                    f'<ResultSummary outcome="{outcome}"/></TestRun>')
out = pathlib.Path(args[args.index("--results-directory") + 1]) if "--results-directory" in args else None
if mode == "gruen":
    for i in range(n): block("Successful", 0, 7, 0, 7, i)
    sys.exit(0)
if mode == "absturz":
    print("Testhost crashed"); sys.exit(1)
out.mkdir(parents=True, exist_ok=True)
for i in range(n):
    if mode == "teilabsturz-konsole" and i == n - 1:
        print("Testhost crashed"); continue
    aborted = mode == "teilabsturz-trx" and i == n - 1
    trx(out / f"p{i}.trx", "Aborted" if aborted else "Failed",
        "Testhost crashed" if aborted else "Originaldaten fehlen (Ordner gamedata), aber EM_REQUIRE_GAMEDATA=1 …")
    if mode == "anderer-fehler" and i == 0:
        trx(out / f"p{i}.trx", "Failed", "Assert.Equal() Failure")
    block("Failed", 1, 6, 0, 7, i)
sys.exit(1)
DOUBLE
chmod +x "$TMP/bin/dotnet"

lauf() {  # $1 = Modus, $2 = erwartet (bestanden|abgelehnt)
  if DOUBLE_MODUS="$1" DOUBLE_PROJEKTE="$PROJEKTE" PATH="$TMP/bin:$PATH" "$ROOT/tools/pruefen.sh" > "$TMP/$1.txt" 2>&1
  then ist=bestanden; else ist=abgelehnt; fi
  if [ "$ist" = "$2" ]; then echo "ok: Pflichtlauf '$1' → $ist"
  else echo "FEHLER: Pflichtlauf '$1' → $ist, erwartet $2"; tail -5 "$TMP/$1.txt"; fehler=1; fi
}

# Tabellenzeile des Modus $1 muss das Muster $3 (feste Zeichenkette, grep -F) enthalten; $2 = Beschreibung.
zeile() {
  if grep -qF -e "$3" "$TMP/$1.txt"; then echo "ok: Tabelle '$1': $2"
  else echo "FEHLER: Tabelle '$1': $2 – erwartet: $3"; grep '^| ' "$TMP/$1.txt"; fehler=1; fi
}

lauf korrekt bestanden               # alle Projekte scheitern nur wegen fehlender Originaldaten
lauf gruen abgelehnt                 # Pflichtlauf ohne Daten grün
lauf absturz abgelehnt               # gar keine Ergebnisse
lauf teilabsturz-konsole abgelehnt   # ein Projekt ohne Ergebniszeile
lauf haenger abgelehnt               # ein Projekt ohne Ergebnisblock in den normalen Läufen
lauf teilabsturz-trx abgelehnt       # ein Projekt mit ResultSummary „Aborted“
lauf anderer-fehler abgelehnt        # ein Fehlschlag mit anderem Grund
# Summen der Tabelle (Review #950 P3-1): bestanden/fehlgeschlagen/übersprungen je Lauf = Summe der Blöcke aller Projekte.
N=$PROJEKTE
[ -d "$ROOT/gamedata/kb" ] && zeile korrekt "Lauf mit Daten" "(\`EM_REQUIRE_GAMEDATA=1\`) | $((7*N)) | 0 | 0 |"
zeile korrekt "Lauf ohne Daten mit Übersprungenen" "(\`EM_SKIP_GAMEDATA=1\`) | $((5*N)) | 0 | $((2*N)) |"
zeile korrekt "Pflichtlauf" "(scheitert wie gewollt) | $((6*N)) | $N | 0 |"
zeile gruen "Pflichtlauf grün: Summen und Fehlervermerk" "| $((7*N)) | 0 | 0 – **FEHLER"
# Tabellenzeile bei Hänger (Review #950 P3-2): unvollständiger Lauf trägt den Vermerk und nur die Summe der fertigen Projekte.
zeile haenger "Hänger gekennzeichnet" "$((5*(N-1))) | 0 | $((2*(N-1))) – **FEHLER: unvollständig**"
# Hängeschutz (#935): jeder dotnet-test-Aufruf trägt Zeitlimit, kein Dump und Fortschrittslogger; EM_HANG_TIMEOUT überschreibt.
for lim in "" 30s; do
  rm -f "$TMP/args.txt"
  # env -u: ohne Vorgabe darf auch eine Variable der aufrufenden Umgebung nicht durchschlagen (Review #950 P3-3)
  env -u EM_HANG_TIMEOUT ${lim:+EM_HANG_TIMEOUT=$lim} DOUBLE_ARGS="$TMP/args.txt" DOUBLE_MODUS=korrekt DOUBLE_PROJEKTE="$PROJEKTE" \
    PATH="$TMP/bin:$PATH" "$ROOT/tools/pruefen.sh" > /dev/null 2>&1
  erwartet="${lim:-20m}"
  aufrufe=$(grep -c . "$TMP/args.txt")
  gut=$(grep -c -e "--blame-hang-timeout $erwartet --blame-hang-dump-type none --logger console;verbosity=normal" "$TMP/args.txt")
  if [ "$aufrufe" -ge 2 ] && [ "$aufrufe" -eq "$gut" ]; then echo "ok: Hängeschutz ($erwartet) in $aufrufe von $aufrufe Testaufrufen"
  else echo "FEHLER: Hängeschutz ($erwartet): $gut von $aufrufe Testaufrufen"; cat "$TMP/args.txt"; fehler=1; fi
done
rm -rf "$TMP"
exit $fehler
