# Review: PR #452 – Integration Runde 11, Spur `geld`

- PR: #452 · Base `02c4bd74d613267626fd0f09712d23f97e262d47` (`main`) · Head `67bae96673a5bd0a838d90d573bd72b008867faa`
- Umfang: Stapel #417 → #429 → #433 → #436 → #442 → #448 → #451 und Leitstellen-Commit `67bae96` (54 Dateien, +3613/−757)
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext)
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; gebaut und getestet (nicht nur Codelesen).
  Mutationen in einem eigenen Worktree desselben Commits, nach jedem Lauf zurückgesetzt.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Alle drei Läufe grün mit denselben Zahlen wie im PR; 39 von 44 Gegenproben werden rot (eine weitere grüne ist gleichwertig).
Die Tabellen und Regeln, die ich gegen `em.exe` nachgelesen habe (Kosten DGROUP 0xC0, Bauzeiten 0xEC/0xED, Wochenschritt
0x3D316, Ø-Platz 0x3F915–0x3F98E, Jugend-Saisonwechsel 0x3A13B, Bezugsstärke 0x4EBA, Teiler 154 bei 0x59A15, Knopf
„Talent“ 0x46704/0x468C1), stimmen mit dem Code überein. Ein echter Spielstand mit Schema 22 vom Base (laufender
Sitzplatzausbau, laufende Talentsuche, Jugendförderung) lädt am Head richtig, übersteht den Rundlauf und läuft bis in die
nächste Saison weiter. Echte Ausgaben laufen über `Book`. Offen sind nur Testlücken (P3): Bei drei Regeln bleibt die
Mutation grün, das Verhalten selbst stimmt aber.

## 2. Befunde

### R11G-01 (P3) – Bauende-Woche des Stadionausbaus ist durch keinen Test festgelegt

- Stelle: [FinanceStadium.cs:295](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/src/EishockeyManager.Core/FinanceStadium.cs#L295).
  Der Code stimmt mit 0x3D316 überein: bei 0 überspringen, `dec`, bei 0 fertig (Datei 0x3D340–0x3D365).
- Gegenbeispiel: `--_weeksLeft[p] > 0` → `> 1` (eine Woche früher fertig). Danach sind alle 39 Stadion-Tests (`FullyQualifiedName~Stadium`)
  weiterhin **grün**. Die Zusammenfassung von
  [StadiumAusbauTests.cs:124](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/tests/EishockeyManager.Core.Tests/StadiumAusbauTests.cs#L124)
  verspricht „fertig genau nach so vielen Bauwochen“, der Test prüft das aber nicht.
  Die Gegenmutation `Begin(...) = weeks + 1` wird rot, allerdings nur über die Spanne ±2.
- Vorschlag: In `WeeksLeft_WithinQuotedTimePlusMinusTwo` `left − 1` Montage vorspulen → Stufe unverändert, ein weiterer
  Montag → Zielstufe und genau eine Meldung.

### R11G-02 (P3) – Bauzeittabelle nur zu 5 von 11 Projekten getestet

- [StadiumAusbauTests.cs:121](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/tests/EishockeyManager.Core.Tests/StadiumAusbauTests.cs#L121):
  Getestet werden nur Überdachung, Zustand, Verkehrsanbindung, Sitz- und Stehplätze. Gegenbeispiel: Anzeigetafel `(12, 17)` → `(12, 18)` bleibt
  grün. Die Tabelle selbst habe ich gegen die Datei geprüft (DGROUP 0x16DD0 + 0xEC: `(20,30) (12,17) (16,25) (6,9) (5,8)
  (40,60) (17,24) (14,18) (10,15) (12,17) (10,14)`); dasselbe gilt für die Kosten 0xC0 / 3. Vorschlag: alle 11 Zeilen als
  `InlineData`.

### R11G-03 (P3) – Jugend: Platzverlauf mit festem Zufall nicht als Sollwert festgehalten

- Laut Abnahme der Spur (`docs/arbeitspakete.md`) ist ein „Platzverlauf mit festem Zufall über 4 Saisonwechsel“ zu liefern.
  [JugendTeamsTests.cs:143](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/tests/EishockeyManager.Core.Tests/JugendTeamsTests.cs#L143)
  prüft nur `Run() == Run()` (Determinismus) und die Richtung der Stärke.
- Gegenbeispiel: [FinanceYouth.cs:208](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/src/EishockeyManager.Core/FinanceYouth.cs#L208)
  `R(0, 8)` → `R(0, 7)`. Danach bleiben alle 41 Jugend-Tests (`~Jugend|~Youth`) grün. Rot werden erst 9 globale Abdrücke (`RngTwinProbeTests`,
  `GameModeTests.Mode4`, `SpielberechnungAnschlussTests`), aus denen sich die Ursache nicht ablesen lässt.
- Vorschlag: In der Mehrsaisonprobe das Protokoll `log` als Sollwert festhalten (Kommentar: Abdruck der Spur geld Teil 3).

### R11G-04 (P3) – Doku von `AnswerConstruction` verspricht eine Kontoprüfung, die es nicht gibt

- [FinanceStadium.cs:486](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/src/EishockeyManager.Core/FinanceStadium.cs#L486):
  Laut Doku liefert die Methode „false, wenn … Konto zu klein“. Geprüft werden aber nur das Projekt (noch frei?) und die Kosten. Im
  Bildschirm ist die Rückfrage modal, dort ändert sich also nichts. Nur ein Aufrufer, der zwischen `QuoteConstruction` und `AnswerConstruction` Geld
  ausgibt, kann ins Minus bauen. Vorschlag: Doku an den Code anpassen. Prüft das Original bei „Ja“ nicht noch einmal, ist das dort
  `[ORIGINAL]`.

### R11G-05 (P3, nur Hinweis) – `BoerseStartTests` rechnet die Gebühr mit derselben Formel wie der Code

- [BoerseStartTests.cs:261](https://github.com/kaaasi/eishockey-manager-redone/blob/67bae96673a5bd0a838d90d573bd72b008867faa/tests/EishockeyManager.Core.Tests/BoerseStartTests.cs#L261)
  nimmt den Sollwert aus `FinanceRules.StockFee`. Gegenbeispiel `StockFeePerMille = 0` bleibt dort grün (16/16). Die Gebühr
  ist trotzdem festgelegt: `FinanceBankBoerseTests` rechnet mit `value * 15 / 1000` als Zahl (Mutation 15 → 10: 5 Tests rot),
  die Ganzprozent-Regel für den Rest legt `BoerseStartTests` fest (Mutation „Rest anteilig genau“: `MultiSeason_FourChanges_PassiveAndActiveManager`
  rot, Seeds 7 und 8). Nachgerechnet: Erlös `w − ⌊15w/1000⌋`, abgehender Kaufpreis `K − ⌊K · ⌊(n − v) · 100 / n⌋ / 100⌋`.
  Das entspricht der beschriebenen Regel 0x5915F–0x5952D und dem Code in `FinanceStocks.cs`. Kein Handlungsbedarf.

### R11G-06 (P3, Prozess) – Tagesanschlüsse der Spur in `GameState.Manager.cs`

- `DailyYouth`, `DailyTalentSearch`, `DailyStadium` und `YouthSeasonChange` hat die Spur selbst in die bestehende tägliche Finanzschleife
  bzw. den Saisonwechsel in `GameState.Manager.cs` gehängt. Laut Plan sollte der Jugend-Saisonwechsel „Anschluss über die
  Leitstelle“ sein. AGENTS.md §4 nennt für Tagesanschlüsse `GameState.Integration.cs`. Sachlich ist das richtig (dieselbe
  Schleife, die vorher `AdvanceTalentSearch` rief; Reihenfolge wie 0x55008/0x54C9F/0x3ED19 beschrieben) und ohne fremde
  Dateien. Nur für den Integrationsabschnitt erwähnen.

Keine P0–P2.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe) am Head `67bae96`, SDK 10.0.402

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1712 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1427 | 0 | 256 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1427 | 285 | 0 |

Ergebnis: bestanden. CI (`build`) am Head grün.

### Gegenproben (Mutation → gezielter Testlauf, `EM_REQUIRE_GAMEDATA=1`)

| ID | Mutation | Ergebnis |
|---|---|---|
| G1 | Kosten Verkehrsanbindung 1,3 → 1,2 Mio. | rot (`FirstStep_CostsAsInVideo`) |
| G1 | Restwochen `random(0,4) − 2` → `− 1` | rot (2) |
| G1 | Bauende eine Woche früher (`> 0` → `> 1`) | **grün** → R11G-01 |
| G1 | Restwochen bei Beginn +1 | rot (`WeeksLeft_WithinQuotedTimePlusMinusTwo`) |
| G1 | Kosten steigend je Stufe statt linear | rot (3) |
| G1 | Kontoprüfung 0x3E107 entfernt | rot (`Quote_NeedsMoneyForFullCost`) |
| G1 | Bauzeit Anzeigetafel (12,17) → (12,18) | **grün** → R11G-02 |
| Sponsor | Summe 0 → 6 auf 5 | rot (6) |
| Sponsor | Fenster 5 → 4 Felder | rot (`AverageRank_LastFiveFields_IntegerDivision`, `Rating_UsesRanksOfCurrentSeason`) |
| Sponsor | Teiler ohne übersprungene Felder (n ≤ 5) | rot (2) |
| Sponsor | Feld 0 mitzählen (`i > 0` → `i >= 0`) | rot (7) |
| Sponsor | Monatseinnahme ohne Teamwert-Abweichung | rot (4, inkl. beider Mehrsaisonproben) |
| Sponsor | Angebote bei laufendem Vertrag wieder unterdrückt (A039) | rot (`Offers_AlsoFromPartnerWithRunningContract`) |
| G2 | Aufstieg ab Platz 3 statt 2 | rot (`SeasonChange_Table`) |
| G2 | Stärkemischung (2s + s₋₁)/3 vertauscht | rot (`SeasonChange_YoungerTeamMovesUp`) |
| G2 | Schrittwürfel `R(0,8)` → `R(0,7)` | **grün** in Jugend-Tests, rot in 9 Abdrücken → R11G-03 |
| G2 | Platzschritt `R(1, T)` → `R(1, T+1)` | rot (`MatchDay_StepsAreBounded…`) |
| G2 / #327 | Startliga aus Vereinsliga statt Setup | rot (4) |
| G2 | Saisonwechsel 0x3A13B nicht angeschlossen | rot (`SeasonChange_PromotesTopTwo…`) |
| G3 | Kreditrahmen 2 → 1 Mio. | rot (2) |
| G3 | Grenze zwischen Managern 1 → 2 Mio. | rot (3) |
| G3 | Gebühr 15 → 10 ‰ | rot (5) |
| G3 | Rest anteilig genau statt ganze Prozent | rot (5, inkl. `BoerseStartTests` Seeds 7/8) |
| G3 | Kauf ohne Gebühr | rot (4) |
| G4 | Höchstwert Bank = Rahmen + Sparbuch (0x597FE) | rot (2) |
| G4 | eigene Schuld erhöht Grenze (0x59924) | rot (2) |
| G4 | Forderung nicht als Mindestwert | rot (2) |
| G4 | keine Vorgabe Forderung (0x59872) | rot (1) |
| G4 | Teiler 154 → 155 (0x58409) | rot (2) |
| G4 | ohne Rundung auf 1.000 DM | rot (1) |
| G4 | „−“ ohne vorzeichenlosen Überlauf | rot (1) |
| G4 | negatives s → 0 statt Höchstwert | rot (1) |
| R-K1 | Grenze 28 → 29 | rot (`Talent_NotWith28OwnedPlayers_CountingLoanedOut`) |
| R-K1 | verliehene Spieler nicht gezählt (0x3CCEB) | rot (dto.) |
| R-K1 | Preisbasis 60.000 → 50.000 (0x3EEAB) | rot (`Price_ChangesOnOfferDays…`) |
| R-K1 | Start bei Konto = Preis erlaubt (0x469A8) | rot (`Start_NeedsMoreMoneyThanThePrice`) |
| R-K1 | Takt nur Tag ≡ 0 (mod 7) | rot (`PlayerFactoryTests.TalentSearch_TalentIsComplete`) |
| R-K1 | Technik `s+1` → `s+2` (0x06D7F) | rot (`Talent_ComesFromThePool_WithOriginalValues`) |
| R-K1 | Zusatztakte 6 → 5 | rot (1) |
| R-K1 | Gehalt ohne 80 % | rot (1) |
| R-K1 | Preiswürfel `== 0` ↔ `!= 0` | grün – gleichwertige Mutation (50/50), kein Befund |
| #352 | Entlassungsgrenze −1,5 Mio. → −0,2 Mio. | rot (2, Bundesliga passiv; ohne Ausweg) |
| Leitstelle | `TransferScreen.cs` auf Stand `49bfb2a` | rot (`TalentButton_HiddenAndInactiveWhileSearchRuns`) |
| Leitstelle | `StockFeePerMille = 0` gegen `BoerseStartTests` | grün → R11G-05 (Formel selbstbezüglich) |

### Weitere Prüfungen

- **Disassembly** (`ndisasm` auf `gamedata/em.exe`): 0x3F915–0x3F98E (N = DGROUP 0x7A9 + Liga, Schleife N − 5 … N − 1 nur
  i > 0, Teiler N − max(0, N − 5), Summe 0 → 6). 0x3A13B (Aufstieg p < 2 und l ≠ 0 → p + 10, Abstieg p > 10 und l < 2 → p − 10,
  sonst 5; Stärke (2 · s[t] + s[t − 1]) / 3). 0x3D316 (Wochenschritt). 0x59A15 (q = max / 154 + 1). 0x46704/0x468C1 (Zähler
  DGROUP 0x9747). Tabellen DGROUP 0xC0, 0xEC/0xED, 0x4EBA aus der Datei gelesen. Alles stimmt mit dem Code überein.
- **Migration 22 → 26 mit echtem Altstand:** Am Base (`02c4bd74`) per Wegwerftest einen Spielstand Schema 22 geschrieben:
  Sitzplatzausbau 2 Schritte, 3 Restwochen; Talentsuche 3 Wochen; Juniorenförderung voll; 4 Spieltage. Am Head geladen:
  `SeatsTarget = 4000`, Restwochen 3, `TicksLeft = 6`, Preis 90.000, Jugendstärke 80 (Startstärke Bundesliga),
  `RankSlotsRecorded = 0` (wird laut `[APPROX]` beim nächsten Tagesabschluss nachgetragen). Danach Rundlauf identisch,
  Saison bis zum Ende gespielt (Sitzplätze 4.000, Baustelle leer), `StartNextSeason`, ein Spieltag, Rundlauf erneut
  identisch. Die Wegwerftests sind nicht committet.
- **`GameState.Book`:** Im Diff ändern nur `ShiftLoan` (Umbuchung zwischen Managern, neutral), `BuyStock` (Kaufpreis samt
  Gebühr aktiviert, geht beim Verkauf ins Buch „Aktien“; so wie bisher) sowie `Book(… Bau …)` und `Book(… Talentsuche …)` das Konto.
  Die UI ändert `Money` nirgends direkt. Die Stadionprobe prüft nach jedem `Continue()` Konto = Start + Buch.
- **Geänderte Abdrücke und Referenzwerte:** `RngTwinProbeTests` (6), `SpielberechnungAnschlussTests` (2) und
  `GameModeTests.Mode4` (nur Kontostände, R/C-Prüfsummen und Titel unverändert – im Diff nachgesehen) sind je Teil
  (2, 3, 6) mit Ursache und Gegenprobe begründet. `StadiumTests`: Straße 1,3 Mio. aus der Tabelle (gegen Datei geprüft),
  linear statt steigend, Zustand statt Gastronomie wegen Bauzeit – begründet. `FinanceCalibrationTests`: Ausweg entfernt
  (#352), jetzt `SeasonOver` und keine Entlassung je Saison – Gegenprobe oben.
- **Dateigrenzen:** Die Spur hat außerhalb der eigenen Dateien nur Folgendes geändert: `Core/SaveGame.cs` (Schema, Migrationsliste; minimal),
  `Core/Player.Factory.cs` (nur `ApplyScoutedTalentValues`/`ScoutedMinAge`, wie erlaubt), `Core/GameState.Manager.cs` (siehe
  R11G-06) und `tests/…UI.Tests/BoerseStartScreenTests.cs` (Startauswahl `None` statt `Bank`, Folge von G4 „ohne Auswahl“ – keine der
  Leitstellen-Dateien). `BoerseStartTests.cs` und `UI/Screens/Team/*` wurden nur im Leitstellen-Commit geändert.
- **Mehrsaisonproben (Geld, 4 Wechsel):** Stadion (aktiv/passiv, Kontobuch), Werbung (aktiv/passiv), Jugend (aktiv/passiv),
  Bank/Börse (passiv und aktiv in einem Spiel), Finanzbedienung, Talentsuche (aktiv/passiv), `FinanceCalibrationTests` (4 Fälle
  je Datenbank) – alle im Lauf mit Originaldaten grün.

### Akzeptierte `[APPROX]` (bestätigt offen, wie in den PR-Beschreibungen)

Gebühr in ganzen DM (Drittel-Rest verworfen); Preis der Talentsuche und Stadionausbau als Buchung („Talentsuche“, „Bau“, E1)
statt direkt vom Konto; Besitz verliehener Spieler über `PlayerLoan.Origin`; Poolsuche nach 1.500 Versuchen; keine
Nachbearbeitung 0x118:0x4D am Talent; Baufirmen-Würfel nur für gesetzte Merker; Balkenlänge auf 0–154 begrenzt;
Wiederholtakt der Finanzknöpfe in 60-Hz-Bildern; Migrationsvorgaben für ältere Spielstände (Platzverlauf 0, Startstärke,
Zähler = 2 · Restwochen, Preis 90.000); Zähler über 44 nimmt das letzte Feld; E2 (Leihen beim Mitmanager sofort,
Anfrage nur für den Netzwerkmodus). **Bildschirmfoto-Abnahme des Knopfs „Talent“ gegen ein Video steht weiter aus**
(kein Videobild mit laufender Suche bekannt).

### Annahmen (nicht geprüft)

- Nicht disassembliert habe ich: Monatseinnahme 0x3E657, Angebote 0x3FB47, Jugend-Spieltag 0x39DEC im Einzelnen, Talent
  0x06D7F/0x06FA5, Betragsbalken 0x58409 außer dem Teiler. Diese Stellen sind nur über Tests und Mutationen abgesichert.
- Dass ein Klick auf die Stelle des Knopfs bei laufender Suche zurück ins Hauptmenü führt (`TransferTalentButtonTests`),
  folgt aus dem Remake-Verhalten für Klicks außerhalb. Den Sprung 0x6A65 des Originals habe ich nicht verfolgt.
