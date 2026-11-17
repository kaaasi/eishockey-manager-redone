# Review PR #905 – Spur `zufallsstrom` (Erstreview)

Geprüft: Base `348016fa74f09a11dddb7c4f443b8aea88ce0767`, Head `56e1c3ebe356542ce98edeba419bd4f3bfac375d` (sauberer Checkout,
`git rev-parse HEAD` = Head, `git status --porcelain` leer). Reviewer: Claude Opus 5.5, geplanter Lauf, eigene Sitzung (nicht an
#905 beteiligt; die Spur lief mit Claude Fable 5.2). Originaldateien aus `kaaasi/eishockey-manager-referenz` (`original/`).

## 1. Merge-Empfehlung

**Ja für `56e1c3e`.** Die Stichprobe der Zugstellen gegen em.exe trägt (Spielstart, Managerschleife, Werbeangebote, Tageslauf,
Europapokal, Startwerte); alle drei Läufe von `tools/pruefen.sh` grün, CI auf dem Head grün. Die neuen Proben der Teile 1–3 sind
auf der Base rot (14/14), die Proben der Spielstart- und Teil-4-Schritte werden unter gezielter Rückschaltung im Head rot.
Migrationen 51/52/53 → 54 mit echten älteren Spielständen laden, setzen über einen Saisonwechsel fort und bleiben im Zwilling
mit Speichern/Laden bytegleich. Öffnen aller Kachel-Bildschirme zieht nichts; N6-Stellen unverändert. Befunde nur P3: mehrere
fremde Testanpassungen sind auf dem Head nicht (mehr) nötig bzw. ihre Begründung trifft nicht mehr zu (P3-1), eine Teil-4-Stelle
hat nur Abdruckschutz (P3-2) – kein Merge-Hindernis.

## 2. Befunde

**P3-1 (fremde Tests, R21-D2 = A) – Anpassungen aus Zwischenständen, auf dem Head unnötig.** Probe: Base-Fassung der Testdatei in
eine Kopie des Heads, `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter …`:
- [AbgleichRestPreparationTests.cs:220](https://github.com/kaaasi/eishockey-manager-redone/blob/56e1c3ebe356542ce98edeba419bd4f3bfac375d/tests/EishockeyManager.Core.Tests/AbgleichRestPreparationTests.cs#L220) (Saat 23): Auf
  dem Head sind **alle** Saaten 21–28 grün (je Saat Zeile 220 ersetzt, beide Theorie-Fälle). Der Kommentar „unter 21–28 sind 22,
  24, 25 rot“ gilt nur für den Zwischenstand `9278c82`. Zur Frage „trägt Saatfall?“: Diagnoseprobe (je Saisonende Computervereine
  ≠ 40/60 und Schritt der letzten Änderung) zeigt auf dem Head je Saison höchstens einen solchen Verein, stets im selben Schritt
  wie das letzte Spiel des Managervereins (z. B. Saat 22, Saison 2: Verein 18, 2. Liga, Schritt 63 = Managerverein). Die
  Erklärung „Gegner im letzten Spiel“ ist damit plausibel, ein Fehler des Umbaus nicht erkennbar – aber die Saatänderung ist
  unnötig. Vorschlag: Saat 21 zurück, Kommentarzeilen entfernen.
- [IntegrationRunde9Tests.cs:22](https://github.com/kaaasi/eishockey-manager-redone/blob/56e1c3ebe356542ce98edeba419bd4f3bfac375d/tests/EishockeyManager.Core.Tests/IntegrationRunde9Tests.cs#L22), `:55`: Base-Fassung (ein
  Spielstand für beide Läufe) auf dem Head grün. Die Umstellung auf frische Stände schadet nicht (die Aussage ist so sauberer),
  aber die Begründung „mit der Wanderung in Satzfolge … gilt nur noch für gleiche Ausgangsstände“ ist für den Head nicht belegt.
- [AbgleichOriginalPaareTests.cs:101–107, 143, 170–171](https://github.com/kaaasi/eishockey-manager-redone/blob/56e1c3ebe356542ce98edeba419bd4f3bfac375d/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L101):
  Base-Liste auf dem Head: 135/137 grün, rot nur `FILE02→FILE03` Saaten 3 und 77 mit `LEER4.DefenseCondition: 10 → -6`. Nötig ist
  also nur das Teil-4-Muster; `ESC FRANKFURT.DefenseCondition`, `LEER3.DefenseCondition`, `EC RATINGEN.TeamValues.*` und
  `StrainSawtooth` für `FILE02→FILE50/51` sind unnötige optionale Muster (schwächen die Liste). Kein neu eingetragener
  Originalwert – bestätigt.
- Saatänderungen, deren Base-Fassung auf dem Head grün ist: `EuropeanCupTests`, `WorldCupTests`, `WettbewerbeSpiel1Tests`,
  `KaderRestSaisonendeTests`, `KaderTransferTeil7Tests`, `TransferTests`, `KaderRestTransferTests`, `KaderTransferTeil4Tests`,
  `PlayoffSemifinalTests` (152 Tests der 15 Dateien: rot nur `TeamwertAufstellung` passiv, `KaderJugendSaisonende` passiv,
  `PruefungKarteRunde16` Weltkugel-Export, `SeasonEndStatusTests` ×5, `NationalCoachMode4Tests` ×2 – diese Saatänderungen sind
  nötig, jeweils Vorbedingung „Manager unter den ersten fünf“ bzw. Saat-abhängiger Ablauf, keine Sollwertänderung).
  `AbgleichRestPenaltyTests` (64 statt 16 Saaten) ist nötig: Base-Fassung auf dem Head rot, Mittel 476,9 > 476,3; Sollwert und
  Grenze unverändert – trägt.
- Bestätigt (rot→grün wie im PR): `LivespielUi2Tests.PlayMatchDay_ReturnsToMenu…` und `JugendTeamsTests.MatchDay_StepsAreBounded…`
  (Head-Fassung auf `637d0bc`, Stand vor Teil 4: beide grün – Aussage unverändert, Testfehler korrigiert).
  `Pool_OriginalDatabase_MatchesFile05Layout` ab 180: Platz 179 ist damit gar nicht mehr geprüft (`< 179` und `≥ 180`) – als
  erwartet offen (#897) akzeptiert.

Empfehlung: im nächsten ohnehin laufenden Lauf (z. B. `pruefung-karte`) die unnötigen Anpassungen auf die Base-Fassung
zurücksetzen; kein eigener Fixlauf.

**P3-2 (Testlücke) – DEB-Pokal Runden 2–5 (0x3489D) nur über Abdrücke geschützt.** Mutation in einer Kopie des Heads:
[CupsDeb.cs:113](https://github.com/kaaasi/eishockey-manager-redone/blob/56e1c3ebe356542ce98edeba419bd4f3bfac375d/src/EishockeyManager.Core/CupsDeb.cs#L113) `var random = new GameRandom(1);` → `--filter
"FullyQualifiedName~Zufallsstrom"`: 36/36 grün; rot nur die Fingerabdrücke `RngTwinProbeTests` (6), `GameModeTests.Mode4…`,
`SpielberechnungAnschlussTests` (2). Ein Abdruck wird bei der nächsten Spur wieder „neu eingetragen“; eine gezielte Probe wie
`SeasonStart_Cups_DrawInOriginalOrderFromSharedStream` für `DrawNextRound` fehlt. Zum Vergleich: dieselbe Mutation in
`CupsEurope.cs:104` macht drei `ZufallsstromTeil4Tests` rot.

**Hinweis (kein Befund) – Mehrsaisonprobe unterscheidet nicht.** `ZufallsstromMehrsaisonTests` (Fassung `4475faf`) ist auch auf
`4d56908` grün (2/2): Sie belegt Speichern/Laden-Stabilität über 4 Saisonwechsel (AGENTS.md §5), nicht die neue Reihenfolge – im
PR auch nicht anders behauptet.

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh` (alle drei Läufe, Head `56e1c3e`, SDK 10.0.402):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3574 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2504 | 0 | 584 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2505 | 1069 | 0 |

**CI auf dem Head:** Check-Runs `build` success (Läufe 44027734543, 44027575355), `publish` ×3 success.

**Rückschaltproben (Gegenbeispiele):**
- Teile 1–3 auf der Base `348016f`: Testdateien in der Fassung ihres Commits (`cd6361e`, `d4de04b`, `ad85f37`), nur
  Sichtbarkeit `private → internal` für `DailyRunMarket`, `ManagerSquadRun`, `DrawContractTurnFactor` und `ReservePool(a, 3)`;
  `TeamValueDrift_FollowsOriginalRecordOrder` entfernt (`MatchEngine.DriftOrder` fehlt): **14/14 rot**.
- `ZufallsstromSpielstartTests` und Teil-4-Schritte übersetzen auf Vorständen nicht (Beobachter `NewGame.StartStep`); daher
  Mutationen im Head (`--filter "…Zufallsstrom…"`): Europapokal hinter die Managerschleife → 4 rot (u. a.
  `GameStart_EuropeanCup_…`, `ManagerLoop_PlaceThenStartValues_…`, `GameStart_SponsorOffers_…`); Kondition/Technik in
  `RollStartValues` vertauscht → `ManagerLoop_PlaceThenStartValues_…` rot; Verhandlungssperre, Talentsuche, Börsentag aus eigener
  Quelle → `Negotiation_…`, `TalentSearch_Start_…`, `StockDay_…` rot; Eintrag `[53]` der Migration entfernt →
  `SaveGame_Schema53_…` und `SaveGame_Schema51_…` rot.
- Bildschirme: `TrainingMenuEntries.Open` zieht `host.Game.Random.Next()` → `OpeningAllMenuTileScreens_…` rot. N6 = A:
  `Highscore.cs`, `LiveClock.cs`, `LiveMatchScreen.cs` unverändert, `DebCupDrawLive.cs` nur Kommentar (A061, A271, A274, A438,
  A452); `src/EishockeyManager.UI` greift nirgends auf `Random`/frühere Quellen zu (`grep`).
- Migrationen: echte Spielstände (zwei Manager, Saaten 5/6, drei Spieltage) erzeugt auf `b620c33` (Schema 51), `348016f` (52),
  `637d0bc` (53); im Head geladen: Laden ok, `_trendRandom`/`_turnRandom`/`_cupRandom`/`_financeRandom` fehlen nach erneutem
  Speichern, `_contractRandom` bleibt, Speichern→Laden→Speichern bytegleich, Fortsetzen bis zum Saisonwechsel 1994 (119–127
  Schritte) im Zwilling mit Laden alle 40 Schritte bytegleich (6/6).

**em.exe (Stichprobe aller vier Teile; `tools/reverse-engineering/disasm.py`, `ndisasm`):**
- Spielstart Datei 0x29896 → 0x5AF50 (Pool), 0x298A3 → 0x30E7B(0), 0x298B1 → 0x3139F(0), dann Managerschleife 0x29B57–0x2A0A3:
  Platzwahl-Wurf 0x29C91, Startwerte 0x29DA4–0x29EF7, Preis/Gehalt 0x29EB4/0x29EE4, Werbebewertung 0x2A03B → 0x3F833,
  Angebote 0x2A061 → 0x3FB47, Teamwerte 0x2A073 → 0x0952C; danach 0x2A0B5 → 0x5ACA0. Reihenfolge wie `NewGame.Create`.
  0x0952C erreicht keinen Zufallsaufruf (Aufrufgraph aus `funktionskarte.py --csv`), daher ist `MatchEngine.StartGame` nach der
  Schleife unschädlich. 0x49D36 zieht nur bei +0x09 Bit 7 (0x4A0C8–0x4A0F9) – beim Spielstart ohne Angebot, wie im Code.
- **Nebenbefund (3) bestätigt:** 0x29DF2–0x29E8E, k = 0: Kaderplatz +0x10 und Pool +0x15 (Kondition), k = 1: +0x11/+0x16
  (Technik), k = 2: dritter Wurf, dann Form random(40, 60) → +0x12/+0x17. Erster Wurf = Kondition.
- Tageslauf 0x07B4E (Heilung 0x07C12) → 0x07B5E (Training 0x0659C) → 0x07B77 random(0, 3) (Tendenz) je Kaderplatz verschränkt,
  wie `ManagerSquadRun`.
- 0x341AB: Würfe 0x342D7, 0x343C9, 0x34413, 0x345E4 (Push 0x345DD) wie im PR; zusätzlich 0x34533 random(0, 31) nur bei DGROUP
  0xE068 ≠ 0 (Live-Aufdecken) – das ist A271/A274 (N6 = A, erwartet). 0x0C068/0x0C097 sind Mausabfragen.
- AddManager: Hinzufügen-Modus 0x298BF → Kader 0x2993A–0x29A5E, dann dieselbe Schleife (Platztausch 0x29C3B, Startwerte,
  Werbeangebote) – wie `AddManager`.

**Erwartet offen – bestätigt:** `ComputerSquadRandom` ([Transfer.Squads.cs:115–129](https://github.com/kaaasi/eishockey-manager-redone/blob/56e1c3ebe356542ce98edeba419bd4f3bfac375d/src/EishockeyManager.Core/Transfer.Squads.cs#L115))
und `StartOrder`/`ComputerSquadSource` mit `[APPROX]` außerhalb des Stroms; A093-Wurf 0x548A8 unberührt (Runde 22); Platz 179,
`SlotSeed`, Verletzung 0x07462 → #897; A100 nicht bitgenau (`GameRandom` ist kein Borland-`rand`, `random(a,b)` = a + rand() %
(b − a + 1) in 0xC037); Auslosungen „nur Weltkugel“, Lage der Finanzwürfe im Tagesablauf, Wertebereich `random.Next` der
Auslosungen nicht geprüft (wie Übergabe).

**Akzeptierte `[APPROX]`:** Migration 52 → 53 und 53 → 54 (Folge setzt anders fort), `DailyRunMarket` (Verfall/Sperre/Gebote
nacheinander statt je Platz), `WorldCup.StartSeason`/`InitializeCompetitions` (Länderreihenfolge aus dem Stromzustand ohne
Weiterschalten), `GameState.Stocks` (Börse beim ersten Zugriff ohne Spielstart), `DriftOrder`-Rest, Zufallswahl eines Vereins vor
dem Pool. **Annahmen (nicht geprüft):** 0x30E7B(0) zieht nichts (Aufrufgraph erreicht Zufall nur über andere Zweige, Zweig für
Argument 0 nicht einzeln verfolgt); `s_startDrawsAtOriginalPlace` ist `[ThreadStatic]`, parallele Tests unbedenklich.
