# Aufräumen vor v1.0 – Konzept (#936)

Spur `aufraeum-konzept` (Runde 24, R24-D3 = B), Agent Claude Code. **Stand:** `main` `e0b0ea5` (23.11.2026). Nur Konzept:
keine Datei außer dieser wurde verschoben oder geändert. Zeilennummern gelten für `e0b0ea5`. Einordnung: Aufräumen ist der
letzte Schritt vor v1.0 (Nutzer 17.11.2026), nach Runde 25 (Änderungswünsche aus `abweichungen-v1.md`); danach nur noch die
Abschlussprüfung. P3 #978 und #980 sind nach R24-D2 = B Teil des Umbaus (§2.4).

Kennzeichnung: **K** = Befund im Core, **U** = UI, Tools, Formats, Tests-Hilfen, **T** = Testdateien, **D** = Doku, **W** =
Werkzeuge. Jeder Befund unter §1.5 ist ein **Verdacht mit Fundstelle**, am Code gelesen, aber nicht durch Umbau bestätigt; das
Erstreview soll Stichproben ziehen. „Neutral“ heißt: Zusammenlegen ohne Verhaltensänderung möglich.

## 1. Bestandsaufnahme

### 1.1 Umfang je Teil

| Teil | Dateien | Zeilen | davon nach Spur/Runde benannt (`.cs`) |
|---|---:|---:|---:|
| `src/EishockeyManager.Core` | 208 | 33 577 | 58 Migrationen + 4 weitere (§1.2) |
| `src/EishockeyManager.UI` | 103 | 18 564 | 0 |
| `src/EishockeyManager.Tools` | 50 | 4 185 | 5 Abläufe (§1.2) |
| `src/EishockeyManager.Formats` / `Audio` / `Desktop` / `Viewer` | 24 / 5 / 5 / 3 | 3 610 / 338 / 312 / 166 | 0 |
| `tests/EishockeyManager.Core.Tests` (1 699 Fälle) | 277 | 53 229 | 118 von 271 `.cs` |
| `tests/EishockeyManager.UI.Tests` (630 Fälle) | 159 | 19 056 | 50 von 158 `.cs` |
| `tests/EishockeyManager.Formats.Tests` / `Audio.Tests` (113 / 19 Fälle) | 13 / 2 | 2 004 / 311 | 0 |
| `tools/` | 11 | 1 987 | – |
| `docs/` | 347 | 51 583 | davon `reviews/` 259 Dateien, 29 210 Zeilen |

Fälle = Anzahl `[Fact]`/`[Theory]`/`[GameDataFact]`/`[GameDataTheory]` (grep, ohne `InlineData`-Zeilen).

Core-Klassen mit den meisten `partial`-Teilen: `SaveGame` 61, `MatchEngine` 24, `Transfer` 16, `GameState` 13, `Squad` 12,
`CupsNational` 12, `OriginalImport` 8, `SeasonFlow` 7, `Player` 7, `OriginalExport` 6. Die Aufteilung von `MatchEngine`, `Squad`,
`Transfer`, `Player`, `CupsNational` ist **schon nach Thema** (z. B. `Squad.Training.cs`, `Transfer.Contract.cs`,
`MatchEngine.Injuries.cs`) und braucht keinen Umbau. Auffällig groß: `Transfer.Pool.cs` (1 013 Z.), `MatchEngine.Minutes.cs`
(747), `Transfer.cs` (739), `MatchEngine.Managed.cs` (659), `Squad.Lineup.cs` (655), `OriginalImport.cs` (653), `Player.Contract.cs`
(648) – groß, aber jeweils ein Thema (Agent-Prüfung, §1.6); Teilen nur bei Bedarf.

### 1.2 Dateien nach Spur/Runde statt nach Thema

**`src/`**

| Datei | Inhalt | Zielname (Vorschlag) |
|---|---|---|
| `Core/SaveGame.Migration*.cs` (58 Dateien) | je ein Schemaschritt, benannt nach Spur/Teil (`Geld1`…`Geld6`, `KaderTransfer4`, `WettbewerbeSpiel12` …) | nach Entscheidung A1 (§3) |
| `Core/Manager.Einzelpunkte.cs` | Sammelteil der Spur `einzelpunkte` (A847 u. a.) | nach Inhalt aufteilen bzw. `Manager.Season.cs` |
| `Core/OriginalImport.Rest.cs` (390 Z.) / `Core/OriginalExport.Rest.cs` (185 Z.) | „übrige Felder“ | nach Feldgruppen benennen, z. B. `.Finance.cs`, `.Season.cs` |
| `Core/GameState.Integration.cs` | Anschlusspunkt für `Continue()` | **bleibt** (in `AGENTS.md` §4 verbindlich benannt) |
| `Tools/Scenarios/FinanzenRest.cs`, `KaderStartRunde14Teil6.cs`, `UiRestTeil2.cs`, `UiRestVideo.cs`, `BundestrainerModus4.cs` | Bildschirmfoto-Abläufe | nach Bildschirm (`Finanzen`, `Aufstellung`, `Meldungen`, `Nationalteam` …) in bestehende Dateien |

**`tests/`** – Dateien mit Runde, Teil, Fix, Review, Integration, Welle, Abgleich, Rest oder Nummer im Namen, gebündelt nach Familie
(Core / UI):

| Familie | Core | UI | Beispiele | Ziel nach Thema |
|---|---:|---:|---|---|
| `Abgleich*` | 19 | 2 | `AbgleichFixMoneyTests`, `AbgleichDosboxPenaltyTests`, `AbgleichRestWorldOnlyTests` | in die Thementests (Geld, Strafen, Weltkugel …); DOSBox-Belege als `Trait` |
| `KaderStart*` | 11 | 5 | `KaderStartRunde14Teil2bTests`, `KaderStartTeil2cScreenTests` | `NewGameSquadTests`, `StartLeagueTests`, `SetupScreenTests` |
| `KaderTransfer*`, `TransferVertraege*`, `Kaderplatz*` | 8 + 5 + 2 | 3 + 4 | `KaderTransferTeil5PoolTests`, `TransferVertraegeKomplettTeil4ScreenTests` | `TransferPoolTests`, `ContractTests`, `SquadSlotTests`, `TransferScreenTests`, `ContractsScreenTests` |
| `Integration*` | 8 | 9 | `IntegrationRunde7MigrationTests`, `IntegrationRunde6AdLampTests` | Migration → `SaveGameMigrationTests`; Rest in Thementests |
| `WettbewerbeSpiel*`, `WettbewerbeRest*` | 8 + 2 | 2 | `WettbewerbeSpiel12Teil3Tests` | `DebCupTests`, `EuropeanCupTests`, `PlayoffTests`, `WorldCupTests` (bestehen schon) |
| `National*`, `Nationalteam*` | 5 + 6 | 1 + 3 | `NationalTeil3Tests`, `NationalteamRunde14Teil4Tests` | `NationalCoachTests`, `NationalLineupTests` (bestehen schon) |
| `PruefungKarte*` | 5 | 4 | `PruefungKarteRunde17Tests` | nach Thema der Fundstelle (Spielberechnung, Tagesablauf …) |
| `SpielberechnungKomplett1…5` | 5 | – | | `MatchEngine…Tests` nach Teil (Minuten, Strafen, Verletzungen) |
| `KaderRegeln*`, `KaderRest*` | 4 + 4 | – | `KaderRegelnTeil3Tests`, `KaderRestGeldTests` | `SquadRulesTests`, `SalaryTests` |
| `Zufallsstrom*`, `ZusehenRest*`, `FeldspielerSechs*` | 4 + 3 + 2 | 2 | `ZufallsstromTeil2Tests` | `RngStreamTests`, `WatchTests`, `SixthSkaterTests` |
| `NutzerBefunde*` | 1 | 6 | `NutzerBefundeFinanceTests` | Thementests, Befund als `Trait` |
| Einzelne (`OriginalImportReviewR14Tests`, `Tagestakt…ReviewTests`, `TagesablaufRunde15Tests`, `WaveBReview…`, `ReResteTests`, `AutoLineupTeil8Tests`, `TaktikTeil5Tests`, `SaisonwechselSchema22Tests`, `SpielstaerkeManager2Tests`, `FinanceNachpruefungTests`, `EinzelpunkteTests`, `UiRestTeil2Tests`, `UiMeldungenTeil1Tests`, `LivespielUi2Tests`, `MainMenuBefundeP3Tests`, `MenueBefundeTests`, `LineupShortMessagesE2Tests`, `BildschirmeFreipruefungTests`) | 15 | 9 | | je nach Thema |

Gleicher Dateiname in Core- und UI-Tests (verschiedene Namensräume, verwirrend): `FinanceBedienungTests`, `GegnerFensterTests`,
`NationalTeil4Tests`, `Nationalteam2Tests`, `PruefungKarteRunde18Tests`, `WettbewerbeNationalteamTests`.

Sprache der Testnamen: Testklassen sind Code-Bezeichner (Regel: englisch), tragen aber überwiegend deutsche Namen
(`SaisonwechselKontoTests`, `StadiumAusbauTests`). Das ist eine Entscheidung (A3), kein Fehler im Sinn der Regeln.

### 1.3 Migrationen

- `SaveGame.SchemaVersion = 63` (`src/EishockeyManager.Core/SaveGame.cs:48`), 62 Schritte im Wörterbuch `Migrations`
  (`SaveGame.cs:153` ff.), 58 Dateien, 2 465 Zeilen. Ein Schritt ist leer (`[36] = MigrateSaisonPlatzhalter37`,
  `SaveGame.cs:191` → `SaveGame.Migration.Saison.cs:32`). Die Liste der Schritte steht doppelt: als XML-Kommentar
  (`SaveGame.cs:54-151`, je Schritt Spur und Runde) und als Wörterbuch.
- Der Lader bricht mit „Spielstände mit Schema n können nicht mehr geladen werden.“ ab, wenn ein Schritt fehlt
  (`SaveGame.Reader.cs:47-48`). Alte Stände abzuschneiden heißt also nur Einträge entfernen – der Weg ist schon da.
- Abhängigkeit über den Lader hinaus: `AfterLoadPool(game, loadedSchema)` (`SaveGame.Reader.cs:55`,
  `SaveGame.Migration.Pool.cs:37-39`) entscheidet nach dem **geladenen** Schema; beim Abschneiden prüfen.
- Tests: 70 Testdateien berühren Migrationen oder `SchemaVersion` (grep `Migrat|SchemaVersion|schema[0-9]+-synth`), darunter
  `MigrationEchteStaendeTests` mit echten Altständen `tests/EishockeyManager.Core.Tests/Fixtures/schema{19,22,51,52,53}-synthetisch.ems.gz`.
  Die meisten anderen bauen Altstände aus dem aktuellen Stand nach (Feld entfernen, `schema` herabsetzen).
- Kopierte Hilfen in den Migrationen: siehe K14.
- Der Nutzer hat bisher keine Zusage gemacht, Remake-Spielstände vor v1.0 zu laden; Spurbranches sind ausdrücklich keine
  Ausgabestände (R8-A01, `docs/arbeitspakete.md` „Zurückgestellt“). Original-Spielstände (`.hok`) laufen über `OriginalImport`
  und sind vom Schema unabhängig.

### 1.4 Werkzeuge (`tools/`, `.github/`)

Alle 11 Dateien unter `tools/` sind außerhalb von `docs/archiv` und `docs/reviews` verwiesen (1–27 Verweise); kein verwaistes Skript.

| Nr | Fundstelle | Befund | Vorschlag |
|---|---|---|---|
| W1 | `tools/markerbericht.py` (CI: `.github/workflows/build.yml:62`) ↔ `tools/reverse-engineering/markerliste.py` | Beide zählen `[APPROX]` in `src/`; `markerbericht` zusätzlich `[ORIGINAL]` je Datei für die CI-Zusammenfassung, `markerliste` vergibt IDs und klassifiziert. | behalten; prüfen, ob `markerliste.py` die CI-Zählung mit übernehmen kann (A5) |
| W2 | `docs/analyse/abgleich-dosbox/*.py` (3), `docs/analyse/belastung-staerke/*.py|*.sh|*.patch` (13) | Einmalskripte der Analysen liegen in `docs/`, nicht in `tools/`; Verweise nur aus der jeweiligen Analyse. | mit der Analyse ins Archiv (D-Spur) oder nach `tools/analyse/` |
| W3 | `.github/workflows/aufraeumen.yml` | Branch-Aufräumen per Hand (`workflow_dispatch`, Vorgabe „nur anzeigen“); vor v1.0 einmal mit `nur_anzeigen=false` laufen lassen. | Nutzer/Leitstelle, kein Umbau |

### 1.5 Verdacht auf doppelte Logik und tote Pfade

**Core** (Pfade relativ zu `src/EishockeyManager.Core/`)

| Nr | Art | Fundstelle(n) | Befund | neutral | Größe |
|---|---|---|---|---|---|
| K1 | doppelt | `Squad.Lineup.cs:603` `Club.ClickPlayer` und `:463/484/530/562` (`AssignToLineup`/`ClearLineupSlot`/`Undress`/`Dress`) ↔ `CupsNational.Lineup.cs:260` `NationalCoach.ClickPlayer` und `:145/194/204/214` | Klickablauf der Aufstellung zweimal (beide Kommentare: „derselbe Code 0x40D8D“); Unterschiede: Spieler vs. `SlotOf`, `_dressed` über Id vs. Objekt, `TakeOverDressed` nur beim Verein, Reihenfolge von `DisableAutoLineup`. | fraglich | L |
| K2 | doppelt, mögliche Lücke | `Squad.Lineup.InMatch.cs:36/88` ↔ `CupsNational.Lineup.InMatch.cs:23/68` | Klickregel im Spiel kopiert; nur der Verein prüft die Strafbank (`Squad.Lineup.InMatch.cs:46` `MatchEngine.PenaltyBoxMinutes`), der Bundestrainer nicht, obwohl der Kopf „dasselbe 0x40364“ sagt. | nein – erst klären (Verhalten) | M |
| K3 | tot | `MatchEngine.cs:17` `Play` (+ `Poisson`), `Playoffs.cs:14` `KnockoutMatch.Play(int,…)`, `Squad.Strength.cs:15/109` `MatchStrength`; `ReferenceRating` geschrieben in `GameState.Integration.cs:41-44`, `GameState.AddManager.cs:162`, `Squad.Setup.cs:88/133` | Alter Poisson-Ergebnisweg ohne Aufrufer im Spielablauf (nur `SeasonTests`, `SquadTests`, `ClubDressedTests`); `ReferenceRating` nur von diesem Weg gelesen, aber Teil des Spielstands (`Tools/HokCompareClasses.cs:133`). | ja (Feld: Schemaschritt oder liegen lassen) | M |
| K4 | nur Tests, abweichend | `MatchEngine.Injuries.cs:255` `GameState.HealInjuries` ↔ `GameState.DailyRun.cs:135/148/161` (`HealSquad`/`HealManagerSquads`/`HealUnmanaged`) | Alter Heilweg ohne Aufrufer in `src/`, ohne `HealSlot` (A497) und ohne Fall „entlassener Manager“ – Tests prüfen damit nicht den echten Weg. | ja (Tests auf Tageslauf) | S |
| K5 | nur Tests | `Squad.Training.cs:469` `CatchUpTraining` → `:445` `TrainOneDay` → `:455` `TrainManager` ↔ `GameState.DailyRun.cs:117` `ManagerSquadRun` | Nachhol-Training nur für Tests (Kommentar selbst); widerspricht dem Verbot eigener Nachhol-Zähler (`AGENTS.md` §4). | ja | S |
| K6 | nur Tests | `Transfer.cs:292` `CatchUp` + `:298` `AdvanceDay` | Markttag „in einem Zug“, laut Kommentar seit #678 nicht mehr in der Zeitleiste; 19 Testverweise. | ja | S |
| K7 | nur Tests, abweichend | `StatisticsClub.cs:294` `CountPowerPlayGoal` ↔ `OriginalMinutes.GoalScored` / `LiveTeam.PowerPlayCounts` | Parallele Überzahlzählung ohne die Ausnahme 5 gegen 6 („bleibt für Tests und Werkzeuge“). | ja | S |
| K8 | doppelt | `CupsNational.Strength.cs:41` `NationalTeamValues` ↔ `StatisticsStrength.cs:81-88` (Nationalteam-Zweig in `FieldValues`) | Gleiche Rechnung (`TeamValuesOf` + `NationalRatings`, Form aus `DefenseForm`/`AttackForm`). | ja | S |
| K9 | doppelt, Anzeige weicht ab | `CupsNational.Strength.cs:56` `NationalPanelLineValue` ↔ `UI/Screens/Competitions/NationalWidgets.cs:69` `LineValue` | Kern hat den belegten Reihenwert (+4−4·Reihe), die Anzeige rechnet `(K+T)/2` ohne Reihenabschlag; den Kernwert nutzt nur ein Test. | nein – Anzeige ändert sich; in die Abweichungsliste | S |
| K10 | doppelt | `Season.cs:345` `Add(TableRow…)` ↔ `CupsGroup.cs:108` | Wörtlich gleiche Bilanzfortschreibung (S/U/N); die Vergleiche (`Season.cs:322`, `CupsGroup.cs:101`) unterscheiden sich absichtlich. | ja | S |
| K11 | doppelt, Werte verschieden | `OriginalExport.Squads.cs:9` `RoleValue` (0/30/70) ↔ `MatchEngine.Statistics.cs:33` `RoleValue` (0/33/83) | Zwei `[APPROX]`-Werte für dasselbe Feld +0x18. | nein – Export-Bytes bzw. Torwürfe ändern sich; in die Abweichungsliste | S |
| K12 | ähnlich | `MatchEngine.DebCupFastPath.cs:25` `DebCupExtraRoll` ↔ `MatchEngine.PlayoffFastPath.cs:13` `FirstPlayoffPairingOfDate` | Gleiche Prüfung „Paarungen des Termins schon gerechnet < Grenze“, Grenze 2 bzw. 1. | ja (Helfer mit Grenze) | S |
| K13 | doppelt, verschieden | `NewGame.cs:260` `PlaceInStartLeague` + `:380` `SwapStartSlot` ↔ `GameState.AddManager.Swap.cs:27` `DrawStartLeaguePlace` + `SwapPlaceFields` | Zwei Umsetzungen der Platzwahl 0x29C3B (Spielstart / späterer Manager), verschieden in Zufallsquelle (`Next` vs. `Random16`), Europapokalfilter und Tauschumfang. | fraglich – erst gegen em.exe klären | M |
| K14 | kopiert | Rekursive JSON-Durchläufe in 14 Migrationsdateien, davon fast gleiche Objektsammler: `SaveGame.Migration.National.cs:47`, `.Nationalteam.cs:95`, `.TransferOriginalVertrag.cs:36`, `.TransferVertraegeKomplett5.cs:43`, `.PlayoffHalbfinale.cs:59`, inline `.HokPlayOffMarken.cs:42`, `.WettbewerbeSpiel.cs:60`; Array-Bauer `Zeros` (`.StatistikenVerein.cs:35`), `Filled` (`.WettbewerbeKomplett.cs:29`), `ZeroLine` (`.Nationalteam.cs:86`); Raster `EmptyGrid` (`SaveGame.Migration3.cs:44`), `Grid` (`SaveGame.Migration4.cs:48`) | Gleicher Sammler, nur das Prädikat ändert sich. | ja (entfällt bei A1 = A/B weitgehend) | M |
| K15 | toter Zweig | `GameState.LoadQuirk.cs:53` `if (MatchEngine.IsDriftDay(Counter(day)))` | Laut eigenem Kommentar (#703, R701-03) unerreichbar: Import lässt nur Zähler 218 zu, 218 mod 7 = 1. | ja (oder als Absicherung belassen, A4) | S |
| K16 | tot | `MatchEngine.Minutes.cs:190` `TacticsParity` | Weder gesetzt noch gelesen („wird nicht mehr ausgewertet“); Verweis nur noch im Kommentar `:181`. | ja | S |
| K17 | tot | `Squad.Strength.cs:117` `AverageStrength`, `Player.Contract.cs:467` `MonthlyCosts`, `Player.Attributes.cs:164` `RoleName`, `FinanceStadium.cs:121` `IsMaxed`, Konstanten `MatchEngine.Injuries.cs:29` `MatchKinds`, `MatchEngine.Intervention.cs:44` `InMatchFlag`, `Transfer.Contract.cs:23` `AfterSeasonDay`, `Transfer.cs:145` `StartListings` | Kein Verweis in `src/` oder `tests/`. | ja | S |
| K18 | nur Tests | `MatchEngine.Minutes.cs:611` `FastCupResult`, `FinanceBank.cs:230` `ManagerLoanBalance`, `FinanceInbox.cs:113` `RequestLoan`, `GameOptions.cs:95` `ToggleReport`, `Cups.GameState.cs:143` `CupMatches`, `Playoffs.cs:264` `IsFinished`, `Playoffs.cs:307` `Qualified`, `CupsNational.TeamInfo.cs:109` `EnterHeadToHead`, `CupsNational.WorldCupHistory.cs:37` `WorldCupPoints`, `Player.Factory.cs:168/234` `FreeNumber`/`Validate` | Nur Tests rufen sie; teils gewollte Testhaken (wie `ConsumeLoanSalary`, `CaptureRng`), teils API ohne Anschluss. | je Stelle | S |
| K19 | uneinheitlich | 16× `Managers.Any(m => m.ClubIndex == …)` in 14 Core-Dateien, dazu `Transfer.cs:339` `IsManaged`, `MatchEngine.Live.cs:298` `IsManaged` | „Verein hat Manager“ inline mit verschiedener Bedeutung (`Managers` vs. `ActiveManagers`, mit/ohne Bundestrainer). | fraglich (je Stelle) | M |

**UI, Tools, Formats, Test-Hilfen**

| Nr | Art | Fundstelle(n) | Befund | neutral | Größe |
|---|---|---|---|---|---|
| U1 | doppelt | `src/EishockeyManager.UI/Screens/Team/TransferScreen.cs:616`, `Screens/Team/ContractsScreen.cs:366` ↔ `src/EishockeyManager.Core/GameState.cs:159` `FormatMoney` | Geldformat zeichengleich nachgebaut (`ManagerText.cs:36` nutzt schon `FormatMoney`). | ja (11 Aufrufer) | S |
| U2 | doppelt | `UI/Screens/SeasonEndWidgets.cs:72` `LeagueGenitive`, `:80` `LeagueName`, `Screens/Statistics/HistoryScreen.cs:95`, `Screens/Competitions/LeaguePairingsScreen.cs:83`, `Screens/SaveLoad/SaveInfo.cs:21/55` | Liga → „Bundesliga/Zweite Liga/Oberliga“ fünfmal (`StrengthScreen.cs:48` mit „Länderteams“ bleibt eigen). | ja | S |
| U3 | doppelt | `UI/LiveWidgets.cs:130` ↔ `UI/Screens/Competitions/NationalWidgets.cs:32` `FillInclusive` | Fläche em.exe 0x902:0x800 zweimal; Live-Fassung normalisiert mit Min/Abs (bei x1 ≤ x2 gleich). | ja | S |
| U4 | tot + doppelt | `UI/Screens/Team/TeamUi.cs:201` `TrendText` ↔ `Screens/Team/PlayerHintLine.cs:70` `Trends` | `TrendText` ohne Aufrufer, Texte stehen als Array noch einmal. | ja | S |
| U5 | tot | `UI/Screens/Team/PlayerDataWindow.cs:79` `LeagueGoals` | Kein Aufrufer. | ja | S |
| U6 | tot | `UI/Screens/Manager/ManagerText.cs:14` `TwoTone` (+ Puffer `Scratch`, `:8`) | Kein Aufrufer. | ja | S |
| U7 | kopiert (Tests) | `tests/EishockeyManager.UI.Tests/SpielstaerkeManager2UiSupport.cs:10-46` ↔ `tests/EishockeyManager.Core.Tests/SpielstaerkeManager2Tests.cs:425-461` | `ManagedSquads` Zeile für Zeile kopiert. | ja (Link-Datei wie `GameDataFact.cs`) | S |
| U8 | kopiert (Tests) | `tests/EishockeyManager.UI.Tests/KeepSquadHelper.cs:104-119` ↔ `tests/EishockeyManager.Core.Tests/TestSeason.cs:63-77` | `KeepSquads` = `TestSeason.KeepSquad` als Schleife. | ja | S |
| U9 | kopiert (Tests) | `tests/EishockeyManager.UI.Tests/AdvertisingScreenTests.cs:12`, `StadiumScreenTests.cs:12`, `FinanceWerbungScreenTests.cs:14`, `AdvertisingPanelOriginalTests.cs:14`, `NutzerBefundeAdvertisingTests.cs:15` | `Click(ScreenDriver, Rect)` fünfmal; `ScreenDriver` (`src/EishockeyManager.UI/Testing/ScreenDriver.cs:41`) hat nur `Click(int, int)`. | ja | S |
| U10 | Zahlen mehrfach | `src/EishockeyManager.Tools/HokDump.cs:69-74`, `src/EishockeyManager.Formats/OriginalSaveGamePatcher.cs:89-108`, `src/EishockeyManager.Core/OriginalImport.cs:405`, `OriginalExport.Managers.cs:96` | Offsets des Manager-Datensatzes (Konto +0x18F, Eintritt 0x973F, Stadion 0x9771 +0x1D/+0x21) viermal als Zahl. | ja (Konstanten in `OriginalSaveGame`) | M |
| U11 | kopiert | `src/EishockeyManager.Tools/HokCompare.cs:37/76`, `HokExport.cs:21/116`, `HokImport.cs:16/74`, `HokPatch.cs:68` | `DefaultSeed = 1993` dreimal, Argumenthelfer `Next(args, ref i)` viermal. | ja | S |
| U12 | doppelt | `src/EishockeyManager.Tools/SoundExport.cs:12-21` ↔ `src/EishockeyManager.Audio/SoundEffect.cs:36`, `SoundBank.cs:10` | Dateinummern 237–241, 248 neu festgelegt (Tools ohne Referenz auf Audio). | fraglich (neue Projektreferenz) | S |
| U13 | doppelt | `src/EishockeyManager.Tools/HokExport.cs:79-84` ↔ `tests/EishockeyManager.Core.Tests/TestSeason.cs:46-54` | „bis zum nächsten Ligaspieltag `Continue()`“ zweimal. | fraglich (keine Core-API) | S |
| U14 | ungeprüft | `src/EishockeyManager.Tools/Scenarios/*` (75 Abläufe), z. B. `KaderStartRunde14Teil6.cs:13` `aufstellung-reihen`, `WettbewerbeGegnerFenster.cs:15` `gegner-fenster`, `Manager.cs:106` `posteingang` | Kein Test startet einen Ablauf; diese drei werden außerhalb von `docs/archiv` nirgends erwähnt. Veraltet oder nicht, zeigt nur ein Lauf. | Rauchtest oder löschen (A5) | M |

### 1.6 Geprüft, kein Befund

- Automatische Aufstellung: Verein und Bundestrainer nutzen dieselbe Regel `AutoLineupRule.Apply`.
- `CupsNational.Strength.cs` vs. `Squad.Strength.cs`: verschiedene Rechenwege (`NationalLiveTeam`), keine Kopie.
- `MatchEngine.Live`/`LiveStep`/`LiveDriver` vs. `Managed`/`Minutes`/`Original`: Schichten mit gemeinsamem Einstieg
  (`BeginMatch`/`FinishMinuteLoop`/`EndMatch`), kein zweiter Weg; die Pokal-Schnellwege liefern nur Kennzeichen (K12).
- `Rng.cs` (`GameRandom`) vs. `MatchEngine.LRandom.cs` (lrandom des Originals): getrennte Zwecke.
- `OriginalImport` ↔ `OriginalExport`: spiegelbildlich (`ApplySlot` ↔ `WriteSlot`); `Formats/OriginalSave*` regelt nur Blöcke und
  Kopf; die Tools `Hok*.cs` sind Hüllen um `TryImport`/`Export` (Ausnahme U10).
- `Transfer.Pool`/`PoolDistribution`/`Squads`, `Statistics*`, `GameState.AddManager`/`.Swap`: getrennte Aufgaben.
- UI: `HeadToHeadSlot`/`-Text` (`ClubInfoWindow.cs:176/188`, `TeamInfoWindow.cs:244/256`), `Centered`-Varianten
  (`GameOptionsScreen.cs:228`, `CompetitionWidgets.cs:19`, `Widgets.cs:93`), `GoalScenePlayer.Box:222` vs. `NationalWidgets.Box:15`,
  `Widgets.Lamp` vs. `LiveWidgets.Lamp`: verschiedene Originalroutinen bzw. belegte Rundung.
- Scheinbar ungenutzte Typen in UI/Tools werden per Reflexion angemeldet (`[MenuEntry]`, `[Scenario]`).
- Stichprobe „Bildschirm ändert Zustand beim Öffnen“: kein Fund (`Continue()` in `SetupScreen.cs:267`,
  `SeasonEndSummaryScreen.cs:142`, `MainMenuScreen.cs:251` nur auf Nutzeraktion).

### 1.7 Doku (`docs/`)

| Nr | Fundstelle | Befund | Vorschlag |
|---|---|---|---|
| D1 | `docs/reviews/` (259 Dateien, 29 210 Z.; 52 aus Oktober, 207 aus November) | Größter Doku-Block; abgeschlossene Runden stehen schon in `docs/archiv/runde-*.md`. 20 Verweise aus `src/`/`tests/`/`tools/` auf `docs/reviews/…` (17 Testdateien). | nach A2 |
| D2 | `docs/analyse/re-runde-12.md`, `-13`, `-14`, `re-nachtrag.md`, `re-rest.md`, `re-reste-23.md`, `re-vor-dosbox.md`, `re-geld.md`, `re-livespiel.md`, `re-meldungen.md`, `re-saisonwechsel.md`, `abgleich-*.md`, `belastung*.md`, `inventur.md` (1 075 Z.) | Arbeitsstände der RE-Spuren; Ergebnisse stecken als `[ORIGINAL]`-Fundstellen im Code, Klassen in `restliste-v1-klassen.csv`. Aber 164 Verweise aus Code/Tests/Werkzeugen auf `docs/analyse/…`, und `markerliste.py` liest `inventur.md` (Startwerte der IDs). | `inventur.md`, `restliste-v1*`, `abweichungen-v1.md`, `aufraeumen-v1.md` bleiben; Rest nach A2, Verweise mitziehen |
| D3 | `docs/prozess/2026-10-27-audit-validierung.md` | 0 Verweise; Maßnahmen des Audits #42 sind umgesetzt. | ins Archiv |
| D4 | `docs/bestandsaufnahme.md` (Stand 25.10.2026, 1 Verweis), `docs/re-abdeckung.md` (Auftakt Runde 8) | Zwischenstände; `formate/dateiformate.md` bzw. `reverse-engineering.md` sind aktueller. | prüfen, Gültiges übernehmen, Rest ins Archiv |
| D5 | Themendokus `saison.md`, `tagesablauf.md`, `spielberechnung.md`, `transfer.md`, `wettbewerbe.md`, `finanzen-rekonstruktion.md`, `highscore.md` | 133 Verweise aus Code/Tests auf `docs/*.md`; Inhalt nicht gegen den Code geprüft (außerhalb dieses Konzepts). | in der Doku-Spur gegen den Stand nach Runde 25 prüfen |
| D6 | #980 P3-1, P3-2 | `docs/analyse/restliste-v1.md:52` und `docs/arbeitspakete.md` nennen A964 statt A899; Abschnittsverweise A962 (§3 statt §2), A966 (§5 statt §6). | Doku-Spur (§2.4) |

## 2. Zielstruktur und Umbau

### 2.1 Zielregeln

1. **Dateinamen nach Thema**, nie nach Spur, Runde, Teil, Fix, Review, Welle oder Befund. Herkunft (Befund-ID, Runde, Review)
   steht im Kommentar bzw. als xUnit-`Trait("Befund", "A612")`, damit sie suchbar bleibt.
2. **`partial`-Teile je Klasse** als `Klasse.Thema.cs` (englisch). Vorhandene Themenaufteilung bleibt (§1.1). Ausnahmen mit
   festem Namen in `AGENTS.md` (`GameState.Integration.cs`, `GameState.HotSeat.cs`) bleiben.
3. **Tests:** eine Datei je Thema und Schicht (`<Thema>Tests.cs`, UI: `<Bildschirm>ScreenTests.cs`); keine gleichen Dateinamen in
   Core- und UI-Tests. Gemeinsame Hilfen in `tests/*/Support/` als Link-Datei (Muster `GameDataFact.cs`), nicht kopiert (U7–U9).
4. **Migrationen** nach A1 (§3).
5. **Doku:** oben nur gültige Doku; Arbeitsstände und Reviews abgeschlossener Runden im Archiv; Verweise aus Code zeigen auf
   gültige Doku oder auf das Archiv, nie ins Leere.
6. **Verhalten unverändert.** Befunde, die das Verhalten ändern würden (K2, K9, K11, K13, Teile von K19), sind **nicht** Teil des
   Umbaus, sondern gehen in die Abweichungsliste bzw. an den Nutzer (A4).

### 2.2 Umbau-Spuren

Alle Spuren: Branch `paket/aufraeumen-<n>` von aktuellem `main`, **alle drei Prüfläufe** (`tools/pruefen.sh` ohne Option),
keine Verhaltensänderung, kein `markerliste.py --write` außer in A6 vorgesehen. Größen grob nach Dateizahl.

| Spur | Inhalt | Dateien (Grenzen) | Größe | Modell (Vorschlag) |
|---|---|---|---|---|
| `aufraeumen-tot` | Tote und Nur-Test-Pfade im Core entfernen: K3 (ohne Feld `ReferenceRating`, falls A1 = C/D), K4–K7, K15 (nach A4), K16, K17, K18 (nur „API ohne Anschluss“); Tests auf den echten Weg umstellen; #980 P3-3 (Kommentar `MatchEngine.Managed.cs:110`, Bedingung +0x0A ≠ 0) | `src/EishockeyManager.Core/**` außer `SaveGame*`; betroffene Tests | M | Sonnet |
| `aufraeumen-doppelt` | Neutrale Zusammenlegung: K8, K10, K12, K14 (nur wenn A1 = C/D), U1–U6, U10, U11; K1 nur, wenn das Erstreview „neutral“ bestätigt (sonst liegen lassen) | Core (ohne `SaveGame*`), UI, Tools, Formats | M | Opus |
| `aufraeumen-migration` | Migrationen nach A1; Kommentarliste `SaveGame.cs:54-151` kürzen; Migrationstests nachziehen; `AfterLoadPool` prüfen | `src/EishockeyManager.Core/SaveGame*.cs`, Migrationstests, `Fixtures/` | S–M | Sonnet |
| `aufraeumen-tests-core` | §1.2-Tabelle für `tests/EishockeyManager.Core.Tests`: umbenennen und nach Thema bündeln, `Trait` für Herkunft, U7/U8 als Support-Link; **#978** (eigener Fall Europapokalfeld `[bp+0xA] = 8` in `SpielwertungVorbereitungTests.cs:104`, Datei 0x5494C–0x5499E) – einziger neuer Test des Umbaus | `tests/EishockeyManager.Core.Tests/**` | L (≈ 120 Dateien) | Sonnet; ggf. 2 Teile |
| `aufraeumen-tests-ui` | dasselbe für `tests/EishockeyManager.UI.Tests` (≈ 50 Dateien), U9, Abläufe U14 nach A5, Tools-Abläufe aus §1.2 umbenennen | `tests/EishockeyManager.UI.Tests/**`, `src/EishockeyManager.Tools/Scenarios/**` | M | Sonnet |
| `aufraeumen-doku` | D1–D6, W2, W1 nach A5; Verweise aus Code/Tests/Werkzeugen mitziehen (nur Kommentare und Pfade); #980 P3-1/P3-2; P3-4 (PR-Text) braucht keine Dateiänderung, nur Vermerk | `docs/**`, `tools/**`, Kommentare mit Doku-Pfaden in `src/`/`tests/` (nur die Pfadangabe) | M | Haiku oder Sonnet |

### 2.3 Reihenfolge

1. **Welle A (parallel, Dateien getrennt):** `aufraeumen-migration` (nur `SaveGame*`) und `aufraeumen-tot` (Core ohne `SaveGame*`).
   Beide entfernen Code – zuerst, damit später nichts Totes umbenannt wird.
2. **Welle B:** `aufraeumen-doppelt` (braucht den Stand nach A, berührt Core/UI/Tools).
3. **Welle C (parallel):** `aufraeumen-tests-core` und `aufraeumen-tests-ui` – zuletzt im Code, weil A und B Tests ändern; reine
   Umbenennung mit unveränderter Fallzahl lässt sich dann sauber nachweisen.
4. **Welle D:** `aufraeumen-doku` – nach C, weil Verweise auf umbenannte Testdateien und verschobene Doku erst dann feststehen.
5. **Abschlussprüfung** (#936: letzter Schritt vor v1.0) auf dem Stand nach D, mit Gesamtreview.

Gemeinsame Dateien (`AGENTS.md` §2.5): `GameState.cs` (K3 `ReferenceRating`, U1 `FormatMoney`), `GameState.Integration.cs` (K3) und
`Widgets.cs` (Ziel für U2/U3) werden berührt; je Spur einzeln im PR begründen, nie zwei Spuren derselben Welle.

### 2.4 Abnahmeproben je Umbau-Spur

- **Fallzahl und Namen:** `dotnet test --list-tests` vor und nach dem Umbau; die Liste der Testfälle ist bis auf Umbenennungen
  gleich (Abbildung alt → neu im PR). Entfernte Fälle nur mit totem Weg (K3–K7) und je Fall Begründung; neu nur #978.
- **Verhalten:** Zustandsabdruck (`tests/EishockeyManager.UI.Tests/Support/StateFingerprint.cs:16`) einer festen Partie über mindestens 4 Saisonwechsel (passiver und
  aktiver Manager, `AGENTS.md` §5) auf `main` vor der Spur und auf dem Head – gleich. Dazu Bildschirmfotos der Abläufe
  (`emtool screens`) bytegleich, wo die Spur UI berührt.
- **Marker:** `python3 tools/reverse-engineering/markerliste.py` (ohne `--write`) vor/nach: keine neuen oder verlorenen IDs außer
  bei gelöschten toten Pfaden (Liste im PR). Verschobene Zeilen behalten ihre ID nur bei unverändertem Wortlaut (Schlüssel = Datei +
  Wortlaut, `markerliste.py` Kopf) – Kommentare mit `[APPROX]` beim Verschieben **nicht** umformulieren.
- **Doku:** Linkprüfung aller relativen Verweise (Skript im PR, nicht eingecheckt) ohne tote Ziele.

### 2.5 Risiken

| Risiko | Wirkung | Gegenmittel |
|---|---|---|
| Umbenennung verdeckt Verhaltensänderung | Fehler ohne roten Test | Wellen trennen: Löschen/Zusammenlegen (A, B) nie zusammen mit Umbenennen (C); Zustandsabdruck §2.4 |
| Marker-IDs springen (Wortlaut geändert, gleicher Wortlaut in mehreren Dateien) | Restliste und Abweichungsliste verlieren Zuordnung | Probe §2.4; `--write` nur nach A6 |
| Verweise ins Leere (164 auf `docs/analyse`, 133 auf `docs/*.md`, 20 auf `docs/reviews`) | Fundstellen im Code nicht mehr nachvollziehbar | Doku-Spur zuletzt, Linkprüfung; Archiv-Pfade statt Löschen |
| Alte Spielstände laden nicht mehr | Nutzer verliert Stände aus der Testphase | A1 vorher entscheiden; Meldung des Laders gibt es schon |
| Große Test-Spur sprengt einen Lauf | Abbruch, Teil-Umbau | in 2 Teile schneiden (Core-Tests: Familien `Abgleich…Kader…` / Rest); höchstens 3 Fortsetzungen |
| K1/K19 sehen neutral aus, sind es nicht | stille Abweichung | nur nach bestätigter Gleichheit; sonst liegen lassen |
| Lauf mit Originaldaten fehlt | Umbau nur synthetisch geprüft | alle drei Prüfläufe Pflicht (§2.2), blockiert = blockiert |

## 3. Offene Nutzerentscheidungen

**A1 – Migrationen (62 Schritte, Schema 63).**
- A: Alle Remake-Stände vor v1.0 aufgeben: v1.0 beginnt mit Schema 1, alle Migrationen und ihre Tests fallen weg, Lader meldet
  „kann nicht mehr geladen werden“. Am wenigsten Code; Stände der Testphase sind weg.
- B: Nur den aktuellen Stand weiter laden: Schema 63 wird v1.0-Schema, alle Schritte 1–62 entfallen. Wie A, aber Stände ab Runde 23
  bleiben ladbar.
- C: Bündeln: alle Schritte in **eine** Datei je Bereich (z. B. `SaveGame.Migrations.Finance.cs`), Wirkung unverändert, gemeinsame
  Helfer (K14); alle Stände bleiben ladbar.
- D: Ab Schema n (z. B. 51, ältester echter Altstand in den Fixtures) behalten und bündeln, ältere aufgeben.
- E: Nichts ändern.
- **Vorschlag:** B (keine Ausgabestände vor v1.0, R8-A01; spart ≈ 2 400 Zeilen und die meisten der 70 Migrationstests; aktuelle
  Stände des Nutzers bleiben ladbar).

**A2 – Reviews und Arbeitsanalysen (`docs/reviews/` 259 Dateien, `docs/analyse/re-*`).**
- A: Je abgeschlossene Runde in `docs/archiv/reviews/runde-<n>/` verschieben, Inhalt unverändert, Verweise anpassen.
- B: Je Runde zu einer Datei verdichten (Befunde, Entscheidungen, offene Punkte), Einzeldateien löschen (Git-Verlauf bleibt).
- C: Nur Reviews und Analysen ohne Verweis aus Code/Tests archivieren, Rest bleibt.
- D: Nichts ändern.
- **Vorschlag:** A (geringstes Risiko; Fundstellen im Code bleiben über den neuen Pfad erreichbar; verdichten kostet viel und
  verliert Belege).

**A3 – Sprache der Testnamen.**
- A: Englisch wie der übrige Code (`SeasonChangeAccountTests`), zusammen mit der Bündelung.
- B: Deutsch behalten, nur Spur/Runde aus den Namen nehmen (`SaisonwechselKontoTests`).
- C: Englisch nur für neu gebündelte Dateien, bestehende Themennamen bleiben.
- **Vorschlag:** B (Fachbegriffe des Originals sind deutsch; halbiert die Umbenennungen; Regel „Code englisch“ für Tests
  ausdrücklich ausnehmen).

**A4 – Verhalten ändernde Befunde und tote Absicherungen (K2, K9, K11, K13, K15).**
- A: Alle in die Abweichungsliste (Runde 25), nicht im Umbau.
- B: K2/K9 als Fehler beheben (eigene Fix-Spur vor dem Umbau), K11/K13 in die Liste, K15 löschen.
- C: Nur dokumentieren (Kommentar `[APPROX]`/Hinweis), nichts ändern.
- **Vorschlag:** A, K15 als Absicherung belassen (Kommentar genügt); da Runde 25 die Abweichungsliste umsetzt, kommen K2/K9/K11/K13
  dort rechtzeitig vor dem Umbau.

**A5 – Werkzeuge und Bildschirmfoto-Abläufe (W1, W2, U14).**
- A: Abläufe per Rauchtest prüfen (jeder Ablauf einmal in der UI-Test-Spur, Laufzeit +?), kaputte löschen; Analyse-Skripte nach
  `tools/analyse/`; `markerbericht.py` bleibt.
- B: Abläufe ohne Erwähnung außerhalb des Archivs löschen, Rest bleibt; Analyse-Skripte ins Archiv mit ihrer Analyse.
- C: `markerbericht.py` in `markerliste.py` aufgehen lassen (eine Zählung), sonst wie B.
- D: Nichts ändern.
- **Vorschlag:** B (Abläufe sind Werkzeuge zum Vermessen, ein Rauchtest über 75 Abläufe kostet CI-Zeit; die CI-Zählung ist billig
  und unabhängig).

**A6 – `markerliste.py --write` im Umbau.**
- A: Einmal am Ende der Doku-Spur (`--write`), Restliste und Abweichungsliste auf die neuen Dateinamen.
- B: Nach jeder Umbau-Spur.
- C: Gar nicht vor v1.0 (Restliste zeigt alte Dateinamen).
- **Vorschlag:** A (eine Neuerzeugung, Probe ohne `--write` in jeder Spur, §2.4).
