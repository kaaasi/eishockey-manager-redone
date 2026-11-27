# Review PR #946 – Spur `pruefung-karte`, Runde 22 (P3-Sammlung, Markerliste, Funktionskarte)

**Geprüft:** Head `cb5f501` (`git rev-parse HEAD` = Head, `git status --porcelain` leer), Base `4f2ec64`, Klon mit Originaldaten
(Referenz-Repo, `gamedata/` verlinkt), SDK 10.0.402. Reviewer: Claude (Sonnet 5.5), neue Sitzung, ohne Mitarbeit am PR. Geplanter Lauf, unbeaufsichtigt.

## 1. Merge-Empfehlung

**Ja** für genau `cb5f501`. Kein P0–P2. Alle genannten Gegenbeispiele sind rot, die Markerliste ist reproduzierbar (V = 5, 0 unklassifiziert),
keine V ohne Beleg verschoben. Drei P3 (Verdacht/Doku) unten; keiner blockiert. Einschränkung: Die Einordnung der 90 IDs wurde als **Stichprobe**
geprüft (siehe 3), nicht lückenlos.

## 2. Befunde (alle P3, nichts blockierend)

**P3-1 – Abweichende Klassen gegenüber der PR-Angabe der Spur, ohne Vermerk (Verdacht).** `#921` nennt für `MatchEngine.National.cs`
„Tore +0x1B/Gegentore (D)“ und „Poolfelder Torhüter nicht gelesen, D“ → CSV A906 = **K**; `#921` nennt „Summe mit einem Wertesatz (K)“ und
„Name/Alter/versteckte Werte am Platz (K)“ → CSV A872, A877 = **E**. Das sind keine Verschiebungen aus V (kein Blocker-Verlust), aber K
heißt „ohne Spielwirkung“, und A906 stützt sich auf „nicht gelesen“ (`restliste-v1-klassen.csv`, Zeile A906, Feld Abweichung). Vorschlag:
Begründung ins Feld „Beleg“ oder Klasse D. Beleg: PR-Text #921 („Restlücken Teil 2/3“) gegen CSV-Zeilen A872/A877/A906; kein Test möglich → Verdacht.

**P3-2 – D mit „RE offen“/„nicht gelesen“ (Verdacht).** A945 (Verdacht „RE offen“), A899 („0x54910 … nicht gelesen“), A910 („Zufallsverbrauch der
Szene nicht gelesen“) stehen als **D** („nur DOSBox/Video“, `restliste-v1.md` Erzeugung). Laut Regel „RE vor DOSBox“ (Spur `re-vor-dosbox`) wäre
zuerst `em.exe` zu lesen; D ist hier also vorläufig. Kein Verlust an V, da alle drei mit dem Remake-Verhalten dokumentiert sind; nur die
Abschlussprüfung sollte sie nicht als „DOSBox nötig“ zählen. Verdacht, kein Beleg.

**P3-3 – Veraltete Querverweise im Feld „Verdacht“ (Doku).** A060 (jetzt X): „Pokalbyte 0x9744 siehe **A559 (V)**“ – in `restliste-v1-klassen.csv`
gibt es keine Zeile A559 (`grep -n ',A559,' docs/analyse/restliste-v1-klassen.csv` → leer; A559 kommt nur im Text von A060 vor), V ist 5 ohne A559.
Zudem stehen die Marker von A677 (`Squad.Lineup.InMatch.cs:19–21`) und A864 (`Transfer.Squads.cs:151`) weiter im Quelltext mit altem Wortlaut („rechnet
das Remake nicht“), obwohl sie als X (veraltet) geführt werden; der Code rechnet die Verletzung (`RollInjuryOutsideMatch`, Zeile 166). Folge: Der
Generator zählt sie weiter als `[APPROX]`-Zeilen; `marker-x` (Welle 4) muss sie bereinigen – so vorgesehen, hier nur zur Kenntnis.

## 3. Besonders geprüft

**Gegenbeispiele (Rückmutation in temporärer Kopie, danach `git checkout -- src`; Befehl je Zeile:
`dotnet test tests/EishockeyManager.Core.Tests -c Release --filter "FullyQualifiedName~<Test>"`, `EM_REQUIRE_GAMEDATA=1`):**

| Issue | Mutation | Test | Ergebnis |
|---|---|---|---|
| #912 | `CopyPreparation` ohne `TacticsStrength`/`TacticsOpponentStrength` (`MatchEngine.Live.cs`) | `BeginMatch_ManagedClub_StrengthsEqualPreparationWithoutSquad` | rot |
| #914 | `Migrations[51]` bzw. `[53]` = Identität (`SaveGame.cs`) | `Migration_RealSavesOfSchema51To53_LoadAndPlayOn` | 1 von 3 rot (51) bzw. 3 von 3 rot (53) |
| #928 R924-01 | `var onAway = true;` (`OriginalExport.Cups.cs`) | `Export_PlayoffMarks_FifthGameHomeWin_StayOnHomeByte` | rot |
| #928 R924-02 | `false && …` in `PlayoffSeries.MarksFromReport` | `MarksFromReport_TieAfterSixtyMinutes_TwoMarks` | rot |
| #928 R924-03 | `TieMarks : 0` → `0 : 0` (`SaveGame.Migration.HokPlayOffMarken.cs:28`) | `SaveGame_Schema55_GetsPlayoffMarks` | rot |
| #930 | Aufruf `ApplyLoadLineupValues` entfernt (`OriginalImport.cs`) | `Import_File72_LikeLoading_WritesClubFieldsAfterRelineup` | rot |
| #938 M6 | `PoolForm ?? Form` → `Form` (`CupsNational.Slot.cs:21`) | `CopyForNationalSlot_TakesPoolForm_ElseSlotForm` | rot |
| #938 M7 | `ClampStrain`-Zeile entfernt (`MatchEngine.National.cs:223`) | `CreditWorldCupSquad_ClampsStrainOfAllSquadSlots` | rot |
| #938 M8 | Zähler-Import = 0 (`OriginalImport.National.cs:107`) | `Import_NationalCoachPowerPlayCounts_FromRecordFive` (FILE19/FILE20) | 2 von 2 rot |
| #941 P3-2 | Rückgabe der Leihe (`Player.Contract.cs:156–158`) entfernt | `MultiSeason_FourChanges_LoanSalaries` | 1 von 2 rot |
| #945 P3-1 | `hg + extraHome, ag + extraAway` → `hg, ag` (`Cups.Tiebreak.cs`) | `FromReport_ShootoutTieStays_KeepsExtraGoalsInScore` | rot |
| #945 P3-2 | `_goaliePulledAtEnd = false;` entfernt (`Squad.Tactics.GoaliePulled.cs:35`) | `StartMatchAfterPulledGoalie_ClearsBit_WithoutMatchEnd` | rot |

#914: Die Fixtures sind `{schema:51|52|53}`, tragen die Datenbank von `TestGames.Create(8)` (keine Namen/Strings aus Originaldaten;
Vorbild `schema19/22-synthetisch.ems.gz` im selben Ordner); der Test prüft `schema`-Feld, Laden, Saison weiterspielen, bytegleiche Sicherung. „Mit altem Code
rot“ ist nicht herstellbar (die Migration selbst ändert der PR nicht); die Identitäts-Mutationen zeigen, dass die Dateien die Stufen wirklich ausüben.
#928 (die drei Tests) und #930 (`ZusehenRestAufstellungTests.A858…`: Sollwert jetzt als Rohbytes 69/70/69/70/55/48, kein Abschwächen – `NotEqual`
zu FILE71 bleibt) gelesen; #941 P3-1 (Mutation gleichwertig) nachvollziehbar (kein Verhalten).

**`markerliste.py` ohne `--write` gegen den Head:** Rückgabe 0, 0 stderr-Zeilen „unklassifiziert“ (nur „Klasse ohne Marker (entfallen)“-Meldungen);
Ausgabe **byteidentisch** zum eingecheckten Block in `restliste-v1.md` (383 644 Zeichen). Zählung Marker: 231 Dateien, 644 Zeilen; V 5 / D 30 / E 324 /
K 213 / X 72; mit Funktionskarte 71 (E 38, K 9, X 24) → **V 5, D 30, E 362, K 222, X 96, Summe 715** wie `restliste-v1.md`. Summentabelle je Bereich
nachgerechnet (Spalten und V-Aufwand 2/1/2 stimmen). `restliste-v1-klassen.csv`: V = A873, A875, A913, A937, A951 (in der CSV
steht V nur noch für diese fünf Marker; die übrigen CSV-V-Zeilen gehören entfallenen IDs, siehe Meldungen).
**Erwartet offen bestätigt:** V = 5 (A875/A937 → #939, R22-E2 = B, Runde 23; A873; A913; A951 → #948); zurückgestellt #938 P3-1 (A876, K, Beleg FILE19→20→21),
P3-3 (Verdacht), #925 DEB-Pokal-Rest, #930 Rest (A917, K); 96 X für `marker-x`.

**Einordnung gegen Spur-PRs (Stichprobe):**

| Spur-PR | geprüft | Ergebnis |
|---|---|---|
| #921 `national` | alle 20 IDs + A874/A907 gegen PR-Abschnitte „Restlücken Teil 1–4“ | V (A873) ✓, E/K stimmen bis auf P3-1; A918 D ✓; A932/A933 E ✓ |
| #922 `zusehen-rest` | 5 von 5 (A892, A894, A896, A909, A954) | ✓ |
| #923 `kaderplatz-werte` | 11 IDs | V A913 (0x547AA) und A951 (0x4786A) ✓; „Verletzter spielt Reihe 0 ein“ → A939 E |
| #924 `hok-play-off-marken` | A915, A924, A929; PR nannte „A873 `Playoffs.cs:113` V“ | jetzt A925 **K**: Zusatzwurf selbst ist umgesetzt (`FastCupResultWithTie`, `playoffExtraRoll`; Review #944), offen bleibt nur die Marke im HOK-Export – Einordnung K belegt (Test `FastPath_*` grün) |
| #931 `transfer-pool` | A945–A952 | Belege/Datei-Angaben vorhanden; P3-2 für A945 |
| #932 `kader-regeln` | A060 (V→X), A889/A890/A938 E | A060: Level/Startliga/Startstärke je Spiel umgesetzt, Marker nur noch Sammelkopf → X vertretbar; P3-3 |
| #933 `wettbewerbe-regeln` | A456 (V→K), A677 (V→X), A898/A899/A910/A925 | A456: `MatchEngine.Minutes.cs:581` nennt Schützen auch bei vergebener Chance, Test `WettbewerbeRegelnLiveTests` ✓; A677: `CreditLineup` zählt `PlayedFromBench` mit Wettbewerbsfeld, Rest als A898 (K) geführt ✓ |
| #934 `einzelpunkte` | A665 (V→X), A671 (V→D), A886/A887/A897/A914 | A665: `InnerMoves` in `SeasonFlow.Zwangsabstieg.cs:121`, `Season.cs:308`, Tests in `EinzelpunkteTests`/`SaveGameTests` (Mutation nicht gelaufen, nur Codesuche) ✓; A671: Konto Satz 0 umgesetzt, Rest Modus 1 ohne Lesung → D passt |

Nicht stichprobenhaft geprüft (Annahme): übrige E/K-Zeilen aus #932–#934 (A889 ff., A926–A936, A955–A960), A864-Umsetzung (nur Codesuche: `RollInjuryOutsideMatch`,
`Transfer.Squads.cs:166`, keine Mutation), Funktionskarte (`re-abdeckung.md`: Zählung gelesen, nicht neu erzeugt).

**§5 Punkt 7 (keine V ohne Beleg verschoben):** Acht alte V neu eingeordnet – A060, A665, A677, A864 → X; A456, A545 → K; A671, A713 → D. Jede Zeile trägt im
Feld „Beleg“ den Zusatz „R22: …“ mit Spur-PR (#932, #934, #933, #931, #923) und Codefundstelle; die Umsetzungsbehauptungen A456, A677, A665, A864, A713 per
Codesuche bestätigt (siehe Tabelle). Keine Verschiebung „V → E/K“ ohne Umsetzung oder Lesung; die vier echten Restlücken (A873, A913, A951, dazu A875/A937) bleiben V.

## 4. Ausgeführte Prüfungen und `[APPROX]`

`source ~/claude-tools/env.sh; tools/pruefen.sh` (alle drei Läufe, Erstreview), Head `cb5f501`, SDK 10.0.402:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3787 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2673 | 0 | 612 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2674 | 1113 | 0 |

Ergebnis: bestanden (Build mit Warnungen = Fehler grün). Zusätzlich: `python3 tools/reverse-engineering/markerliste.py` (ohne `--write`, `--repo` Standard),
13 Mutationsläufe oben, Fixture-Prüfung per `zcat`/Python. Keine Pixel-/Bildvergleiche (nicht Gegenstand des PR).

**Akzeptierte `[APPROX]` (belegt/begründet im PR):** A876 (Zähler Satz 5, K, Beleg FILE19→FILE20→FILE21), A917 (HOK-Import nur Manager am Zug; Nachbilden für
andere Sätze ändert den Export, FILE06 rot – im PR belegt), A925 (Marke Zusatzwurf im Export). **Annahmen (nicht geprüft):** Gültigkeit der Datei-Angaben
(0x54DAA, 0x547AA, 0x4786A …) gegen `em.exe` – nur die Übereinstimmung zwischen PR-Text, CSV und Code gelesen, keine Disassembly.
