# Nachprüfung Review PR #672 – Spur `nationalteam` Teil 4 (Fixlauf zu Review #674 und #675)

**Reviewer:** Claude (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext (Fixlauf-Sitzung `…01GhACvTk…`,
diese Sitzung `…01GhACvSFJ…`). Nachprüfung von `docs/reviews/2026-11-09-nationalteam-r14-4.md`. Geprüft wurde nur das Delta
**`d57962a5c9da019d71cefbd6869bb1b1e163403e..66a54bb9bd55813fd3308183e351f7680a6a0875`**, und zwar vollständig
(21 Dateien ohne den Merge, +419/−131). Klon, `git rev-parse HEAD` = Head, `git status --porcelain` vor und nach den Läufen leer.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head, aber knapp. R14NT4-01 bis -07 und #675 sind richtig umgesetzt und durch Mutationen abgesichert.
Der Tageszähler ist an 11 Original-Spielständen belegt, [0x6AF] = 0 auch. Nach dem Zählerversatz ist aber der Übergang vom
13. auf den 14. April falsch zugeordnet (R14NT4N-01, P2). Das Original rechnet dort bei Zähler 217 noch den Takt des Hauptwegs,
also Wanderung, Talentsuche und 0x07169 je Manager. Das Remake rechnet `OnDayManager(216)` schon als Tag der WM-Schleife. Zwei
neue Tests schreiben diese Abweichung als `[ORIGINAL]` fest. Jeder Fingerabdruck müsste nach dem Fix noch einmal geändert werden.
Darum besser vor dem Merge beheben (ein Tag, eine Stelle).

## 2. Befunde

### Nachprüfung der Befunde aus #674

| Befund | Status | Beleg |
|---|---|---|
| R14NT4-01 (P1) | **behoben** | `WorldCupLoopStepC` prüft `IsDriftDay(DayCounter.Of(day − 1))` → (c) nach den WM-Spielen der Tage 216/220/230 (Zähler 217/221/231). Rückmutation R01 macht 20 Tests rot (Teil-4-Tests, Fingerabdrücke, `Mode4Reference`, Jugend) |
| R14NT4-02 (P2) | **behoben** | `Mode4_WorldCupLoop_TalentSearchStands` (mit/ohne Bundestrainer); Rückmutation R02 (`clubDay: true` in der Schleife) → beide rot |
| R14NT4-03 / #675 (P2) | **behoben** | Wanderung, `TalentScout.IsTick` und `HealInjuries` mit Zähler `Tag + 1`; R03drift 14 rot, R03talent 1 rot (`MainPath_TalentSearchTicks_FollowDayCounter`), R03heal 5 rot (u. a. `Heal_Weekly_PermanentStays`) |
| R14NT4-04 (P2) | **behoben** (mit Lücke, s. N-02) | `OnDayWorldOnlyLoop`; R04 → `WorldCupLoop_MarketDecay_…(worldOnly: true)` rot |
| R14NT4-05 (P3) | **teilweise, wie angegeben** | (c) ohne `TransferMarket.CatchUp`, Markt-Verfall 0x071BB; R05decay 10 rot, R05catchup 12 rot. Verträge für alle Manager bleiben `[APPROX]` (→ #678) |
| R14NT4-06 (P3) | **behoben** | `NationalLineup_LeftClickOnInjuredOrSuspended_StaysWithMessage`; R06 (Bedingung `false`) → beide Fälle rot |
| R14NT4-07 (P3) | **behoben** | Kopie entfernt, gemeinsamer Kern `AdvanceFinance(day, clubDay)`; Rechtsklick `LineupScreen.cs` unverändert (→ #669, erwartet offen) |

### R14NT4N-01 (P2) – Übergang 13. → 14. April (Zähler 217) gehört im Original noch zum Hauptweg

[`CupsNational.WorldCupLoop.cs#L37`](https://github.com/kaaasi/eishockey-manager-redone/blob/66a54bb9bd55813fd3308183e351f7680a6a0875/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L37)
`InWorldCupLoop(day) => day > ClubSeasonEnd`, verwendet in
[`GameState.Integration.cs#L71`](https://github.com/kaaasi/eishockey-manager-redone/blob/66a54bb9bd55813fd3308183e351f7680a6a0875/src/EishockeyManager.Core/GameState.Integration.cs#L71).
Damit läuft `OnDayManager(216)` (= im Remake der Tageswechsel nach den Spielen von Tag 215, Zähler `DayCounter.Of(216)` = 217) als
Tag der Schleife: keine Wanderung, keine Talentsuche, (d) statt 0x07169.

Im Original (`disasm.py gamedata/em.exe`, selbst gelesen):

- Der Tag mit Markierung 255 (13.04., Remake-Tag 215) geht im Hauptweg über 0x54D5F → 0x55073 und von dort ohne Sprung hinter
  0x55204 weiter (alle Sprünge in 0x55073–0x55204 geprüft).
- Datei 0x55204 zählt 0x3B7 auf 217. Datei 0x55253–0x552D3 prüft 217 ≡ 0 (mod 7): Talentsuche 0x06FA5 (0x5528A), kleine
  Wanderung 0x9D1B(1) (0x55292), 0x07169 je Manager (0x552A6), Stadionbau 0x3D316 (0x552C3).
- **Erst danach** prüft Datei 0x552D5 die Markierung des gespielten Tags (`[0xA969]` steht noch auf ihm, weitergezählt wird erst bei
  0x56665 bzw. 0x553A2): 255 → 0x552E3 → Ernennung/Würfeln → 0x553FF → Schleife.
- Dazu passen die Spielstände: FILE22 und FILE44 (Modus 4, 14.04.) haben 0x3B7 = 217, es gibt also keinen weiteren Zählschritt
  zwischen Hauptweg und Schleife. Ein Spielstandpaar, das genau diesen Übergang einrahmt, gibt es nicht (FILE16A → FILE22 umfasst
  über 70 Tage). Der Befund stützt sich daher auf das Disassembly.

Folge im Remake, in allen Spielarten mit Vereinssaison und in jeder Saison: Wanderung und Talentsuche fehlen einmal. Die
Heilung trifft den Tag zufällig trotzdem, weil (d) bei 217 ≡ 0 ebenfalls heilt. Was 0x07169 des Hauptwegs tun würde (Training,
Markt-Verfall), ist Teil von #678.

Probe N01 (`InWorldCupLoop` erst ab Tag 217, sonst Head): Es fallen unter anderem `ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift`
([L90/L106](https://github.com/kaaasi/eishockey-manager-redone/blob/66a54bb9bd55813fd3308183e351f7680a6a0875/tests/EishockeyManager.Core.Tests/NationalteamRunde14Teil4Tests.cs#L106),
Teamwerte ab dem Menü des 14.04. gleich) und `Mode4_WorldCupLoop_TalentSearchStands` (Suche bleibt bei 3). Das Original
würde einmal wandern und einmal herunterzählen. Beide Tests sichern damit die Abweichung ab. N01 ist kein fertiger Fix, denn
er bringt auch das Training und den Markt des Remake-Hauptwegs an diesem Tag zurück.

Fix-Vorschlag: `OnDayManager(ClubSeasonEnd + 1)` rechnet vor (d) den Takt des Hauptwegs bei Zähler 217, also Wanderung,
Talentsuche und Heilung. Training und Markt folgen dort den Regeln des Hauptwegs (#678). Danach die Sollwerte und
Kommentare neu herleiten („keine Wanderung der Teamwerte vor den WM-Spielen“ in `GameModeTests`, Kopf von `Teil4Tests`).

### R14NT4N-02 (P3) – Modus 1: Heilung in der Schleife ungetestet

Die Mutation R04heal (`HealInjuries(day)` in `OnDayWorldOnlyLoop` entfernt) und R04first (`day >= FirstWorldCupDay`) bleiben
grün ([L82–L85](https://github.com/kaaasi/eishockey-manager-redone/blob/66a54bb9bd55813fd3308183e351f7680a6a0875/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L82)).
Die Regel ist im Klassenkommentar mit Fundstelle begründet (Datei 0x556D2–0x55712, Sprung 0x54CF7–0x54D38) und plausibel.
Im Modus 1 gibt es aber keinen Test mit einem verletzten Spieler über die WM.

### R14NT4N-03 (P3) – Untergrenze 50 des Markt-Verfalls ungetestet

R05clamp (`Math.Max(…, MinStrain)` entfernt) bleibt grün. `SetListingStrains` setzt 60, und 60 − 10 = 50 liegt genau auf der
Grenze ([L148](https://github.com/kaaasi/eishockey-manager-redone/blob/66a54bb9bd55813fd3308183e351f7680a6a0875/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L148)).
Ein Startwert von 57–59 würde die `[APPROX]`-Grenze prüfen.

### Erwartet offen (bestätigt)

- #678 (offen): 0x07169 läuft im Hauptweg nur an Takttagen. Markt-Verfall und Gebotsverfall fehlen im Hauptweg, und die
  Verträge in (c) gelten für alle Manager. Im Code als Restlücke bzw. `[APPROX]` benannt.
- #679 (offen): Stadionbau 0x3D316 bei Zähler ≡ 0 statt montags (`DailyStadium` im gemeinsamen Kern, unverändert).
- Rechtsklick `LineupScreen.cs` → #669, nicht geändert.
- `[APPROX]` Untergrenze 50: **nicht belegbar, Original widerspricht.** roh 57–59 → 47–49 (Review #674).
  Der Grund steht im Code (Validierung 50 … 150 in `kader-start`). Hingenommen, Wirkung nur auf den Wert.
- `[APPROX]` Modus 1 ohne Tagesabschluss/Training: **teilweise belegbar.** Das Konto bleibt FILE19 → FILE20 unverändert (500.000 DM).
  Training der Spieler Deutschlands lässt sich mit den Ständen nicht von der Spielabnutzung trennen. Hingenommen.
- #669: Ein Probe-Merge (`git merge-tree`) mit dem Head `fd7cf66` gibt Konflikte in `GameModeTests`, `JugendTeamsTests`,
  `RngTwinProbeTests` und `SpielberechnungAnschlussTests`, nur Sollwerte und Fingerabdrücke. Die müssen beim Zusammenführen neu
  gerechnet werden. Den gemeinsamen Kern in `GameState.Manager.cs` merged Git ohne Konflikt.
- #670 ist inzwischen auf `main` (`91c5a98`). Merge `66a54bb` + `origin/main` (`8a4643c`) läuft ohne Konflikt; Core 1665/0 und
  UI 653/0 mit Originaldaten grün.

## 3. Ausgeführte Prüfungen

### Lauf `tools/pruefen.sh` (alle drei Läufe, Head, Originaldaten aus `kaaasi/eishockey-manager-referenz` `original/gamedata`)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2350 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1969 | 0 | 342 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1970 | 380 | 0 |

Commit `66a54bb`, SDK 10.0.402, Ergebnis bestanden. Das deckt sich mit dem PR. Schema unverändert: `SaveGame*.cs` ist nicht im
Delta, also bleibt es bei 41.

### Merge von `main` im Delta

`e047d64` hat die Eltern `d57962a` und `dff7a50`. `git diff --stat d57962a e047d64` zeigt nur
`docs/reviews/2026-11-09-nationalteam-r14-4.md` (+201). `git diff dff7a50 e047d64 -- <diese Datei>` ist leer.
`dff7a50` ist Vorfahr von `main`. Der Merge bringt also nur bereits gemergte Inhalte.

### Gegenbeispiele (Original-Spielstände, `emtool hok dump … 03B7`, Transfermarkt-E)

| Datei | Datum | Remake-Tag | 0x3B7 | | Paar | Takte ≡ 0/4 nach Weiterzählen | Markt E | Remake (Head) |
|---|---|---:|---:|---|---|---|---|---|
| FILE01 | 10.09.1993 | 0 | 1 | | FILE24 → FILE25 | 16, 17, **18** → 1 | −6/−7 | Takt nur an Tag 17 ✓ |
| FILE24 | 24.09. | 14 | 15 | | FILE30 → FILE31 (Modus 2) | 2, 3, **4** → 1 | −6/−7 (alle 6) | Takt nur an Tag 3 ✓ |
| FILE25 | 27.09. | 17 | 18 | | FILE38 → FILE39 | **98**, 99 → 1 | −6/−7, ein Wert gleich (≤ 0x38) | Takt nur an Tag 97 ✓ |
| FILE13 | 31.01.1994 | 143 | 144 | | FILE18 → FILE48 (Modus 1) | (c) bei 217 → 1 | −6/−7 | `seen[218]` = 1 Verfall ✓ |
| FILE18/19/22/44 | 14.04. | 216 | 217 | | FILE19 → FILE20 (Modus 1) | (c) bei 217, 221 → 2 (231 erst danach) | −13/−14 | `seen[230]` = 2 ✓ |
| FILE48 | 16.04. | 218 | 219 | | | | | |
| FILE20 | 28.04. | 230 | 231 | | | | | |
| FILE14 | 10.09.1994 | 0 | 1 | | | | | |

Die Spalte „0x3B7 = Tag + 1“ gilt an allen 11 Ständen, auch im Modus 4 (FILE22, FILE44) und nach dem Saisonwechsel (FILE14).
Der #675-Takt des Hauptwegs (3, 6, 10, 13, 17 …) stimmt an drei Paaren der Vereinssaison mit dem Original überein. In (c)
gibt es eine Ausführung zwischen FILE18 und FILE48 und zwei zwischen FILE19 und FILE20, in den Modi 1 und 4.

**[0x6AF] = 0 selbst geprüft:** `disasm.py --xref --imm` über 0x690–0x6B0 zeigt für 0x6AF nur die Leser 0x07C3B, 0x43A93 und
0x565A7. Es gibt keinen Wortschreiber auf 0x6AE und keinen Schreiber auf 0x6AF. Der DGROUP-Startwert (Datei 0x16DD0 + 0x6AF) ist
`00`. 0x6AF liegt in keinem Spielstandblock (`hok dump`: nur `06AC`).

**Modus 1 in der Schleife:** Datei 0x54CEE–0x54D38 gelesen. Der Zähler läuft bis zum Tagesbyte 0x10 weiter (0x54CF7), dann geht
es über 0x553FF in dieselbe Schleife. Die Abfolge in `OnDayWorldOnlyLoop` passt dazu.

### Rückmutationen (je einzeln, in einer Kopie; Core-Testklassen des Deltas bzw. UI-Klassen, mit Originaldaten)

| Mutation | Befund | rot |
|---|---|---|
| R01 (c) mit Remake-Tag statt Zähler | R14NT4-01 | 20 (alle Teil-4-Trainingstests, Markt-Verfall 1/4, Heilung, Mehrsaisonprobe, 8 Abdrücke, `Mode4Reference`, Jugend) |
| R01heal Heilung in (c) mit Remake-Tag | R14NT4-01 | 4 (`Mode4_NoCoach_HealingFollowsDayCounter`, 3 Abdrücke) |
| R02 Talentsuche/Jugend in der Schleife | R14NT4-02 | 2 (`TalentSearchStands` beide) |
| R03drift Wanderung mit Remake-Tag | #675 | 14 (u. a. `Day_MainPathOnly_…` 2, `StatistikenPraemien`, Abdrücke) |
| R03talent `IsTick` mit Remake-Tag | #675 | 1 (`MainPath_TalentSearchTicks_FollowDayCounter`) |
| R03heal `HealInjuries` mit Remake-Tag | #675 | 5 (`Heal_Weekly_PermanentStays`, 4 Abdrücke) |
| R04 Modus 1 ohne Schleife | R14NT4-04 | 1 (`MarketDecay_…(worldOnly: true)`) |
| R04heal / R04first Modus 1 Heilung weg / ab erstem WM-Tag | R14NT4-04 | **0** (N-02) |
| R05decay ohne Markt-Verfall | R14NT4-05 | 10 |
| R05catchup `TransferMarket.CatchUp` zurück in (c) | R14NT4-05 | 12 |
| R05clamp ohne Untergrenze 50 | `[APPROX]` | **0** (N-03) |
| R06 Sperrmeldung nie | R14NT4-06 | 2 |
| R07youth Jugend in der Schleife | R14NT4-07 | 0. Gleichwertig: Nach dem 13.04. gibt es keine Ligaspieltage, die Talentsuche deckt R02 ab |

### Rückführung der Sollwerte, Saaten und Fingerabdrücke

- **FULL** (Schleife wie `d57962a`, also der Remake-Tag in (c), Training vor Heilung, `CatchUp`, kein Verfall; dazu die Hauptweg-
  Rückmutationen R03drift/talent/heal, R04 und **alle** Testdateien des Deltas auf dem Stand `d57962a`): Core 165/165 und UI
  (`DebCupDrawFlowTests`, `NationalteamRunde14UiTests`, `NationalLineupScreenTests`) 15/15 grün. Jede geänderte Zahl ist damit auf
  diese Regeln zurückgeführt.
- **Nur Hauptweg zurück** (R03drift/talent/heal, alte Testdateien außer `Teil4`): Die Saattests (`NationalCoachMode4Tests`
  17/2, `SeasonEndStatusTests`, `StatistikenPraemienTests`, `SpielstaerkeManager2Tests`) und `Heal_Weekly` sind mit den alten
  Saaten und Werten grün, gehen also allein auf #675 zurück. Die 8 Abdrücke, `Mode4Reference` und Jugend aktiv/passiv bleiben
  rot.
- **Nur Schleife zurück** (alte Schleife, R04, alte Testdateien außer `Teil4`): 20 rot, darunter alle Saattests und
  `DebCupDrawFlowTests`.
- Abdrücke, `Mode4Reference`, `JugendTeamsTests` und `DebCupDrawFlowTests.Reference` brauchen also beide Teile. Das bestätigt die
  Angabe im PR „Gegenprobe nur Hauptweg → weder alter noch neuer Wert“. Jede einzelne Regel (R01, R01heal, R03drift, R03heal,
  R05decay, R05catchup) verändert mindestens einen Abdruck (Tabelle oben).

### Gemeinsamer Kern `AdvanceFinance(day, clubDay)`

Gelesen und mit der entfernten Kopie verglichen. Für `clubDay: true` ist der Kern bis auf den Code identisch mit dem alten
`AdvanceFinanceTo` (Aufrufer: Hauptweg, `GameState.Manager.cs#L108`, Tests). Für `false` entspricht er der alten Kopie, nur stehen
die Schritte jetzt hinter `FinanceDay > 0`. Das ist nach Tag 215 ohne Wirkung. In anderen Spielarten wirkt am Kern also nichts;
die Wirkung kommt allein über `TalentScout.IsTick` (#675).

### AGENTS.md §4 und Dateigrenzen

- `Core/DayCounter.cs` ist eine reine Abbildung `Of(day) = day + 1` ohne Zustand, Zähler oder Nachholschleife. Zeit vergeht weiter
  nur über `Continue()`, und angeschlossen ist allein über `GameState.Integration.cs`. Das ist mit §4 vereinbar.
- Fremde Dateien im PR begründet: `GameState.Integration.cs` (gemeinsam, zwei Stellen), `GameState.Manager.cs` (R14NT4-07,
  berührt #669), `Transfer.Talent.cs`, `MatchEngine.Injuries.cs`, `docs/spielberechnung.md`. Neu ist `DayCounter.cs` als Baustein
  für alle. Fremde Tests ändern nur Sollwerte und Saaten, jeweils mit Kommentar. `docs/arbeitspakete.md` ändert nur den eigenen
  Abschnitt.
- Der Bildschirm ändert sich nur auf Klick. Der Merge-Commit `e047d64` hat keinen Agenten-Trailer, das ist im PR genannt.

### Mehrsaisonprobe, Modi

Grün am Head (im Lauf mit Originaldaten enthalten):

- `Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach`: 4 Wechsel, passiv und aktiv, je Saison 3 × Training von Manager 0,
  Speichern und Laden.
- „Nur Weltkugel“ über eine WM mit Automatik an und aus: `WorldOnly_OneWorldCup_AutoOnOff`.
- Ohne Bundestrainer über eine WM: `ClubOnly_…`, `Mode4_NoCoach_…`.

Die Mehrsaisonprobe sichert R14NT4N-01 nicht ab (siehe dort).

### Akzeptierte `[APPROX]` (getrennt von Annahmen)

- Untergrenze 50 beim Markt-Verfall (Original ohne Grenze; Grund Validierung).
- Modus 1 ohne Tagesabschluss/Training (Konto unverändert belegt, Training nicht trennbar).
- Verträge in (c) für alle Manager; Gebots- und Angebotsverfall fehlen (#678).
- Tageslauf des Remakes nach dem Menü des Tages statt vor ihm (im PR benannt; Zwischenstände im Menü weichen um einen Schritt ab).
- Nicht hingenommen: Tag 216 als Schleifentag (R14NT4N-01).

**Annahmen:** `[0xA969]` zeigt bei Datei 0x552D5 auf den gespielten Tag. So liest es auch 0x54D41 am Anfang des Hauptwegs, und
weitergezählt wird erst bei 0x56665. Die Markt-E-Änderungen der Paare kommen allein aus 0x071BB, wie schon in #674 angenommen.
