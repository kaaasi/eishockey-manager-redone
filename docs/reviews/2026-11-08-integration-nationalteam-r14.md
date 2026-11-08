# Review: Integration Runde 14, Spur `nationalteam` (PR #661)

Base `3e0cf69afdd680a7710497e9c24b82170456f6be` · Head `b09c755715173457b8b8ab79f5fce6cbf5776ee3` · Reviewer: Claude (geplanter Lauf,
frische Sitzung ohne Implementierungskontext) · 08.11.2026 · Checkout: Worktree, `git rev-parse HEAD` = Head, `git status --porcelain`
leer. Vollprüfung des ganzen Deltas (21 Dateien, +839/−41; enthält #630, #637, #653). Codelesen **und** Ausführung am nachgewiesenen
Checkout mit Originaldaten. Fundstellen selbst mit `tools/reverse-engineering/disasm.py` nachgelesen.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3`

## 1. Merge-Empfehlung

**Nein** für diesen Head. A009, A012, A085, WM-Kader nach Art, Sperre Transfer/Training, Schema 41 und die Migration sind in Ordnung
und per Gegenprobe belegt. Echte Schema-39- und -40-Stände laden. Der Fix zu #594 macht aber das Spiel schlechter, als es auf Base war:
Im Modus 4 spielt Deutschland die WM-Termine an Wandertagen (5. Gruppentermin Tag 224, Halbfinale Tag 228) jetzt mit gewürfelter
Form statt mit der Form der Aufstellung (R14NT-01, P1). Im Original läuft die WM nach der Ernennung in einer eigenen Schleife. Dort gibt
es weder 0x54F9C noch die kleine Wanderung. Die Reihenfolge, die #594 herstellt, wird mit Bundestrainer praktisch nie erreicht.

## 2. Befunde

### R14NT-01 (P1) – #594: Die WM läuft im Original nicht durch den Tageslauf 0x54D41–0x55292; Head würfelt die Form vor WM-Spielen

[`GameState.Integration.cs` Z. 88–92](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.Core/GameState.Integration.cs#L88-L92) ruft im Modus 4 an jedem Tag
`OnDayNationalTeam` (Länderwerte aus der Aufstellung) und danach an Wandertagen `DriftTeamValues`. Die WM-Spiele des Tages folgen erst
danach. Das ist auch nach der Ernennung so.

Nachgelesen in der Tagesfunktion 0x54C9F:

- Am Tag mit Markierung 255 (Ende der Vereinssaison) läuft 0x54EC3 → 0x55374 … 0x553A2 → 0x553FF. Dort wird der Bundestrainer ernannt
  (0x55482/0x55485: DGROUP 0x9EDA := 0x6AC, das eigene Land). Danach geht es in die Schleife **0x55570–0x55740**:
  - 0x55587–0x55597: Menü des Bundestrainers nur bei Tagesbyte ≠ 0.
  - 0x55599–0x555D6: 0x37F/0x380, 0x426BC, **0x952C(5, 0)**.
  - 0x555F3: **0x30E7B(1)**, die WM-Spiele.
  - 0x55634–0x5565C: nur die Heilung 0x07169 an Tagen ≡ 0/4 mod 7.
  - 0x55670 und 0x55724: nächster Tag. Bei Byte 0x10 oder 0 geht es zurück nach 0x55570, bei 255 weiter zum Saisonwechsel.
  
  In dieser Schleife gibt es **keinen Aufruf von 0x9D1B** (Wanderung) und kein 0x54F9C. Die Liste der Aufrufe habe ich mit `grep call`
  über 0x555D6–0x56680 geprüft. 0x9D1B steht dort nur bei 0x56448, im Saisonwechsel.
- Nur auf dem Hauptweg gilt „0x952C(5, 1) bei 0x54F9C vor der Wanderung 0x5525A–0x55292“. Den erreicht man nur an Tagen mit Ereignis
  (Byte ∉ {0, 9, 0xFF}; Tage ohne Ereignis springen bei 0x54D5C nach 0x55073, hinter 0x54F9C). Satz 5 braucht dort außerdem
  0x9EDA < 0x3F, also einen Bundestrainer. Mit Bundestrainer springt der Hauptweg aber schon nach dem ersten Managermenü bei
  0x54EC6–0x54ECF (0x9EDA = 0x6AC) nach 0x555B7. 0x9EDA wird nur bei 0x55485 gesetzt und bei 0x5580E (Saisonwechsel) bzw. 0x54CC9
  (Neustart) auf 0xFD zurückgesetzt. Ich habe keinen Weg gefunden, auf dem 0x54FBF mit Satz 5 und danach 0x55292 laufen.
- Der Regressionstest [`Day_NationalValuesBeforeDrift_DriftDayShowsRolledForm`](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/tests/EishockeyManager.Core.Tests/NationalteamRunde14Tests.cs#L24)
  ernennt den Bundestrainer künstlich zu Saisonbeginn (`AppointNationalCoach` am Tag 0). Er prüft also einen Zustand, den das Original
  nicht hat.

**Gegenbeispiel** (am Head und am Base ausgeführt, instrumentierte Kopie: Form Deutschlands beim Aufruf von `WorldCup.PlayDay`). Modus 4,
`GameModeTests.Create(seed, true, true, 2)`. Bis Tag 216 weiter, dann `AppointNationalCoach(Managers[0])`, `AutoNominate`,
`AutoFormation`. Vor jedem `Continue` erhalten die Aufgestellten Form 1.

| Seed | Stand | 216 | 218 | 220 | 222 | **224** | 226 | **228** | 230 |
|---|---|---|---|---|---|---|---|---|---|
| 3 | Base | 1/1 | 1/1 | 1/1 | 1/1 | **1/1** | 1/1 | **1/1** | 1/1 |
| 3 | Head | 20/1 | 1/1 | 1/1 | 1/1 | **49/54** | 1/1 | **48/48** | 1/1 |
| 11 | Base | 1/1 | 1/1 | 1/1 | 1/1 | **1/1** | 1/1 | **1/1** | 1/1 |
| 11 | Head | 19/1 | 1/1 | 1/1 | 1/1 | **50/50** | 1/1 | **53/55** | 1/1 |

(Abwehr-/Sturmform 0x1C/0x1D.) Im Original gilt an diesen Tagen die Form aus 0x952C(5, 0) bei 0x555D6, also die der Aufstellung, wie
auf Base. Am Head kommt sie aus der Wanderung. Die Spalte 216 betrifft R14NT-02.

**Vorschlag:** Im Modus 4 nach dem Ende der Vereinssaison (ab der Ernennung) keine kleine Wanderung und kein `OnDayNationalTeam` im
Tageslauf. Die Länderwerte schreibt dann nur 0x952C(5, 0) vor den WM-Spielen, wie 0x555D6. Alternativ als kleinster Schritt die
Reihenfolge von Base wiederherstellen. Die Wanderung im WM-Zeitraum ist eine eigene Abweichung, die schon vorher bestand
(#204/`spielberechnung-komplett`). Die Leitstelle sollte sie als Befund aufnehmen. Den `[ORIGINAL]`-Kommentar in
[`GameState.Integration.cs` Z. 81–87](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.Core/GameState.Integration.cs#L81-L87) berichtigen: Die Reihenfolge ist
belegt, die Erreichbarkeit nicht. Regressionstest: Modus 4, WM-Termin an einem Wandertag (224 oder 228). Das Spiel Deutschlands
nimmt die Form der Aufstellung. Der Test ist am Head rot.

### R14NT-02 (P2) – A009: Neuaufstellung „Tag mit Ereignis“ (0x54D41–0x54DAA) im WM-Zeitraum nicht erreichbar

[`CupsNational.AutoLineup.cs` Z. 124–130](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.Core/CupsNational.AutoLineup.cs#L124-L130) stellt an jedem Tag mit
Ereignis mit der gesicherten Einstellung neu auf. Das geschieht nach dem Zug des Bundestrainers und nach Training und Markt in
`OnDayClubSeason`. Im Original gibt es diesen Aufruf in der WM-Schleife nicht (siehe R14NT-01). Dort stellen nur die Menüaufbauten
im Zug des Bundestrainers neu auf (0x450C6) und dann 0x426BC bei 0x555CA mit 0x37F = 0, also wirkungslos. Ändern sich die
Spielerwerte zwischen Menü und Spiel, spielt das Remake mit einer anderen Aufstellung als der, die der Bundestrainer gesehen hat. In der
Tabelle oben ist das Spalte 216 (Abwehrform 20 statt 1). Die Formen hat dort der Test verändert; im Spiel kann das Training im
Remake-Tageslauf sie verändern. Gleiche Ursache wie R14NT-01, gleiche Behebung. Der `[ORIGINAL]`-Verweis auf 0x54D41 gilt für den
Hauptweg, nicht für die WM.

### R14NT-03 (P2) – `bundestrainer-modus4` bricht ab: Szenario setzt eine Ernennung voraus, die nie eintritt (älter als #652)

Am Base und am Head nachgestellt: `dotnet run --project src/EishockeyManager.Tools -- screens gamedata <ziel> bundestrainer-modus4`
endet mit „Kein Zug des Bundestrainers erreicht.“
([`BundestrainerModus4.cs` Z. 23–24](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.Tools/Scenarios/BundestrainerModus4.cs#L23-L24)).

Ursache, eingegrenzt mit Diagnoseausgabe je Schleifendurchlauf: Der Tag läuft normal bis 216 (`01_verein_erster_wm_tag` wird
gespeichert). Dann folgt ohne Zug des Bundestrainers die nächste Saison (Tag 216 → 0, siebenmal in 400 Durchläufen). Der Manager von
Verein 2 wird nie ernannt, denn `TopFiveManager` ([`CupsNationalMode.cs`](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.Core/CupsNationalMode.cs)) ernennt
nur den Manager eines Vereins unter den ersten fünf der 1. Liga. Ein passiver Manager mit Verein 2 erreicht das nicht.

Keine Regression aus #652: gleicher Abbruch an `b7e1eb2` (vor #652), zurück bis `5f4092c`/`f52ffa7` (#519/#520, Runde 12). An
`796ea11` (#518) bricht das Szenario schon früher ab, im Spielstart („Bildschirm SetupScreen statt Hauptmenü“). Damit fehlen seit
mindestens Runde 12 die Bildschirmfotos für den Zug des Bundestrainers. **Vorschlag** (`pruefung-karte` oder Leitstelle): Im Szenario
einen Verein wählen, der sicher unter die ersten fünf kommt, oder nach Tag 215 ausdrücklich `AppointNationalCoach` aufrufen und das
im Szenario als `[APPROX]` vermerken.

### R14NT-04 (P3) – WM-Stufe nach dem Finale 8 statt 7

[`CupsNational.GameState.cs` Z. 103–105](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.Core/CupsNational.GameState.cs#L103-L105) zählt Gruppentermine plus
K.-o.-Termine, nach dem Finale also 8. Im Original springt der Finaltermin bei 0x31152 (`cmp [0x7ae],7` / `jnl 0x311CF`) am einzigen
Zähler `inc [0x7ae]` (0x311C8) vorbei, und die Stufe bleibt 7. Der Kommentar „+ 1 nach jedem Termin“ und
[Test Z. 163](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/tests/EishockeyManager.Core.Tests/NationalteamRunde14Tests.cs#L163) (`GroupDates + 3`) schreiben 8 fest. Heute ohne
Wirkung, weil nur `> 0` und `>= 5` abgefragt werden.

### R14NT-05 (P3) – A293: Startseite „ab Stufe 5 immer Endrunde“ statt „erster Aufbau, Stufe = 5“

Gegenbeispiel (4) am Head nachgestellt: Stufe 5, erster Aufbau, „Paarungen → WM“ öffnet die Endrunde. „Gruppe“ öffnet
„Gruppe 1 5.Spieltag“. Dann `OnMainMenu(isReturn: true)` und die Kachel erneut: Das Remake zeigt wieder
`WorldCupKnockoutScreen`. Das Original zeigt die gemerkte Gruppenseite (0x45961 → 0x44FBD umgeht 0x44A48; 0x44B15 nur bei
`[0x7ae] = 5`, beides nachgelesen). Der Code markiert das ehrlich als `[APPROX]`
([`WorldCupScreen.cs` Z. 135–149](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/src/EishockeyManager.UI/Screens/Competitions/WorldCupScreen.cs#L135-L149)). `arbeitspakete.md` verlangt aber
„Startseite nur beim ersten Aufbau eines Zugs, nur bei WM-Stufe = 5; ohne gemerkte Seite `[APPROX]`“. Umgesetzt ist keine der beiden
Bedingungen. Der UI-Test schreibt die Abweichung bei Stufe 6 fest
([Z. 97](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/tests/EishockeyManager.UI.Tests/NationalteamRunde14UiTests.cs#L97)). Die Leitstelle sollte entscheiden, ob die Näherung
genügt oder ob die Oberfläche eine gemerkte Seite bekommt. Das wäre ein Zustand der Oberfläche, kein Spielzustand.

### R14NT-06 (P3) – Testschwächen und eine ungenaue PR-Angabe

- [`SeasonChange_GermanyConditionTechniqueKept_FormRolled`](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/tests/EishockeyManager.Core.Tests/NationalteamRunde14Tests.cs#L79):
  Der Teil „erster Tag der neuen Saison schreibt nichts“ (Z. 107) läuft nie. `Continue` hält vor dem ersten WM-Tag, ohne dass ein Tag
  mit Bundestrainer gerechnet wurde. Die Mutation M2b (Wächter `Squad.Count > 0` entfernt) bleibt grün. Der Rest des Tests ist per
  M19/M20 belegt.
- [`FullSquad_ClickDoesNothing_KeeperSwapTakesRowThree`](https://github.com/kaaasi/eishockey-manager-redone/blob/b09c755715173457b8b8ab79f5fce6cbf5776ee3/tests/EishockeyManager.Core.Tests/NationalteamRunde14Teil3Tests.cs#L218):
  `Rows[2]` hängt nicht an der Reihenfolge von `Add`. Die Mutation M3 (Aufnahme hinten an) bleibt hier grün und wird nur von
  `HandNomination_InsertsByRole` gefangen. Die Aussage „ENGLBRECHT auf Platz 3 wie FILE49A“ ist damit nicht durch diesen Test belegt.
- PR #661 nennt `Migration_Chain39To41_AddsBothSettings` einen „echten Schema-39-Stand“. Der Test baut ihn aber aus einem 41er-Stand,
  indem er Felder entfernt. Ich habe echte Stände nachgeholt (Abschnitt 3, Gegenbeispiel 5): Sie laden.

### R14NT-07 (P3) – Dateigrenzen: fremde Testdateien

`IntegrationRunde4Tests.cs` und `NationalLineupScreenTests.cs` gehören nicht zu den Testdateien der Spur. Fremde Testdateien darf laut
`arbeitspakete.md` nur `pruefung-karte` ändern. Beide Änderungen sind minimal, sachlich nötig (A009 ändert das Verhalten nach dem
Laden bzw. nach „auto“) und in #653 genannt. Die Gegenprobe N1 zeigt, dass die neue Zeile in `NationalLineupScreenTests` nötig ist.
Sonst halten sich die Dateigrenzen nach L8 an die Vorgaben:

- `GameState.MainMenu.cs`: ein Aufruf; das entfernte `_ = isReturn;` und der Kommentar sind ohne Wirkung.
- `GameState.HotSeat.cs`: ein Aufruf, nur für A009.
- `WorldCupKnockoutScreen.cs`: Konstante und Kommentar.
- `CupPairingsScreen.cs`: unverändert.
- `GameState.Integration.cs`: gemeinsame Datei, in #630/#653 begründet.
- `SaveGame.cs`/`SaveGameTests.cs`: Umnummerierung der Leitstelle.

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh`** (alle drei Läufe, Head, Originaldaten aus dem Referenz-Repo):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2305 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1939 | 0 | 328 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1940 | 365 | 0 |

Commit `b09c755`, SDK 10.0.402, Ergebnis: bestanden.

**Gegenproben** (Mutation in einer Kopie des Heads, Filter auf die betroffenen Tests, danach zurückgesetzt):

| Mutation | Ergebnis (rot) |
|---|---|
| M1 `OnDayNationalTeam` hinter die Wanderung (Rückmutation #594) | `Day_NationalValuesBeforeDrift…` (2/2). Am Base ebenfalls rot (2/2) |
| M19 `_germanyRatings = null` beim Saisonwechsel | `SeasonChange_GermanyConditionTechniqueKept_FormRolled` (am Base grün; Regressionstest für Bestehendes) |
| M20 Saisonwanderung lässt die Länderform aus | `SeasonChange_…` |
| M2 Saisonwanderung bewegt K/T der Länder · M2b Wächter leerer Kader | **grün** (K/T liegen nicht in `TeamValues`; zu M2b siehe R14NT-06) |
| M3 WM-Kader hinten anhängen | `HandNomination_InsertsByRole` (`FullSquad_…` grün, R14NT-06) |
| M4 Sperre nie · M4b Stufe ohne K.-o. | `WorldCupStage_LocksNationalSquadFromFirstDate`, `CoachMenu_TransferTraining_LockedFromFirstWorldCupDate` · `WorldCupStage_…` |
| M13 Sperre auch „Aufstellung“ | `CoachMenu_TransferTraining_…` |
| M5 `OnNationalCoachMenu` aus `OnMainMenu` entfernt | `ClearedLineup_SavedInTurn_LoadLinesUpAgain`, `ClearedLineup_SwitchesOff_…`, `FourSeasons_…(active)` u. a. (7) |
| M6 Eingriff schaltet nicht ab | `ClearedLineup_SwitchesOff_…`, `Click_SwitchesOff_EmptySlotDoesNot`, `FourSeasons_…`, `IntegrationRunde4Tests.NationalLineup_SurvivesSaveLoadThroughDialog_InCoachMenu` |
| M16 leerer Platz schaltet ab | `Click_SwitchesOff_EmptySlotDoesNot` |
| M7 Zugende sichert nicht (0x380 := 0x37F) | `ClearedLineup_SwitchesOff_…`, `FourSeasons_…(active)`, `IntegrationRunde4Tests.NationalLineup_…` |
| M8 Amtswechsel setzt Einstellung zurück | `FourSeasons_SettingCarriesOver(active)` |
| M17 Ersatz-Automatik auch ohne Einstellung | `ClearedLineup_SwitchesOff_ValuesZero_PlaysEmpty` |
| M9 Migration immer 1 | `Migration_SetsSettingFromFormation(handMade: True)` |
| M10 Migration 40 nicht eingetragen | `Migration_…` und 15 weitere Migrationstests |
| M11 voller Kader nimmt auf | `FullSquad_ClickDoesNothing_…` |
| M12 A293 nie Endrunde · M18 `>= 5` → `== 5` | `CoachMenu_WorldCupPairings_OpensKnockoutFromStageFive` (beide) |
| N1 neue Zeile `OnMainMenu` in `NationalLineupScreenTests` entfernt | `CoachMenu_OpensNationalLineupAndTactics_WithoutChangingState` |
| M15 Schema 40 · M14b Feld `_turnOpen` umbenannt · M14c `_nationalAutoLineupSaved` umbenannt | `SchemaFingerprint_MatchesSchemaVersion` (alle drei; Abdruck `E34C26033CF5029C` stimmt am Head) |

**Gegenbeispiele des Auftrags:**

1. und 2.: siehe Tabelle.
3. Wandertag ohne WM-Spiel: Am Head zeigen die Länderwerte die gewürfelte Form. An den Tagen ohne Ereignis 217/221 stimmt das mit dem
   Original überein, dort ohne Wanderung, weil die Werte von 216/220 stehen bleiben. Die Abweichung entsteht an WM-Wandertagen
   (R14NT-01).
4. Stufe 5 → Endrunde → „Gruppe“ → Hauptmenü → Kachel: das Remake zeigt die Endrunde, das Original die Gruppe (R14NT-05, `[APPROX]`).
5. Echte Stände: mit `3e0cf69` (Schema 40) und `b7e1eb2` (Schema 39) erzeugt. Jeweils „nur Weltkugel“ mit geleerter Aufstellung, mit
   Handaufstellung (nur TW) und Modus 4 mitten in der Saison. Am Head geladen: Schema 41, `AutoLineupEnabled` = !Handaufstellung,
   Menüaufbau → 22 eingesetzt bzw. 1 Spieler. Speichern/Laden ist stabil, Saisonende, `StartNextSeason` und weiter laufen ohne Fehler
   (6/6 grün).

**Mehrsaisonprobe:**

- `FourSeasons_SettingCarriesOver` („nur Weltkugel“, 4 Wechsel, passiv und aktiv, Automatik an und aus): grün und per M5/M6/M7/M8
  belegt.
- Eigene Probe Modus 4, Seeds 5/9, passiv und aktiv (Leeren im ersten Zug des Bundestrainers): je Saison 8 Züge, WM vollständig. Passiv
  ist die Aufstellung voll, aktiv bleibt sie über die Saisons leer (15 Tage leer, Einstellung aus). Speichern/Laden ist stabil. Die Probe
  endet nach 2 Wechseln, weil der passive Vereinsmanager in der dritten Saison entlassen wird (`GameOver`).
- „Nur Weltkugel“ über eine WM mit Automatik an und aus: durch `FourSeasons_…` und `ClearedLineup_SwitchesOff_…` abgedeckt (leer
  0 Spiele, Länderwerte 0).

**Fundstellen, selbst nachgelesen:**

- 0x4509E–0x450A4 (nur bei `[bp−0xA]` = (0xA70E = 5)) ✓
- 0x30E89 (0x7AE := 0 bei Argument 0) und 0x311C8 (einziger Zähler; Finale ausgenommen, R14NT-04)
- 0x55599–0x555D6 ✓
- 0x426CC liest 0x375 + 2·0xA70E ✓
- 0x40AC4/0x40ADE und 0x40DCD–0x40DE4 ✓
- 0x450C6 hinter 0x44FBD ✓
- 0x44A5A (Argument 0x10) und 0x44B15/0x44B1C ✓
- 0x45961/0x4596C → 0x44FBD ✓
- 0x3C97A → 0x3CABF → 0x42CB1 mit Grenze 0x1D (0x3CAF6/0x3CB09) ✓
- 0x3C9BA → 0x3CA71 ✓
- 0x54F9C–0x54FBF vor 0x5525A–0x55292 ✓ (linear; zur Erreichbarkeit siehe R14NT-01)
- A012: Bild `file36_1` angesehen (MENDE nicht eingesetzt mit „VERL.“, HOCK ohne Nr. mit „GESP.(2)“) ✓

**AGENTS.md §4:** Der Hauptmenü-Zustand läuft nur über `OnMainMenu` (ein Aufruf `OnNationalCoachMenu`) bzw. den Zuganfang
(`HotSeat.cs`, `PassNationalTurn`). `WorldCupMenuEntries.Pairings` und `NationalMenu.IsLocked` lesen nur. Ein doppelter `BeginTurn`
(Zuganfang und erster Aufbau) ist idempotent.

**Erwartet offen, bestätigt:** keine Inventurzeilen im Delta (`docs/analyse/inventur.md` unverändert) → `pruefung-karte`.
`bundestrainer-modus4` bricht am Base und am Head ab (R14NT-03).

**Akzeptierte `[APPROX]`:**

- Ersatz-Automatik bei leerer Formation mit Einstellung.
- Leerer WM-Kader schreibt nichts.
- „A“ schaltet die Einstellung wieder ein.
- Länderwerte im Modus „nur Weltkugel“ im Tageslauf.
- A293 ohne gemerkte Seite: vertretbar, aber siehe R14NT-05.

**Annahmen:**

- Der Ablauf der WM-Schleife (R14NT-01) ist aus dem Code abgeleitet. Ein DOSBox-Stand an Tag 224/228 mit Formvergleich liegt nicht vor.
- Der Bildbeleg `file19_26` für die Sperre wurde nicht angesehen; der Code ist nachgelesen.
