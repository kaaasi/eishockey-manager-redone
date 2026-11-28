# Review PR #1069 – `aufraeumen-tests-core`

Base `70b15af`, Head `d850f07` (Checkout verifiziert: `git rev-parse HEAD` = Head, `git status --porcelain` leer).
Reviewer: Claude Sonnet 5.5, geplanter Lauf, kein Bezug zur Implementierung.

## 1. Merge-Empfehlung

**Ja** für Head `d850f07`. Reine Umbenennung belegt, Testliste vollständig abgebildet, #978-Fall rot→grün, alle drei Läufe
bestanden. Nur P3-Befunde.

## 2. Befunde

- **P3-1 – Statuszeile ungenau** ([docs/arbeitspakete.md](https://github.com/kaaasi/eishockey-manager-redone/blob/d850f07/docs/arbeitspakete.md), Zeile 142).
  „145 Dateien … umbenannt und in Themenordner gelegt“: tatsächlich 138 Renames (`git diff -M --name-status 70b15af d850f07`
  → 138 `R`, 12 `M`); 145 ist die Zahl der Dateien, die unverändert im Wurzelordner blieben
  (`ls tests/EishockeyManager.Core.Tests/*.cs | wc -l` → 145). Nur Doku, kein Merge-Hindernis.
- **P3-2 – PR-Vorgabe „Dateiüberschneidung: keine“** stimmt nicht wörtlich: Die Probe liefert `docs/arbeitspakete.md`
  (je eigene Spur, Statuszeile; erwartet, kein Konflikt zu erwarten, da verschiedene Abschnitte).
- **P3-3 – Herkunft ohne `Trait`**: Der Plan nennt „`Trait("Befund", …)` bzw. Kommentar“. Der Diff enthält keine neuen
  `Trait`-Zeilen; Herkunftskommentare blieben erhalten. Zulässig („bzw.“), nur festgehalten.

Keine P0–P2.

## 3. Ausgeführte Prüfungen

**Reine Umbenennung.** `git diff -M --stat`: 150 Dateien, +217/−179; 138 Renames (R95–R100), 12 Änderungen. Alle Zeilen
des Test-Diffs ohne Klassen-/Namensraumzeilen, `Trait`, Kommentarzeilen und Leerzeilen (Filter per `grep -v`) ergeben nur:
(a) Verweise auf umbenannte Klassen im Code (`KaderTransferTeil4Tests.TwoManagers()` → `MarketCycleTests.TwoManagers()`,
2×; `AbgleichOriginalPaareTests.Pairs()` → `OriginalPairsTests.Pairs()`), (b) csproj-Kommentar (`Tests AbgleichOriginal*` →
`Compare/*`), (c) den neuen #978-Test. Keine Assertion, kein Testaufbau, keine Daten geändert; `Trait`-Zeilen: keine
Änderung. Namensraum bleibt `EishockeyManager.Core.Tests`.

**Testliste** (`dotnet test tests/EishockeyManager.Core.Tests --list-tests`, Base per `git worktree`, Head im Checkout):
Base 2868, Head 2869. Abbildung alt→neu über Klassenzuordnung je Rename-Paar (140 Klassen, Python-Skript, Klassenanzahl je
Datei gleich): jede Base-Zeile hat genau eine gleiche Head-Zeile (inkl. Theory-Fälle), Rest-Differenz leer. Zusätzlich im
Head genau 1: `SpielwertungVorbereitungTests.AwayShortHanded_LowerIndexHomeManager_EuropeanCup_CountsInEuropeField` (#978).

**#978-Fall.** Test grün (1/1). Rückschaltprobe (temporäre Mutation, danach `git checkout -q src`, Baum sauber):
in `Player.CountGame` (`src/EishockeyManager.Core/Player.Competitions.cs`) `EuropeGames++;` → `{ }`:
`Failed … Expected: Tuple (1, 0, 1) / Actual: Tuple (1, 0, 0)` → rot. Der Test deckt den Forfeit-Pfad `CountForfeitGames`
(`MatchGate.cs`) für Wettbewerb Europapokal ab (Lineup zählt im Europafeld, Belastung geklammert, Gegner unverändert). Der
Bezug auf die Originalstelle (Datei 0x5494C–0x5499E; `[bp+0xA] = 8` = Europapokal) steht im Code
(`Player.Competitions.cs` Zeile 7, `MatchGate.cs` Zeile 158/175); gegen die Originalbinärdatei nicht neu disassembliert
(Annahme, im Test nicht als Neubefund behauptet).

**Namen.** Dateinamen im Head: kein „Spur/Runde/Teil/Fix/Review/Welle/Nachpr“. Treffer nur auf englisch-fachliche
„Round“ (`CupsDebDrawNextRoundTests`, `OriginalExportRoundTripTests`) – Fachbegriff, kein Verstoß. Je Datei eine Klasse
gleichen Namens (Ausnahme `SeasonTests.cs` mit `SeasonCalendarTests`/`LeagueTableTests`, unverändert zur Base). Keine
gleichen Dateinamen Core/UI (Head Core gegen Head UI und gegen `origin/paket/aufraeumen-tests-ui`: `comm -12` leer). Deutsche
Fachnamen (z. B. `KaderVertragsloseTests`, `HokImportWerteTests`) bleiben nach A3 = B erlaubt.

**Support.** `git diff --name-only 70b15af d850f07 | grep -c Support` → 0. `tests/*/Support/` unverändert; Link-Datei
`GameDataFact.cs` in `Core.Tests.csproj` unberührt (nur der Kommentar der `HokCompare*`-Zeile geändert; Link-Pfad
`Abgleich\%(Filename)` unverändert).

**Dateiüberschneidung mit #1070:** `comm -12 <(git diff --name-only origin/main...origin/paket/aufraeumen-tests-core | sort)
<(git diff --name-only origin/main...origin/paket/aufraeumen-tests-ui | sort)` → nur `docs/arbeitspakete.md`.

**Erwartet offen (bestätigt):** Klassen nicht zusammengelegt (Ordner nach Thema); alte Namen in Doku und UI-Kommentaren
bleiben bis `aufraeumen-doku` (nicht Gegenstand dieses PR).

**`tools/pruefen.sh`** (alle drei Läufe, SDK 10.0.402, Commit `d850f07`, Warnungen = Fehler, Build Release grün, damit beide
Testprojekte gebaut):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4005 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2813 | 0 | 673 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2814 | 1191 | 0 |

Ergebnis: bestanden. Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`, Lesezugriff, nichts geschrieben).

**`[APPROX]`:** keine neuen, keine akzeptiert nötig. Annahme (nicht belegt): Originaltreue der #978-Erwartung beruht auf
den vorhandenen `[ORIGINAL]`-Kommentaren, nicht auf neuer Disassembly.
