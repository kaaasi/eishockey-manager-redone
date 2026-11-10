# Review: Spur tagesablauf – Restlücken Tagestakt (PR #725, #699 Punkte 1–3)

Unabhängiges Erstreview. Base `3af4ec9ba3b530ee2efb81ab4849e4d81eaa9b8e` (= `main`), Head `59d72d19c4a75372a29eee2079b6400caf79286d`.
Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 10.11.2026.

Checkout: Klon, `git rev-parse HEAD` = `59d72d1`, `git status --porcelain` leer. Originaldaten aus `kaaasi/eishockey-manager-referenz`
`303db8c` (`original/gamedata/`, `original/spielstaende/`). Rückschaltproben und Mutanten nur in einem eigenen Worktree desselben Heads,
nichts committet.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/59d72d19c4a75372a29eee2079b6400caf79286d`

## 1. Merge-Empfehlung

**Nein** für Head `59d72d1` – nur wegen R725-01 (P2, ein fehlender Regressionstest). Der Fix ist ein Test, keine Codeänderung.

- Die Umsetzung stimmt mit em.exe überein (Tag 0, Hot-Seat-Würfe, Bundestrainer vor den Menüs; selbst nachgelesen, siehe 3).
- **R15-E1 = A ist eingehalten.** Jede Anpassung fremder Tests ist am Test begründet. Die Rückschaltprobe gibt für alle geprüften
  Fälle die alten Werte zurück. Kein Sollwert wurde nur an den neuen Code angeglichen. Zwei Prüfungen sind dabei schwächer
  geworden (R725-03, R725-04). Das blockiert nicht.
- Alle drei Läufe von `tools/pruefen.sh` sind grün. Mehrsaisonprobe mit Tag 0 (passiv/aktiv, 4 Wechsel) vorhanden und grün.
- **R725-01 (P2):** Der Weg, über den Spieler die Saison wechseln, öffnet Tag 0 erst in `ReturnToFirstActiveManager`.
  Kein Test sichert ihn ab, der Mutant überlebt alle 2474 Tests.

## 2. Befunde

**R725-01 (P2) – Tag 0 über die Oberfläche ungesichert.** Der Saisonwechsel aus `SeasonEndSummaryScreen` läuft über
`StartNextSeasonWithRenewals` (dort kein Tag 0, `s_deferRenewals`) und danach über `ReturnToFirstActiveManager`, das `OpenStartDay`
ruft ([H/src/EishockeyManager.Core/GameState.SeasonEndTurn.cs#L44](https://github.com/kaaasi/eishockey-manager-redone/blob/59d72d19c4a75372a29eee2079b6400caf79286d/src/EishockeyManager.Core/GameState.SeasonEndTurn.cs#L44)).
Kein Test prüft, dass danach `DayOpened` gilt. `TagestaktHauptwegReviewTests` prüft an der Kopie nur `Assert.False(copy.DayOpened)` vor
dem Rücksprung. Die Mehrsaisonprobe nimmt `StartNextSeason`.
Beleg (Mutant: Zeile 44 durch einen Kommentar ersetzt):
`EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests` → 1798/0; `… tests/EishockeyManager.UI.Tests` → 676/0. Beide
Läufe sind grün. Ohne die Zeile holt `ProcessDay` den Tageswechsel erst nach dem ersten Menü nach. Das ist genau das alte Verhalten
#699 Punkt 2, und zwar im eigentlichen Spielweg.
Vorschlag: In `TagestaktHauptwegReviewTests` (Kopie) oder `TagesablaufRunde15Tests` nach `StartNextSeasonWithRenewals()` noch
`ReturnToFirstActiveManager()` aufrufen. Danach prüfen: `DayOpened`, `FinanceDay == 0` und Bewertung = `SponsorRating.Compute`. Am besten
über mehrere Saisonwechsel, Mutant → rot.

**R725-02 (P3) – Abnahme mit Originaldaten ist kein Regressionstest.** `Original_DayZero_OpenedWithDayClose` (FILE01/10/14/27)
und `Original_File02ToFile03_OnlyGateReceipts` sind auch mit den Teil-1-Quellen von `4194e9d` grün. Beleg:
`git checkout 4194e9d -- <4 Teil-1-Quellen>; dotnet test … --filter FullyQualifiedName~TagesablaufRunde15Tests.Original` → 5/5 grün.
Das Rot der Spur kommt nur aus den synthetischen Tests (6 rot, wie im PR angegeben). Der Grund: Der Import setzt Zinskonto und
Bewertung aus dem HOK (`ApplyOriginal` nach `BeginFirstTurn`). FILE02→03 hat Sparbuch 0, das Zinskonto ist dort also ohnehin 0.
Die Abnahme „FILE01 (Zähler 1)“ belegt deshalb nur die Lesart des Zählers, nicht den Code. Für die Inventur bitte so führen.

**R725-03 (P3) – `NationalteamRunde14Tests`: der Zweig „ohne Ereignis“ ist tot.** Der umbenannte Test
`…_EventDayHaltShowsLineupForm` läuft nie in den Zweig mit gewürfelter Form 45–55 bzw. dem Vergleich am Tag ohne Wanderung. Beleg:
`Assert.Fail` vor `Assert.InRange(defense.Form, 0, 44)` eingefügt → 5/5 grün. Ein Halttag ist immer ein Spieltag und damit ein Tag
mit Ereignis. Der Kommentar „gewürfelte Form bleibt an Halttagen ohne Ereignis sichtbar“ wird also nicht geprüft. Die
Erwartung selbst stimmt mit der Reihenfolge im Original überein (0x55204–0x552D3 → 0x54D41 → 0x54DAA vor 0x54EB2). Der tote Zweig
gehört mit Mutante N33 zu `pruefung-karte` (erwartet offen, bestätigt). Wanderung und Würfelform des Vortags sind damit hier nicht
mehr abgedeckt.

**R725-04 (P3) – `SponsorTests.Offers_AtStart…` prüft die Startbewertung nicht mehr unabhängig.** Die Bewertung der Hüllkurve kommt jetzt
aus `ads.Offers…First().Rating`. Damit ist nur noch geprüft, dass alle Angebote dieselbe Bewertung haben und die Beträge dazu passen.
Dass das die Bewertung beim Start ist (0x97CE, vor dem Tagesabschluss zu Tag 0), prüft der Test nicht mehr. Die Begründung ist
richtig, weil die aktuelle Bewertung jetzt die von Tag 0 ist. Vorschlag: Die Startbewertung vor `BeginFirstTurn` festhalten oder
gegen `SponsorRating.Compute` auf dem Startzustand prüfen. Nicht blockierend.

**R725-05 (P3) – veralteter Satz.** Der erste Satz im Summary von `DayOpened`
([H/src/EishockeyManager.Core/SeasonFlow.cs#L180-L186](https://github.com/kaaasi/eishockey-manager-redone/blob/59d72d19c4a75372a29eee2079b6400caf79286d/src/EishockeyManager.Core/SeasonFlow.cs#L180-L186)) lautet weiter „ein neues
Spiel und der erste Tag einer Saison holen ihn vor den Spielen des Tages nach“. Das widerspricht dem neuen `[ORIGINAL]`-Absatz und
`docs/tagesablauf.md` („nur Spielstände vor Schema 45“). Außerdem setzt Datei 0x565A7/0x43A93 den Zähler auf `[0x6AF]` und nicht auf
die Konstante 0. Das Ergebnis ist gleich, weil `[0x6AF]` = 0 ist (Review #688; FILE14: Zähler 1). Die Fundstelle sollte das so sagen.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe, Erstreview), SDK 10.0.402, Commit `59d72d1`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2641 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2087 | 0 | 454 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2088 | 553 | 0 |

**Rückschaltprobe (R15-E1 = A), selbst ausgeführt** (`EM_REQUIRE_GAMEDATA=1`, Worktree auf Head):

| Probe | Ergebnis |
|---|---|
| Teil-1-Quellen (`GameState.MainMenu.cs`, `GameState.Manager.cs`, `GameState.SeasonEndTurn.cs`, `SeasonFlow.cs`) und 8 fremde Testdateien von `4194e9d`; Filter: `RngTwinProbeTests.FourSeasonChanges` (6), `Mode4_ReferenceValues_Unchanged`, `SponsorTests.Offers_AtStart`, `FinanceMonthlyTests.CreditInterest`, `TagestaktHauptwegReviewTests.FourSeasonChanges_TwoManagers` (2), `JugendTeamsTests.FourSeasonChanges` (2), `SpielberechnungAnschlussTests.FourSeasonChanges` (2), `CatchUpManagerDay` | 18/18 grün = alte Werte |
| Teil-1-Quellen zurück, Testdateien vom Head (dieselben Filter + `TagesablaufRunde15Tests`) | 21 rot: alle oben außer `CatchUpManagerDay` (robuster gemacht) + 6 Tag-0-Tests; 13 grün (HotSeat, Bundestrainer, `Original_*`, R725-02) |
| UI: Teil 1 zurück + alte `LifecycleScreenRegressionTests`, `KaderTransferTeil3ScreenTests` | 2/2 grün; mit Head-Testdateien R204 rot, `ComputerMarketLoan` grün (Saatenschleife) |
| Teil-3-Quellen (`CupsNational.AutoLineup.cs`, `GameState.DailyRun.cs`, `GameState.Integration.cs`) + alte `NationalteamRunde14Tests` | 5/5 `…_DriftDayShowsRolledForm` grün, nur `NationalCoach_EventDayLineup_BeforeMenus` rot |
| Teil 3 zurück, Head-Testdatei | 6/6 rot (5 × `…_EventDayHaltShowsLineupForm` mit Saaten 3, 7, 11, 13, 17 + `NationalCoach_…`) |

Die Proben decken die Fingerabdrücke aus `RngTwinProbeTests` und den Fall aus `NationalteamRunde14Tests` ab. Bei den robuster
gemachten Tests (`CatchUpManagerDay`: „mindestens einmal ein Gebot“, Verkäufer jetzt je Schritt geprüft; `ComputerMarketLoan`: Saaten
1993–1997) kommt die Abschwächung aus dem Zufall und nicht aus dem Sollwert. Das ist vertretbar.

**Weitere Mutanten:** In `PassTurn` ohne `RollListChangesThrough` werden 3/3 `HotSeat_ListChangeRollOfSecondManager_AfterFirstMenu`
rot. `DailyYouth(0)` in `ProcessDayCore` entfernt → 2 rot (`JugendTeamsTests.FourSeasonChanges_*`). `OpenStartDay` in
`ReturnToFirstActiveManager` entfernt → 0 rot (R725-01).

**Tag 0 gegen FILE01/FILE02→FILE03:** Am Head grün. FILE01 hat Block 03B7 = 1. FILE02→03 bucht nur Eintritt
(237.354 = 13.962 · 17 DM), HOK 0x02A0 = 0. Zur Aussagekraft siehe R725-02.

**em.exe selbst nachgelesen** (`tools/reverse-engineering/disasm.py gamedata/em.exe …`, Ausgaben nicht im Repo):
- Datei 0x54D41–0x54D65: Bei einem Tagesbyte `[0xA969]+0x828` von 0, 9 oder 0xFF geht es nach 0x55073. Neuaufstellungen, Würfe und
  Menüs entfallen dann. Datei 0x565A1 setzt den Index auf 0 und 0x565B0 den Zähler auf `[0x6AF]`. Index 0 springt also ohne Menü
  zum Tageswechsel 0x55204. Dort folgt `add [0x3B7],1`, je Manager 0x3ED01 (0x5522B) und 0x3F833 (0x55230), danach Takt 0x552A6
  bei Zähler ≡ 0/4 und `inc [0xA969]` bei 0x553A2. Danach kommen Index 1, Neuaufstellungen und das erste Menü. Das stimmt mit
  `OpenStartDay` überein (Remake-Tag 0 = Index 1 = 10. September).
- Datei 0x55008–0x5502C (Jugend 0x39DEC) liegt nach den Menüs und vor 0x55073. Für Index 0 wird sie übersprungen. Die Jugend von
  Remake-Tag 0 fällt deshalb nach dem ersten Menü. `DailyYouth(0)` in `ProcessDayCore` stimmt damit.
- Datei 0x54D68–0x54DBA: Die Schleife läuft über die Sätze 0…5. Satz 5 nur bei `[0x9EDA] < 0x3F`. Je Satz wird 0x375 aus 0x376
  übernommen, danach folgen 0x426BC und 0x952C(Satz, 0). Teil 3 ist damit `[ORIGINAL]`. Für die Sätze 0…4 bleibt 0x952C(Satz, 0) wie im
  PR angegeben `[APPROX]`.
- Datei 0x54E3F–0x54EF4: Je Platz 0…`[0x386]`−1 wird der Wurf 0x54E65–0x54E7C gemacht (random(0, n+2) = 0 → 0x494EF). Danach folgt
  das Menü 0x54EB2, aber nur bei Tagesbyte & 7 oder Merker `[bp−0x1A]`. Der Wurf hängt nicht davon ab, ob der Manager entlassen ist.
  Das bestätigt Teil 2 und das `[APPROX]` für entlassene Manager.

**Erwartet offen, bestätigt:** `[APPROX]` Jugend ab Tag 1 vor dem Menü (Original 0x55008 nach 0x54EB2). Testname
`…_EventDayHaltShowsLineupForm` und Mutante N33 gehen an `pruefung-karte`, siehe auch R725-03.

**Akzeptierte `[APPROX]`:** Jugend ab Tag 1 im Tageswechsel. 0x952C(Satz, 0) der Managersätze nicht nachgebildet. Entlassene Manager
würfeln mit dem nächsten aktiven Zug. FILE02→03 nicht kontogenau (R15-D2).

**Annahmen (nicht geprüft):** Die Reihenfolge der DEB-Pokal-Auslosung beim Saisonwechsel relativ zu Tag 0 ist nicht geprüft. Die
Auslosung liegt im Kern vor dem Bildschirm, der Bildschirm zeigt nur an. Ebenso ungeprüft ist die Wirkung auf Spielstände, die
während der Vertragsverhandlungen am Saisonende gesichert werden. Einen solchen Speicherweg habe ich nicht gefunden.
