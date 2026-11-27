# Review PR #672 – Spur `nationalteam` Teil 4 (WM-Schleife ohne Bundestrainer, R14NT-N1, Rechtsklick Aufstellung)

**Reviewer:** Claude (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext (PR-Sitzung `…01GhACvUo…`,
diese Sitzung `…01GhACvSXo…`). **Base:** `fe09c20d75cb3f46d9578e27c7b6d0e9240ef41b` · **Head:**
`d57962a5c9da019d71cefbd6869bb1b1e163403e` · Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer (vor und nach den
Läufen). Gesamtes Delta gelesen (13 Dateien, +572/−90), keine Stichproben.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head. Der Kern von #664 (Schleife auch ohne Bundestrainer, kein Training/Markt/Wanderung/Talentsuche an
den übrigen Tagen, eigener Tagesabschluss) ist richtig gelesen und sauber angeschlossen. Falsch ist aber die als `[ORIGINAL]`
markierte Tagesauswahl für Schritt (c): Das Original rechnet 0x07169 nach den WM-Spielen der Remake-Tage **216, 220 und 230**
(dreimal), nicht 224/228 (zweimal) – der Tageszähler DGROUP 0x3B7 ist Remake-Tag + 1 (R14NT4-01, belegt mit Original-Spielständen).
Davon hängen die neuen Sollwerte (`−2 × Kosten`) und alle neu eingetragenen Fingerabdrücke ab.

## 2. Befunde

### R14NT4-01 (P1) – Schritt (c) an den falschen Tagen: Tageszähler = Remake-Tag + 1

[`CupsNational.WorldCupLoop.cs#L55`](https://github.com/kaaasi/eishockey-manager-redone/blob/d57962a5c9da019d71cefbd6869bb1b1e163403e/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L55)
prüft `MatchEngine.IsDriftDay(previous)` mit dem Remake-Tag der Spiele. Datei 0x5561A–0x5565C prüft aber DGROUP 0x3B7 (vor dem
Weiterzählen bei 0x55670), und 0x3B7 ist im Spielstand „Spielstart = 1“, also Remake-Tag + 1:

- `emtool hok dump FILE18.HOK 03B7` (14.04.1994 vor dem ersten WM-Spiel, Remake-Tag 216 = `SeasonCalendar.LayoutDate(216)`):
  `D9 00` = 217. FILE48 (16.04.): 219 (`DB`), FILE20 (28.04.): 231 (`E7`). Ebenso Vereinssaison: FILE24 (24.09.1993, Tag 14) = 15,
  FILE25 (27.09., Tag 17) = 18; FILE13 wurde für 31.01.1994 (Tag 143) mit `zaehler=144` erzeugt.
- WM-Spiele also bei 0x3B7 = 217, 219, …, 231; ≡ 0/4 (mod 7) sind **217, 221, 231** → Remake-Tage 216, 220, 230. An 224/228
  (0x3B7 = 225/229) läuft 0x07169 nicht.
- **Gegenbeispiel im Original:** Der Markt-Verfall „Platz +0x13 > 0x38 → −10“ steht in em.exe nur an Datei 0x071BB (Byte-Suche
  `26 80 7F 13 38`, einziger Treffer) und nur im Zweig 0xA70E = 0 von 0x07169 (Datei 0x0718F–0x071CC). FILE18 → FILE48
  (nur Weltkugel, ein WM-Spieltag 14.04.): alle 7 Marktspieler E −6/−7 (= −10 roh, E = +0x13·10/15) – **eine** Ausführung,
  die es nach dem PR-Modell (erst Tag 224) nicht gäbe. FILE19 → FILE20 (0x3B7 217 → 231): E −13/−14 = **zwei** Ausführungen
  (217, 221); die dritte (231) folgt erst nach dem gesicherten Stand. Der Modus ist für 0x5561A gleich (Modus 1 springt bei
  Datei 0x54D38 in dieselbe Schleife).
- Folge: Training/Heilung/Markt/Verträge/Auto-Aufstellung von Manager 0 dreimal je WM statt zweimal, an anderen Tagen;
  `[ORIGINAL]`-Texte („nur … Tage 224 und 228“, Kopf von `CupsNational.WorldCupLoop.cs`, `NationalteamRunde14Teil4Tests`,
  Kommentare an den Sollwerten in `GameModeTests`, `JugendTeamsTests`, `RngTwinProbeTests`, `SpielberechnungAnschlussTests`,
  `DebCupDrawFlowTests`) sind falsch; die Tests erwarten `−2 × CostPerTrainingDay`
  ([Teil4Tests#L129](https://github.com/kaaasi/eishockey-manager-redone/blob/d57962a5c9da019d71cefbd6869bb1b1e163403e/tests/EishockeyManager.Core.Tests/NationalteamRunde14Teil4Tests.cs#L129),
  L158, L198).
- Fix-Vorschlag: Bedingung auf den Zähler des Spieltags (`IsDriftDay(previous + 1)` o. ä., Begründung mit Fundstelle), Sollwerte
  `−3 ×`; Heilung in (c) (0x07C12 mit 0x3B7 − [0x6AF]) dabei gesondert betrachten, [0x6AF] ist ungelesen
  (`docs/spielberechnung.md` Z. 630).

### R14NT4-02 (P2) – „keine Talentsuche/Jugend in der Schleife“ ist von keinem Test abgedeckt

Mutation M5 (`AdvanceFinanceInWorldCupLoop(day)` → `AdvanceFinanceTo(day)`, also Talentsuche 0x06FA5 und Jugend 0x39DEC zurück in
der Schleife) lässt **alle** 21 neuen/geänderten Core-Tests grün, auch alle Fingerabdrücke. Die Kommentare an den neuen Sollwerten
nennen „keine Talentsuche“ als Ursache; das stimmt für keinen der Werte (die Rückführung in Teil 3 kommt ohne sie aus).
Regel selbst ist richtig (0x06FA5 und 0x39DEC haben nur den Aufrufer 0x54C9F, in 0x55570–0x55740 kein Aufruf). Bitte ein Test mit
laufender Talentsuche über den WM-Zeitraum und die Kommentare berichtigen.

### R14NT4-03 (P2, Nebenbefund außerhalb des Deltas) – derselbe Versatz im Hauptweg

Datei 0x55204 zählt 0x3B7 nach den Spielen weiter, Talentsuche/Wanderung/0x07169 (0x55253–0x552D3) prüfen den neuen Wert. Für
`OnDayClubSeason(e)` (vor den Spielen von Tag e) ist das 0x3B7 = e + 1; das Remake prüft `IsDriftDay(e)`
([`GameState.Integration.cs#L105`](https://github.com/kaaasi/eishockey-manager-redone/blob/d57962a5c9da019d71cefbd6869bb1b1e163403e/src/EishockeyManager.Core/GameState.Integration.cs#L105),
`Talent.IsTick`). Beleg: FILE24 (0x3B7 = 15) → FILE25 (18): Marktspieler E −6/−7, also 0x07169 bei 0x3B7 = 18 (vor Tag 17); das
Remake hat zwischen Tag 14 und 17 keinen Tick (erst Tag 18). Gehört zu `spielberechnung`/`kader-start`/`geld` – nur für die
Leitstelle.

### R14NT4-04 (P2, Nebenbefund) – Modus „nur Weltkugel“ läuft im Original durch dieselbe Schleife

Datei 0x54CEE–0x54D38: Modus 1 springt nach 0x553FF und damit in 0x55570. Schritt (c) wirkt dort nachweislich (Markt-Verfall
FILE18 → FILE48, siehe 01), ebenso (d) (0x3ED01 je Manager). Das Remake rechnet im Modus 1 keinen Schleifentag
(`OnDayManager`, `if (!WorldOnly)`); `WorldOnly_OneWorldCup_AutoOnOff` sichert „kein Training“ ab, belegt ist das nicht
(Konto im Dump FILE19/FILE20 je 500.000 DM, Training von DEUTSCHLAND nicht unterscheidbar von Spielabnutzung). Der Klassenkommentar
benennt den Sprung richtig; offen für die Leitstelle.

### R14NT4-05 (P3) – `[APPROX]` Markt/Verträge in (c) „für alle Manager samt Listenwechsel“: nicht belegbar, Abweichung bleibt

0x07169 arbeitet für einen Manager (Prüfung 0x07172; Gebote nur mit Bit `1 << [0xA70E]`, Datei 0x071D8–0x07202); 0x494EF
(Listenwechsel) hat nur die Aufrufer 0x437D5 und 0x54C9F (Hauptweg 0x54E7C), in 0x07169 kein Aufruf. Im Original bleibt die Liste
in der WM gleich (FILE19 → FILE20: dieselben 7 Spieler). `TransferMarket.CatchUp` an [L104](https://github.com/kaaasi/eishockey-manager-redone/blob/d57962a5c9da019d71cefbd6869bb1b1e163403e/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L104)
würfelt den Listenwechsel und Gebote für alle Manager. Als `[APPROX]` mit Begründung (Dateien `kader-start`) hingenommen; als
Restlücke für `kader-start` führen. Mutation M8 (Markt in (c) aus) fällt nur in Fingerabdrücken auf.

### R14NT4-06 (P3) – Zweig „Spieler ist / noch gesperrt !“ in `NationalLineupScreen` ungetestet

Mutation U5 (Bedingung an [L126](https://github.com/kaaasi/eishockey-manager-redone/blob/d57962a5c9da019d71cefbd6869bb1b1e163403e/src/EishockeyManager.UI/Screens/Competitions/NationalLineupScreen.cs#L126)
nie wahr) bleibt grün. Regel selbst nachgelesen und richtig (Datei 0x40B37–0x40B5D: +0x0D ≠ 0 und +0x09 & 3 ≠ 0; Texte DGROUP
0x1255 „Spieler ist“, 0x1251 „noch gesperrt !“; vorher 0x40ACF Automatik aus mit 0x1249/0x124D).

### R14NT4-07 (P3) – Wartung und Nebenbefunde

- `AdvanceFinanceInWorldCupLoop` ist eine Kopie von `AdvanceFinanceTo` ohne `DailyYouth`/`DailyTalentSearch` (sonst gleich, auch
  die Reihenfolge). Läuft auseinander, sobald #669 `AdvanceFinanceTo` ändert (im PR genannt).
- Vereins-Aufstellung `LineupScreen.cs` Z. 150–163: Rechtsklick auf einen Platz leert ihn weiter (`[APPROX]` „Prüfung fehlt“). Die
  hier gelesene Stelle 0x40364 ist derselbe Code: rechte Taste → 0x40D6B, Zeile 0x7F (0x41683 begrenzt x auf ≤ 0xE9, Datei
  0x41710–0x41736) → Bildschirm verlassen. Für die Spur der Vereinsaufstellung.
- `WorldCupPlayedOn`: Mutation M10 (Modusprüfung entfernt) überlebt – im Modus 2 gibt es keine WM-Spiele, die Prüfung ist
  gleichwertig zur Bedingung `WorldCup is not null`; kein Handlungsbedarf.

### Erwartet offen (bestätigt, nur benannt)

- #669 (`kader-start`): ändert `AdvanceFinanceTo` und fremde Sollwerte; die neuen Fingerabdrücke dieses PR werden beim Zusammenführen
  erneut fallen, die Kopie (R14NT4-07) muss mitgezogen werden.
- #670 (`hok-import`): ohne Merge kein Laden der Original-Stände im Remake; Teil 3 deshalb nur über `emtool hok dump`.

## 3. Ausgeführte Prüfungen

### Lauf `tools/pruefen.sh` (alle drei Läufe, Head, Originaldaten aus `kaaasi/eishockey-manager-referenz` `original/gamedata`)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2342 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1963 | 0 | 341 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1964 | 378 | 0 |

Commit `d57962a`, SDK 10.0.402, Ergebnis bestanden (deckt sich mit dem PR). Schema unverändert: `SaveGame.SchemaVersion = 41`,
`SaveGame*.cs` nicht im Delta.

### (1) Gegenprobe je neuem/geändertem Test

„Base“ = Base-Code mit den Testdateien des Heads (eigener Worktree). Mutationen in einer Kopie, je einzeln:
M1 Schleife nur mit Bundestrainer · M13 Weg mit Bundestrainer wie Base · M3 (c) ohne WM-Bedingung · M4 (c) für alle Manager ·
M6 keine Heilung in (d) · M7 Wanderung in der Schleife · M9 Markt täglich · M11 (c) für den letzten Manager · M12 keine Heilung
in (c) · M14 (c) am selben Tag · U1 Bildschirm wie Base · U2 ohne Meldung „Auto-Aufstellung“ · U3 Blockklick wählt keine Reihe ·
U4 Rücksetzen der WM-Seite auskommentiert (M5a) · U6 Rechtsklick auf Platz leert wieder.

| Test | rot durch |
|---|---|
| `ClubOnly_WorldCupPeriod_NoTrainingNoMarketNoDrift` (3, 11) | Base, M1, M3, M6, M7, M9 |
| `Mode4_NoCoach_TrainingOnlyManagerZeroAfterWorldCupDriftDays` (3, 11) | Base, M1, M3, M4, M7, M11, M14 |
| `Mode4_Coach_TrainingOnlyAfterWorldCupDriftDays` (3, 11) | Base, M13, M3, M7, M14 |
| `Mode4_FourSeasonChanges_WorldCupLoopWithAndWithoutCoach` (passiv / aktiv) | Base; passiv M1, aktiv M13; beide M3, M4, M7, M11, M14 |
| `WorldOnly_OneWorldCup_AutoOnOff` (an / aus) | keine (Regressionsschutz, am Base grün – so im PR angegeben) |
| `RngTwinProbeTests` (6 Abdrücke) | M1 (alle 6), außerdem M3, M7, M8, M9, M14; teils M6, M12 |
| `SpielberechnungAnschlussTests.…CountersPerSeason` (2) | M1, M3, M6, M7, M8, M9, M12, M14 |
| `GameModeTests.Mode4_ReferenceValues_Unchanged` | M1, M3, M4, M6, M7, M8, M9, M11, M12, M14 |
| `JugendTeamsTests.FourSeasonChanges_StrengthFollowsFunding` aktiv / passiv | aktiv M1, M6; passiv M13; beide M3, M7, M8, M9, M14 |
| `DebCupDrawFlowTests.MultiSeason_CupResultsUnchanged_WithAndWithoutDisplay` | M1 |
| `NationalteamRunde14UiTests.CoachMenu_WorldCupPairings_SeasonChange_ResetsRememberedGroup` | U4 (am Base grün, R14NT-N1 ist nur ein Test) |
| `NationalteamRunde14UiTests.NationalLineup_RightClickOnSlotLeaves_LeftClickClears` | Base, U1, U2, U3, U6 |
| `NationalLineupScreenTests.Lineup_Clicks_FollowOriginalRules_AndTacticsBelongToNationalTeam` | Base, U1, U3 |
| `NationalLineupScreenTests.A010_FullLine_NoRoom_ClickOnPlacedTakesOut` | U3 (am Base grün) |

Überlebende Mutationen: M5 (Talentsuche/Jugend zurück, R14NT4-02), U5 (Sperrmeldung, R14NT4-06), M10 (gleichwertig).

### (2) Rückführung der Sollwerte und Fingerabdrücke

- M1 allein: alle 8 Hex-Abdrücke (`RngTwinProbeTests` 6, `SpielberechnungAnschlussTests` 2) zeigen exakt die Base-Werte
  (z. B. `EB4E4CBD6E34161E` → `1501663F0197426F`, `8FE6B0F5FA66E66F` → `FB60D817157E2013`). M1 mit den Base-Sollwerten für
  RngTwin, Spielberechnung, `Mode4Reference`, `DebCupDrawFlowTests.Reference` und `JugendTeamsTests.ExpectedActive` (passiv
  Head-Wert): alle grün.
- M13 mit Base-`ExpectedPassive`, sonst Head-Werte: alle grün → `ExpectedPassive` hängt allein am Weg mit Bundestrainer.
- M1 + M13 mit allen Base-Sollwerten: alle 11 Core-Tests und `DebCupDrawFlowTests` grün → jede Änderung ist auf die Schleife
  zurückgeführt, wie im PR angegeben. Wegen R14NT4-01 sind die neuen Werte trotzdem nicht die des Originals.

### (3) Original-Spielstände (`original/spielstaende/`, `emtool hok dump`, Blöcke `03B7`, `A969`)

Im WM-Zeitraum gibt es nur Paare im Modus 1 (FILE18 → FILE48, FILE19 → FILE20); FILE22/FILE44 (Modus 4, 14.04.) haben ihr nächstes
Gegenstück erst nach dem Saisonwechsel (FILE23/FILE46), dort ist der WM-Zeitraum nicht zu trennen.

| Paar | Zähler 0x3B7 | Beobachtung | Remake (Head) |
|---|---|---|---|
| FILE18 → FILE48 | 217 → 219 | Markt: 7× E −6/−7 (eine 0x07169-Ausführung); Liste gleich; Konto gleich; 2 Kaderwerte −1 nach dem Spiel | keine (c)-Ausführung vor Tag 224 – **weicht ab** (R14NT4-01) |
| FILE19 → FILE20 | 217 → 231 | Markt: 7× E −13/−14 (zwei Ausführungen), Liste gleich (kein Listenwechsel), Konto gleich | Modus 1 rechnet keinen Schleifentag (R14NT4-04); in Modus 2/4 zwei Ausführungen an anderen Tagen |
| FILE24 → FILE25 (Vereinssaison) | 15 → 18 | Markt: E −6/−7 (0x07169 bei 18) | kein Tick zwischen Tag 14 und 17 (R14NT4-03) |

Verletzungen, Verträge und Finanzen lassen sich mit diesen Paaren nicht getrennt prüfen (Konto im Modus 1 unverändert, keine
Verletzten im Zeitraum).

### (4) em.exe selbst nachgelesen (`tools/reverse-engineering/disasm.py`, `--xref 0xA70E 0xA70E`)

- 0x553FF–0x554CF: Ernennung nur bei Modus ≠ 2 (0x55436) und Kennzeichen je Manager (0x55446); 0x554CF: ohne Bundestrainer
  (0x9EDA = 0xFD) Würfeln 0x554D9–0x5556D, beide Wege nach 0x55570 – bestätigt.
- 0x55570–0x55740 Schritt (a)–(d) wie im Klassenkommentar – bestätigt, bis auf die Tagesbedingung (R14NT4-01). (a) 0x952C mit
  (5, 0) (Push-Reihenfolge 0, dann 0xA70E = 5) bestätigt.
- 0xA70E nach 0x30E7B: Schreiber im Spielpfad nur 0x36B5D (0x36B7A := 0), 0x35818 (0x35ABC := Feld +0x16, bei 0 Sprung 0x35AC3 →
  0x35C1C ohne `dec`, sonst 0x35AC6 −1) und 0x38B8F (sichert und stellt wieder her, 0x38D36/0x38D75) – Aussage „nur Manager 0,
  mit Bundestrainer ggf. 5“ bestätigt.
- 0x07172–0x0717B (0xA70E ≥ Managerzahl → 0x07C0C), 0x07B4E (0x07C12), 0x07B5E (0x0659C), 0x07C07 (0x426BC) – bestätigt.
- Keine Aufrufe von 0x9D1B, 0x06FA5, 0x39DEC, 0x494EF in der Schleife; deren Aufrufer sind 0x54C9F (und 0x437D5) – bestätigt.
- 0x40886–0x4088D, 0x40AA3–0x40B5D, 0x40D6B–0x40D78, 0x41710–0x41736 (Zeile 0x7F außerhalb x ≤ 0xE9) – bestätigt.

### (5) Mehrsaisonprobe, Modi

- `Mode4_FourSeasonChanges_…` (4 Wechsel, passiv/aktiv, Speichern/Laden) am Head grün; WM ohne Bundestrainer: `ClubOnly_…` und
  `Mode4_NoCoach_…` grün; „nur Weltkugel“ Automatik an/aus grün.
- Eigene Probe (nicht committet): Modus 4, Manager 0 in der 2. Liga, Manager 1 bei Verein 2 und Bundestrainer – (c) trainiert
  Manager 0 (am Head zweimal), Manager 1 nie (Seeds 3, 11 grün); entspricht 0x55494 (Feld +0x16 = 6 unabhängig vom Manager).

### AGENTS.md §4, Dateigrenzen

- Zeit nur über `Continue()`: Anschluss allein in `GameState.Integration.cs` (`OnDayClubSeason`), Tagesabschluss über das vorhandene
  `FinanceDay`, kein neuer Nachhol-Zähler; `CatchUpTraining` hat keinen weiteren Aufrufer. Training über `GameState.Book`.
- Bildschirm ändert nur auf Klick (Aufstellung leeren, Automatik aus); Öffnen ändert nichts.
- Dateien: `Core/CupsNational*.cs`, `NationalLineupScreen.cs` allein `nationalteam`; `GameState.Integration.cs` gemeinsam, eine
  Stelle mit Begründung im PR; fremde Tests nur Sollwerte mit Kommentar; `docs/arbeitspakete.md` nur eigener Abschnitt.

### Akzeptierte `[APPROX]` (getrennt von Annahmen)

- (d) Heilung für alle Spieler wie `HealInjuries` – nicht belegbar (Original: nur Kaderplätze der Manager, 0x556D2–0x55712);
  gleich dem Hauptweg des Remakes, hingenommen.
- (c) Markt/Verträge für alle Manager samt Listenwechsel – nicht belegbar, Abweichung (R14NT4-05), hingenommen als Restlücke.
- (c) mit Bundestrainer immer Manager 0 – hingenommen.
- Nicht hingenommen: die `[ORIGINAL]`-Angabe der Tage 224/228 (R14NT4-01).

**Annahmen:** Der Markt-Verfall bei 0x071BB ist die einzige Ursache der E-Änderung der Marktspieler (Byte-Suche ohne weiteren Treffer,
kein täglicher Verfall: FILE19 → FILE20 nur −20 in 14 Tagen).
