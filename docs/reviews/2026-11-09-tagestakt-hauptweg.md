# Review: Tagestakt im Hauptweg (PR #688, Fixes #678, #679)

Unabhängiges Erstreview. Base `4e72f01b326a387a6961d5139ec9599c350b4c8e`, Head `ca47ff0c59bc61bfd68f571acb3613aa02e219de`.
Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext; die Umsetzung stammt aus der Sitzung
`session_01GhACvSmWMYoEr7e7nVbPvY`), 09.11.2026.

Checkout: Klon, `git rev-parse HEAD` = `ca47ff0`, `git status --porcelain` leer. Ausgeführt wurde mit Originaldaten
(`kaaasi/eishockey-manager-referenz` `779a1c8`, `original/gamedata/` und `original/spielstaende/`). Mutationen, Probe-Tests und der
Probe-Merge mit `main` liefen nur in einem eigenen Worktree desselben Heads und wurden nicht committet.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/ca47ff0c59bc61bfd68f571acb3613aa02e219de`

## 1. Merge-Empfehlung

**Nein** für Head `ca47ff0`.

- **R688-01 (P1):** Der PR ist nicht mehr mit `main` mergebar (GitHub `mergeable: false`). Fünf Testdateien kollidieren mit #683. Die
  Fingerabdrücke müssen nach dem Merge mit beiden Ursachen neu ermittelt werden. Das gibt einen neuen Head, der neu zu prüfen ist.
- **R688-02 (P2):** Ein als Regressionstest angegebener Teil („Verträge nur Manager 0“ in der WM-Schleife) wird von keinem Test
  abgesichert. Die Rückmutation bleibt grün.
- **R688-04 (P2):** Der Takt läuft im Remake einen Menüschritt später als im Original (Gegenbeispiel FILE24 → FILE25). Trotzdem ist er
  als `[ORIGINAL]` „Mittwoch und Samstag“ beschrieben.
- R688-03 und R688-05 (P2) hat dieser PR nicht verursacht. Sie liegen aber im Bereich von #679 bzw. der neuen Fundstellen und sind
  durch Spielstände widerlegt. Vorschlag: als Folge-Issues führen, außer der Nutzer will sie mit #679 schließen.
- Sonst ist der Kern richtig. Alle neuen `[ORIGINAL]`-Fundstellen zu 0x07169, zum Takt, zum Listenwechsel und zum Stadionbau sind
  in em.exe nachgelesen und stimmen. Alle geänderten Sollwerte führen per Rückmutation auf den alten Takt zurück. AGENTS.md §4 ist
  eingehalten. Alle drei Läufe von `tools/pruefen.sh` sind grün.

## 2. Befunde

### R688-01 (P1): Konflikt mit `main` (#683), Fingerabdrücke neu ermitteln

`git merge-tree --write-tree origin/main ca47ff0` (`main` = `d931ade`) meldet Inhaltskonflikte in:

- `tests/EishockeyManager.Core.Tests/GameModeTests.cs` (`Mode4Reference`)
- `JugendTeamsTests.cs`
- `RngTwinProbeTests.cs` (6 Werte)
- `SpielberechnungAnschlussTests.cs` (2)
- `tests/EishockeyManager.UI.Tests/DebCupDrawFlowTests.cs`

Beide PRs haben dieselben Abdrücke mit verschiedenen Ursachen geändert (#683: Halbfinale nach 0x144; #688: Tagestakt).

Probe-Merge (Konflikte mit der Seite des PR gelöst, volle Lösung mit Originaldaten): Es scheitern genau die 11 + 1 Tests dieser
fünf Dateien. Sonst ist nichts rot, auch nicht die neue Play-off-Mehrsaisonprobe aus #683. Eine inhaltliche Kollision über die
Abdrücke hinaus gibt es also nicht.

Behebung:

1. `main` in den Branch holen.
2. Die Abdrücke neu ermitteln.
3. Je Wert zwei Gegenproben, wie in #683 vorgemacht:
   - alte Paarung (`Playoffs.cs` vor #683) → Wert des PR
   - alter Takt → Wert von `main`

Das Schema ist nicht betroffen: Der PR ändert weder `SaveGame*` noch die Migrationen.

### R688-02 (P2): WM-Schleife (c), „Verträge nur Manager 0“ ohne wirksamen Test

[`TagestaktHauptwegTests.WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero`]($H/tests/EishockeyManager.Core.Tests/TagestaktHauptwegTests.cs#L189)
gilt laut PR als Regressionstest, auch für „Verträge aller Manager“.

- Mutation E: In [`CupsNational.WorldCupLoop.cs` Z. 181]($H/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L181)
  `AnnounceContractEnds(day, manager)` zurück auf `AnnounceContractEnds(day)` (alle Manager). Ergebnis: **alle 11 Tests der Klasse
  grün**. In den wenigen Takten nach dem 13. April ändert sich an Meldung, Sperre und Angebot der Spieler von Manager 1 bei Sperre 0
  praktisch nichts.
- Ebenso im Hauptweg (Mutation I, `ManagerDailyRun` ruft `AnnounceContractEnds(day)` ohne `only`): Nur der Abdruck `Mode4Reference`
  wird rot, kein eigener Test.

Behebung (erprobt): Vor der Schleife bei allen Spielern von Manager 1 `NegotiationLock = 50` setzen. Dann ist der Test am Head grün
und mit Mutation E rot. Für den Hauptweg einen gleichartigen Zwei-Manager-Test ergänzen: Die Sperre von Manager 1 zählt je Takt genau
um 1.

### R688-03 (P2, nicht durch den PR verursacht): Stadionbau in der Sommerpause fehlt (dritte Aufrufstelle von 0x3D316)

Der Stub `0x100:0x25` (0x3D316) wird in em.exe an drei Stellen gerufen: `9A 25 00 00 01` bei Datei **0x552C3**, **0x556CD** und
**0x5650D**.

Die dritte Stelle liegt in der Sommerschleife des Saisonwechsels, Datei 0x564D2–0x56566, nach `inc [0x3BD]` bei Datei 0x56476. Dort
passiert je Kalendertag bis zum Zieldatum `0x5A1C…0x5A22` Folgendes:

- je Manager der Stadionbau, wenn `0x3B7 ≡ 0` (Datei 0x564F5–0x5650D)
- Tagesabschluss 0x3ED01 (Datei 0x56512)
- Heilung 0x07C12 je Kaderplatz
- `0x3B7 + 1` (Datei 0x56566)

Der PR schließt #679 und nennt nur die ersten beiden Stellen.

Gegenbeispiel FILE44 → FILE44A:

- Original: Restaurant laut Angebot „ca. 23 Wochen“, Restwochen also 21–25. Am 10.09.1994 (Zähler 1) bleibt noch **1 Woche**
  (`+0x12E` = 01). Das Kino „MITTEL“ (15–19 Wochen) ist fertig. Es sind also 20–24 Bauwochen vergangen, davon nur 2 in der Saison
  (Zähler 224 und 231).
- Remake (Probe: FILE44 importieren, Gastronomie +1 beauftragen, bis zum Saisonende spielen, `StartNextSeason`): 20 → **17 Wochen**
  am ersten Tag der neuen Saison.

Behebung: die Sommertakte im Saisonwechsel nachrechnen (Zahl der Tage mit Zähler ≡ 0 zwischen Saisonende und Zieldatum). Sonst als
Restlücke von #679 offen lassen. Die Schleife rechnet außerdem täglich Tagesabschluss und Heilung. Das ist eine eigene Restlücke.

### R688-04 (P2): Takt und Listenwechsel einen Menüschritt später als im Original, als `[ORIGINAL]` beschrieben

Im Original läuft der Takt nach den Spielen des Vortags (Datei 0x55204–0x552D3) und vor dem Hauptmenü des Tages. Der Listenwechsel
fällt im Zug des Managers vor dessen Menü: Datei 0x54E7C liegt vor 0x44E22 in der Schleife 0x54E3F–0x54EF4.

Das Remake rechnet beides in `OnDayManager(e)`, also **nach** dem Menü von Tag e:
[`SeasonFlow.RunDays`]($H/src/EishockeyManager.Core/SeasonFlow.cs) hält mit `CurrentDay = e`, bevor `ProcessDay(e)` läuft.

Gegenbeispiel FILE24 (Tag 14, Zähler 15) → FILE25 (Tag 17, Zähler 18):

- Original: Im Menü von FILE25 ist der Markt schon einmal verfallen (Import: 104 → 94, 107 → 97, 81 → 71, 91 → 81, 113 → 103,
  97 → 87; Liste gleich).
- Remake: Ab dem importierten FILE24 steht es im Menü von Tag 17 bei `FinanceDay` = 16. Zwischen den beiden Menüs liegt **kein**
  Takt (die Takttage sind 13 und 17), also auch kein Verfall.

Folgen:

- Ein im Menü geänderter Trainingsplan wirkt einen Takt früher als im Original.
- Ein auf die Liste gesetzter Spieler bekommt einen Angebotswurf einen Takt früher.
- Ein Bau, der im Menü eines Tages mit Zähler ≡ 0 beauftragt wird, bekommt eine Woche gutgeschrieben, die das Original schon vor dem
  Menü gerechnet hat. Probe FILE44: bis Saisonende 3 statt 2 Wochen.
- Im Hot-Seat sieht Manager 1 einen Listenwechsel aus dem Zug von Manager 0 erst am nächsten Halt.

Die Abweichung ist seit #675 bekannt, steht aber nur in einem Testkommentar
(`NationalteamRunde14Teil4Tests.MainPath_TalentSearchTicks_FollowDayCounter`). Zum Takt dieses PR fehlt sie: In
[`docs/tagesablauf.md` Z. 24]($H/docs/tagesablauf.md#L24), [`Squad.Training.cs` Z. 423]($H/src/EishockeyManager.Core/Squad.Training.cs#L423)
und `GameState.DailyRun.cs` steht „Mittwoch und Samstag“ als `[ORIGINAL]`.

Behebung, mindestens: `[APPROX]` mit dieser Begründung und Restlücke in `docs/arbeitspakete.md`. Ganz behoben wäre es, wenn der
Takt nach den Spielen von Tag e liefe (Zähler e + 2) und der Listenwechsel vor dem Halt. Das ist eine Entscheidung zur
Zeitleiste für Leitstelle und Nutzer.

### R688-05 (P2, nicht durch den PR verursacht): Listenwechsel beim Saisonwechsel hat keine Grundlage in em.exe

[`GameState.DailyRun.cs` Z. 22]($H/src/EishockeyManager.Core/GameState.DailyRun.cs#L22) sagt: „Weitere Aufrufer: nur der Saisonstart
(Datei 0x437D5)“. Das trifft nicht zu.

- 0x437D5 ist der **Spielstart**. Datei 0x43A93 setzt dort 0x3B7 = [0x6AF], Datei 0x43A9F ruft `0x118:0x5C` = 0x494EF. Es gibt nur zwei
  Aufrufe `9A 5C 00 18 01`: Datei 0x43A9F und 0x54E7C.
- Der Saisonwechsel (Datei 0x564D2–0x565A7) ruft 0x494EF nicht.

Spielstände:

- FILE44 (14.04.1994) → FILE46 und FILE44A (10.09.1994): Marktliste gleich, nur der eigene HOHENADL ist in den Kader
  zurückgekehrt.
- FILE22 → FILE23: Liste gleich (8 Namen).

Das Remake verwirft die Liste dagegen in `TransferMarket.StartSeason` und würfelt neu
([`Transfer.cs` Z. 178]($H/src/EishockeyManager.Core/Transfer.cs#L178), dort als „[ORIGINAL] Saisonstart 0x43A9F“ bezeichnet).

Behebung: im PR den Kommentar richtigstellen („Spielstart“). Den Saisonwechsel des Markts in einem Folge-Issue nach dem Original
umstellen (Liste behalten).

### R688-06 (P3): Ist Manager 0 entlassen, hören Markt-Verfall, Gebotsverfall und Lösen der Sperre auf

`DailyRunMarket` und `AdvanceNegotiationLocks` hängen an `Managers[0] == manager`, `MainPathDailyRun` überspringt entlassene Manager
([Z. 49–79]($H/src/EishockeyManager.Core/GameState.DailyRun.cs#L49)).

Probe mit zwei Managern, `Managers[0].Dismissed = true`, 60 Tage: Kein einziger Verfall auf der unveränderten Liste. Mit Manager 0
aktiv verfällt die Liste an jedem Takt.

Im Original läuft 0x07169 für jeden Platz 0 … [0x386] − 1, der Block „Manager 0“ hängt an `[0xA70E] = 0`. Der `[APPROX]`-Vermerk
nennt nur, dass Entlassene übersprungen werden, nicht diese Folge. Behebung: den Markt-Teil von Platz 0 auch dann rechnen, wenn
Manager 0 entlassen ist, oder den Vermerk ergänzen.

### R688-07 (P3): Restlücken laut PR – Einordnung gegen em.exe

1. **„Heilung der Markt-Plätze 0x07C12 in 0x07169 (Datei 0x072CB) nicht umgesetzt“:** Das trifft so nicht zu.
   - Die Heilung ist seit R14KS-02 umgesetzt: `HealInjuries` heilt `TransferMarket.ListedOutsideSquads` an Tagen mit Zähler ≡ 0
     (`MatchEngine.Injuries.cs`).
   - In 0x07C12 wirkt die Heilung nur bei `(Zähler − [0x6AF]) ≡ 0 (mod 7)` (Datei 0x07C34–0x07C5A, [0x6AF] = 0). Das ist eine
     Teilmenge der Takttage, das Ergebnis ist also gleich.
   - Es fehlt nur die Reihenfolge je Manager (Heilung vor dem Angebotswurf desselben Managers). Den Wurf beeinflusst das nicht, weil
     die Heilung des Tages schon vorher läuft.
   - Behebung: Restlücke streichen bzw. berichtigen.
2. **Heilung und Aufstellung „für alle Vereine“:**
   - Heilung: Für die Managervereine ist sie gleichwertig (siehe 1). Für Computervereine und Vertragslose rechnet das Remake
     zusätzlich. Wie im Original umsetzbar: Heilung in `ManagerDailyRun` auf die Kaderplätze und eigenen Markt-Plätze des Managers
     beschränken. Die Heilung in Schritt (d) der WM-Schleife (Datei 0x556F7) bleibt täglich.
   - Aufstellungsautomatik: Das Original ruft 0x426BC an drei Stellen:
     - am Takt (Datei 0x07C07)
     - nach einer Heilung (Datei 0x07C77)
     - an Tagen mit Ereignis je Satz vor den Zügen (Datei 0x54D9E)

     Das Remake ruft sie täglich, und zwar für die Managervereine. Umsetzbar: `club.ApplyAutoLineup()` ans Ende von
     `ManagerDailyRun` und an die Tage mit Ereignis setzen.
3. **Nicht in der Liste:** Nach dem Training zieht 0x07169 je Kaderplatz noch die Tendenz `+0x0E/+0x14` (Datei 0x07B64–0x07BFC:
   random(0, 3), Zähler `+0x14`, 0x4BD35(30, 68), bei 0 0x4907A). Das Remake schließt das nicht an (A134). Mit dem neuen Takt ist der
   Zeitpunkt jetzt bekannt.

### R688-08 (P3): Reihenfolge 0x952C ↔ Training umgekehrt, Test dafür abgeschwächt

Im Original: Training am Takt nach den Spielen von Tag e, danach am Tag e + 1 0x952C (Datei 0x54FBF).

Im Remake vorher: `CatchUpTraining` vor `OnDayNationalTeam`, also wie im Original. Jetzt läuft `MainPathDailyRun` danach. Der Test
[`NationalteamRunde14Tests` Z. 59]($H/tests/EishockeyManager.Core.Tests/NationalteamRunde14Tests.cs#L59) prüft deshalb nur noch
Tage ohne Takt. Seine „Gegenprobe alte Reihenfolge“ ist die Reihenfolge des Originals.

Wirkt nur mit einem Bundestrainer vor dem 13. April, also nur in Tests (siehe Kommentar in `GameState.Integration.cs`). Behebung:
den Vermerk richtigstellen („Reihenfolge umgekehrt, [APPROX]“), sonst nichts.

### R688-09 (P3): Kleinigkeiten

- **Doku nicht nachgezogen:**
  - Sperren und Verfall heißen weiter „random(9, 17) Tage“ ([`Transfer.Squads.cs` Z. 145]($H/src/EishockeyManager.Core/Transfer.Squads.cs#L145)),
    zählen aber jetzt je Takt.
  - In den Zusammenfassungen von `AnnounceContractEnds`/`RollUnsolicitedOffers` steht noch „Täglich“.
  - In `docs/transfer.md` fehlt bei „Tagesbyte ∉ {0, 255}“ die 9.
- **Alte Spielstände:** Gespeicherte `NegotiationLock`-Werte (Tage) laufen jetzt in Takten ab, also etwa 3,5-mal so lange. Das ist
  vertretbar, sollte aber im PR stehen.
- **Mehrsaisonprobe:** `FourSeasonChanges_TwoManagers_TrainingOnTicks` bleibt mit der alten Regel Mi/So grün (Mutation A), weil die
  Zahl der Trainingstage bis Tag 216 gleich ist. Als Probe in Ordnung, als Beleg für die Regel nicht.
- **Ungenutzt:** `TransferMarket.CatchUp`/`AdvanceDay` und `GameState.CatchUpTraining` haben keinen Aufrufer im Produktcode mehr. Nur
  Tests nutzen sie. Entweder als „nur Tests/Werkzeuge“ kennzeichnen (bei `AdvanceDay` schon so) oder entfernen.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` am Head (alle drei Läufe, Originaldaten aus dem Referenz-Repo)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2575 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2049 | 0 | 435 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2050 | 525 | 0 |

Commit `ca47ff0`, SDK 10.0.402, Ergebnis bestanden. CI auf dem Head: `build` success.

### em.exe selbst nachgelesen (`tools/reverse-engineering/disasm.py gamedata/em.exe …`)

**Takt im Hauptweg, Datei 0x55204–0x552D3:**

- Datei 0x55204 zählt `0x3B7` weiter.
- Datei 0x55253–0x55288: `≡ 0` oder `(+3) ≡ 0` mod 7, sonst Sprung nach 0x552D5.
- Danach 0x06FA5, 0x9D1B(1) und eine Schleife `[0xA70E]` = 0 … `[0x386]` − 1 mit 0x07169 (Datei 0x552A6).
- In derselben Schleife bei `≡ 0` der Stadionbau 0x3D316 (Datei 0x552C3). Er ist je Manager, denn 0x3D316 adressiert
  `0x9771 + [0xA70E] · 0x1AF`.
- **Bestätigt.** Talentsuche, Wanderung und Stadionbau (einmal je Manager über `WeeklyFinances`) passen dazu.

**0x07169:**

- Datei 0x0718F–0x07197: Block nur bei `[0xA70E] = 0`. Darin:
  - Verfall bei `+0x13 > 0x38`: −10 (0x071BB)
  - Sperre bei `(+0x1A & 0x7F) == 1 << m` mit random(0, 2) = 0 (0x071CC)
  - Gebotsverfall random(0, 3) = 0 bei `+0x09 > 0x1F` und Pool `+0x1E` ≠ 0 (0x07215)
- Datei 0x0727C–0x073CE: Schleife je Manager über die Markt-Plätze mit Pool `+0x1A = [0xA70E]` (0x072BA). Darin Heilung 0x07C12
  (0x072CB), Überspringen bei `+0x18 & 0x80`, Wurf ab 0x072DD.
- Datei 0x073E3: Verträge nur bei Zähler < 233 (0xE9). Die Kaderplätze kommen aus `0x5DC3 + [0xA70E] · 0x618 + i · 0x34`
  (0x07402).
- Datei 0x07AF2: Trainingskosten.
- Datei 0x07B4E Heilung, 0x07B5E Training. 0x0659C hat nur diesen einen Aufrufer (Suche nach `E8`-Ziel: nur 0x07B5E).
- Datei 0x07B64–0x07BFC: Tendenz (siehe R688-07).
- Datei 0x07C07: Aufstellung.
- **Bestätigt.** Das Training liegt damit nur am Takt (Mi/Sa im Remake-Tageslauf).

**Listenwechsel:**

- Datei 0x54D4C–0x54D65: 9 → 0, Byte 0 oder 255 springt nach 0x55073.
- Schleife 0x54E3F–0x54EF4: Wurf random(0, [0x386] + 2) = 0 (0x54E71) → 0x494EF (0x54E7C), vor dem Menü 0x44E22 (0x54EB2).
- Aufrufer von 0x494EF nur Datei 0x43A9F (Spielstart in 0x437D5) und 0x54E7C.
- **Bestätigt**, bis auf das Wort „Saisonstart“ (R688-05).

**WM-Schleife:**

- 0x07169 bei Datei 0x5565C vor dem Weiterzählen (0x55670).
- Stadionbau bei Datei 0x556CD nach dem Weiterzählen.
- Heilung bei Datei 0x556F7 täglich.
- **Bestätigt.**

**Sommerschleife Datei 0x564D2–0x56566** (R688-03).

### Gegenbeispiele an Original-Ständen (`emtool hok dump`, Block `03B7`; Import über `GameState.ImportOriginal`)

| Fall | Original | Remake / Ergebnis |
|---|---|---|
| Markt-Verfall FILE24 → FILE25 | Zähler 15 → 18, ein Takt (18 ≡ 4). Alle sechs Marktspieler E −6/−7 (Rohwert −10), Liste gleich | Je Takt einmal Verfall: richtig (`MainPath_MarketDecay_OncePerTick`). Zeitpunkt einen Menüschritt später: R688-04 |
| Trainingstage | Training nur in 0x07169 am Takt. In den Spielständen nicht sichtbar (Dump-Spalten FILE24 → FILE25 gleich) | Mi/Sa im Remake-Tageslauf, nur per Disassembly belegt |
| Gebotsverfall nur Manager 0 | Datei 0x0718F–0x07197 | richtig (`CatchUpManagerDay_TwoManagers…`, Mutation G rot) |
| Angebote je Manager | Datei 0x072B6 | richtig (gleicher Test) |
| Stadionbau FILE43 → FILE44 | Zähler 102 → 217, Sitzkomfort 8 Wochen → fertig | passt zu alter und neuer Regel |
| Stadionbau FILE44 → FILE44A | 20–24 Bauwochen, Restaurant noch 1 Woche | Remake 3 Wochen, 20 → 17: R688-03, R688-04 |
| Markt über den Saisonwechsel FILE44 → FILE46/44A, FILE22 → FILE23 | Liste gleich | Remake würfelt neu: R688-05 |

### Mutationen (Worktree des Heads, gezielte Testklassen mit Originaldaten)

| Nr. | Mutation | Ergebnis |
|---|---|---|
| A | `IsDailyRunDay` = Mi/So (alte Regel) | rot: `TrainingDays_AreDayCounterTicks`, `MainPath_TwoManagers_TrainOnlyOnTicks` ×2, `MainPath_MarketDecay_OncePerTick` ×2. Grün: `FourSeasonChanges_TwoManagers_TrainingOnTicks` (R688-09) |
| B | kein Markt-Verfall in `DailyRunMarket` | rot: `MainPath_MarketDecay_OncePerTick` ×2 |
| C2 | Listenwechsel jeden Tag | rot: `ListChangeDays_OnlyEventDaysOfMainPath` |
| C3 | Listenwechsel auch Tag 215/216 | rot: `ListChangeDays_OnlyEventDaysOfMainPath` |
| D/J | Stadionbau zurück auf Montag | rot: `Construction_AdvancesOnCounterZeroDays`; alle Abdrücke grün (205 Tests). Die Angabe des PR „Stadionbau allein ändert keinen Abdruck“ ist bestätigt |
| E | WM-Schleife (c): Verträge aller Manager | **grün** (R688-02). Mit `NegotiationLock = 50` für Manager 1 rot |
| F | WM-Schleife (c) ohne Markt | rot: `WorldCupLoop_StepC_MarketAndContractsOnlyManagerZero` |
| G | Gebotsverfall in jedem Managerlauf | rot: `CatchUpManagerDay_TwoManagers…` |
| H | Listenwechsel ein Wurf je Tag statt je Manager | nur `Mode4Reference` rot |
| I | Hauptweg: Verträge aller Manager je Lauf | nur `Mode4Reference` rot (R688-02) |

**Rückmutation „alter Takt“:**

- Vorgehen: `GameState.Integration.cs`, `CupsNational.WorldCupLoop.cs` und `GameState.Manager.cs` vom Base. `IsTrainingDay` = Mi/So.
  Übriger Head unverändert (Parameter `only`, `TrainManager`, Quelle je Managernummer mit Index 0).
- Mit den **Base-Fassungen** der 14 geänderten Testdateien: 194 Core- und 6 UI-Tests grün. Die alten Sollwerte, Abdrücke und Saaten
  kommen also zurück.
- Mit den **Head-Fassungen**: rot sind `Mode4Reference`, `JugendTeams` ×2, RngTwin ×6, Spielberechnung ×2, `DebCupDrawFlow`, die vier
  WM-Sollwerte aus `NationalteamRunde14Teil4Tests` (je Saat) samt Mehrsaisonprobe ×2 und `MarketDecay_LikeFile18…` sowie die Saat 71
  (`SeasonEndStatus`, `NationalCoachMode4`).
- Jeder geänderte Sollwert und Abdruck geht damit auf den neuen Takt zurück.
- Unter beiden Regeln grün, also nur gelockerte Vorbedingungen oder Prüfungen:
  - `FinanceCalibrationTests` (Saat 5)
  - `KaufEinsortierenTests` (Torhüter per `ChangeList` herbeigeführt)
  - `HeimspielStatistikTests` (`InRange` mit nicht zugeordneten Strafen)
  - `TransferOriginalTests` (passiver Kader 0 erlaubt)
  - `TransferOriginalVertragTests` (3 Takttage)

  Ihre Begründungen sind tragfähig. Ein Kader 0 beim passiven Manager folgt aus den `[ORIGINAL]`-Würfen je Takt: Verlängerung nur
  nach Angebot, Verhandlung am Saisonende.

### Probe-Merge mit `main` `d931ade`

Siehe R688-01: Nur die 12 Tests der fünf Konfliktdateien sind rot. Alles andere ist grün (Core 1738/1749, UI 672/673, Formats,
Audio).

### Weitere Pflichtpunkte

- **Mehrsaisonprobe** (4 Wechsel, passiv/aktiv, zwei Manager): grün.
  - `FourSeasonChanges_TwoManagers_TrainingOnTicks`
  - `Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach`
  - `WorldOnly_OneWorldCup_AutoOnOff`
  - RngTwin/Jugend/Spielberechnung
- **„Nur Weltkugel“ und Modus 4 mit/ohne Bundestrainer:** die Tests der Klasse `NationalteamRunde14Teil4Tests` grün. Modus 1 ohne
  Listenwechsel (`IsListChangeDay` = false) und ohne Verträge.
- **AGENTS.md §4:**
  - Zeit vergeht nur über `Continue()`: `MainPathDailyRun`/`MainPathListChanges` werden nur in `GameState.Integration.cs` gerufen,
    Schritt (c) nur in der WM-Schleife.
  - Training bucht über `Book` (`TrainManager`).
  - Kein Bildschirm ist geändert, kein Hauptmenü-Ereignis berührt.
- **Dateigrenzen:** gemeinsame Datei nur `GameState.Integration.cs`, im PR begründet. Neue Datei `GameState.DailyRun.cs`. Spurfremde
  Dateien sind im PR genannt.

### Akzeptierte `[APPROX]`

- Quelle der unaufgeforderten Angebote zusätzlich je Managernummer, Manager 0 wie bisher.
- Lösen der Sperre 0x071CC bei den Verträgen mit `ContractRandom`.
- Untergrenze 50 des Verfalls.
- Heilung zusätzlich für Computervereine (R688-07).
- Entlassene ohne 0x07169, aber Folge für Manager 0 siehe R688-06.

### Annahmen

- [0x6AF] = 0 (laut `DayCounter.cs`).
- `0xA70E = 0` beim Aufruf 0x5565C (nach den WM-Spielen) ist aus dem PR übernommen und nicht selbst nachverfolgt.
- Das Zieldatum der Sommerschleife ist nur aus FILE44A erschlossen (20–24 Bauwochen), nicht aus den Bytes `0x5A1C…`.
