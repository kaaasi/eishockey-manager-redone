# Review: Integration Runde 13 – `kader-start` (PR #600)

- **Reviewer:** Claude Code (Claude Opus 5.5), geplanter Lauf, frische Sitzung ohne Implementierungskontext
- **PR:** #600 · **Base:** `cc8b0a0` (`main`) · **Head:** `d92078ceae521e99781f1f1f17b5f9d3a1cdc57c`
- **Checkout:** Klon, `git rev-parse HEAD` = Head-SHA, `git status --porcelain` leer; gebaut und ausgeführt (kein reines Codelesen)


## 1. Merge-Empfehlung

**Ja für `d92078c`.** Alle drei Läufe von `tools/pruefen.sh` am Head bestanden und decken sich mit der PR-Tabelle. Jedes
Gegenbeispiel ist am Head grün. Am Base ist es rot (Base-Lauf) oder wird es durch eine gezielte Mutation am Head (Tabelle in
Abschnitt 3). Die Integration aus `paket/kader-start-4-main` stimmt inhaltlich mit dem Stapel #568 → #578 → #589 → #599
überein. Die Dateigrenzen halten Runde 13 und L1–L15 ein. Die geänderten Saaten in fremden Testdateien folgen nach Stichprobe
der Originalregel (Tausch bei Start-Liga Bundesliga immer, Teamwerte bleiben am Platz). Ein Fehler ist dadurch nicht verdeckt.
Eine Begründung ist allerdings falsch (R13KI-01). Alle Befunde sind P3 und blockieren den Merge nicht.

## 2. Befunde

### R13KI-01 (P3) – Saat 3 → 5 in `IntegrationRunde9Tests`/`SaisonwechselProbenTests`: Begründung „Spiel ohne Gleichstand“ stimmt nicht

[`IntegrationRunde9Tests.cs:78`](https://github.com/kaaasi/eishockey-manager-redone/blob/d92078ceae521e99781f1f1f17b5f9d3a1cdc57c/tests/EishockeyManager.Core.Tests/IntegrationRunde9Tests.cs#L78),
[`SaisonwechselProbenTests.cs:71`](https://github.com/kaaasi/eishockey-manager-redone/blob/d92078ceae521e99781f1f1f17b5f9d3a1cdc57c/tests/EishockeyManager.Core.Tests/SaisonwechselProbenTests.cs#L71).
Diese Tests vergleichen den schnellen Weg eines Play-off- bzw. Relegationsspiels mit dem schnellen Weg eines DEB-Pokalspiels. Beide
Spiele haben dieselben Vereine und `new Random(7)`.

**Gegenbeispiel (am Head, Saat zurück auf 3, Diagnose im Test):** Vereine 0 und 1. Play-off und Relegation enden 3:2, DEB-Pokal
5:2. Kein Gleichstand, keine Verlängerung. Folge-Zufall 1659008096 bzw. 300338940. Die Reihenfolge der Aufrufe und
`ShowDebCup` ändern daran nichts. Mit Saat 5 enden beide 0:1, weil die Teamwerte dort zufällig zum selben Ergebnis führen.
Die Wege unterscheiden sich also nicht über einen Gleichstand. Vermutlich wirkt `rules.Playoff` in `FastCupResultWithTie`
([`MatchEngine.Live.cs:98`](https://github.com/kaaasi/eishockey-manager-redone/blob/d92078ceae521e99781f1f1f17b5f9d3a1cdc57c/src/EishockeyManager.Core/MatchEngine.Live.cs#L98)). Die Gleichheit mit dem DEB-Pokal hält nur
für bestimmte Teamwerte. Die Prüfung aus Runde 9 ist also schon vorher brüchig, die neue Saat verdeckt das nur.
Ursache ist nicht kader-start, und der Code des PR ist nicht betroffen.
**Vorschlag (Runde 14, Testdatei der Eigner-Spur):** Den schnellen Weg mit Play-off-Kennung als Erwartung nehmen
(`FastCupResultWithTie` mit `rules.Playoff`), nicht den DEB-Pokal. Den Kommentar zur Saat korrigieren.

### R13KI-02 (P3) – Saaten als Vorbedingung, aber ohne Prüfung der Vorbedingung

Beispiel: [`NationalCoachMode4Tests.cs:60`](https://github.com/kaaasi/eishockey-manager-redone/blob/d92078ceae521e99781f1f1f17b5f9d3a1cdc57c/tests/EishockeyManager.Core.Tests/NationalCoachMode4Tests.cs#L60). **Gegenprobe:**
Mit Saat 3 am Head fällt Verein 2 aus den ersten fünf, weil er als Europapokal-Teilnehmer immer tauscht. Die Erwartung
`top.IndexOf(1) < top.IndexOf(2) ? 1 : 0` ergibt dann 0, der Bundestrainer ist Manager 1. Die Meldung lautet „Expected 0,
Actual 1“, nicht „Vorbedingung verletzt“. Ebenso
`AutoLineupTeil8Tests.Migration_31To32_KeepsHandMadeLineup` mit Saat 3: Der von Hand aufgestellte Spieler ist nach `Continue`
verletzt (`InjuredGames` = 7), deshalb Erwartet 60 / Tatsächlich 61. Beides folgt aus der Originalregel bzw. aus dem
Zufallsverlauf, ist also kein Fehler. Künftige Änderungen am Starttausch werden aber wieder als scheinbare Regelfehler
auftauchen. **Vorschlag:** Die Vorbedingung in den betroffenen Tests ausdrücklich prüfen (beide Manager unter den ersten
fünf, Spieler verfügbar, „Verein 2, stark“). Zuständig sind die Eigner-Spuren oder `pruefung-karte`.

### R13KI-03 (P3) – Start-Liga vor dem ersten Manager nicht einstellbar, `GameSetup.Step` meldet trotzdem Erfolg

[`GameSetup.cs:191`](https://github.com/kaaasi/eishockey-manager-redone/blob/d92078ceae521e99781f1f1f17b5f9d3a1cdc57c/src/EishockeyManager.Core/GameSetup.cs#L191). Ohne Manager ergibt
`Step(SetupSlider.StartLeague, +1, null)` `true`. Der Setter von `GameSetup.StartLeague` läuft aber über eine leere Liste, der
Wert bleibt Bundesliga. Das widerspricht dem `<returns>` („true, wenn sich ein Wert geändert hat“). Im Original ist
DGROUP 0x11A ein globaler Wert (L1), unabhängig von den Managern. Gegenüber dem Base ist das keine Verschlechterung: Dort
brauchte der Regler einen gewählten Manager. **Vorschlag:** Die Start-Liga in `GameSetup` selbst führen und beim Anlegen eines
Managers übernehmen (heute in `SetupScreen` schon so). Mindestens ohne Manager `false` liefern. Testidee:
`new GameSetup().Step(StartLeague, +1, null)`, danach einen Manager anlegen → 2. Liga.

### R13KI-04 (P3, nur Codelesen) – Alte Spielstände: Nr. = Kaderplatz + 1 erst nach dem nächsten Ereignis

[`Squad.AutoLineup.cs:184`](https://github.com/kaaasi/eishockey-manager-redone/blob/d92078ceae521e99781f1f1f17b5f9d3a1cdc57c/src/EishockeyManager.Core/Squad.AutoLineup.cs#L184). `RenumberPlayers` läuft bei Managervereinen
über `ApplyAutoLineup`, also im Hauptmenü und am Zuganfang. Bei Computervereinen läuft es nur bei Kauf oder Abgang. Ein
geladener Stand aus der Zeit der freien Rückennummern zeigt bei Computervereinen bis dahin die alte Nr. Die Spielfolge ist
nicht betroffen, denn sie nimmt `Players`. Es gibt keine Migration und keinen Test mit altem Stand. **Vorschlag:** Beim Laden
einmal umnummerieren, oder in der Restliste als bekannt führen.

## 3. Ausgeführte Prüfungen

**Umgebung:** SDK 10.0.402 (Release `claude-werkzeuge`), Originaldaten aus `kaaasi/eishockey-manager-referenz`
(`git ls-remote … HEAD` = `36aeca9`, `original/`), Spielstände `original/spielstaende/`.

**`tools/pruefen.sh` (alle drei Läufe) am Head `d92078c`:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2257 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1896 | 0 | 325 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1897 | 360 | 0 |

Ergebnis: bestanden. Die Zahlen sind gleich wie im PR (dort an `9ab735d`; `9ab735d..d92078c` ändert nur `docs/arbeitspakete.md`).

**Gegenbeispiele – rot am Base bzw. durch Mutation, grün am Head:**

Base-Lauf: Die neuen Testdateien wurden in einen Base-Checkout kopiert. Nur dort, wo eine API fehlt, wurde minimal angepasst:
`setup.StartLeague` → je Manager, `RenumberPlayers`/`RemovePlayer` → Schleife bzw. `Players.Remove` + `Lineup.Remove`,
`DatabaseLeague` als Hilfsfunktion. Mutation: Eine gezielte Änderung am Head, dann die neuen Testklassen ausgeführt.

| ID | Base | Mutation am Head → rot | Head |
|---|---|---|---|
| R12K-01 (#530) | grün (Testlücke, Code war da) | Aufruf `RenewAgedPoolPlayer` in `StartPoolSeason` entfernt → `FreePoolSlotAtAge60…` (2) rot | grün |
| R12K-02 (#531) | – (Kommentar `Transfer.Pool.cs`) | – | gelesen, stimmig |
| R12K-03 (#532) | `KeepSquad_DefaultExtendsOnlyOfferedContracts` rot | Vorgabe `extendExpired = true` → rot | grün |
| R12K-04 (#533) | – (Doku `docs/transfer.md`) | – | gelesen, stimmig |
| U15-a (0x29C3B–0x29CF2) | `KaderStartStartLigaTests` 13 von 15 rot; grün nur `FixedClubAlreadyInStartLeague_Kept` (Gegenprobe) | `PlaceInStartLeague` → `null` → alle Tausch-Tests rot (auch FILE27 mit Originaldaten, Mehrsaisonprobe passiv/aktiv) | grün |
| U15-a gegen FILE27/FILE28 | – | – | FILE27/FILE28 nicht selbst gedumpt (Beleg aus `re-runde-13.md`, U15-a); Test `OriginalDatabase_KoelnSecondLeague_LikeFile27` mit Originaldatenbank grün |
| U15-b (0x2890E) | API fehlt (Übersetzungsfehler) | Sperrbedingung abgeschaltet → `Duration_OtherStartLeague…` (2), `StartLeague_OneSeason…` rot | grün (auch UI `OneSeason_LocksStartLeague_ForAllManagers`) |
| L1 | API fehlt | – | `StartLeague_SharedByAllManagers` grün; Code: Setter setzt alle Manager, `SetupScreen` übernimmt den Wert für neue Manager |
| R-K5 (0x43A17 → 0x35EDD, 0x28030) | API fehlt | Tausche in `StartOrder` abgeschaltet → `StartOrder_PagesHoldTheirLeague…` (3) rot; `PlaceInStartLeague` → `null` → `StartSwap_ExchangesSlots…` (3) rot | grün (auch UI `ClubPicker_ShowsStartOrder…`, Seite 3 Zelle 8 = DB 44 wie `_0136`) |
| A091 | 4 von 6 Kaderplatz-Tests rot (`AfterSale_GivesFile26`, `SaleThenPurchase…`, `LiveSquad…`, Mehrsaisonprobe passiv **und** aktiv) | Kauf vorne statt hinten (`Join` → `Insert(0, …)`) → `AfterPurchase_GivesFile07`, `SaleThenPurchase…`, `LiveSquad…`, Mehrsaisonprobe aktiv rot | grün |
| A166 (0x426BC) | `Dressed_WithoutAutoLineupRun_UsesReserveRound` rot; `AutoLineup_File05Values_GiveFile05Lineup` und `…AfterPurchase_GivesFile07` schon am Base grün (Rolle/Nr. ergibt dort dieselbe Folge) | alter Rückfall K + T + F in `AutoDressed` → `Dressed_WithoutAutoLineupRun…` rot | grün |

**Testdaten gegen die Originalstände:** Die Werte K/T/F der Plätze 1–25 in `KaderStartRunde13Teil4Tests.File05` und DAFFNER
(67/88/51) stimmen mit `emtool hok dump FILE05.HOK` überein. Reihen, Torhüter und Nicht-Eingesetzte der Tests stimmen mit
FILE05, FILE07 (DAFFNER Platz 26, Reihe 3S; nicht eingesetzt 3, 19, 20, 23) und FILE26 (HOCK verkauft, Plätze rücken auf;
nicht eingesetzt 3, 19) überein.

**Mehrsaisonprobe:** `KaderStartStartLigaTests.MultiSeason_FourChanges_AfterStartSwap` (U15-a) und
`KaderStartRunde13Teil4Tests.MultiSeason_FourChanges_SlotOrderKept` (A091) laufen je über 4 Wechsel, passiv und aktiv. Sie sind
am Head grün und rot unter der jeweiligen Mutation bzw. am Base. Ausnahme: A091 passiv ist mit der Kauf-Mutation grün, denn
ohne Kauf hat sie keine Wirkung. Am Base ist der Test rot.

**Fingerabdrücke und Saaten in fremden Testdateien (L13), Stichprobe mit Gegenprobe:** Saat am Head zurückgesetzt, Ursache
der roten Tests per Diagnose bestimmt:
- `NationalCoachMode4Tests.Mode4_TwoManagersInTopFive` (17 → 3): Verein 2 tauscht als Europapokal-Teilnehmer immer und fällt
  aus den ersten fünf → Originalregel (R13KI-02).
- `NationalCoachMode4Tests.Mode4_NoManagerInTopFive` (4 → 3): grün. Die Saat-Änderung war für den integrierten Stand nicht
  mehr nötig, schadet aber nicht.
- `AutoLineupTeil8Tests.Migration_31To32…` (4 → 3): Der Spieler ist nach einem Tag verletzt → Zufallsverlauf (R13KI-02).
- `IntegrationRunde9Tests`/`SaisonwechselProbenTests` (5 → 3): Grund ist ein Unterschied der beiden schnellen Wege, kein
  Gleichstand → R13KI-01. Das ist eine Schwäche des Tests aus Runde 9 und kein Fehler von kader-start.
- Neue Fingerabdrücke (`RngTwinProbeTests`, `SpielberechnungAnschlussTests`, `GameModeTests`, `JugendTeamsTests`,
  `DebCupDrawFlowTests`): Jede Stelle hat einen Kommentar mit der Ursache (Starttausch, Startmischung, Kaderplatzfolge). Die
  Ursachen sind am Code nachvollziehbar. Nicht einzeln nachgerechnet, nur Codelesen.
- `ClubAssignmentTests`, `GameModeTests`, `SeasonFlowTests` u. a.: je Aufbau eine Zeile `StartLeague = DatabaseLeague(club)`
  wie L13. `SaisonSatzfolgeSaisonendeTests` setzt die Liga des ersten Vereins für alle Manager, passend zu L1.

**`ScreenDriver.cs`:** Die Vereinswahl läuft über `SetupScreen.CellOf` (gemischte Seite). Dazu kommt die Start-Liga passend zum
Verein. Beides ist minimal und entspricht L13. **`SeasonFlow.LeagueSlots.cs`:** Geändert sind nur `InitializeLeagueSlots`
(Parameter `places`, Rückfall auf die Indexfolge bei ungewöhnlicher Belegung) und der Kopfkommentar, wie L3.

**Integration statt PR-Branches:** Der Stapel `8afd750..ede32ea` (#568 → #599) und `cc8b0a0..9ab735d` haben in allen 18
`src`-Dateien identische Änderungszeilen. Unterschiede gibt es nur in `GameModeTests`, `RngTwinProbeTests`,
`SpielberechnungAnschlussTests` (Fingerabdrücke neu auf dem integrierten Stand), in `StatistikenPraemienTests` (Saat 3 → 4 nach
S-K6-c) und in `SaisonSatzfolgeSaisonendeTests` (eine Zeile, Datei kam mit `saison`). `paket/kader-start-4-main`
(`6c3cb53`) und `9ab735d` unterscheiden sich nur um 4 Zeilen in `docs/arbeitspakete.md`.

**Dateigrenzen (Runde 13, L1–L15):** `NewGame.cs`, `Transfer*.cs`, `Player*.cs` (nur Kaderreihenfolge, L4), `Squad*.cs`,
`StadiumStatistics.cs`, `MatchEngine.Managed.cs` (nur Kaderreihenfolge), `SetupScreen.cs`, `GameSetup.cs` (L11),
`SeasonFlow.LeagueSlots.cs` (L3), `ScreenDriver.cs` (L13), `docs/transfer.md`, eigene Tests, fremde Tests nach L13 und R12K-03.
Die Testhelfer `TestSeason.cs`, `TestSquadKeeper.cs` und `KeepSquadHelper.cs` sind nach Teil 1 R12K-03 geändert. Nicht
geändert: `tests/**/Support/`, `GameState.Integration.cs`, `MainMenuScreen.cs`, `inventur.md`, `re-abdeckung.md`.
`docs/arbeitspakete.md`: eigener Statuspunkt und Abschnitt Integration (Leitstelle).

**Architektur:** `SetupScreen` leitet die Mischung beim Aufbau nur aus der Saat ab, ohne Zufallszug und ohne Spielzustand.
`RenumberPlayers` in `ApplyAutoLineup` wird nur aus Spielkern-Ereignissen gerufen (Hauptmenü-Ereignis, Zuganfang, Spieltag)
sowie beim Verlassen der Transferliste (Datei 0x47BB5).

**Erwartet offen – bestätigt:** `[APPROX]` Zufallsquelle der Startmischung (`NewGame.StartOrder`, Saat statt srand(time)) und
des Starttauschs (`GameState.Random`); Leihrückkehr hinten an (`Player.Contract.cs`, A125 nicht gegen das Original geprüft);
Gesperrte in der Aufstellung und Handmodus ohne Neuaufstellung (L7, `StadiumStatistics.cs`, `Squad.Lineup.cs`); U15-c bleibt
`[APPROX]` (L2); `SortedPlayers` nur für Oberflächenlisten (`[APPROX]` in `Squad.Strength.cs`); Kommentar A112 in
`SeasonFlow.Oberliga.cs` (`OutsidePlaces`) veraltet, Spur `saison`, Runde 14.

**Akzeptierte `[APPROX]`:** die oben genannten; außerdem Europapokal-Zeile 1 zu Spielbeginn nur Verein 2 und die Zufallswahl
nach Start-Stärke ohne festen Verein (Remake-Erweiterung, kein Platztausch).

**Annahmen (nicht geprüft):** Wie Teamwerte und Zufallsplatz im Original genau verteilt sind, ist nur so weit gegen
FILE27/FILE28 geprüft, wie es `re-runde-13.md` belegt (ein Lauf, keine Statistik). Fingerabdrücke wurden nicht unabhängig
neu berechnet.
