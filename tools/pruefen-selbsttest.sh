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
n = int(os.environ["DOUBLE_PROJEKTE"]); mode = os.environ["DOUBLE_MODUS"]
line = "{}!  - Failed: {:>5}, Passed: {:>5}, Skipped: {:>5}, Total: {:>5}, Duration: 1 s - P{}.dll (net10.0)"
pflicht = os.getenv("EM_REQUIRE_GAMEDATA") == "1" and os.getenv("EM_SKIP_GAMEDATA") == "1"
if not pflicht:
    for i in range(n): print(line.format("Passed", 0, 7, 0, 7, i))
    sys.exit(0)
ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
def trx(path, outcome, msg):
    path.write_text(f'<TestRun xmlns="{ns}"><Results><UnitTestResult outcome="Failed"><Output><ErrorInfo>'
                    f'<Message>{msg}</Message></ErrorInfo></Output></UnitTestResult></Results>'
                    f'<ResultSummary outcome="{outcome}"/></TestRun>')
out = pathlib.Path(args[args.index("--results-directory") + 1]) if "--results-directory" in args else None
if mode == "gruen":
    for i in range(n): print(line.format("Passed", 0, 7, 0, 7, i))
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
    print(line.format("Failed", 1, 6, 0, 7, i))
sys.exit(1)
DOUBLE
chmod +x "$TMP/bin/dotnet"

lauf() {  # $1 = Modus, $2 = erwartet (bestanden|abgelehnt)
  if DOUBLE_MODUS="$1" DOUBLE_PROJEKTE="$PROJEKTE" PATH="$TMP/bin:$PATH" "$ROOT/tools/pruefen.sh" > "$TMP/$1.txt" 2>&1
  then ist=bestanden; else ist=abgelehnt; fi
  if [ "$ist" = "$2" ]; then echo "ok: Pflichtlauf '$1' → $ist"
  else echo "FEHLER: Pflichtlauf '$1' → $ist, erwartet $2"; tail -5 "$TMP/$1.txt"; fehler=1; fi
}

lauf korrekt bestanden               # alle Projekte scheitern nur wegen fehlender Originaldaten
lauf gruen abgelehnt                 # Pflichtlauf ohne Daten grün
lauf absturz abgelehnt               # gar keine Ergebnisse
lauf teilabsturz-konsole abgelehnt   # ein Projekt ohne Ergebniszeile
lauf teilabsturz-trx abgelehnt       # ein Projekt mit ResultSummary „Aborted“
lauf anderer-fehler abgelehnt        # ein Fehlschlag mit anderem Grund
rm -rf "$TMP"
exit $fehler
