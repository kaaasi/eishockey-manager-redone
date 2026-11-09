# Review: Integration `pruefung-karte` (Runde 14, Welle 2, PR #701)

Erstreview des Integrations-PRs #701 (Stapel #693 ← #694 ← #695 ← #696 ← #697 ← #700). Base `01f15f4`, Head
`1ee008eadccf25be5344f888791d1f49391d6142`.

Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext; die Spur stammt aus der Leitstelle), 09.11.2026.

Checkout: Klon, `git rev-parse HEAD` = `1ee008eadccf25be5344f888791d1f49391d6142`, `git status --porcelain` leer. Originaldaten und
DOSBox-Bilder aus `kaaasi/eishockey-manager-referenz` `779a1c8` (`original/`). Zugriff vorab geprüft (`git ls-remote … HEAD` →
`779a1c8`). Mutationen, Gegenproben und Probe-Tests liefen nur in zwei eigenen Worktrees und sind nicht committet.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/1ee008eadccf25be5344f888791d1f49391d6142`

## 1. Merge-Empfehlung

**Ja** für Head `1ee008e`.

- Alle drei Läufe von `tools/pruefen.sh` sind am Head grün (2625/0/0 mit Originaldaten), die CI ebenfalls.
- Die Ladeeigenheit (#697) ist in em.exe nachgelesen und stimmt: Lader, Tagesfunktion mit `bp+6`, zweiter Tageswechsel, Ende beim
  Saisonwechsel. Sie greift nur bei FILE22 und FILE44. Remake-Stände und andere Importe bleiben unverändert (Versatz 0, siehe 3.4).
- Die Gegenbeispiele sind nachgestellt: FILE44 → FILE46 ergibt −2.106.011 DM (Original −2.139.514 DM). R688S2-03, R14KS9-03 und
  R14UR-10 sind behoben.
- Jeder neue oder geänderte Test der Spur wird mit seiner Gegenprobe rot. Ausnahme ist `CoachWarning_Boundary` (R701-02).
- A158 `[ORIGINAL]` und A383 sind nachgelesen.
- Offen bleiben nur P3-Punkte. Keiner betrifft Spielregeln in erreichbaren Abläufen.

## 2. Befunde

### R701-01 (P3): Kopflinie der Transferliste (y 36) beginnt bei x 181 statt 180

[`TransferScreen.cs:418`]($H/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L418) zeichnet die Linie unter dem
Listenkopf mit `EntryLine(s, 36, …)`, also wie die Eintragslinien von x 181 bis 313.

Das Original zieht diese Linie aber getrennt (Datei 0x46693–0x466AC: `0x0B7B5` mit (0xB4 | 0x24)–(0x139 | 0x24), Farbe 0x1F), also
von **x 180** bis 313. Die DOSBox-Bilder bestätigen das: In `file40_03`, `file40_04` und `file40_05` (2×-Aufnahme, x = (px − 1) / 2,
y = (py − 26) / 2) steht in Zeile 36 Index 31 ab x 180, in den Zeilen 52 + 16 · k erst ab x 181.

Probe am Head (Transfer nach neuem Spiel, Maus im Kader): Zeile 36 hat bei x 180 Index 4, ab x 181 Index 31, bei x 314 Index 4.
Vorher (Base) lief die Linie von 180 bis 314. Der linke Rand ist mit Teil 2 also falsch geworden, der rechte richtig.

Vorschlag: Zeile 36 ohne Markierung mit x 180–313 zeichnen (Datei 0x46693). Nach einer Markierung von Eintrag 0 gilt 180–314
(Datei 0x46AFE). Dazu eine Zeile im Test `TransferList_Separators_181To313_LongerAfterHover`.

### R701-02 (P3): `CoachWarning_Boundary` erkennt die Rücknahme von A028 weiterhin nicht (#620)

#696 führt den Test als erledigten Punkt von #620 („Halt genau am nächsten WM-Tag“). Die Gegenprobe hat #696 aber nicht ausgeführt
(„Nicht mutiert: `CoachWarning_Boundary`“).

Gegenprobe hier: In [`CupsNationalMode.cs:108`]($H/src/EishockeyManager.Core/CupsNationalMode.cs#L108) wird
`NationalCoachPlaysOn` auf die alte Regel „nur an Spieltagen Deutschlands“ zurückgesetzt (`… && MatchOn(day, NationalCoach.Nation)
is not null`). Ergebnis:

- `CoachWarning_Boundary` bleibt grün (2/2).
- Kontrolle: `NationalteamRunde13Teil2Tests.A028_CoachHaltsOnEveryWorldCupDay` wird rot (Saat 7). Die Mutation trifft also die Regel.
- Mit der Umkehrung (`… is null`) bleibt der Test ebenfalls grün. Der Halt am nächsten WM-Tag entsteht hier nicht über den Zug des
  Bundestrainers.

Die neue Zeile [`WettbewerbeNationalteamTests.cs:84`]($H/tests/EishockeyManager.UI.Tests/WettbewerbeNationalteamTests.cs#L84) prüft
damit nichts von A028. Abgedeckt ist A028 weiter durch `A028_CoachHaltsOnEveryWorldCupDay`.

Vorschlag: Den Punkt in #620 offen lassen (#620 bleibt ohnehin offen). Entweder einen Tag wählen, an dem Deutschland spielfrei ist,
und den Zug des Bundestrainers prüfen (`NationalTurn`). Oder die Zeile streichen und auf `A028_…` verweisen.

### R701-03 (P3): Zählerbezogene Zeilen der Ladeeigenheit ohne Test

#697 stellt sieben Stellen von `DayCounter.Of(…)` auf `Counter(…)` um:

- [`CupsNational.WorldCupLoop.cs:138`]($H/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L138) (Schritt c)
- [`GameState.DailyRun.cs:94/110/133/146`]($H/src/EishockeyManager.Core/GameState.DailyRun.cs#L94)
- [`MatchEngine.Injuries.cs:247`]($H/src/EishockeyManager.Core/MatchEngine.Injuries.cs#L247)

Je eine Rückmutation auf `DayCounter.Of`, mit der ganzen Core-Testsuite (Originaldaten), ergab:

| Mutation | Ergebnis |
|---|---|
| M07: Schritt c | grün |
| M08: `HealManagerSquads` | grün |
| M09: `ManagerSquadRun` | grün |

Das ist kein Produktfehler. Am Code ist die Umstellung folgerichtig: Das Original prüft in der WM-Schleife den Zähler 0x3B7, und der
ist nach dem Laden einen höher. Bei FILE44 sind in der Restsaison wohl weder Verletzte noch Takttage mit Wirkung sichtbar.

Vorschlag: In `LoadQuirkTests` nach dem Import von FILE44 eine Verletzung setzen und den Heiltag mit Versatz prüfen. Ebenso einen
Takt der WM-Schleife (Schritt c), etwa den Marktverfall von Manager 0.

Außerdem: Der Zweig `IsDriftDay(Counter(day))` in
[`GameState.LoadQuirk.cs:53`]($H/src/EishockeyManager.Core/GameState.LoadQuirk.cs#L53) ist nicht erreichbar. Der Import lässt nur den
13. April mit Zähler 218 zu ([`OriginalImport.cs:101`]($H/src/EishockeyManager.Core/OriginalImport.cs#L101)), und 218 mod 7 = 1.
M20 (Zweig auf `if (false)`) bleibt in der ganzen Core-Suite grün. Der Kommentar „für andere Markierungstage“ verspricht mehr, als
erreichbar ist. Hinweis genügt.

### R701-04 (P3): Wochentagskorrektur des Saisonendes hängt am Tag, nicht am Zähler

[`Season.cs:108`]($H/src/EishockeyManager.Core/Season.cs#L108) addiert den Montag-Versatz bei `day == SeasonEnd`. `DateText` übergibt
mit Ladeversatz aber `Day + 1`.

- Am Saisonende (Tag 231) ergibt das richtig „Montag, 30. April“ (`file46_2`).
- Ein Halt an Tag 230 mit Versatz zeigte dagegen „Montag, 29. April“. Das Original zeigt beim Zähler 232 „Sonntag“
  ((232 + 5) mod 7 = 6).

Nicht erreichbar mit FILE22 oder FILE44: Probe am Head, Import und `Continue()` bis zum Saisonende, beide Male ein einziger Halt am
Saisonende, kein Halt zwischen 15. und 30. April. Wird der Versatz auf weitere Stände ausgedehnt, die Korrektur an den Zähler hängen
(Wochentag aus `Counter`, wie 0x43B2E).

### R701-05 (P3): Reflexion auf private Namen in neuen Tests

Diese Tests rufen private Namen per Reflexion:

- `AutoLineup_OnlyOnTickOrEventDays` ([`PruefungKarteRunde14Tests.cs:101`]($H/tests/EishockeyManager.Core.Tests/PruefungKarteRunde14Tests.cs#L101):
  `MainPathEventDayLineups`, `MainPathDailyRun`)
- `Preparing_NextSlots_DoesNotAdvanceGameRandom` (`PrepareNextLeagueSlots`)
- `NationalMenu_Open_AllCoachTiles_DoNotChangeGameState` (`_nationalTurn` per `SetValue`)

Bei einer Umbenennung scheitern sie laut (`!` → `NullReferenceException`), nicht still. Bewertung: zulässig.

Ein Nachteil bleibt: Der dritte Test setzt den Zug des Bundestrainers an `PassNationalTurn` vorbei. Er prüft also einen Zustand, den
das Spiel so nicht herstellt (Manager am Zug ohne `BeginNationalCoachTurn`). Besser wären auf Dauer `internal`-Testhaken wie
`OriginalMinutes.FastCupCalls`.

### R701-06 (P3, Hinweis): Restabweichung April-Abschluss ist genau 115.000 DM

Probe `File44_SeasonEndScreen_…` am Head: Konto am Bildschirm „Saisonende“ 17.638.876 DM, `file46_2` 17.523.876 DM. Die Differenz
ist **genau** 115.000 DM.

Ein so runder Betrag spricht eher für einen einzelnen fehlenden Festposten des Monatsabschlusses als für Streuung der Gehälter. Das
`[APPROX]` ist akzeptiert (siehe 3.5). Für die Ursachensuche in #699 bzw. Folgeteil: Festposten im Monatsabschluss 0x3F833 gegen
`MonthlyBaseExpenseThirds` und Stadionunterhalt prüfen.

## 3. Ausgeführte Prüfungen

### 3.1 `tools/pruefen.sh` (alle drei Läufe) am Head

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2625 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2076 | 0 | 452 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2077 | 548 | 0 |

Commit 1ee008e, SDK 10.0.402. Ergebnis: bestanden. CI am Head: `build` success.

### 3.2 Gegenbeispiele

| ID | Ablauf | Ergebnis |
|---|---|---|
| R688S2-03 | `Schema45_WithDebtDaysAndDismissed_…`: Stand Schema 45 mit `DebtDays`/`Dismissed` lädt über 45 → 46 → 47, `Dismissed` bleibt, Spielende beim nächsten Tag | grün. Rot ohne `[46] = MigrateLadeeigenheit` (M03) und mit gelöschtem `Dismissed` in der Migration (M11) |
| FILE44 → FILE46 | Import FILE44 (Saat 4711) bis zur neuen Saison | Remake **−2.106.011 DM**, Original −2.139.514 DM (FILE46 15.887.669 DM). Wie im PR, innerhalb ±100.000 |
| R14KS9-03 | Kontoleiste: em.exe Datei 0x46533–0x4657A nachgelesen (x = 3 + 65 · k, Grundlinie 0xE5, Saldo-Farbe 7 − 0x17 = 240; Betrag 0xEC Datei 0x465B8) | stimmt. Test rot bei y 226 (Label und Saldo getrennt, U2/U2b) und bei Anlagen x 132 (U3) |
| R14UR-10 | Trennlinien: Datei 0x46386–0x463AB (0xB5–0x139), Markierung/Rücksetzen 0x46AFE–0x46BAF (0xB4–0x13A) nachgelesen; Bilder `file40_04`–`07` vermessen (Einträge 181–313, nach Markierung 180–314) | stimmt für die Eintragslinien. Kopflinie siehe R701-01 |

### 3.3 Besonders prüfen (1): Ladeeigenheit #697 in em.exe

Selbst nachgelesen mit `tools/reverse-engineering/disasm.py gamedata/em.exe …`:

- **Lader:** Datei 0x43A61 ruft 0x28165, Datei 0x43A67 sichert AL nach `[bp−3]`. Datei 0x43B05–0x43B09 übergibt ihn an 0x54C9F.
- **Tagesfunktion 0x54C9F:** `cmp byte [bp+6], 0` (Datei 0x54CA6). Bei ≠ 0 Sprung nach 0x54CEE (Spielstart 0x54CC4–0x54CE9
  übersprungen). Bei `[0xD0B9]` ≠ 1 weiter nach 0x54D3B.
- **Markierung des Tagesindex:** Datei 0x54D41 liest die Markierung von `[0xA969]`. 9 → 0, 0 bzw. 0xFF → `jmp 0x55073` (Datei
  0x54D5C/0x54D65).
- **Zweiter Tageswechsel:** Datei 0x55204 `add [0x3B7], 1`, dann 0x367B1 und je Manager 0x3ED01/0x3F833 (Datei 0x5522B/0x55230).
- **Rückkehr zum Menü:** Datei 0x552D5 Markierung 0xFF → weiter (sonst 0x56665). Datei 0x55332–0x5533E stellt bei `bp+6` ≠ 0 den
  Manager am Zug wieder her. Der Index zählt erst Datei 0x553A2.
- **Ende:** Saisonende Datei 0x26F55 `mov word [0x3B7], 0xE9`. In 0x26F20 gibt es keinen Aufruf von 0x367B1, das Fenster kommt über
  0x26D9A → 0x43E21. Das bestätigt auch A158, siehe 3.6.

**Gegenprobe ohne Versatz:**

- Alle geänderten Regeln addieren `LoadCounterOffset`. Der ist nur nach `ReopenMarkerDayAfterLoad` 1, und nur bei Index = Zähler − 1
  am 13. April.
- `OnlyMarkerDaySaves_GetTheOffset` zeigt, dass nur FILE22 und FILE44 den Versatz bekommen. Rot, wenn nur das Datum geprüft wird (M04).
- `NewGame_HasNoOffset` ist grün. `RngTwinProbeTests` und die Mehrsaisonproben haben unveränderte Sollwerte (nur Kommentare im Diff).
  Remake-Spiele laufen also unverändert.
- Der Schemafingerabdruck ändert sich nur um das Feld.
- Gegenprobe Versatz-Ende: Mit `EndLoadCounterOffset` als No-op werden `File44_Offset_…_FourSeasons` (beide) und
  `File44_SeasonEndScreen_…` rot (M02).

**Zeilen außerhalb der Dateigrenze:**

| Datei | Prüfung | Bewertung |
|---|---|---|
| `GameState.cs` | `DateText` + Versatz | Nebenwirkung nur mit Versatz, siehe R701-04 |
| `GameState.Integration.cs` | `EndLoadCounterOffset()` vor `StartFinanceSeason` | richtig |
| `GameState.Manager.cs` | `FinanceTarget(day)` | `FinanceInbox` nutzt `Math.Max(Day, FinanceDay)`; mit Versatz verfallen Meldungen einen Tag früher, passend zum Zähler |
| `GameState.DailyRun.cs`, `CupsNational.WorldCupLoop.cs`, `MatchEngine.Injuries.cs` | `Counter` | folgerichtig, ohne Test (R701-03) |

Nicht umgestellt sind `GameState.Integration.cs:108`, `IsDailyRunDay`, `TalentScout.IsTick` und `GameState.Manager.cs:170`. Sie
laufen nur im Hauptweg vor Tag 216 oder über `FinanceDay`, also unschädlich.

### 3.4 Besonders prüfen (2): Gegenproben je neuem oder geändertem Test

Die Mutationen liefen in einer Kopie, `dotnet test` mit Filter und `EM_REQUIRE_GAMEDATA=1`.

| Test | Mutation | Ergebnis |
|---|---|---|
| `LoadQuirkTests.File44_AfterImport_SecondDayChange_…` | M01 kein `ReopenMarkerDayAfterLoad`; M05 `DateText` ohne Versatz; M06 `FinanceTarget` = day | rot / rot / rot |
| `LoadQuirkTests.File44_Offset_…_FourSeasons` (2) | M01; M02 kein Versatz-Ende; M06 | rot / rot / rot |
| `LoadQuirkTests.OnlyMarkerDaySaves_GetTheOffset` | M04 Markierungstag nur am Datum | rot |
| `LoadQuirkTests.Schema46_WithoutOffset_LoadsWithZero` | M03 ohne `[46]` | rot |
| `LoadQuirkTests.NewGame_HasNoOffset` | – (Invariante, trivial) | – |
| `TagestaktSommerpauseReviewTests.File44_SeasonEndScreen_LoadQuirk…` | M01, M02, M05, M06 | rot (alle) |
| `TagestaktHauptwegReviewTests.File44_ToNewSeason_AccountFollowsFile46` (Sollwert jetzt Original −2.139.514) | M01; M06 | rot / rot. Ursache des neuen Sollwerts ist die Ladeeigenheit, ohne sie −1.617.704 DM laut PR; Diagnose hier −2.106.011 mit |
| `PruefungKarteRunde14Tests.Schema45_WithDebtDays…` | M03; M11 `Dismissed` gelöscht | rot / rot |
| `PruefungKarteRunde14Tests.ListedInjuredOwnPlayer_…` | M10 Markt-Platz-Heilung entfernt | rot |
| `PruefungKarteRunde14Tests.AutoLineup_OnlyOnTickOrEventDays` | M13a täglich aufstellen | rot |
| `IntegrationRunde9Tests.PlayoffWithoutManager_…` (Saat 5 → 3) | M14a Play-off immer Minuten; M14b nie | rot 2/2 / rot 2/2 |
| `SaisonwechselProbenTests.PlayoffWithoutManager_…` (Saat 5 → 3) | M14a; M14b | rot 6/6 / rot 6/6 |
| `NummerierungMigrationTests.CurrentSave_RoundTrip_Unchanged` | M15 Migration 38 → 39 auch auf aktuelle Stände | rot |
| `SaisonSatzfolgeSaisonendeTests.Preparing_NextSlots_…` | M16 `Random.Next()` in `PrepareNextLeagueSlots` | rot |
| `StatistikenPraemienTests.MultiSeason_FourChanges_…` | M17 Fair-Play-Gleichstand: letzter statt erster | rot |
| `NationalteamRunde14Tests.Day_MainPathOnly_…` (+ Saaten 7, 13, 17) | M19b Takt nach 0x952C in `OnDayManager` | rot für 7, 11, 13, 17, grün für 3 (wie im Kommentar) |
| `PruefungKarteRunde14UiTests.TransferList_Separators_…` | U1 immer lang; U1b nie lang | rot / rot |
| `PruefungKarteRunde14UiTests.TransferScreen_AccountLabels_…` | U2, U2b, U3 | rot (alle) |
| `BildschirmeFreipruefungTests.AllScreens_…` / `NationalMenu_Open_AllCoachTiles_…` | U4 `NationalSquadScreen.Update` ändert ein Konto | rot / rot |
| `SeasonEndColorsTests.Palette18_12_22_…` | U5 Index 18 → 238 | rot |
| `WettbewerbeNationalteamTests.CoachWarning_Boundary` | U6b alte Regel vor A028 | **grün** (R701-02) |
| `AutoLineupTeil8Tests`, `NationalCoachMode4Tests` (Saaten zurück auf 3/5, ausdrückliche Vorbedingungen) | Saaten wie vor L13, am Head grün | Rücknahme belegt, die Änderung aus L13 war nicht nötig |

Eine Mutation „0x952C im Tageswechsel vor dem Takt“ (M19) bleibt grün. Wie in R688N-03 beschrieben ist sie keine Umkehrung.

**Geänderte Sollwerte:**

- `ExpectedSchemaFingerprint` folgt aus dem neuen Feld `LoadCounterOffset`.
- Schema 47 folgt aus der Migration.
- Die Sollwerte in `TagestaktHauptweg`/`Sommerpause` gehen auf die Ladeeigenheit zurück (M01/M06 rot).
- Bei `RngTwinProbe` haben sich nur Kommentare geändert.

**Reflexion:** siehe R701-05.

### 3.5 Akzeptierte `[APPROX]`

- April-Abschluss des Remakes 115.000 DM unter `file46_2` (R701-06). Spanne 0 bis +130.000 DM.
- `ReopenMarkerDayAfterLoad`: keine Wiederholung von Neuaufstellung 0x426BC / 0x952C(m, 0) und der Rücksetzungen Datei
  0x550A0–0x55202 (im HOK schon enthalten).
- Kontoleiste: feste Einrückung des Betrags.
- Trennlinien vergessen den Zustand bei geänderter Liste.
- A166-Rückfall, `+0x09 & 3 = 3` meldet „verletzt“, Tag 0 (#699).

### 3.6 Besonders prüfen (3): Inventur und Statuswechsel (Stichprobe)

- **A158 `[ORIGINAL]`:** Datei 0x43B2E–0x43B48 nachgelesen: Wochentag = (`[0x3B7]` + 5) mod 7, Tabelle 0xAA0. Tag und Monat aus
  0x5A20/0x5A1C (Datei 0x43B75/0x43BA3). Letztes 0x367B1 nach Datei 0x55670 bei 0x5567D. 0x26F20 ohne 0x367B1, Fenster über
  0x26D9A (Datei 0x2700A). Bestätigt.
- **A383:** Datei 0x4509E–0x450A4 (`[0x7AE]` → `[0x518C]`/`[0x518D]`) nachgelesen, Zweig 0x45057–0x4509C für die Vereinsmanager.
  `NationalSquadLocked` und `NationalMenu.IsLocked` stimmen.
- **#598:** 0x44E22 ist der Funktionsanfang (Größe 3205), 0x44E3F ist `call 0x38:0x847`, 0x44E46 `mov [0xE1A4], al`. Bestätigt.
- **#619:** Datei 0x40E01–0x40E0B (nur 1 oder 2 → Meldung, 3 läuft weiter), Datei 0x27B96–0x27BD8 (Merker `[bp−0x18]`, Prüfung
  0x27BCF) und 0x426C4–0x426E4 (0x375 − 1 = 0xFF → Ende) nachgelesen. Bestätigt.
- **Inventurzeilen der Runde:** Stichprobe A012, A046, A082, A158, A383 gegen Code und Fundstelle, ohne Widerspruch.

### 3.7 Erwartet offen (bestätigt)

- #620 Rest (`Schema37Save_…`, `SponsorOfferTests.Rating_FreshClub_…`; dazu R701-02)
- #698 (offen)
- #699 (offen)
- `[APPROX]` April-Abschluss 115.000 DM (offen, R701-06)

### 3.8 Annahmen

- Die Saat 4711 im Import ist die der Tests. Die Restabweichung FILE44 → FILE46 gilt nur für diese Saat.
- Die DOSBox-Bilder sind 2×-Aufnahmen mit Rand (x = (px − 1) / 2, y = (py − 26) / 2). Abgeleitet aus den Trennlinien, die bei
  y 52 + 16 · k liegen.
