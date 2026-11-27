# Review: Integration Runde 14, Spur `kader-start` (PR #669)

Gesamtreview (Vollprüfung) des Deltas `1e9a2335162918473c6190a6435a2f49f61dc641..c02c0157d1915b59a2c17a88638df55afaa345d4` (enthält #631, #646,
#655, #659, #663, #667). Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · 09.11.2026.
Checkout: Klon, `git rev-parse HEAD` = `c02c015…`, `git status --porcelain` leer. Codelesen **und** Ausführung am nachgewiesenen
Checkout mit Originaldaten (`kaaasi/eishockey-manager-referenz` @ `779a1c8`). Mutationen in einem eigenen Worktree desselben Commits,
Proben (nicht committet) in Scratch-Dateien. Fundstellen selbst mit `tools/reverse-engineering/disasm.py` nachgelesen.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4`

## 1. Merge-Empfehlung

**Nein** für Head `c02c015`.

- **R14KS-01 (P0):** Mit einem eigenen Spieler auf der Transferliste lässt sich das Spiel nicht mehr sichern. Das gilt auch für den
  Autospeicher, der dann still nichts schreibt. Ursache ist A380: Der Spieler steht nur noch im Markt-Platz, `GameState.AllPlayers`
  kennt ihn nicht.
- **R14KS-02 (P1):** Ein Spieler, der über das Saisonende auf der Liste steht, behält nach der Rückkehr seine Saisonzähler (Spiele,
  Tore, Strafen). Das Remake setzt die Zähler vor der Rückkehr zurück, das Original danach. Die Zähler gehen in den Spielerwert ein.
- `main` hat inzwischen #666 (`6789e3d`); der PR ist nicht mehr konfliktfrei (`docs/arbeitspakete.md`,
  `tests/EishockeyManager.UI.Tests/TacticsClubScreenTests.cs`).

Alles andere hält: Die drei Läufe von `tools/pruefen.sh` sind grün. Die Schemakette 39/40/41 → 43 lädt echte Stände. Die
Saisonende-Zahlen stimmen am echten Spielstand. Die Fundstellen stimmen, die Kopierrichtung der Leihrückkehr ist alt → neu. Von den
geänderten Tests werden alle mit eigener Regel per Mutation rot, mit zwei Ausnahmen (R14KS-03). Offene Prüfbereiche gibt es nicht.

## 2. Befunde

### R14KS-01 (P0) – Spielstand mit eigenem Spieler auf der Transferliste nicht speicherbar

`TransferMarket.Offer` nimmt den Spieler aus dem Kader ([Transfer.cs Z. 491](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/Transfer.cs#L491)).
`GameState.AllPlayers` ([Player.Identity.cs Z. 41](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/Player.Identity.cs#L41)) zählt nur Kader,
Vertragslose, Laufbahnende und Pool-Spieler mit Besitzer ≠ `Taken` auf. Der Spieler auf dem eigenen Markt-Platz fehlt dort. Deshalb meldet
`ValidatePlayerIdentity` „Transfermarkt: … fehlt im Spielstand“, und `SaveGame.Save` wirft. `SaveLoadScreen` zeigt „Der Spielstand lässt
sich nicht sichern“; `AutoSave` fängt die Ausnahme ab und schreibt nichts. Auch `FindPlayer(id)` liefert für diesen Spieler `null`.

Gegenbeispiel mit Originaldaten (Probe `OriginalData_SaveWithListedPlayer`): `ScreenDriver.StartNewGame()` → `SaveGame.Save` geht →
`Offer(game, manager, Verteidiger)` → `SaveGame.Save` wirft `InvalidOperationException: Spielstand nicht speicherbar: Transfermarkt:
KÄNDLER (ID 886) fehlt im Spielstand.` Synthetisch (`TestGames.Create(3)`, `Offer`): dasselbe mit `SPIELER 43 (ID 43)`, `FindPlayer` =
`null`. Am Base bleibt der Spieler im Kader, dort gibt es den Fehler nicht.

Keiner der Tests sichert mit einem gelisteten Spieler. Auch die Mehrsaisonprobe `FourSeasonChanges_ListedPlayersReturn_NoDuplicates`
tut das nicht. Zu ergänzen sind ein Regressionstest (Offer → Save → Load → `OwnListingsOutsideSquad` gleich, Withdraw nach dem Laden) und
Speichern je Saison in der aktiven Mehrsaisonprobe.

### R14KS-02 (P1) – Saisonzähler gelisteter Spieler werden beim Saisonwechsel nicht zurückgesetzt

`OnNewSeasonManager` ruft `CarryGamesForClub` und `ResetMatchCounters` ([GameState.Integration.cs Z. 119–120](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/GameState.Integration.cs#L119)).
Beide laufen über `Clubs…Players` (+ Vertragslose). Erst danach holt `OnNewSeasonTeam` → `StartSquadSeason` die gelisteten Spieler zurück
([Player.Contract.cs Z. 127](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/Player.Contract.cs#L127)). Ein Spieler, der über das Saisonende auf der Liste
steht, kommt mit den Zählern der Vorsaison zurück: `Games`, `CupGames`, `Goals`, `Assists`, `Penalties2/5`, Gegentore.

Im Original liegt die Rückkehr (Datei 0x27547–0x27665, Kopie 0x275D2) in der Saisonschleife 0x26F20 **vor** dem Löschen der Zähler.
Das Löschen kommt erst ab Datei 0x27A0C, nach dem Ende der Satzschleife (0x279ED–0x27A09); beide Stellen habe ich nachgelesen. Die
Zähler +0x00/+0x01/+0x03–0x05 gehen in Wert und Gehalt ein (`PlayerValue.Inputs`). Die Spiele beim Verein bleiben dabei zufällig
richtig, weil weder übertragen noch gelöscht wird.

Gegenbeispiel (Probe `Probe_ListedPlayerOverSeasonEnd_SeasonCountersReset`): `TestGames.Create(3)`, Verteidiger mit Laufzeit 3
auflisten, bis `SeasonOver` spielen (nicht verkauft). Dann `Games=7, CupGames=2, Goals=3, Penalties2=4, Assists=5` setzen, ebenso
bei einem Stürmer im Kader, und `StartNextSeason` aufrufen. Der Stürmer steht danach auf (0,0,0,0,0), der zurückgekehrte Verteidiger
auf **(7,2,3,4,5)**.

Abhilfe: `ReturnListedPlayers` vor `CarryGamesForClub`/`ResetMatchCounters` ziehen (Integrationsdatei) oder die Markt-Plätze in
beide Schleifen aufnehmen; Test wie oben. Am Rand und nicht entscheidbar: Heilung (`HealInjuries`, [MatchEngine.Injuries.cs Z. 247](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/MatchEngine.Injuries.cs#L247))
und Sperren laufen für gelistete Spieler nicht weiter. Ob die Tagesschleife 0x7C12 im Original Satz 4 (Markt) einschließt, habe ich
nicht gelesen. Der Punkt gehört zu `[APPROX]` oder in einen Folgeauftrag.

### R14KS-03 (P2) – Tests zu A115 „Marktleihe endet frei“ erkennen eine Rückkehr zum Computerverein nicht

[`KaderStartTeil4Tests.MarketLoan_EndsFree_NotBackToComputerClub`](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/tests/EishockeyManager.Core.Tests/KaderStartTeil4Tests.cs#L55)
und das abgeschwächte [`LifecycleRegressionTests.R204_MarketLoanEndsFree_NotBackWithItsRemainingContract`](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/tests/EishockeyManager.Core.Tests/LifecycleRegressionTests.cs#L129)
prüfen nur „nicht mit dem alten Vertrag (2, 4.321)“. Mutation N14: Der Spieler geht statt frei mit Vertrag 0 zurück in den Kader des
Computervereins. Das Ergebnis: Alle 302 Runden-Tests bleiben grün. Zu ergänzen ist die Prüfung „in keinem Kader, unter den Vertragslosen“
(bzw. „nur über das Auffüllen der Computervereine“) und Besitzer frei. Der alte R204-Test prüfte „ein Kader, eine Nummer“; die neuen
Behauptungen (`<= 1`, `NotEqual`) sind schwächer als vorher.

### R14KS-04 (P3) – Herkunft der neuen Abdrücke falsch dokumentiert

Die Kommentare bei `RngTwinProbeTests` (Z. 187), `SpielberechnungAnschlussTests` (Z. 198), `GameModeTests.Mode4Reference` und
`JugendTeamsTests` nennen „A135 und Taktik 375h“. Die Rückmutationen zeigen etwas anderes:

- **375h ändert keinen Abdruck.** M09 allein lässt alle Abdrücke grün.
- **A046 ändert die aktiven Abdrücke** (RngTwin aktiv, Mode4, JugendTeams aktiv). M10 bzw. M11 machen sie rot. Ohne die A046-Rückmutation
  (C9) bleiben RngTwin aktiv Seed 3/11 neu.
- **Die Abdrücke enthalten die neuen Spielerfelder.** `Dump` nimmt alle öffentlichen einfachen Eigenschaften auf, also auch
  `CupGames`, `ClubCupGamesCarry` usw. und `StoredTableRank`.

Mit der Rückmutation von A046 + A135 (+375h) und den neuen Feldern in `Excluded` (C12) kommen alle acht alten Abdrücke wieder:
`1501663F0197426F`, `57912317E80C4918`, `6B6DAAD441C0C2A2`, `FC146C171B47B16F`, `C2A2F88417587DD1`, `771D1BE3680653F5`,
`6C2D30784D430A5A`, `FB60D817157E2013`. An der ersten Abweichung kommen auch die alten Werte von Mode4 (`2154396`) und JugendTeams
(`1:Schueler:2/6/67`) wieder. Die Werte selbst sind damit belegt, nur die Begründung ist zu berichtigen.

### R14KS-05 (P3) – Zwei Darstellungsänderungen ohne Test

- M43: Das `*` wieder in der Kaderliste der Transferliste ([TransferScreen.cs Z. 397](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L397)).
- M45: Trennlinie x 5/159 und Doppelpunkt x 77 im Verhandlungsfenster ([ContractDialog.cs Z. 170](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.UI/Screens/Team/ContractDialog.cs#L170)).

Beide Mutationen lassen alle Tests grün. Die Lage x 6–163 / x 76 habe ich in `file46_3` nachgemessen: Die Trennlinie y 43 stimmt in
164 von 165 Pixeln, die eine Abweichung ist die Ecke x 166.

### R14KS-06 (P3) – Doku-Kommentare und `[APPROX]`-Begründungen

- [GameSetup.cs Z. 175–191](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/GameSetup.cs#L189): Der `<summary>` von `CanStart` steht jetzt vor dem
  `<summary>` von `ManagerRowFor`. `ManagerRowFor` hat damit zwei, `CanStart` keinen.
- [Player.Contract.cs Z. 221](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/Player.Contract.cs#L221): „[APPROX] Rückkehr zu einem Computerverein … hinten an“
  ist seit A115 nicht mehr erreichbar, weil eine Leihe vom Computerverein frei endet. Der `returns`-Zweig betrifft nur noch Managervereine.
- [Squad.Lineup.cs Z. 458](https://github.com/kaaasi/eishockey-manager-redone/blob/c02c0157d1915b59a2c17a88638df55afaa345d4/src/EishockeyManager.Core/Squad.Lineup.cs#L458): Das `[APPROX]` „volle Reihenhälfte ohne Ausweichhälfte“
  beschreibt das Original richtig. Nachgelesen: Datei 0x414D3 setzt `[bp−0x1a] = 3 − [bp−0x1a]` und versucht die andere Hälfte, der
  zweite Fehlschlag in Reihe 1 öffnet den sechsten Platz (0x413C9–0x414B4), sonst „Kein / Platz mehr !“. Die Begründung „feste Plätze je
  Art“ trifft das Remake aber nur halb, denn `Lineup` kennt Plätze ohne Artprüfung (`SetUnchecked` für die Ausweichrollen). Ich
  akzeptiere den `[APPROX]` und empfehle, die Begründung zu schärfen.

### R14KS-07 (P3) – Dateigrenzen

- `Core/GameState.Manager.cs`: zwei Aufrufe `MonthlyWages/MonthlyInsurance(this, club)` (A380). Die Datei steht nicht in den Grenzen der
  Spur und wird im PR nicht genannt.
- **Fremde Testdateien angepasst:** `GameModeTests`, `JugendTeamsTests`, `LifecycleRegressionTests`, `NationalteamTests`,
  `NewGameTests`, `RngTwinProbeTests`, `SaveGameTests`, `SpielberechnungAnschlussTests`, `TacticsOriginalTests`, `TaktikTeil5Tests`,
  `TransferOriginalTests`, `TransferTransactionRegressionTests`, `TransferVertraegeKomplettTeil4Tests`, `WaveBReviewRegressionTests`,
  `KaufEinsortierenTests`, `KaderTransferTeil5Tests`, dazu UI-Tests. Laut `arbeitspakete.md` sind fremde Testdateien Sache von
  `pruefung-karte`. Die Anpassungen sind durch Regeländerungen bedingt und per Gegenprobe belegt (Tabelle unten); im PR gehören sie
  einzeln genannt.
- `TransferScreen.cs`: Das Entfernen des `*` ist Darstellung der Transferliste und damit `ui-rest`-Gebiet. Es ist aber eine Folge von
  A380 und im Inventar `kader-start / ui-rest` zugeordnet, also vertretbar.

Kein Verstoß gegen AGENTS.md §4:

- Zeit vergeht nur über `Continue`.
- Die Rückkehr von der Liste läuft im Saisonhaken `OnNewSeasonTeam`.
- Ablöse und Gehälter laufen über `Book`.
- Bildschirme ändern beim Öffnen nichts: `ContractsScreen` hält nur ein Abbild, `SalaryBeforeRenewal` liest nur. Zeichnen ändert nichts;
  geprüft per Save vorher/nachher im Test `SeasonEnd_ListIsPicture…`.

### R14KS-08 (P2, `ui-rest`, kein Merge-Hindernis für #669) – `TransferDialog.OfferBox` liegt 1 Pixel links

`OfferBox = (1, 29, 166, 71)`, unverändert auch auf `main` nach #666. Im Original beginnt jedes Fenster dieser Art bei x 2, Breite 165:

- Kauffenster `BuyBox` (2, 29, 165, 43), A377
- Verhandlungsfenster `file46_3` (2 | 29)–(166 | 95)

Am Head habe ich gegen `file46_3` gemessen (Umrechnung 642 × 507 → (1 + 2x | 26 + 2y)):

- `ContractsScreen.DialogRect` (2, 165) stimmt in der Lage.
- Abweichend sind Rahmenfarbe und Ecken: hell (146|178|195) statt Weiß, Ecke (2|29) Füllfarbe, (166|95) schwarz. Das zeichnet
  `TeamUi.DialogFrame`; #666 hat das auf `main` schon berichtigt (`DialogLight`, Ecke 0).
- Kopfzeile y 19–23 (1806/1806), Linien y 25 und y 209 (252/252) stimmen in `file46_3` und `file40_11` überein.

`OfferBox` nutzt der neue A380-Weg (Klick auf den eigenen Markt-Platz mit Gebot → `TransferDialog.ForBid`). Ein DOSBox-Bild dieses
Fensters gibt es nicht. Ich empfehle, x 2 / Breite 165 wie `BuyBox` zu übernehmen oder einen `[APPROX]` zu setzen; Zuständigkeit `ui-rest`.

### Erwartet offen (bestätigt)

- Rechtsklick in `NationalLineupScreen` → #664. Die Datei ist im Delta nicht geändert.
- Inventurzeilen → `pruefung-karte`. `docs/analyse/inventur.md` ist nicht geändert, die Zeilen stehen im PR.
- #666 (`ui-rest`) ist inzwischen auf `main` (`6789e3d`, `8436a9b`). `git merge-tree origin/main c02c015` ergibt Konflikte in
  `docs/arbeitspakete.md` und `tests/EishockeyManager.UI.Tests/TacticsClubScreenTests.cs`. `TransferScreen.cs` und
  `ContractDialog.cs` mergen automatisch. `TransferDialog.cs`, `TeamUi.cs` und `OriginalDialog` berührt #669 nicht.

## 3. Ausgeführte Prüfungen

### Tests am Head (`tools/pruefen.sh`, alle drei Läufe, Originaldaten)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2374 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1996 | 0 | 340 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1997 | 377 | 0 |

Commit `c02c0157d1915b59a2c17a88638df55afaa345d4`, SDK 10.0.402, Ergebnis: bestanden.

### Gegenbeispiele und Prüfpunkte

- **#605:** Start-Liga ohne Manager bleibt (`_startLeague`), `AddManager` übernimmt sie. Gegenproben M01, M02, M41 rot.
- **#625:** Talentsuche nach Spielerart einsortiert. M03 rot. Aufruferliste in `Player.Factory.cs` gegen das Disassembly geprüft.
- **A056:** Datei 0x28AA9–0x28ADB nachgelesen: Zeile = (y − 0x26)/0x33, höchstens 3, bei `[bp−0x57] = 2` → 0. M04 und M42 rot.
- **U15-c:** Datei 0x29CFA–0x29D9C nachgelesen. Die Bedingung prüft die Liga des Vereins nicht, die Zusatzbedingung im Remake
  (`League == Bundesliga`) ist aber nicht erreichbar: „eine Saison“ erzwingt Start-Liga Bundesliga (`GameSetup.Step`). M05 rot. U15-a
  „nie Platz 0“ (Datei 0x29CC5–0x29CD5): M06 rot.
- **A180:** Startwerte 0 und Anzeige der letzten Automatik. M07 und M08 rot.
- **Taktik 375h:** Datei 0x54FCE–0x54FE8 (0x376 := 0x375, 0x375 := 0) sowie 0x54D8C–0x54D9E und 0x552F9–0x5530B (zurück, dann
  0x426BC) nachgelesen. M09 und N01 rot.
- **A046:** Schreiber/Leser; M10, M11 und M39 (Migration) rot.
- **A135:** Datei 0x5494C–0x5499E nachgelesen: +0x06 + {0, 1, 2} und Wort +0x1C + 2 · {0, 1, 2} für Liga, Pokal (Argument 1) und
  Europapokal (8). M12–M17 und N02 rot.
- **A380:**
  - Datei 0x47093 (+0x0A = 0), 0x470C4 (Kopie Kader → Markt) und 0x470D6 (Platz frei) nachgelesen.
  - Gehaltssumme Datei 0x3E8DC–0x3E99F: Kaderplätze und Markt-Plätze (Satz 4, DGROUP 0x7623) mit Pool +0x1A = Manager, +0x28 + +0x30.
  - M18–M22, M44, N08–N10 rot. Befunde R14KS-01 und R14KS-02.
- **A362:** M27–M31 rot. Das Abbild ändert keinen Zustand. Pixel siehe R14KS-08.
- **A091:** M21 und N06 rot.
- **A115:** M24–M26 und P4 rot; zu N14 siehe R14KS-03.
  - Kopierrichtung: `0x48:0x2BD4` = Datei 0x105C4 lädt `ds:si` aus `[bp+6]` (erstes Argument = alter Leihplatz, Satz `[bp−9]`) und
    `es:di` aus `[bp+0xA]` (neuer Platz beim Besitzer), dann `rep movsb`. Also alt → neu, wie im Code.
  - Laufzeit vorher heruntergezählt (Datei 0x27500).
  - Besitzer ≥ 4: +0x1A = 6 (Datei 0x275FF), Platz frei (0x27628).
- **A088:** Datei 0x27A0C–0x27AA8 löscht +0x00, +0x01, +0x03–0x05 und +0x06–0x08 der Managersätze. M15 und M16 rot.
- **A364:** Datei 0x42027–0x42031 (NR leer bei +0x0A = 0) nachgelesen. M32 rot.
- **A010:** M33–M37, N11a–N13, P1 und P2 rot. Den `[APPROX]` „volle Reihenhälfte“ habe ich gegen 0x412A1–0x414ED geprüft (R14KS-06).
- **Schema 42/43:** Abdruck `502E418048BEF58F`. Ein zusätzliches Feld (M40) macht `SaveGameTests` rot. M38 und M39 (Migrationen) rot.

### Saisonende-Zahlen am echten Spielstand

`OriginalSaveGame.Read` und `PlayerValue.Compute` mit den Feldern des Kaderplatzes von KJELLBERG:

- **FILE44A** (aus FILE44 geladen, Saisonwechsel gespielt): Alter 33, Tendenz 51, KO 87, TE 97, Zähler 0, Level 3 → `[0xED8]` = 2.
  - Wert(Gehalt) = **16.800 DM**, · (147 − 33)/100 = **19.152 DM**. Das ist auch Kaderplatz +0x28 in FILE44A und das Fenster `file46_3`.
  - Wert(Preis) = 311.000 DM, / 2 = **155.500 DM** (`file46_5`).
- **FILE44B:** 16.000 · 114/100 = 18.240 DM = +0x28.
- Mit dem Alter vor dem Altern (32) ergeben sich andere Werte. Das bestätigt „Alter nach dem Altern“.
- Prämie 100.000 DM: nur am Bild `file46_2` belegt. Der Kontostand FILE44 → FILE46 enthält zu viele andere Buchungen zum Nachrechnen;
  `SeasonPremium(Bundesliga, 10, [TopScorer])` = 100.000 und `Premium` = 0 stimmen mit dem Bild.
- Gegenproben N03 (147 → 148) und P3 (/2 → /4) rot.

### Echte Spielstände Schema 39/40/41 → 43

Erzeugt mit Originaldaten an `750a38c` (39), `3df714f` (40) und `1e9a233` (41): Köln fest, Saat 1993, zwei Stände je Commit.

- **„mitte“:** 14 Spieltage, ein eigener Verteidiger auf der Liste (Altstand: im Kader).
- **„saison2“:** ganze Saison, `StartNextSeason`, 3 Spieltage.

Am Head geladen (6/6 erfolgreich):

- Schema 43 nach erneutem Sichern, Rundreise bytegleich.
- `ValidatePlayerIdentity` leer.
- `_startLeague` = Liga von Manager 0.
- `_tableRank` = `_rankSlots[RankSlotsRecorded − 1]` (Platz 8 bzw. 11).
- Zähler je Wettbewerb 0, Tore beim Verein = Saisontore.
- Gelisteter Altspieler im Kader und nicht doppelt im Gehalt (`MonthlyWages(game, club)` = `MonthlyWages(club)`).
- Vier weitere Spieltage, `Withdraw` des Altspielers, erneut sichern.

### Mehrsaisonprobe

Grün am Head (je 4 Wechsel, passiv und aktiv):

- `KaderStartTeil2cTests.FourSeasonChanges_ListedPlayersReturn_NoDuplicates`
- `KaderStartTeil4Tests.FourSeasonChanges_LoansEnd_NoDuplicates`
- `KaderStartRunde14Teil2bTests.MultiSeason_TableRankAndCompetitionCounters`
- `KaderStartStartLigaTests.MultiSeason_FourChanges_AfterStartSwap`
- `RngTwinProbeTests`, `SpielberechnungAnschlussTests`, `JugendTeamsTests`, `KaderTransferTeil5Tests.GamesForClub_AccumulateOverFourSeasonChanges`

Keine davon sichert mit gelistetem Spieler (R14KS-01) oder prüft die Zähler des Rückkehrers (R14KS-02).

### Pixelvergleich

Umrechnung DOSBox 642 × 507 → (1 + 2x | 26 + 2y).

- **Verträge/Saisonende** (`file46_3`, `file40_11`): siehe R14KS-08.
- **Aufstellung** (`file34_3`): NR nur bei Eingesetzten, Rahmen, Blöcke und „Eingesetzt“ in Lage gleich; gesehen, Stände verschieden.
- **Transferliste** (`file40_05`): eigener Markt-Platz unten mit „A“, Spieler nicht in der Kaderliste. Farbe des „A“ und Statuszeile
  „von …“ sind `ui-rest`.
- **Spielstart** (`file29_1`): Zeilenklick bei „nur Weltkugel“ über Test und M42 belegt.

### Gegenproben je neuem oder geändertem Test

Ermittelt per Rückmutation der Regel im Worktree, gefiltert auf die geänderten Testklassen (302 Testfälle, Basislauf 0 rot). IDs:

| Gruppe | Mutationen |
|---|---|
| Start-Liga, Talentsuche, Spielstart | M01–M06 Start-Liga/#625/A056/U15 |
| Taktik, Tabellenfeld, Zähler je Wettbewerb | M07/M08 A180, M09/N01 375h, M10/M11 A046, M12–M17 A135/A088 |
| Transferliste, Leihe, Saisonende | M18–M23 A380/A091/Listenpreis, M24–M26/N14/P4 A115, M27–M31 A362 |
| Aufstellung | M32 A364, M33–M37/N11–N13/P1/P2 A010 |
| Schema, Spielvorbereitung, Darstellung | M38–M40 Schema, M41/M42 Spielvorbereitung, M43–M45 Darstellung |
| Weitere | N02 Play-offs, N03/P3 Saisonende-Zahlen, N04 Ligatausch, N05 Übertrag, N06 Leihrückkehr einsortieren, N07 `LegacyStart`, N08/N09 Verfügbarkeit Markt-Platz |
| Test-Gegenprobe | T1 = Test-Gegenprobe (alte Behauptung) |

| Test | rot bei | Anmerkung |
|---|---|---|
| `GameModeTests.Mode4_ReferenceValues_Unchanged` | M10,M11,M14,M15,M16,N05,P3 |  |
| `JugendTeamsTests.FourSeasonChanges_StrengthFollowsFunding` | M10,M11,M14,M16,N04,N05 |  |
| `KaderStartRunde14Teil2Tests.ClubTactics_StartAtZero_AutoLineAfterFirstMatchDay` | M07,M08 |  |
| `KaderStartRunde14Teil2Tests.NoSpecialCase_PlaceZeroUntouched` | M06,N04 |  |
| `KaderStartRunde14Teil2Tests.OneSeason_SingleManager_ClubOnPlaceZero` | M05,N04 |  |
| `KaderStartRunde14Teil2Tests.StartLeagueDraw_NeverPlaceZero_ChampionPossible` | M06,N04 |  |
| `KaderStartRunde14Teil2Tests.WorldOnly_EveryManagerRowMapsToRowZero` | M04 |  |
| `KaderStartRunde14Teil2bTests.AutoLineup_AfterMatchWithoutInjury_LinesUpAgain` | M09 |  |
| `KaderStartRunde14Teil2bTests.CupMatch_CountsInCupFields` | M12,M13 |  |
| `KaderStartRunde14Teil2bTests.EuropeanCupMatch_CountsInEuropeFields` | M13 |  |
| `KaderStartRunde14Teil2bTests.FirstMatchDay_NewGame_UsesRankZero` | M10 |  |
| `KaderStartRunde14Teil2bTests.Join_ResetsClubCountersOnly` | M12,M17 |  |
| `KaderStartRunde14Teil2bTests.ManualLineup_AfterMatchWithoutInjury_Unchanged` | N01 |  |
| `KaderStartRunde14Teil2bTests.Migration_OldSave_LeagueOnlyAndTableRankFromRankSlots` | M39 |  |
| `KaderStartRunde14Teil2bTests.MultiSeason_TableRankAndCompetitionCounters` | M11,M15,M16,N05 |  |
| `KaderStartRunde14Teil2bTests.Postseason_CountsAsLeague` | N02 |  |
| `KaderStartRunde14Teil6Tests.ClickOnPlaced_TakesOut_SelectsHisLine_LeftUndresses_RightKeeps` | M34 |  |
| `KaderStartRunde14Teil6Tests.LeftClick_FirstFreeSlotOfLine_FullLineNoRoom_GoalkeeperWrongLine` | M33 |  |
| `KaderStartRunde14Teil6Tests.LeftClick_WithoutLine_NoLineSelected_NothingChanges` | M35 |  |
| `KaderStartRunde14Teil6Tests.RightClick_TogglesDressed_TwentyThirdRefused_AutoLineupOff` | M37,N13 |  |
| `KaderStartRunde14Tests.JoinScoutedTalent_GoalkeeperOrDefender_SortedIn` | M03 |  |
| `KaderStartRunde14Tests.Migration_41To42_TakesStartLeagueOfFirstManager` | M01,M38 |  |
| `KaderStartRunde14Tests.StartLeague_SurvivesRemovalOfLastManager` | M01,M02 |  |
| `KaderStartRunde14Tests.StartLeague_WithoutManagers_IsKept_AndAdoptedByNewManager` | M01,M02 |  |
| `KaderStartRunde14Tests.StartLeague_WithoutManagers_StepReportsOnlyRealChanges` | M01 |  |
| `KaderStartStartLigaTests.Bundesliga_AlwaysDraws_NeverTheEuropeanCupPlace` | M06 |  |
| `KaderStartStartLigaTests.NoLeagueClub_LandsInStartLeague_DisplacedLosesLeague` | N04 |  |
| `KaderStartTeil2cTests.FourSeasonChanges_ListedPlayersReturn_NoDuplicates` | M20 |  |
| `KaderStartTeil2cTests.ListedPlayer_KeepsCostingSalary` | M18,M19 |  |
| `KaderStartTeil2cTests.Offer_PlayerLeavesSquad_SlotsMoveUp_ListingWithSeller` | M18 |  |
| `KaderStartTeil2cTests.SeasonChange_UnsoldListedPlayerReturnsByRole_NotInLineup_ContractCountedDown` | M18,M20,M21 |  |
| `KaderStartTeil2cTests.SoldListedPlayer_GoesToBuyer_SellerPaid` | N08 |  |
| `KaderStartTeil2cTests.Withdraw_FullSquad_StaysOnMarket` | M18 |  |
| `KaderStartTeil2cTests.Withdraw_ReturnsPlayerByRole_NotInLineup_ContractUnchanged` | M18,M22 |  |
| `KaderStartTeil4Tests.FourSeasonChanges_LoansEnd_NoDuplicates` | P4 |  |
| `KaderStartTeil4Tests.ManagerLoan_ExpiringLoanSlot_NegotiatedAtOrigin` | M25,M26,M27,P4 |  |
| `KaderStartTeil4Tests.ManagerLoan_ExpiringLoanSlot_WithoutScreen_LeavesOrigin` | M25,M26,P4 |  |
| `KaderStartTeil4Tests.ManagerLoan_ReturnsWithLoanSlotContract_SortedIn` | M25,N06,P4 |  |
| `KaderStartTeil4Tests.MarketLoan_EndsFree_NotBackToComputerClub` | P4 | rot nur bei P4; bleibt grün bei M24-Variante N14 (Rückkehr zum Computerverein) → R14KS-03 |
| `KaderStartTeil4Tests.Offer_ListPriceIsSlotPrice_LoanFeeThird` | M23 |  |
| `KaderStartTeil4Tests.RenewalSalary_UsesAgeAfterAgeing` | M27,N03 |  |
| `KaderStartTeil4Tests.SeasonChange_ResetsSeasonCounters_KeepsClubCounters` | M12,M15,M16,N05 |  |
| `KaderStartTeil4Tests.SeasonEndNumbers_File44To46` | N03,P3 |  |
| `KaderTransferTeil5Tests.GamesForClub_AccumulateOverFourSeasonChanges` | N05 |  |
| `KaderTransferTeil5Tests.SeasonEnd_Compensation_KeepsGamesForClub` | P3 |  |
| `KaderTransferTeil5Tests.SeasonEnd_OfferSalary_KeepsGamesForClub` | M13,N03,N05 |  |
| `KaufEinsortierenTests.LoanReturn_ToManagerClub_SortedIn` | N06 |  |
| `LifecycleRegressionTests.R204_MarketLoanEndsFree_NotBackWithItsRemainingContract` | M24,P4 |  |
| `NationalteamTests.Club_AutoDisplay_KeepsLastLine` | M07 |  |
| `NationalteamTests.Club_AutoDisplay_Unchanged` | N07 |  |
| `NationalteamTests.Mode1_AfterFirstWorldCupMatch_AutoShowsLastMatchLine` | – | nur Nachbarzeilen geändert (Kommentar/Hilfsfunktion), Regel unverändert |
| `NationalteamTests.Mode4_AfterAppointment_AutoShowsZero` | – | nur Nachbarzeilen geändert, Regel unverändert |
| `NationalteamTests.SaveLoad_KeepsLastMatchLine_AndHolder` | – | nur Nachbarzeilen geändert, Regel unverändert |
| `NationalteamTests.Schema11_WithoutCoach_IsMigrated` | T1 |  |
| `NewGameTests.Create_FixedClubKeepsChosenEuropeanCupParticipant` | – | Test selbst unverändert (Nachbartest geändert) |
| `NewGameTests.Create_RandomBundesligaClub_NeverTakesEuropeanCupParticipant` | M06 |  |
| `RngTwinProbeTests.FourSeasonChanges_FingerprintUnchangedBySerializableGenerator` | M10,M11,M12,M13,M14,M15,M16,M17,N02,N05,P3 |  |
| `RngTwinProbeTests.FourSeasonChanges_OriginalDatabase_FingerprintUnchanged` | M11,M12,M14,M15,M16,M17,N04,N05,P3 |  |
| `SaveGameTests.NewGame_RoundTripIsByteIdentical_AndSavingChangesNothing` | – | Test selbst unverändert (Konstante daneben) |
| `SaveGameTests.SchemaFingerprint_MatchesSchemaVersion` | M40 |  |
| `SpielberechnungAnschlussTests.FourSeasonChanges_OriginalDatabase_CountersPerSeason` | M11,M12,M13,M14,M15,M16,M17,N05,P3 |  |
| `SpielberechnungAnschlussTests.NewSeason_ResetsCounters` | – | Test selbst unverändert (Abdruck-Kommentar daneben) |
| `TacticsOriginalTests.Defaults_ShowEvenMatchRowOfOriginal` | M07,N07 |  |
| `TacticsOriginalTests.EffectiveAttack_KeepsThreeBits` | N07 |  |
| `TacticsOriginalTests.EffectiveEffort_IsBasePlusSituation` | N07 |  |
| `TacticsOriginalTests.EffortIsPercent_AttackUpToSeven_BaseAndSurchargeShareTheLimit` | N07 |  |
| `TaktikTeil5Tests.Manual_KeepsEffortAndAttack_DuringMatch` | N07 |  |
| `TransferOriginalTests.MultiSeason_Transfers_StayConsistent` | N08 |  |
| `TransferTransactionRegressionTests.R209_InvalidBidCannotBookOrMove` | N09 |  |
| `TransferVertraegeKomplettTeil4Tests.SponsorRating_PlayerWithBid_DrawsValueAgainBeforeStarCheck` | M18 |  |
| `WaveBReviewRegressionTests.PR3502_BidThatBecameInvalid_IsRejectedWithResultAndRemoved` | M18 |  |
| `KaderStartRunde14Teil6UiTests.A364_NumberOnlyForDressedPlayers` | M32 |  |
| `KaderStartRunde14Teil6UiTests.ClickOnPlacedPlayer_TakesOut_LeftUndresses_RightKeepsDressed` | M34 |  |
| `KaderStartRunde14Teil6UiTests.FullLine_ShowsNoRoom_AndChangesNothing` | N11b |  |
| `KaderStartRunde14Teil6UiTests.GoalkeeperIntoFieldLine_IsRejected` | – | M33 äquivalent (Lineup.Assign prüft die Art ein zweites Mal); kein Rot erzeugt |
| `KaderStartRunde14Teil6UiTests.LeftClickOnReserve_WithoutLine_ShowsNoLineSelected` | M35 |  |
| `KaderStartRunde14Teil6UiTests.RightClick_TogglesDressedForUnplaced` | N13 |  |
| `KaderStartRunde14Teil6UiTests.SelectLine_ThenLeftClick_PlacesInFirstFreeSlotOfLine` | N12 |  |
| `KaderStartRunde14Teil6UiTests.Slot_LeftClickClears_RightClickLeaves` | M36,N12,P2 |  |
| `KaderStartSetupScreenTests.ClubPicker_ShowsStartOrder_PageThreeInDatabaseOrder` | – | Test selbst unverändert (neuer Test dahinter) |
| `KaderStartSetupScreenTests.StartLeague_BeforeFirstManager_IsAdopted` | M01,M02,M41 |  |
| `KaderStartTeil2SetupTests.WorldOnly_ClickOnSecondRow_EditsFirstManager_NoSecondManager` | M04,M42 |  |
| `KaderStartTeil2cScreenTests.OwnListingClick_WithBid_OpensBidWindow` | N10 |  |
| `KaderStartTeil2cScreenTests.OwnListingClick_WithoutBid_ReturnsPlayerToSquad` | M18,M44 |  |
| `KaderStartTeil4ScreenTests.ContractsScreen_HeaderAndDialogFrame_MatchDosBoxPictures` | M29,M30 |  |
| `KaderStartTeil4ScreenTests.SeasonEnd_ListIsPictureBeforeNegotiation_RedrawnAfterDeparture` | M27,M28,M31 |  |
| `LifecycleScreenRegressionTests.R204_OriginalDataLoanAndContractEndAreConsistentAfterContinueThroughSeason` | M24,P4 |  |
| `LineupAutoDisabledMessageTests.ManualChange_DisablesAutoLineup_AndShowsMessageOnce` | P2 |  |
| `LineupShortMessagesE2Tests.Field_ClickOnHealthyPlayer_ShowsNoMessage` | – |  |
| `LineupShortMessagesE2Tests.Field_ClickOnSuspendedOrInjured_ShowsNochGesperrt_AndKeepsPlayer` | N11a |  |
| `LineupShortMessagesE2Tests.List_Injured_ShowsSpielerIstVerletzt` | P1 |  |
| `LineupShortMessagesE2Tests.List_Suspended_ShowsSpielerIstGesperrt` | P1 |  |
| `OriginalDialogCallersTests.Lineup_AutoDisabledMessage_IsShortMessageWith45Ticks` | P2 |  |
| `OriginalDialogCallersTests.Lineup_WrongRole_IsShortMessage_AndIgnoresClicks` | M33 |  |
| `TacticsClubScreenTests.Lineup_RefusesTwentyThirdDressed` | N12,N13 |  |
| `TacticsClubScreenTests.Tactics_OnlySemiEditable` | M07 |  |
| `TeamScreenTests.Lineup_ClickPlayerThenSlot_ChangesGoalkeeper` | N11c,P2 |  |
| `TeamScreenTests.Tactics_ModesBlocksAndSwitchToLineup` | M07 |  |
| `TransferVertraegeKomplettTeil5ScreenTests.MarketBid_KeepDeclines` | M18 |  |
| `WaveBReviewScreenTests.PR3502_SecondBidBecameInvalid_ShowsMessageAndDoesNotReopenSameBid` | N08,N09 |  |
Ergebnis: Alle Tests mit geänderter Behauptung werden rot, bis auf `GoalkeeperIntoFieldLine_IsRejected` (doppelte Artprüfung, äquivalente
Mutation) und `MarketLoan_EndsFree…` gegen die Variante N14 (R14KS-03). Bei acht weiteren Einträgen ist nur der Text um den Test
geändert, die Regel nicht.

### Akzeptierte `[APPROX]` (geprüft)

- U15-c Tausch ohne die Zufallszüge der Schleife
- A046 Migration aus dem letzten Platzfeld (vor dem 1. Spieltag einer Folgesaison 0 statt Endstand)
- A135 WM-Spiele in der Liga
- A180 Altstände mit der ausgeglichenen Zeile
- A362 Hintergrund wie „Verträge“, kein Spielstandsfeld für das alte Gehalt
- A380 voller Kader → vertragslos; Klickfläche des eigenen Markt-Platzes
- A115 Leihe zwischen Managern nur nach dem Code
- A010 volle Reihenhälfte (Begründung schärfen, R14KS-06)
- 375h direkt nach dem Spiel des Vereins

### Annahmen (nicht geprüft)

- Ob das Original Satz 4 (Markt) in Heilung und Sperren einbezieht (siehe R14KS-02, Rand).
- Die Wirkung von DGROUP 0x9744 außerhalb des Highscores.
