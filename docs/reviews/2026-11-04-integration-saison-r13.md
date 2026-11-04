# Review Integration Runde 13 – Spur `saison` (PR #586)

Unabhängiges Review (Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne Mitarbeit an #569/#571/#579/#583/#586).
Base `48e1e67e846432a9433ecec428bcddea44361158`, Head `0a34f8761a324cf3db5b6c40161ea0e6de68d2db`.

## 1. Merge-Empfehlung

**Ja für `0a34f8761a324cf3db5b6c40161ea0e6de68d2db`.** Geprüft am Klon: `git rev-parse HEAD` = Head, `git status --porcelain`
leer. Mit Ausführung. `tools/pruefen.sh` hat am Head alle drei Läufe bestanden. Die Gegenbeispiele verhalten sich wie verlangt.
Die neuen Sollwerte der Mehrsaisonproben kommen allein aus der Fair-Play-Regel S-K6-c. Diese Regel ist in em.exe belegt
(siehe Gegenprobe M2 und Disassembly unten). Es gibt keinen P0–P2-Befund. Die vier P3-Befunde blockieren nicht.

## 2. Befunde

- **P3 – R13IS-01: Kein Test prüft, ob die Folge vom 29. April wirklich übernommen wird.**
  Stelle: [`AdvanceLeagueSlots`](https://github.com/kaaasi/eishockey-manager-redone/blob/0a34f8761a324cf3db5b6c40161ea0e6de68d2db/src/EishockeyManager.Core/SeasonFlow.LeagueSlots.cs#L110).
  Gegenprobe M4: `if (false && prepared.Count == …)`, die Folge wird also in `StartNextSeason` immer neu gebildet. Trotzdem
  bleiben `SaisonSatzfolgeSaisonendeTests` und `SaisonwechselSatzfolgeTests` grün (18/18).
  Grund: `SlotSeed()` hängt nur an `Season` und am Zustand von `Random`. In den Tests verbraucht zwischen 29. April und
  `StartNextSeason` nichts Zufall, also ergibt die Neubildung dieselbe Folge.
  Folge: Wenn ein künftiger Schritt im Fenster „Saisonende“ Zufall zieht (Verhandlungen) und die Übernahme kaputtgeht,
  weicht die Folge der Saison still von der Folge ab, die für die Fair-Play-Prämie galt.
  Vorschlag: In `MultiSeason_NextSlotsFormedOn29April_…` vor `StartNextSeason` einmal `game.Random.Next(…)` ziehen.
- **P3 – R13IS-02: Ohne gebildete Folge fällt die Fair-Play-Prämie still weg.**
  Stelle: [`FairPlayPremiumWinner`](https://github.com/kaaasi/eishockey-manager-redone/blob/0a34f8761a324cf3db5b6c40161ea0e6de68d2db/src/EishockeyManager.Core/SeasonFlow.FairPlay.cs#L26).
  Liefert `SuccessorSlots` null (ungewöhnliche Ligagröße, nur synthetisch, vgl. R12S-02), bleibt `_nextLeagueSlots` leer.
  `NextLeagueSlots` liefert dann nichts, und es gibt nie eine Fair-Play-Prämie. Base hätte sie nach Indexfolge vergeben.
  Ablauf: Aufbau wie `UnusualLeagueSize_AtSeasonChange_SlotsFallBackToIndexOrder`, aber den Verein vor dem 29. April
  verschieben. Den Managerverein mit 0 Minuten als einzigen Verein ohne Minuten führen: keine `SeasonEvent.FairPlay`.
  Vorschlag: Ohne Folge über die Mitglieder nach Index werten (wie `LeagueSlots`) oder einen Kommentar `[APPROX]` setzen.
  Für Spielstände aus dem Original gibt es keine Wirkung.
- **P3 – R13IS-03: Testdateien anderer Spuren geändert, ohne Freigabe für `saison`.**
  Betroffen sind `GameModeTests`, `RngTwinProbeTests`, `SpielberechnungAnschlussTests`, `StatistikenPraemienTests`,
  `SaveGameTests` und `SaisonwechselProbenTests` (Spur `saisonwechsel`, Runde 12). Die Regel ist „Tests nur in den
  Testdateien der eigenen Spur“. L13 erlaubt solche Änderungen nur `kader-start`.
  Die Änderungen sind klein, nötig und in #579/#583 einzeln begründet. Mit Gegenprobe M2 sind sie nachvollziehbar (siehe 3.).
  Vorschlag: Die Leitstelle bestätigt das nachträglich in einem Satz (z. B. im Integrationsabschnitt).
- **P3 – R13IS-04: L14 sagt wörtlich „Kein Schema 38 in dieser Runde“, der Head hat Schema 38.**
  Das Schema 38 stammt aus L5 (S-K6-a/c). L14 meint nur A088. Das stimmt mit `docs/arbeitspakete.md` überein
  („Schema 38 mit Leerschritt 37 (L5)“).
  Vorschlag: L14 bei Gelegenheit klarstellen, z. B. „kein weiteres Schema für A088“. Sonst ist der Text bei der
  Integration von `kader-start` missverständlich.

**Erwartet offen (bestätigt):**
- A088 bleibt `[APPROX]` (L14, Runde 14). Dafür gibt es am Head keine Code-Änderung.
- Leerschritt Schema 37: `[36] = MigrateSaisonPlatzhalter37` ist ein Durchreicher, im Kommentar `SaveGame.cs` steht das als
  „vorbehalten für kader-start (L5)“.
- `[APPROX]` „Spielstand vor Schema 38 vom Bildschirm Saisonende“ (`SaveGame.Migration.Saison.cs`): Eine echte Probe ist
  unten beschrieben. Die Prämie ist dort schon nach alter Regel gebucht, die Folge entsteht wie am Base.
- `[APPROX]` „Doppeltausch Managerverein Modus 1“ (`SeasonFlow.cs` Z. 292 ff.): FILE20/21 sind davon nicht betroffen. Das
  stimmt.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

**Umgebung:** SDK 10.0.402 aus dem Release `claude-werkzeuge` (`tools/claude/setup-workspace.sh`). Originaldaten aus
`kaaasi/eishockey-manager-referenz`, nur `original/` gelesen. Gegenproben liefen in eigenen Worktrees (Base `48e1e67`,
Mutationen auf Head-Kopie) und wurden danach verworfen.

**`tools/pruefen.sh` (ohne Option, Head `0a34f87`, Ergebnis: bestanden):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2178 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1834 | 0 | 308 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1835 | 343 | 0 |

**Gegenbeispiele.** Die Testdateien vom Head wurden auf den Base kopiert und dort ausgeführt (`EM_REQUIRE_GAMEDATA=1`).
Ergebnis: 9 rot, 22 grün.
- **R12S-02:** `UnusualLeagueSize_AtSeasonChange_SlotsFallBackToIndexOrder` ist rot am Base und grün am Head.
- **R12S-01:** `MultiSeason_SlotsRemixedEachSeasonChange_…` ist am Base grün, weil Base mischt. Am Head ohne
  `Mix(_nextLeagueSlots)` (M1) wird der Test rot (beide Varianten), ebenso `Schema32Save_…_MixedAtNextSeasonChange`.
  Die Probe erkennt also eine fehlende Neumischung.
- **R12S-03:** Am Base grün. Das ist erwartet: Base hatte nur Doku und den Testnamen, der Weg 32 → 33 → 34 läuft am Head
  grün.
- **Managerwechsel am Saisonende:** `SaisonendeManagerZugTests` ist am Base grün. Teil 2 ändert nur den Marker, der Code ist
  gleich. Am Head zwei Mutationen: Zeiger nicht auf die Seite i stellen (M8, Z. 33) bzw. danach nicht auf den ersten Manager
  (M9, Z. 42). Beide machen `SeasonEndPages_EachManagerOnTurnForOwnPage_…` rot.
  Fundstellen in em.exe (`ndisasm`):
  - Datei 0x26FB0: `mov al,[0xa70e]` / `mov [0xa70d],al`.
  - Datei 0x27B67: `mov [0xa70e],al` in der Verhandlungsschleife.
  - Datei 0x564E1: `mov byte [0xa70e],0` nach dem Fenster.
- **S-K6-a (Satzfolge am 29. April):** In em.exe gelesen:
  - Datei 0x563AF ruft die Ewige Tabelle.
  - Datei 0x563B2–0x5641A setzt die Liga je Manager (DGROUP 0x9746) aus dem Index nach dem Austausch, im Vergleich mit
    DGROUP 0x7C1/0x7C2.
  - Datei 0x56445/0x56448 ruft die Wanderung mit Modus 10, dann folgt die Mischung bei 0x5644E und das Fenster bei 0x5645D.
  - Die Reihenfolge im Remake (`FinishSeason`: Zwangsabstieg → `RecordSeasonStatistics` → `PrepareNextLeagueSlots` →
    Prämien) passt dazu.
  - `MultiSeason_NextSlotsFormedOn29April_…` ist grün (4 Wechsel, passiv und aktiv).
  - Lücke beim Übernehmen: siehe R13IS-01.
- **S-K6-c (Fair-Play über die neue Liga):** In em.exe Datei 0x2A1E0 gelesen:
  - Die Liga kommt aus `[0x9746 + 0x1AF·[0xA70E]]`, die Basis aus `[0x7C0 + Liga]`.
  - Die Minuten liegen bei `[0x82A1 + 0x50·(Basis + i)]`, also nach Satzplatz.
  - Getauscht wird nur bei `jna` übersprungen, also nur bei echt mehr Minuten: Erster ist der erste Satzplatz mit den
    wenigsten Minuten.
  - Datei 0x5647A–0x56496 löscht die Minuten aller **0x40** Sätze, auch der ligalosen. Nachrücker haben also immer 0, wie
    `ResetLeaguePenalties` (alle Vereine).
  - Mutationen: alte Regel (M2) macht `FairPlay_Promoted_…` (beide), `FairPlay_OberligaManagerWithZeroMinutes_…` und
    `StatistikenPraemienTests.MultiSeason_…` rot. Gleichstand zum letzten Platz (`<=`, M6) macht den Oberliga-Test rot.
- **Schema 37 → 38 mit echtem Schema-36-Stand vom Base:** Am Base wurden zwei Stände gespeichert (Schema 36, Seed 13,
  Manager auf den Vereinen 26 und 5): nach 5 Spieltagen und am Bildschirm „Saisonende“. Beide am Head geladen:
  - Speichern und Laden ergibt dieselben Bytes.
  - Der Stand vom Saisonende hat `NextLeagueSlots` leer. Nach `StartNextSeason` ist die Satzfolge **gleich der am Base
    gebildeten** (`2,5,3,19,…`).
  - Der laufende Stand ist danach über **4 Saisonwechsel** gelaufen. Bei jedem Wechsel waren 12/12/12 Plätze vorbereitet,
    und die Folge wurde übernommen. Eine Kopie nach Speichern und Laden ergab dieselbe Folge.
- **A058/A163 („nur Weltkugel“) gegen FILE20 → FILE21:** Mit `emtool hok dump … far:9657 5CCB` verglichen, nach Vereinsnamen
  (die Sätze sind im Original umsortiert). Kopf: Modus `0xD0B9 = 1`, beide Prüfsummen gültig.
  - 1. und 2. Liga haben dieselben zwölf Namen.
  - Oberliga: HEC HEILBRONN und EC BAD TÖLZ (Platzliste FILE20, Positionen 34/35 = Sätze 32/31) gehen. EHC STRAUBING und
    ESV KÖNIGSBRUNN kommen.
  - Kaufbeuren 0x18–0x1B: 80 78 79 75 → 92 91 90 87. Düsseldorf 94 × 4 → 94 × 4.
  - Alle zwölf Nationalteams sind in 0x18–0x1B unverändert, 0x1C–0x1E sind überall neu.
  - Testläufe: `SaisonWeltkugelTests` (4 Tests) und `SaisonwechselProbenTests.WorldOnly_…` (2) sind am Base rot und am
    Head grün. `GameModeTests.WorldOnly_SeasonRunsThroughWorldCup_…` ist ebenfalls am Base rot.
  - Mutationen: Wanderung nur außerhalb Modus 1 (M7) macht `…SeasonDriftMoves…` rot (2). Austausch nur außerhalb Modus 1
    (M10) macht `…OberligaExchangesLastTwoOfStartOrder…` rot (2).
- **Neue Sollwerte der Mehrsaisonproben (Begründung #579):** Gegenprobe M2 setzt am Head nur die alte Fair-Play-Regel ein
  (`SinglePremiumEvents`). Die Bildung am 29. April bleibt dabei. Danach liefern alle geänderten Abdrücke **genau die
  Base-Werte**:
  - `RngTwinProbeTests`: Seed 3 passiv/aktiv `BAF64BC1…`/`946F98B1…`, Originaldatenbank `8911A65C…`/`78D6E67B…`.
  - `SpielberechnungAnschlussTests` passiv: `2F5CC98E…`.
  - `GameModeTests.Mode4Reference`: der vollständige alte String, eigens eingesetzt, grün.

  Der Umbau von Zeitpunkt und Migration ändert also nichts. Die Abweichung kommt nur von der Prämienregel. Ursache ist,
  dass Manager 1 als Letzter in der Oberliga bleibt und gegen Nachrücker mit 0 Minuten verliert (−100.000 DM in Saison 1
  und 3). Das folgt aus der in em.exe gelesenen Regel und verdeckt keinen Fehler.
  `WorldOnlyReference` in `SpielberechnungAnschlussTests` stammt aus A058/A163: Die Konten bleiben gleich, die Weltmeister
  ändern sich mit der neuen Wanderung. Das ist plausibel, aber nicht einzeln gegengeprüft.
- **Dateigrenzen gegen Runde 13 und L1–L15:**
  - `Core/SeasonFlow*.cs`, `GameState.SeasonEndTurn.cs`: Spur `saison`.
  - `GameState.Integration.cs`: Die Änderung betrifft nur den Saisonwechsel „nur Weltkugel“ (A058), passt zu L9.
  - `SeasonFlow.LeagueSlots.cs` (L3): Geändert sind nur Wege des Saisonwechsels. `InitializeLeagueSlots` ist unverändert,
    `MixLeagueSlots` arbeitet nur noch über `Mix` und verhält sich gleich. Die Methoden sind in #579 genannt.
  - `SaveGame.cs` und `SaveGame.Migration.Saison.cs`: wie L5.
  - `StatisticsClub.cs` (`ui-rest`) ist unverändert, der Anschluss liegt in der neuen Datei `SeasonFlow.FairPlay.cs`.
  - Keine Änderungen an `docs/analyse/re-runde-13.md` und an den L12-Dateien.
  - Fremde Testdateien: siehe R13IS-03.
- **Mehrsaisonprobe (4 Wechsel, passiv und aktiv):** am Head grün in diesen Tests: `SaisonSatzfolgeSaisonendeTests`,
  `SaisonwechselSatzfolgeTests`, `StatistikenPraemienTests`, `RngTwinProbeTests`, `SaisonWeltkugelTests` (Modus 1) und
  `SaisonwechselProbenTests` (Modus 1, 20 Wechsel). Dazu kommt die eigene Probe aus dem Schema-36-Stand.

**Akzeptierte `[APPROX]`:**
- Zufallsquelle der 55 Tausche (lrandom, S-K6-b, nicht zugexakt).
- Entlassene Manager im Fenster „Saisonende“ ohne Seite.
- Spielstand vor Schema 38 vom Bildschirm „Saisonende“.
- Doppeltausch eines Managervereins im Modus 1.
- A088.

**Annahmen (nicht geprüft):**
- Die Auswahl der Nachrücker aus der Hauptquelle im Modus 1 ist nur gegen ein Spielstandpaar abgeglichen.
- Die übrigen Abdrücke von `WorldOnlyReference` sind nicht einzeln nachgerechnet.

---
Review: Claude Opus 5.5 (geplanter Lauf)
