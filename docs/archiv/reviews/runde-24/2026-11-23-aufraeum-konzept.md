# Review PR #988 – Aufräumkonzept (#936), Spur `aufraeum-konzept`

Prüfer: Claude (frische Sitzung, keine Mitarbeit am PR). Geprüft: Base `e0b0ea5`, Head `aa0b7e2` (`git rev-parse HEAD` = Head, `git status --porcelain` leer).
Reine Doku: `git diff --stat e0b0ea5...aa0b7e2` → nur `docs/analyse/aufraeumen-v1.md`, 290 Zeilen (bestätigt). Kein Build, keine Tests
(CI startet bei `docs/**` nicht, `build.yml` `paths-ignore`). Zeilenbelege auf einem Worktree von `e0b0ea5`.

## 1. Merge-Empfehlung

**Nein für `aa0b7e2`** – zwei P2 (Dateiüberschneidung in Welle A; Optionen A1–A6 formal unvollständig), beide klein und nur in dieser Datei
zu beheben. Inhaltlich ist das Konzept tragfähig: alle 14 gezogenen Stichproben (§3) bestätigen den Verdacht (K12-Grenze offen, P3-2), Zahlen stimmen bis auf Kleinigkeiten,
#978 und #980 sind eingeplant (R24-D2 = B). Nach Behebung genügt Delta-Prüfung der Leitstelle (`zusammenarbeit.md` §5 Punkt 4).

## 2. Befunde

**P2-1 – Welle A: `aufraeumen-tot` und `aufraeumen-migration` überschneiden sich in einer Testdatei** (§2.2 Zeilen 197/199, §2.3 Zeile 206 „Dateien getrennt“).
`tests/EishockeyManager.Core.Tests/ClubDressedTests.cs` nutzt den toten Weg K3 (`MatchStrength`/`MatchEngine.Play`; §1.5 nennt die Datei selbst)
und enthält zugleich Migrationstests (`SchemaFourSave_IsMigrated`, Zeile 125; Kopf Zeile 8 „Migration 4 → 5“).
Beleg: `grep -n "Migrat\|SchemaVersion" tests/EishockeyManager.Core.Tests/ClubDressedTests.cs` → Zeilen 8, 125, 137, 159;
`grep -rl "\.MatchStrength\|MatchEngine.Play(" tests` → u. a. `ClubDressedTests.cs`. Bei A1 = B streicht `aufraeumen-migration` die Migrationsfälle,
`aufraeumen-tot` ändert dieselbe Datei für K3 → Konflikt im parallelen Welle-A-Lauf. Gleiches ist für weitere der 71 Dateien mit
Migrationsbezug (`grep -rlE 'Migrat|SchemaVersion|schema[0-9]+-synth' tests | wc -l` = 71) möglich, die K3–K7/K17/K18 berühren.
Fix: Dateiliste beider Spuren vorab schneiden (z. B. `ClubDressedTests.cs` nur `aufraeumen-tot`, Migrationsfälle vorher herauslösen) oder die Spuren
nacheinander legen (Migration zuerst); §2.2 um „Dateiüberschneidung: keine, geprüft mit …“ ergänzen.

**P2-2 – Nutzerentscheidungen A1–A6 erfüllen die Optionsregel nicht durchgehend** (§3, Zeilen 243–290; Anforderung: Problem, 3–5 Optionen mit Vor- **und** Nachteilen,
eine begründete Empfehlung). Optionenzahl 3–5 und je eine Empfehlung sind erfüllt. Es fehlt: (a) ein eigener Problemsatz bei A3–A6 (nur Überschrift);
(b) Vor-/Nachteile fehlen bei A1 C/D/E, A2 B–D (nur „verlustreich“ steckt in der Empfehlung), A3 C, A4 B/C, A5 B–D, A6 B/C (reine Beschreibung);
(c) A5 Option A enthält den Platzhalter „Laufzeit +?“ (Zeile 278) – offene Größe in einer Entscheidungsvorlage. Das ist die Vorlage für den Nutzer, daher P2.
Beleg: Sichtprüfung, `sed -n 243,290p docs/analyse/aufraeumen-v1.md`.

**P3-1 – Zählabweichungen in §1** (Verdacht-Niveau, ohne Folge für den Zuschnitt). Nachzählung auf `e0b0ea5` (`find … -name '*.cs'` ohne `obj`/`bin`, `cat | wc -l`):
Core 207 Dateien / 33 566 Z. (Doku 208 / 33 577), UI 102 / 18 554 (103 / 18 564), Tools 49 / 4 174 (50 / 4 185), Formats 23 / 3 605 (24 / 3 610),
Audio 4 / 327 (5 / 338), Desktop 4 / 296 (5 / 312), Viewer 2 / 151 (3 / 166) – überall eine Datei mehr gezählt (vermutlich `.csproj`); `docs/` 346 Dateien (347),
`docs/analyse/belastung-staerke` 12 Skripte (13), Migrationstests-Dateien 71 (70), `Click(ScreenDriver, Rect)` in 7 Testdateien statt 5
(zusätzlich `SaveLoadDialogOriginalTests.cs`, `Nationalteam2Tests.cs`), K19: 16 Treffer in **15** Core-Dateien (Doku 14).
Exakt bestätigt: 58 Migrationsdateien / 2 465 Z., `SchemaVersion = 63` (`SaveGame.cs:48`), 62 Wörterbuch-Einträge, Core-Tests 271 Dateien / 1 699 Fälle,
UI-Tests 158 / 630, `tools/` 11 Dateien, `docs/reviews` 259 Dateien / 29 210 Z. (52 Oktober, 207 November), 164 Verweise auf `docs/analyse`, 20 auf `docs/reviews`,
75 `[Scenario]`, `DefaultSeed = 1993` ×3, `Next(IReadOnlyList<string>, ref int)` ×4 (`HokPatch/Export/Compare/Import`).
Fix: Zahlen angleichen oder „ca.“ setzen.

**P3-2 – K12 „Grenze 2 bzw. 1“ nicht nachvollzogen.** In den genannten Zeilen (`MatchEngine.DebCupFastPath.cs:25`, `MatchEngine.PlayoffFastPath.cs:13`) sind beide Helfer
ähnlich aufgebaut (Paarung des Termins suchen), die behauptete Grenze 2 bzw. 1 konnte ich in den ersten Zeilen nicht ablesen; weiter unten nicht gelesen. Verdacht plausibel, Fundstelle der Grenze nennen.

## 3. Stichprobe der Fundstellen (14, Datei:Zeile auf `e0b0ea5`)

| ID | Prüfung | Ergebnis |
|---|---|---|
| K3 | `MatchEngine.Play` nur von `Playoffs.cs:16` (`KnockoutMatch.Play(int,…)`), diese nur in Tests/Kommentar; `MatchStrength` nur `MatchEngine.cs:26`; `ReferenceRating` in `Tools/HokCompareClasses.cs:133` als Feldname | bestätigt |
| K2 | `PenaltyBoxMinutes` nur in `Squad.Lineup.InMatch.cs:46`, nicht in `CupsNational.Lineup.InMatch.cs` | bestätigt (Lücke real) |
| K4/K5/K7 | `HealInjuries`, `CatchUpTraining` (nur Kommentar `GameState.Integration.cs:158`), `CountPowerPlayGoal`: je 1 Fundstelle in `src/`, 8/3/4 in `tests/` | bestätigt |
| K6 | `Transfer.cs:292` `CatchUp`/`:298` `AdvanceDay` vorhanden | bestätigt (Aufrufer nicht einzeln gezählt) |
| K9 | Kern `NationalPanelLineValue` mit `ManagedLineValues` ↔ UI `NationalWidgets.LineValue` `(ca+ta)/2` ohne Reihenabschlag | bestätigt, Verhaltensabweichung zu Recht ausgeklammert |
| K10 | `Season.cs:345` ↔ `CupsGroup.cs:108`: Rumpf zeilengleich, Kommentare verschieden | bestätigt |
| K11 | `OriginalExport.Squads.cs:9` 0/30/70 ↔ `MatchEngine.Statistics.cs:33` 0/33/83 | bestätigt |
| K15 | `GameState.LoadQuirk.cs:53` mit Kommentar „nicht erreichbar … 218 mod 7 = 1“ | bestätigt |
| K16/K17 | `TacticsParity` 2 Treffer (Definition + Kommentar); `AverageStrength`, `MonthlyCosts`, `RoleName`, `IsMaxed`, `MatchKinds`, `AfterSeasonDay`, `StartListings`: je 1 Treffer in `src/`, 0 in `tests/`; `InMatchFlag` 2/0 | bestätigt |
| K8 | `NationalTeamValues` ↔ Nationalteam-Zweig `StatisticsStrength.cs:81-88`: gleiche Rechnung | bestätigt |
| U1 | `TransferScreen.cs:616` baut `"#,0"`+„ DM“ nach (`ContractsScreen.cs:366` nicht einzeln gelesen) | bestätigt (eine Stelle) |
| U2 | fünf Liga-Namensfunktionen/-Felder an den genannten Stellen | bestätigt |
| U4–U6 | `TrendText`, `LeagueGoals`, `TwoTone`: je 1 Treffer in `src/`, 0 in `tests/` | bestätigt |
| U14 | `aufstellung-reihen`, `gegner-fenster`, `posteingang` nur als `[Scenario]`-Definition, außerhalb `docs/archiv` unerwähnt; kein Test startet Abläufe | bestätigt |

Nicht bestätigt, aber nicht widerlegt: K1 (Unterschiede im Klickablauf nur am Kommentar „0x40D8D“ in beiden Dateien gesehen), K13 (Zufallsquelle: `NewGame.cs` `random.Next`, `Swap.cs:40` `Random16` – stimmt), K19, U10/U13.
Alle als „Verdacht“ ausgewiesenen Befunde sind im Dokument so gekennzeichnet (Kopf Zeilen 8–10); es wurde nichts umgebaut (erwartet offen: bestätigt).

## 4. Weitere Prüfpunkte

- **Zuschnitt, unverändertes Verhalten:** Verhaltensändernde Befunde (K2, K9, K11, K13, Teile K19) sind aus dem Umbau genommen (§2.1 Nr. 6, A4) – schlüssig.
  Wellen A→D (§2.3): Löschen/Zusammenlegen nie zusammen mit Umbenennen – schlüssig. Abnahmeproben §2.4 (Fallliste, `StateFingerprint`, `markerliste.py` ohne `--write`,
  Linkprüfung) sind prüfbar; `StateFingerprint.cs:16` und `markerliste.py` existieren. Überschneidung: nur P2-1; Welle B/C/D sind nach Wellen getrennt.
  Offen: K3 „Feld `ReferenceRating` entfernen“ ist je nach A1 ein Schemaeingriff (§2.2 Zeile 197 sagt „falls A1 = C/D“, bei empfohlenem B bleibt offen, ob das Feld bleibt) – bei Entscheidung A1 festschreiben.
- **#978/#980 (R24-D2 = B):** beide Issues offen, Inhalte passen zur Einplanung: #978 → `aufraeumen-tests-core` (`SpielwertungVorbereitungTests.cs:104` vorhanden, Theory
  mit `League`/`DebCup`, Europapokalfeld fehlt); #980 P3-1/2 → `aufraeumen-doku` (`restliste-v1.md:52` nennt A964 mit Datei 0x54918, A899 steht Zeile 145),
  P3-3 → `aufraeumen-tot` (`MatchEngine.Managed.cs:110ff.`, Kommentar ohne Bedingung +0x0A ≠ 0), P3-4 nur Vermerk. Vollständig.
- **D2–D6:** `docs/analyse` 164 Verweise, `docs/reviews` 20 Verweise, `docs/prozess/2026-10-27-audit-validierung.md` nur aus `docs/archiv/runde-3.md` verwiesen – stimmt.
- **W1–W3:** nicht einzeln geprüft (Skripte vorhanden, 3 + 12 Analyse-Skripte unter `docs/analyse`).

## 5. Ausgeführte Prüfungen, `[APPROX]`

- `git diff --stat e0b0ea5...aa0b7e2`; `git worktree` auf `e0b0ea5`; `find`/`grep`/`wc` wie oben genannt; Issues #978, #980, PR #988 gelesen.
- Kein `tools/pruefen.sh`, kein Build/Test (reine Doku nach Auftrag; kein Testnachweis nach `zusammenarbeit.md` §6 Nr. 4 nötig, Originaldaten nicht berührt).
  Die Regel „Optionen: Problem, 3–5 Optionen mit Vor- und Nachteilen, Empfehlung“ stand nicht in den gelesenen Repo-Dateien; geprüft nach der Formulierung im Auftrag.
- Akzeptierte `[APPROX]`: keine (Doku fügt keine hinzu).
