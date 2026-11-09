# Nachprüfung: Tagestakt im Hauptweg (PR #688)

Nachprüfung des Reviews `docs/reviews/2026-11-09-tagestakt-hauptweg.md` (bisher geprüfter Head `ca47ff0`). Delta
`ca47ff0..f98f22f`: Merge `5245767` (main `eb7047f`, mit #683) sowie die Fixcommits `d16d487` und `f98f22f`. Sparsame Kette
(`docs/prozess/zusammenarbeit.md` §5). R688-01/-03/-04/-07 hat die Leitstelle erweitert geprüft, R688-02/-05/-06/-08/-09 nur mit je einer
Rückmutation.

Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext; der Fix stammt aus `session_01GhACvUjsvagLLZeJQv36kq`),
09.11.2026.

Checkout: Klon, `git rev-parse HEAD` = `f98f22fff2018efba32dd81e934839c48ddf61b6`, `git status --porcelain` leer. Originaldaten aus
`kaaasi/eishockey-manager-referenz` `779a1c8` (`original/gamedata/`). Mutationen, Gegenproben und Probe-Tests liefen nur in eigenen
Worktrees und sind nicht committet.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/f98f22fff2018efba32dd81e934839c48ddf61b6`

## 1. Merge-Empfehlung

**Ja** für Head `f98f22f`.

- R688-01 bis -09 sind behoben. Jede Gegenprobe zu R688-02 bis -06 und -08 wird rot. R688-01 ist nachgerechnet: Die Abdrücke gehen je
  Ursache auf ihren Ausgangswert zurück.
- Die neuen Fundstellen sind in em.exe nachgelesen und stimmen: Sommerschleife Datei 0x564BC–0x56593, Tendenz 0x07B64–0x07BFC,
  Markt-Platz-Heilung 0x072CB, Aufstellung 0x07C07.
- Schema 45 mit Migration ist geprüft: Ein echter Schema-44-Stand vom Vorgänger lädt mit `DayOpened = false`. Der Tag wird genau
  einmal geöffnet.
- AGENTS.md §4 ist eingehalten.
- `tools/pruefen.sh --schnell` mit Originaldaten ist grün (2602/0/0), die CI ebenfalls.
- Offen bleiben nur P3-Punkte, vor allem Testlücken zu Teilen von R688-07. Sie blockieren den Merge nicht.

## 2. Befunde

### R688N-01 (P3): Aufstellung und Markt-Platz-Heilung aus R688-07 ohne Test

Drei Rückmutationen in [`GameState.DailyRun.cs`]($H/src/EishockeyManager.Core/GameState.DailyRun.cs), zusammen angewandt:

1. `MainPathEventDayLineups` stellt wieder täglich auf (`ApplyAutoLineups()` ohne `IsListChangeDay`).
2. `club.ApplyAutoLineup()` am Ende von `ManagerSquadRun` (0x07C07) entfällt.
3. Die Heilung der eigenen Markt-Plätze in `DailyRunMarket` (0x072CB) entfällt (`if (false)`).

Ergebnis: **alle 1762 Core- und 673 UI-Tests grün** (mit Originaldaten). Jede Mutation für sich war in den gezielten Klassen ebenfalls
grün.

Dass kein Abdruck kippt, steht so im PR („Tendenz, Aufstellung, Heilung je Manager … ändern keinen Abdruck“). Die drei Regeln sind
damit aber nur per Disassembly belegt, nicht durch einen Test. Mutation 3 ist eine echte Lücke: Ohne sie heilt ein verletzter eigener
Spieler auf der Transferliste außerhalb des Kaders im Hauptweg nie mehr, weil `HealUnmanaged` Managervereine auslässt.

Vorschlag (Folge-PR genügt): ein Test „verletzter eigener Listenspieler heilt am Takt mit Zähler ≡ 0“ und ein Test „Aufstellung nur
am Takt bzw. an Tagen mit Ereignis“, z. B. so: Aufstellung künstlich stören, an einem Tag ohne Takt und ohne Ereignis halten, die
Aufstellung bleibt gestört.

### R688N-02 (P3): Tendenz (A134) wirkt in einem neuen Spiel auf keinen Managerkader

[`AdvanceTrend`]($H/src/EishockeyManager.Core/GameState.DailyRun.cs#L171) überspringt Plätze mit `TrendScore == 0` oder
`TrendControl == 0`. Das ist als `[APPROX]` vermerkt (Startkader, ältere Stände).

Probe (`TestGames.Create(3)`, eine Saison bis Phase ≠ Running): Im Managerkader haben **0 von 25** Spielern Wert und Steuerbyte, am
Anfang wie am Ende. Die Rechnung wirkt also nur auf importierte HOK-Stände und auf Plätze, die über 0x4907A neu angelegt werden.

Die Umsetzung entspricht dem Original, Datei 0x07B64–0x07BFC selbst gelesen:

- random(0, 3) ≠ 0 → ± (`+0x14` >> 4) & 7
- Halbbyte − 1, bei 0 neu über 0x4907A ohne Schreiben von `+0x0E`
- sonst 0x4BD35(30, 68)

Die Restlücke „Startkader ohne Tendenzwerte“ sollte aber als Folge ausdrücklich in `docs/arbeitspakete.md` stehen. Der PR meldet A134
als behoben.

### R688N-03 (P3): Gegenprobe zu R688-08 nur bei einer Saat rot

Rückmutation in die Reihenfolge von #678: Takt `MainPathDailyRun` nach `OnDayNationalTeam` in `OnDayManager`, also 0x952C vor dem
Takt desselben Tages.

Ergebnis: `Day_MainPathOnly_NationalValuesBeforeDrift_DriftDayShowsRolledForm` ist **rot für Saat 11, grün für Saat 3**. Der Test
erkennt die Mutation, aber nur zufällig über die Saat. Ausreichend, als Hinweis vermerkt.

Eine Mutation „0x952C im Tageswechsel vor dem Takt“ bleibt grün. Sie ist aber keine Umkehrung, weil 0x952C des Halttags dann
ebenfalls vor dem Halt liegt.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh --schnell` am Head (Originaldaten aus dem Referenz-Repo)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2602 | 0 | 0 |
| Ohne Originaldaten | CI `build` success (Head `f98f22f`) | | |
| Pflichtlauf ohne Daten | nicht verlangt (sparsame Kette) | | |

Commit `f98f22f`, SDK 10.0.402, Ergebnis bestanden.

### R688-01: Merge `5245767` (Eltern `ca47ff0`, `eb7047f`)

**Nichts verloren:**

- `git merge-tree --write-tree ca47ff0 eb7047f` (automatische Lösung) gegen den Tree von `5245767`: Unterschiede **nur** in den fünf
  Konfliktdateien `GameModeTests`, `JugendTeamsTests`, `RngTwinProbeTests`, `SpielberechnungAnschlussTests` und `DebCupDrawFlowTests`.
- Kein `src`-Unterschied.
- Je Konfliktdatei sind Zahl der `Fact`/`Theory`/`InlineData` und der `Assert` gleich wie auf beiden Seiten (21/74, 20/26, 8/4, 12/35,
  6/33).

**Gegenprobe je Ursache** (Stichprobe `RngTwinProbeTests.FourSeasonChanges_*`, 6 Abdrücke, synthetisch und Originaldatenbank):

| Probe am Merge `5245767` | Ergebnis |
|---|---|
| #683 zurück: `Playoffs.cs`, `OriginalImport{,.Market,.Playoffs}.cs`, `SaveGame.cs` von `ca47ff0`; Migration 43 und die #683-Tests entfernt | alle 6 = Werte von `ca47ff0` (`1848EC2F…`, `E230AA72…`, `95F3E977…`, `B5D7765D…`, `49C032F6…`, `289AAB7D…`) |
| alter Takt: `GameState.Integration.cs`, `CupsNational.WorldCupLoop.cs`, `GameState.Manager.cs` von `4e72f01`, Training Mi/So | alle 6 = Werte von `main` `eb7047f` (`975D2A6C…`, `973330F4…`, `F541B517…`, `BFFCFD23…`, `717B7C3D…`, `13BD58ED…`) |

**Werte nach dem Fix:**

- Die Rückmutationen R688-03, -04, -05 und -07h kippen die Head-Abdrücke von RngTwin und `Mode4Reference` wie im PR angegeben (Tabelle
  unten).
- R688-03 allein kippt nur den aktiven Lauf mit Originaldatenbank (`1324E979…`) und `Mode4Reference`.

### R688-04: Tageswechsel vor dem Halt, Schema 45

**Code:**

- [`SeasonFlow.RunDays`]($H/src/EishockeyManager.Core/SeasonFlow.cs#L155) ruft `OpenDay(CurrentDay)` nach `CurrentDay++` und vor beiden
  Halten (14. April und Managerspiel).
- `ProcessDay` holt `OpenDay` nach, falls nötig, und setzt `DayOpened = false`.
- `OriginalImport` öffnet den gesicherten Tag vor `ApplyOriginal`.
- Weitere Aufrufer gibt es nicht (`grep OpenDay`). Damit vergeht Zeit nur über `Continue()` bzw. den Import.

**Spielarten:**

- Hauptweg: `OpenClubSeasonDay`.
- WM-Schleife: `OpenWorldCupLoopDay` = (c) des Vortags und (d), nach dem Menü (a) `WorldCupLoopStepA`.
- Modus 1: `OpenWorldOnlyLoopDay`. Der erste WM-Tag öffnet erst im `ProcessDay`, weil `SkipToWorldCup` ohne `OpenDay` springt. Das
  passt zu Datei 0x54CF7–0x54D38, dort gibt es keinen Tageslauf.
- Modus 4 mit und ohne Bundestrainer.
- Hot-Seat: alle Würfe vor dem ersten Menü (`[APPROX]`, erwartet offen).

**Saisonende:** Am letzten Tag kehrt `RunDays` vor `CurrentDay++` zurück. Danach ist `DayOpened = false`, das bestätigt
`FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak` nach jedem `StartNextSeason`.

**Alte Stände laden:** Probe mit einem echten Schema-44-Stand, `TestGames.Create(3)` am Merge `5245767` bis zum Halt Tag 21 gespielt
(dort `FinanceDay` 20, `TrainedUntil` 20). Am Head:

- Laden gelingt mit `DayOpened = false`.
- `Continue()` → Halt Tag 24 mit `FinanceDay` 24, `TrainedUntil` 24 und `DayOpened = true`.
- Kein Tag ist doppelt oder gar nicht geöffnet.
- Speichern und Laden behalten `DayOpened`.

Das Feld steht im Schema-Abdruck (`SaveGameTests`, Kommentar 45).

**Bildschirme:** Im Delta ist keine Datei unter `src/EishockeyManager.UI` geändert. `PassiveScreenTests` (Öffnen jeder Kachel am Halttag
ändert nichts, jeder Tag genau einmal) sind am Head grün. Mit der Rückmutation (kein `OpenDay` vor dem Halt) werden alle drei
angepassten Tests rot. Das Hauptmenü-Ereignis `OnMainMenu` ist nicht berührt.

**FILE24 → FILE25:** `File24_ToMenuOfDay17_MarketDecayedOnceLikeFile25` ist grün. Rot wird er sowohl ohne `OpenDay` in `RunDays` als auch
ohne `OpenDay` im Import.

### R688-03: Sommerpause, em.exe selbst gelesen (`disasm.py`, Datei 0x56460–0x565B0, 0x367B1)

**Ablauf:**

- Datei 0x564BC: Zähler + 1.
- Schleife ab 0x564D2: 0x367B1(0) **berechnet das Datum aus dem Zähler** (0x5A20 = Zähler + 0xFC, dann − 365 je Jahr; zählt also nicht
  selbst weiter).
- Je Manager 0 … [0x386] − 1:
  - Stadionbau 0x3D316, wenn Zähler mod 7 = 0 (0x56504–0x5650D)
  - Tagesabschluss 0x3ED01 (0x56512)
  - Heilung 0x07C12 je Kaderplatz bis zur Kaderzahl 0x4BE17 (0x5653C)
- Danach Zähler + 1 (0x56566).
- Ende, wenn das Datum des verarbeiteten Zählers Monat 8 (0-basiert, also September) und Tag 9 ist (0x56570–0x56593).

**Nachgerechnet:**

- Zähler 1 + 252 = 253. Als 1-basierter Tag des Jahres ist das der 10.9., passt also zu FILE44A (10.09.1994, Zähler 1).
- Der 9.9. ist Zähler 365. Takte ≡ 0 zwischen 233 und 365 sind 238 … 364, also **19**.
- Die Zahl hängt nicht davon ab, ob der Zähler am Saisonende 232 oder 233 ist.
- Zusammen mit den Takten 224 und 231 der Saison ergeben sich 21 Bauwochen. Das passt zu FILE44A (Restaurant „ca. 23 Wochen“, am 10.9. noch 1 Woche).

**Restlücke:** Der tägliche Tagesabschluss 0x3ED01 der Sommerpause ist erwartet offen und im Code als `[APPROX]` vermerkt.

### R688-07: em.exe selbst gelesen

**Markt-Plätze (Datei 0x0727C–0x072DD):** Schleife über die Plätze `0x7623 + i · 0x34`. Ist der Besitzer im Pool `+0x1A` gleich
`[0xA70E]`, folgt die Heilung 0x07C12 (0x072CB) vor dem Sprung bei `+0x18 & 0x80` und vor dem Angebotswurf. Das Remake
(`DailyRunMarket`, eigene Listings außerhalb des Kaders) entspricht dem. Ohne Test, siehe R688N-01.

**Kaderplätze (0x07B29–0x07C07):** je Platz Heilung (0x07B4E), Training (0x07B5E) und Tendenz (0x07B64–0x07BFC), danach einmal 0x426BC
(0x07C07). 0x07C12 heilt nur bei `(Zähler − [0x6AF])` mod 7 = 0 und stellt nach einer Heilung neu auf (0x07C77). Das Remake hält
`HealSquad` + `RefillLineup`, `TrainManager`, `AdvanceTrend` und `ApplyAutoLineup` je Manager ein. Heilung, Training und Tendenz laufen
nacheinander statt verschränkt; das ist als `[APPROX]` vermerkt und wirkt nur je Platz.

**Tendenz:** siehe R688N-02. Sie stimmt mit dem Original überein, auch bei Halbbyte 0 → −1: Dann wird geschrieben und nicht neu
gewürfelt, im Original ebenso.

### Rückmutationen (Worktree des Heads)

Gezielte Klassen mit Originaldaten: Tagestakt*, Nationalteam*, RngTwin, GameMode, Transfer*, Stadion*, Deadline, TeamValue; 368 Tests.

| Befund | Mutation | Ergebnis |
|---|---|---|
| R688-02 (c) | `AnnounceContractEnds(day)` in `WorldCupLoopDailyRun` | rot: `WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero`, `Mode4Reference` |
| R688-02 Hauptweg | `AnnounceContractEnds(day)` in `ManagerDailyRun` | rot: `MainPath_TwoManagers_NegotiationLockCountsOncePerTick`, `Mode4Reference` |
| R688-03 | ohne `RunSummerBreak()` | rot: `SummerBreak_AdvancesConstructionAndHealsSquad`, `File44_Gastronomy…`, `FourSeasonChanges_…SummerBreak` ×2, RngTwin OrigDB aktiv, `Mode4Reference` |
| R688-04 | ohne `OpenDay` in `RunDays` | 25 rot (u. a. `MainPath_TickOfHaltDay_RunsBeforeHalt`, `File24_…LikeFile25`, WM-Zeitraum ×10, Deadline, `Day_MainPathOnly` ×2); UI: 3 rot `PassiveScreenTests` |
| R688-04 Import | ohne `OpenDay` im Import | rot: `File24_…LikeFile25`, `File44_Gastronomy…` |
| R688-05 | `ChangeList` wieder in `StartSeason` | rot: `SeasonChange_KeepsMarketList…`, `File44_SeasonChange_KeepsListLikeFile46`, `FourSeasonChanges_…` ×2, RngTwin ×6, `Mode4Reference` |
| R688-06 | entlassene Manager im Takt übersprungen | rot: `MainPath_ManagerZeroDismissed_MarketStillDecaysPerTick` |
| R688-07 Tendenz | ohne `AdvanceTrend` | rot: `MainPath_TrendControlCountsDownPerTick` |
| R688-07 Heilung Kader | ohne `HealSquad` in `ManagerSquadRun` | 7 rot (`Mode4_NoCoach_HealingFollowsDayCounter`, `Season_MarketRunsEveryCalendarDay`, RngTwin ×4, `Mode4Reference`) |
| R688-07 Heilung Markt-Platz | ohne Heilung in `DailyRunMarket` | **grün** (R688N-01) |
| R688-07 Aufstellung | täglich bzw. ohne 0x07C07 | **grün**, auch volle Suiten (R688N-01) |
| R688-08 | Takt nach 0x952C (Reihenfolge #678) | rot: `Day_MainPathOnly…` Saat 11 (Saat 3 grün, R688N-03) |
| R688-09 | Takt Mi/So statt Zähler ≡ 0/4 | 21 rot, u. a. `TrainingDays_AreDayCounterTicks`, `MainPath_TwoManagers_TrainOnlyOnTicks` ×2, `MultiSeason_Transfers_StayConsistent`, alle Abdrücke |

R688-09 ist sonst Doku. Nachgesehen: Die Zusammenfassungen sagen „je Takt“, `docs/transfer.md` nennt {0, 9, 255}, die Test-Hilfen sind
als „nur Tests/Werkzeuge“ gekennzeichnet. Die Sperren alter Stände stehen im PR.

### Mehrsaisonprobe

Grün im Lauf `--schnell`:

- `FourSeasonChanges_TwoManagers_MarketKeptAndSummerBreak` (4 Wechsel, passiv und aktiv, zwei Manager, mit Speichern und Laden je
  Wechsel)
- `FourSeasonChanges_TwoManagers_TrainingOnTicks`
- `Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach`
- `FourSeasonChanges_WorldOnly`
- `WorldOnly_OneWorldCup_AutoOnOff`
- RngTwin, Jugend, Spielberechnung

### Erwartet offen (bestätigt, im Code als `[APPROX]` und im PR genannt)

- Sommer-Tagesabschluss 0x3ED01: `RunSummerBreak`
- Hot-Seat-Reihenfolge der Würfe: `MainPathListChanges`
- Tag 0 bzw. neues Spiel öffnen erst in `ProcessDay`: `DayOpened`
- Neuaufstellung des Bundestrainers am Ereignistag nach den Menüs: `MainPathEventDayLineups`
- R204, Vertragslose auf freien der zehn Plätze: `TransferMarket.StartSeason`

### Akzeptierte `[APPROX]`

- Heilung, Training und Tendenz je Platz nacheinander statt verschränkt.
- Heilung der Spieler ohne Managerkader (`HealUnmanaged`).
- Ein entlassener Manager hat nur den Markt-Teil.
- Tendenz nur für Plätze mit Wert und Steuerbyte (siehe R688N-02).
- `NegotiationLock` alter Stände läuft in Takten ab.

### Annahmen

- [0x6AF] = 0, wie im Erstreview.
- Das Datumsformat von 0x367B1 (Tag 1-basiert, Monat 0-basiert) ist aus dem Abgleich Zähler 1 = 10.9. (FILE44A) erschlossen; die
  Monatsaufteilung nach Datei 0x3682A ist nicht gelesen.
