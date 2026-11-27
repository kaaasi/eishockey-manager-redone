# Review PR #910 – Spur `pruefung-karte` (Erstreview)

Geprüft: Base `69c914308be0d36c5f1e8c8b149e199e593d4958`, Head `855e44bc9f36f5c0952739ef25a2ee52e4a907f1` (Checkout, `git rev-parse HEAD`
= Head, `git status --porcelain` leer). Reviewer: Claude Sonnet 5.5, geplanter Lauf, eigene Sitzung (nicht an #910 beteiligt).
Originaldateien aus `kaaasi/eishockey-manager-referenz` (`original/`, Zugriff per `git ls-remote` bestätigt), SDK 10.0.402.

## 1. Merge-Empfehlung

**Ja für `855e44b`.** Alle drei Läufe von `tools/pruefen.sh` grün; der Spielcode ändert sich nur in den P3-Fixes (Bestenliste am
Spielende, `atGameEnd`) und in Kommentaren. Markerliste und Funktionskarte sind reproduzierbar (zweiter Lauf ohne Änderung),
Zählung 626 = `grep`. Von den Gegenproben zu #882/#902 werden zehn von elf Mutationen rot; eine (Kopie der Stärken in
`CopyPreparation`) bleibt grün – P3-1, kein Merge-Hindernis.

## 2. Befunde

**P3-1 – `BeginMatch_ManagedClub_TakesEffortAndStrengthsOfPreparation` schützt die Übertragung der Stärken nicht.**
[MatchEngine.Live.cs:214–215](https://github.com/kaaasi/eishockey-manager-redone/blob/855e44bc9f36f5c0952739ef25a2ee52e4a907f1/src/EishockeyManager.Core/MatchEngine.Live.cs#L214)
(`to.TacticsStrength = …; to.TacticsOpponentStrength = …`) und der Test
[TeamwertAufstellungTests.cs:73](https://github.com/kaaasi/eishockey-manager-redone/blob/855e44bc9f36f5c0952739ef25a2ee52e4a907f1/tests/EishockeyManager.Core.Tests/TeamwertAufstellungTests.cs#L73).
Probe (Kopie des Heads, `sed -i '214,215d' src/EishockeyManager.Core/MatchEngine.Live.cs`, danach
`EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter FullyQualifiedName~BeginMatch_ManagedClub_TakesEffort`):
**Passed 1/1** – der Test bleibt grün. Grund: `LiveTeamOf` setzt `TacticsStrength` schon beim Aufbau (`MatchEngine.Live.cs:363`),
die Sollwerte des Tests werden aus denselben Feldern der Mannschaft berechnet (`PreparedEffort(team.TacticsStrength, …)`), und
`> 0` unterscheidet Kopie und Aufbauwert nicht. Der Kommentar des Tests („die Stärken der Zeile der Automatik stehen danach an
der Mannschaft“) ist damit nur für den Einsatz belegt. Die Mutationen „Einsatz nicht kopieren“ (Zeile 212) und „Kopie ganz
entfernt“ (Zeilen 204–205) werden rot. Vorschlag: Sollwert der Stärken aus einer Vorbereitung auf der Kopie ohne Squad
(`PrepareMatch` mit `LiveTeamOf(..., managerMatch: false)`) berechnen und gegen `team.TacticsStrength` vergleichen.

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh` (alle drei Läufe, Head `855e44b`, Ergebnis: bestanden):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3583 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2510 | 0 | 585 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2511 | 1072 | 0 |

Zahlen stimmen mit der PR-Beschreibung (3583/0/0) überein.

**Gegenbeispiele aus #882/#902 (je Mutation in temporärer Kopie des Heads, nur der betroffene Test; unmutiert jeweils grün):**

| Befund | Mutation | Ergebnis |
|---|---|---|
| #909 P3-2 `CupsDebDrawNextRoundTests` | `CupsDeb.cs:113` `game.Random` → `new GameRandom(1)` | rot (0/1) |
| #907 P3-2 `AddManager_MarksNewManagerClubLive` (0x29C1C) | Zeile `SetWatchMode` in `GameState.AddManager.cs:144` entfernt | rot |
| #907 P3-3 `LiveDriver_RunsPairingOfWatchedComputerClub` (Bit 7) | `WatchedLive` → `IsManaged` in `MatchEngine.LiveDriver.cs:54` | rot |
| #907 P3-4 `WatchSymbol_HasFixedRasterPerMode` | `36 + 11 · Modus` → `37 + 11 · Modus` (`TeamInfoWindow.cs:120`) | rot |
| #881 P3-2 `Export_WritesMatchLines_RoundTrip` | `r[0x30] = 0` / `r[0x2E] = 0` im Export | rot (3/3 bzw. 2/3) |
| #903 P3-3 `SwapStartSlot_ExchangesLoaderValuesAndStrength…` | Tausch der `TeamValues` bzw. der Stärke entfernt | beide rot |
| #906 P3-2 `BeginMatch_ManagedClub_TakesEffort…` | `to.Effort`-Kopie entfernt / `CopyPreparation`-Aufrufe entfernt | rot / rot |
| dasselbe | Kopie von `TacticsStrength`/`TacticsOpponentStrength` entfernt | **grün** (P3-1) |

**Fremde Testdateien (#909 P3-1):** Die zehn auf die Base-Fassung zurückgesetzten Dateien (`AbgleichRestPreparationTests`,
`EuropeanCupTests`, `KaderRestSaisonendeTests`, `KaderRestTransferTests`, `KaderTransferTeil4Tests`, `KaderTransferTeil7Tests`,
`PlayoffSemifinalTests`, `TransferTests`, `WettbewerbeSpiel1Tests`, `WorldCupTests`) sind Byte für Byte die Fassung vor #905
(`git diff 348016f HEAD -- <datei>` leer, 10 von 10); sie laufen im Lauf mit Originaldaten auf dem Head grün (3583/0/0).
`AbgleichOriginalPaareTests` (ZIMMERMANN 90–125, gemessen 97–122) und `IntegrationRunde9Tests` sind bewusst geändert, nicht
zurückgesetzt. Die Leitstelle hat den Bereich 90–125 im PR bestätigt („enger erst mit bitgenauem Zufallsmodus“); angenommen.

**Besonders prüfen:**

1. *Markerliste.* `python3 tools/reverse-engineering/markerliste.py` → Rückgabe 0, „unklassifiziert 0“, zweiter Lauf und `--write`
   ohne Diff; Block = committeter Stand. Zählung 217 Dateien / 626 Markerzeilen = `grep -rc '\[APPROX\]' src/ | awk …` (217 626).
   Klassen im Block: V 67, D 21, E 288, K 185, X 65 (= 626); mit den 71 Funktionszeilen der Karte (E 38, K 9, X 24) ergeben sich die
   Summen der Zusammenfassung (V 67, D 21, E 326, K 194, X 89, gesamt 697) – nachgerechnet aus der CSV. Die ID-Menge der Tabelle
   „Klasse V nach Themen“ ist gleich der Menge der V-Zeilen der CSV (67 = 67, keine fehlende, keine überzählige). Stichprobe A822–A870
   gegen den Code (mindestens zwei je Klasse): E – A822 (`Cups.GameState.cs:87`, Mischung aus Stromzustand, Datei 0x43A17), A848
   (`OriginalExport.Squads.cs:134`, Poolplätze auf dem Markt); V – A825 (`CupsWorld.cs:122`, 40 Würfe beim WM-Start statt 29. April),
   A864 (`Transfer.Squads.cs:151`, Verletzung 0x07462–0x07548 nicht gerechnet), A858 (`Squad.Strength.cs:92`); K – A826
   (`FinanceMonthly.cs:54`), A870 (`HighscoreScreen.cs:152`); X – A823 (`CupsEurope.cs:28`, nur Verweis auf A078), A839
   (`MatchEngine.Live.cs:399`, Verweis „das [APPROX] der einen Zahl“, kein eigener Marker). Wortlaut, Fundstelle und Klasse stimmen
   überein. A059/A480 sind E mit Kennung N2 (R21-E1 = A), A612 bleibt V (R21-E2 = A, #896); A247/A701/A715/A775 weiterhin N2.
2. *Funktionskarte.* `python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write` (`ndisasm`
   installiert) erzeugt `docs/re-abdeckung.md` unverändert zum Head, zweiter Lauf ebenso; 71 Funktionen (E 38, K 9, X 24).
3. *Verhalten und Dateigrenzen.* `git diff 69c9143 HEAD -- src` ohne Kommentarzeilen: nur `SeasonEndGameOverScreen.cs`
   (`atGameEnd: true`) und `HighscoreScreen.cs` (Parameter, Feld, Farbe `NewRowColor` nur außerhalb des Spielendes). Übrige
   Quelldateien nur Kommentare. Geändert werden sonst Tests, `docs/analyse/restliste-v1*`, `docs/re-abdeckung.md` und der
   eigene Abschnitt in `docs/arbeitspakete.md`; keine Originaldateien oder Bilder im Diff. Hinweis des PR (Build auf `54f5809`,
   Head ändert danach nur Doku/CSV/Kommentar) ist durch den Lauf auf `855e44b` überholt.

**Akzeptierte `[APPROX]` (neu, erwartet):** A848 `AddUnseatedSquadPlayers` (E, Runde 22), A858 `Squad.Strength.FieldValue` (V,
#908, Runde 22), A870 Bestenliste am Spielende, Farbe 7 im Menü (K) – jeweils mit Beleg und Grund im Code.

**Erwartet offen, bestätigt (nicht Teil der Spur):** #896, #897, #899, #908 (Runde 22); ZIMMERMANN-Bereich 90–125.

**Annahmen, nicht geprüft:** Ob die Farbe 10 der Bestenliste am Spielende mit dem Original übereinstimmt, wurde nicht gegen das
Videobild gemessen (der PR verweist auf Pixelvergleich gegen `file54_4`; `HighscoreWeltkugelSpielendeTests` grün); Mehrsaisonprobe
entfällt laut PR (kein Kader-, Geld- oder Saisonwechselzustand geändert) – im Diff bestätigt.
