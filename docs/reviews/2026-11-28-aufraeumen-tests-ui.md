# Review PR #1070 `paket/aufraeumen-tests-ui` (Erstreview)

Geprüft: Head `72776ea`, Base `70b15af`. Reviewer: Claude (Sonnet 5.5), frische Sitzung ohne Mitarbeit am PR, vollständiger Checkout,
`git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer. SDK 10.0.402. Unbeaufsichtigter Lauf, Originaldaten vorhanden.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Keine P0–P2-Befunde; zwei P3-Hinweise (unten). Die zwei Punkte „erwartet offen“ sind bestätigt.

## 2. Befunde

Keine P0–P2.

- **P3 (Hinweis) – Testprojekt hängt jetzt an `EishockeyManager.Tools`.**
  [`EishockeyManager.UI.Tests.csproj`](../../tests/EishockeyManager.UI.Tests/EishockeyManager.UI.Tests.csproj) bekommt eine
  `ProjectReference` auf `src/EishockeyManager.Tools` (nötig für `ScenarioRunner.All`). Build mit `-warnaserror` und alle drei Läufe
  grün, also kein Fehler; neue Abhängigkeitsrichtung Tests → Tools nur zur Kenntnis (die Schichtregel in `AGENTS.md` §4 nennt Tests
  nicht).
- **P3 (Hinweis) – Kommentar-Änderungen außerhalb der Spur-Ordner.** `src/EishockeyManager.Core/StatisticsStrength.cs` und
  `src/EishockeyManager.UI/Screens/Statistics/ClubStatisticsScreen.cs` ändern je eine Zeile, nur den Testklassennamen in einem
  `<c>`-Verweis (`git diff 70b15af 72776ea -- src` – keine Codezeile). Liegt außerhalb der Dateigrenze
  `tests/**` + `Scenarios/**`, ist aber die notwendige Folge der Umbenennung. Überschneidung mit #1069 gibt es nicht (s. u.).

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh` (alle drei Läufe, Head `72776ea`, Ergebnis „bestanden“):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4083 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2812 | 0 | 674 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2813 | 1270 | 0 |

**Gegenbeispiele erneut ausgeführt:**

1. **Testliste alt → neu** (`dotnet test tests/EishockeyManager.UI.Tests -c Release --no-build --list-tests`, Base in eigenem
   Worktree gebaut): Base 920 Einträge, Head 999. Klassenabbildung aus den 66 `git diff -M`-Umbenennungen (62 umbenannte Klassen,
   Quell-/Zielklasse je Datei aus `git show`). Alle 920 Base-Tests nach Abbildung im Head vorhanden (0 fehlend); neu sind genau 79
   Einträge `ScenarioSmokeTests.Scenario_RunsAndWritesImages(...)`, sonst nichts. Abbildung vollständig, neu nur Rauchtest (U14, A5 = A).
2. **Rauchtest mit Originaldaten:** `dotnet test … --filter FullyQualifiedName~ScenarioSmokeTests` → 79 von 79 bestanden,
   24 s (nicht ≈ 1 min). 79 = Zahl der `[Scenario]`-Abläufe auf Base und Head. `[GameDataTheory]` (ohne Daten übersprungen, im
   Pflichtlauf rot); eigenes Temp-Verzeichnis je Fall, Aufräumen im `finally`; Hängeschutz kommt vom 20-min-Limit in
   `tools/pruefen.sh`, nichts Eigenes nötig.
3. **Ablaufnamen:** `git grep '\[Scenario("'` auf Base und Head: je 79, `diff` leer. Nur 6 Dateien/Klassen umbenannt
   (`KaderStartRunde14Teil6`→`Aufstellung`, `BundestrainerModus4`→`Bundestrainer`, `FinanzenRest`→`Finanzen`,
   `LiveSeitenRest`→`LiveSeitenAufstiegEuropapokal`, `UiRestVideo`→`PlayoffAufstiegVideo`, `UiRestTeil2`→`Saisonende`).
4. **Bildschirmfotos Base/Head** (`dotnet run --project src/EishockeyManager.Tools -- screens gamedata <ziel> <ablauf>`, je Ablauf
   einzeln): 77 von 79 Abläufen auf beiden Ständen lauffähig, davon 74 Ablauf-Verzeichnisse **bytegleich**. Abweichend nur
   `hok-import`, `speichern`, `speichern-original` (11 PNG). Base gegen Base zweimal wiederholt: dieselben 11 Dateien weichen
   ebenfalls ab → nicht deterministisch, nicht durch den PR verursacht (wie im PR genannt).
5. **Reparierte Abläufe:** Auf Base brechen `pixel-verein-bundestrainer` („Kein Zug des Bundestrainers erreicht.“) und
   `transfer-vertrag-kauf-saisonende` ab; auf Head laufen beide (5 bzw. 2 Bilder), Rauchtest grün.
   - `PixelVerein.cs`: nur Ablaufcode. Neu `game.AppointNationalCoach(game.Managers[0])` am ersten WM-Tag, wenn kein Bundestrainer
     existiert. Das `[APPROX]` ist begründet: Verweis auf `bundestrainer-modus4`/R14NT-03 (Zug läuft ohne Bundestrainer nie an);
     `AppointNationalCoach` ist die vorhandene Kern-API (`CupsNational.GameState.cs:146`), Kern unverändert.
   - `TransferVertragKaufSaisonende.cs`: Schleife schließt Meldungen per `d.CloseMessage()`, wartet Kurzmeldungen ab, dann Escape;
     Obergrenze 40 Durchläufe (kein Endlosrisiko). Nur Testaufbau.
6. **U9 Klickhilfe:** `ScreenDriverClicks.Click(d, r)` = `d.Click(r.X + r.W / 2, r.Y + r.H / 2)`. Die 7 entfernten privaten Kopien
   waren zeichengleich (Diff: je dieselbe Zeile `-`, ersetzt durch `using static`); Build ohne Mehrdeutigkeit, Tests grün.
   `tests/*/Support/`: nichts umbenannt oder verschoben, nur die neue Datei `ScreenDriverClicks.cs` ergänzt (`git diff -M --name-status`).
7. **Reine Umbenennung:** `git diff -M --name-status`: 66 `R` (60 Test-, 6 Ablaufdateien, Ähnlichkeit 90–99 %), 12 `M`, 2 `A`. In
   den Testdateien außerhalb der Klassenzeile nur: Verweise auf umbenannte Klassen (`NationalTeil4Tests.…`, `LivespielUi2Tests.…`
   usw. in Aufrufen und `<c>`-Kommentaren) und die Klickhilfe. Keine Testlogik, kein `Skip`, keine Assert-Änderung gesehen.
8. **Dateiüberschneidung mit `paket/aufraeumen-tests-core` (#1069):** `comm -12` der beiden `git diff --name-only`-Listen →
   nur `docs/arbeitspakete.md` (Statuszeile je eigene Spur, `git merge-tree` beider Köpfe ohne Konflikt).

**Erwartet offen, bestätigt:** (a) Klassen sind nur umbenannt, nicht zusammengelegt (Klassenzahl je Datei unverändert, 62 Klassen
umbenannt, keine verschwunden); (b) Doku-Verweise auf alte Namen bleiben bis `aufraeumen-doku`.

**Akzeptierte `[APPROX]`:** Bundestrainer-Ernennung am ersten WM-Tag in `PixelVerein.CoachRows` (Begründung wie Punkt 5).
**Annahmen, nicht geprüft:** Inhalt der unveränderten Testmethoden nur über Diff-Auszug und grüne Läufe, nicht Zeile für Zeile;
die Gegenprobe (Mutation) für den Rauchtest ist die Base: dort scheitern zwei Abläufe, der Rauchtest hätte sie gefunden.
