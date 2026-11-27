# Nachprüfung: Integration Runde 14, Spur `kader-start` (PR #669)

Nachprüfung des Reviews `docs/reviews/2026-11-09-integration-kader-start-r14.md` (bisher geprüfter Head `c02c015`). Geprüft wird das
Delta `c02c0157d1915b59a2c17a88638df55afaa345d4..dbcadca2e9aa949eb9821e458bd62f42b5e41b20` gegen R14KS-01 bis -08, dazu R14N-01 bis -03
(`2026-11-09-integration-ui-rest-r14-nachpruefung.md`) und R14NT4-05/-07 (`2026-11-09-nationalteam-r14-4.md`). Im Delta liegen auch
die Merges von `main` mit #666 und #670.

Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext), 09.11.2026.

Checkout: Klon, `git rev-parse HEAD` = `dbcadca…`, `git status --porcelain` leer. Ich habe den Code gelesen **und** am nachgewiesenen
Checkout mit Originaldaten ausgeführt (`kaaasi/eishockey-manager-referenz` @ `779a1c8`, 52 Original-Stände). Mutationen und Proben liefen
in einem eigenen Worktree desselben Commits und sind nicht committet. Die Fundstellen habe ich selbst mit
`tools/reverse-engineering/disasm.py` nachgelesen.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/dbcadca2e9aa949eb9821e458bd62f42b5e41b20`

## 1. Merge-Empfehlung

**Ja** für Head `dbcadca`.

- Alle Befunde R14KS-01 bis -08, R14N-01 bis -03 und R14NT4-05/-07 sind behoben. Die Gegenbeispiele aus dem Review laufen jetzt durch:
  KÄNDLER lässt sich mit Originaldaten sichern, laden und automatisch speichern. Der Rückkehrer von der Liste beginnt die Saison mit
  den Zählern 0.
- Die zwei Merges von `main` sind sauber. Nur `docs/arbeitspakete.md` und `TacticsClubScreenTests.cs` hatten Konflikte, beide sind
  richtig gelöst. Von `main` geht nichts verloren. Der Head merged konfliktfrei mit dem aktuellen `main` (`ec2ab1e`, dort nur eine
  Review-Datei mehr).
- Der HOK-Import der Felder aus Schema 42/43 stimmt an allen 52 Original-Ständen. Die Rundreise ist bytegleich.
- Alle drei Läufe von `tools/pruefen.sh` sind grün. Die Zahlen decken sich mit dem PR.
- Es bleiben nur P3-Befunde: Testlücken für eine Methode noch ohne Aufrufer und für einen Importwert, eine doppelte `<summary>` und
  eine Textzeile 1 px tiefer als im Bild (schon vorher so). Keiner davon hindert den Merge.

## 2. Befunde

### Status der Befunde aus dem Review

| Befund | Status | Nachweis (selbst ausgeführt) |
|---|---|---|
| R14KS-01 (P0) | behoben | Probe mit Originaldaten: `StartNewGame` → `Offer` (KÄNDLER, ID 886) → `FindPlayer` liefert ihn. `Save` → `Load`: Spieler gefunden, genau ein eigener Markt-Platz. Autospeicher (Zähler auf `Interval`) schreibt die Datei, sie lädt, `LastAutoSaveError` = null. [`AllPlayers` Z. 44–46](https://github.com/kaaasi/eishockey-manager-redone/blob/dbcadca2e9aa949eb9821e458bd62f42b5e41b20/src/EishockeyManager.Core/Player.Identity.cs#L44-L46). Rückmutation K01 → 2 Tests rot |
| R14KS-02 (P1) | behoben | `ReturnListedPlayers` steht jetzt vor `CarryGamesForClub`/`ResetMatchCounters` ([Integration Z. 119–123](https://github.com/kaaasi/eishockey-manager-redone/blob/dbcadca2e9aa949eb9821e458bd62f42b5e41b20/src/EishockeyManager.Core/GameState.Integration.cs#L119-L123)). Die Probe aus dem Review ist jetzt der Test `ListedOverSeasonEnd_…`. Rückkehrer und Stürmer stehen auf (0,0,0,0,0), die Spiele beim Verein bleiben. Original nachgelesen: Die Managerschleife endet bei Datei 0x279ED–0x27A09, das Löschen beginnt bei 0x27A0C. Rückmutation K02 → rot |
| R14KS-02, Heilung | behoben | Datei 0x0727C–0x072CB selbst gelesen. Die zweite Schleife des Tageslaufs 0x07169 läuft über alle Markt-Plätze (Satz 4, DGROUP 0x7623 + 0x34 · i). Bei Pool +0x1A = `[0xA70E]` ruft sie 0x7C12 auf. 0x7C12 (Datei 0x7C12–0x7C7D) zählt +0x0D nur bei Bit 0x02 (verletzt) herunter, und nur an Tagen mit (Tag − `[0x6AF]`) mod 7 = 0. Bei +0x09 & 3 ≠ 0 und +0x0D = 0 löscht sie beide Bits und ruft 0x426BC auf. Die Umsetzung über `HealInjuries` stimmt damit. Rückmutation K02h → rot |
| „Sperren zählt der Spielkern nicht“ | bestätigt | `SuspendedGames` wird in `src/` nur auf 0 gesetzt (Import, `Player.Factory`), nie > 0. Auch 0x7C12 zählt keine Sperre herunter (nur Bit 0x02) |
| R14KS-03 (P2) | behoben | Mutation N14 (Leihende → zurück in den Kader des Computervereins, Vertrag 0) macht `MarketLoan_EndsFree…` und `R204_…` rot. Geänderte Eingabe `Age` 25 → 32: Ohne sie dürfte die Kader-KI ihn als Vertragslosen ziehen. Das ist begründet und lässt die Behauptung „in keinem Kader“ eindeutig |
| R14KS-04 (P3) | behoben | Nur Kommentare geändert. Im Delta ändert sich kein Abdruck, kein `InlineData` und keine Saat (`git diff` geprüft) |
| R14KS-05 (P3) | behoben | M43, M45a (Linie x 5/159) und M45b (Doppelpunkt x 77) → je 1 Test rot |
| R14KS-06 (P3) | behoben | `<summary>` von `ManagerRowFor`/`CanStart` richtig zugeordnet. `[APPROX]` in `Player.Contract` entfernt (`managerSquad: true`, A115). Begründung in `Squad.Lineup` richtig: `SetUnchecked` gibt es im Remake nicht (`git grep` leer), `Lineup.Fits` prüft die Art. Die Angabe im Review #677 war also falsch |
| R14KS-07 (P3) | behoben | Die fremden Dateien stehen einzeln im PR-Text, auch `MatchEngine.Injuries.cs`, `OriginalImport*.cs`, `TeamUi.cs`, `TransferDialog.cs` und `TransferScreen.cs` |
| R14KS-08 (P2) | behoben | Datei 0x4913E–0x4919A selbst gelesen: Rahmen 0xB155(2, 0x1C + y, 0xA8, 0x1C + y + h, 4, 1), Linie 0xB7B5(6, y + 0x2B, 0xA3, y + 0x2B, Farbe 7). `OfferBox` (2, 29, 165, 71) mit Schatten bis x 168 = 0xA8, Linie x 6–163. K08a und K08b → rot. Knopf `CancelButton.X` = 0x0B (vorher `OfferBox.X + 10` = 11, Wert also gleich) |
| R14N-01 | behoben | Bildpunkte (Umrechnung (1 + 2x \| 26 + 2y)): `file40_08`, `file40_09` (168 \| 31), (4 \| 73), (168 \| 73) und `file46_3`, `file40_12` (168 \| 31), (4 \| 97), (168 \| 97) sind alle (81 \| 113 \| 130) = Index 4. Die Nachbarn (168 \| 32), (5 \| 73), (167 \| 73), (168 \| 72) sind (48 \| 81 \| 97) = Index 6. Damit ist der geänderte Sollwert `At(168, 73) == 4` belegt. N01 → 2 Tests rot |
| R14N-02 | behoben (Rest P3, siehe unten) | Bildschirmfoto `transfer` am Head gegen `file40_03`/`file40_04`: Zeilen 223 hell, 224–236 Füllung, 237 dunkel, 238–239 Schatten. Spalten 317 dunkel, 318–319 Schatten wie im Bild. Abweichend sind 10 von 1280 Pixeln der Rahmenzeilen, nur Ecken und Randspalte x 0. „Saldo“ in (24 \| 36 \| 73) = Index 240, gleich im Bild. Ecken als `[APPROX]` akzeptiert (siehe Testlücken). N02a, N02b → rot |
| R14N-03 | behoben | Datei 0x4BEBD–0x4BFF1 und 0x4BFF2–0x4C196 gelesen. Der Zustand (`[bp+0xE]`, 0/1) wirkt nur auf die Quellzeile 0xB3 + 12 · Zustand der kb\2-Grafik, Lage und Text der Beschriftung bleiben gleich. Rückmutation `+ 1` → `+ 2` (N03b) → rot. (Meine eigene Variante N03, nur die Schattenzeile y + 5, bleibt grün; die prüft der Test nicht, unkritisch) |
| R14NT4-05 | behoben, Testlücke P3 | Datei 0x0718F–0x07279 gelesen: Die erste Schleife mit dem Gebotsverfall (0x07215–0x0726A) läuft nur bei `[0xA70E]` = 0. Die zweite Schleife würfelt nur Markt-Plätze mit Pool +0x1A = Manager. 0x494EF wird in 0x07169 nicht aufgerufen. `CatchUpManagerDay` bildet das ab. T05c (Listenwechsel) → rot, T05a/T05b → grün (R14KS9-01) |
| R14NT4-07 | behoben | `AdvanceFinanceInWorldCupLoop`: im Delta unverändert. Rechtsklick in der Vereinsaufstellung: `Slot_LeftClickClears_RightClickLeaves` ist grün |

**Erwartet offen, bestätigt:**

- Ein importierter eigener Spieler auf dem Markt bleibt im Kader. Die Import-Probe trifft genau einen solchen Fall: FILE26 Satz 0,
  Kaderplatz 24 = HOCK. Der HOK-Kaderplatz ist leer, das Remake führt ihn im Kader mit den Zählern des Markt-Platzes. Gehört zu
  #676.
- `CatchUpManagerDay` hat in `src/` keinen Aufrufer (`git grep`). Den Aufruf stellt #678 um.
- Inventurzeilen → `pruefung-karte`. `docs/analyse/inventur.md` ist im eigenen Delta nicht geändert.

### R14KS9-01 (P3) – Testlücken: `CatchUpManagerDay` und Pokal-/Europapokal-Überträge im Import

- **`CatchUpManagerDay`:** [`KaderStartRunde14FixTests` Z. 150](https://github.com/kaaasi/eishockey-manager-redone/blob/dbcadca2e9aa949eb9821e458bd62f42b5e41b20/tests/EishockeyManager.Core.Tests/KaderStartRunde14FixTests.cs#L150)
  läuft mit einem einzigen Manager (`TestGames.Create`). Deshalb bleiben zwei Mutationen grün:
  - T05a: `expire: true` für jeden Manager.
  - T05b: Filter `l.Seller == only` entfernt, also Angebote auch auf fremde Markt-Plätze.

  Beide Regeln sind belegt (siehe oben), aber ungetestet. Zu ergänzen ist ein Test mit zwei Managern: Manager 1 löscht keine Gebote,
  und auf den Platz von Manager 0 kommt kein Angebot. Am besten zusammen mit dem Aufruf in #678.
- **Import der Überträge:** Mutation I3 (`ClubCupGamesCarry = 0`) lässt
  [`KaderStartRunde14ImportTests`](https://github.com/kaaasi/eishockey-manager-redone/blob/dbcadca2e9aa949eb9821e458bd62f42b5e41b20/tests/EishockeyManager.Core.Tests/KaderStartRunde14ImportTests.cs#L21) grün. In FILE39/FILE40
  gilt +0x1E = +0x07, es gibt dort also keinen Übertrag. Meine Probe über alle 52 Stände wird bei I3 rot (Soll 2, Ist 0). Die
  Abbildung stimmt also, nur die Theorie braucht einen Stand ab der zweiten Saison.
- **Europapokal ohne Daten:** I4 (`ClubEuropeGoalsCarry = 0`) und I7 (`ClubEuropeGamesCarry = 0`) bleiben auch in meiner Probe grün.
  Keiner der 52 Stände hat Europapokalspiele oder -tore (Summe +0x08 = 0, +0x05 = 0). Die Abbildung +0x05/+0x08/+0x20/+0x26 stützt sich
  nur auf die Offsets aus 0x5494C–0x5499E (A135). Das ist schlüssig, aber am Stand nicht belegt. Ein Kommentar dazu in der Feldkarte
  genügt.
- **Ecken der Kontoleiste:** N02c (Ecken nicht zurückgesetzt) bleibt grün. Das ist ein `[APPROX]` ohne Bildbeleg, deshalb hingenommen.

### R14KS9-02 (P3) – Doppelte `<summary>` in `TransferScreen`

[`TransferScreen.cs` Z. 484–490](https://github.com/kaaasi/eishockey-manager-redone/blob/dbcadca2e9aa949eb9821e458bd62f42b5e41b20/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L484-L490): Der neue Block für `AccountBar`
steht direkt hinter dem alten `<summary>` von `DrawAccountBar`. Damit hat `AccountBar` zwei Zusammenfassungen und `DrawAccountBar`
keine. Es ist dasselbe Muster wie R14KS-06. Der alte Block muss vor `DrawAccountBar` stehen.

### R14KS9-03 (P3, schon vorher so, `ui-rest`/`kader-transfer`) – Beschriftung der Kontoleiste 1 px zu tief

Nachgemessen am Bildschirmfoto `transfer` gegen `file40_03`/`file40_04`:

- Die oberste Schriftzeile der vier Felder steht im Bild bei y 225, im Remake bei y 226. Das gilt für „Kontostand“, „Schulden“,
  „Anlagen“ und „Saldo“.
- Die unterste Zeile (Betrag) ist gleich (y 236).
- „Anlagen“ beginnt im Bild bei x 133, im Remake bei x 132.

Das ist der vorhandene `[APPROX]` „Zeilen 226/232 … nicht nachgemessen“. Seit die Leiste eine Zeile höher liegt, ist er messbar. Durch
das Delta wird nichts schlechter. Hinweis an die Spur, die `TransferScreen.cs` besitzt.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe, Head, Originaldaten)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2542 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2019 | 0 | 433 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2020 | 522 | 0 |

Commit `dbcadca2e9aa949eb9821e458bd62f42b5e41b20`, SDK 10.0.402, Ergebnis: bestanden.

### Merges von `main`

Geprüft mit `git show --remerge-diff`. Das zeigt nur Abweichungen vom automatischen Merge.

- **`717cd3d`** (Eltern `c02c015`, `60c4bb0` = `main` mit #666): Konflikte nur in zwei Dateien.
  - `docs/arbeitspakete.md`: Schemazeile vereint (`ui-rest` bleibt 41, `kader-start` 42/43, weiter ab 44), beide Statuszeilen behalten.
  - `TacticsClubScreenTests.cs`: Sollwert 0 aus A180 (kader-start) und der Klick in die untere Hälfte des Modusknopfs (ui-rest).
    Beides ist richtig.
  - Alle übrigen Dateien entsprechen genau dem automatischen Merge.
- **`b268372`** (Eltern `fd7cf66`, `8a4643c` = `main` mit #670/#680): Konflikt nur in `docs/arbeitspakete.md`, beide Zeilen
  behalten. Sonst automatischer Merge.
- Die Feldkarten-Tests, die nach dem Merge rot waren, behebt `f797efb` mit dem Import und der Feldkarte.
- `git merge-tree --write-tree origin/main dbcadca` ist konfliktfrei.

### HOK-Import Schema 42/43 an Original-Ständen

Eigene Probe über **alle 52** `FILE*.HOK` aus `original/spielstaende`: Import, dann je Manager und Kaderplatz.

- **Zähler je Wettbewerb:** Für Liga, Pokal und Europapokal gilt jeweils:
  - `SeasonGames(k)` = +0x06 + k
  - `ClubGames(k)` = Wort +0x1C + 2k
  - bei Feldspielern zusätzlich `SeasonGoals(k)` = +0x03 + k und `ClubGoals(k)` = Wort +0x22 + 2k

  Alles gleich, mit einer Ausnahme: dem erwarteten Altfall HOCK (FILE26, siehe oben).
- **Tabellenfeld:** `StoredTableRank` = DGROUP 0x8288 + 0x50 · Satz, für alle 36 Ligasätze je Stand gleich. Hinweis: Das Original-Feld
  ist keine Permutation. FILE14 Liga 0 enthält zweimal 1, FILE23 ebenso. Der Import übernimmt es unverändert, das ist richtig.
- **Start-Liga:** `Setup.StartLeague` = DGROUP 0x11A in allen Ständen. Im Test bleibt der Wert auch ohne Manager stehen.
- **Rundreise:** Import → `Save` → `Load` → `Save` ist bytegleich in allen 52 Ständen.
- Der Torhüter bleibt `[APPROX]` (Gegentore ohne Wettbewerb). Das ist akzeptiert.

### Gegenbeispiele

- **R14KS-01:** Probe `Probe_KS01_OriginalData_ListedPlayer_SaveLoadAutosave` (UI-Tests, Originaldaten): KÄNDLER, ID 886, bestanden.
  Synthetisch: `ListedOwnPlayer_SaveLoad_ListingKept_WithdrawAfterLoad`.
- **R14KS-02:** Siehe Tabelle. Die Heilung der Markt-Plätze habe ich in Datei 0x0727C–0x072CB und 0x7C12 selbst gelesen.
- **R14KS-03, -05, -08, R14N-01 bis -03, `CatchUpManagerDay`:** Siehe Tabelle und Mutationen.

### Mutationen (Worktree am Head, je Mutation einzeln, gefilterte Testklassen, Basislauf 0 rot)

| ID | Rückmutation | rot |
|---|---|---|
| K01 | `AllPlayers` ohne Markt-Plätze | `ListedOwnPlayer_SaveLoad…`, `FourSeasonChanges_WithListedPlayers_SaveAndLoadEachSeason` |
| K02 | Rückkehr wieder nach dem Löschen der Zähler (`StartSquadSeason`) | `ListedOverSeasonEnd_ReturnsWithSeasonCountersReset…` |
| K02h | keine Heilung auf dem Markt-Platz | `ListedOwnPlayer_InjuryHealsLikeSquad` |
| N14 | Marktleihe endet im Kader des Computervereins (Vertrag 0) | `MarketLoan_EndsFree_NotBackToComputerClub`, `R204_MarketLoanEndsFree…` |
| M43 | `*` wieder in der Kaderliste | `TransferScreen_SquadList_NoStarForListedPlayer_EvenInLegacyState` |
| M45a / M45b | Linie x 5/159 bzw. Doppelpunkt x 77 | `ContractDialog_SeparatorSixTo163_ColonsAt76` |
| K08a / K08b | `OfferBox` (1, 29, 166, 71) bzw. Linie x 5/159 | `OfferWindow_AtXTwo_Width165_LineSixTo163` |
| N01 | Schattenecken in Index 6 | `DialogFrame_ContractWindow_OuterShadowCornersInFillColor`, `TransferBuyWindowDosboxTests.BuyWindow_…` |
| N02a / N02b | „Saldo“ Index 29 bzw. Leiste y 224 | `TransferScreen_AccountBar_LikeDosBox` |
| N02c | Ecken der Kontoleiste nicht zurückgesetzt | **keiner** (`[APPROX]`, R14KS9-01) |
| N03b | gedrückter Knopf: Beschriftung x + 1 | `ContractDialog_PressedButton_LabelAtSamePlace` |
| T05a | Gebotsverfall für jeden Manager | **keiner** (R14KS9-01) |
| T05b | Angebote auf alle Markt-Plätze | **keiner** (R14KS9-01) |
| T05c | Listenwechsel in `CatchUpManagerDay` | `CatchUpManagerDay_NoListChange_BidsOnlyOnOwnListings` |
| I1 / I2 | Pokal-/Europaspiele bzw. -tore 0 | `Import_CountersPerCompetition_FromSquadSlots` (beide Stände) |
| I3 | Übertrag Pokalspiele 0 | **keiner** im Repo; eigene 52-Stände-Probe rot (R14KS9-01) |
| I4 / I7 | Übertrag Europapokal 0 | **keiner**, auch Probe grün: keine Daten (R14KS9-01) |
| I5 | `_tableRank` nicht importiert | `Import_TableRankAndStartLeague` |
| I6 | `_startLeague` nicht importiert | `Import_TableRankAndStartLeague` |

### Geänderte Sollwerte und Eingaben (einzeln)

- `TransferBuyWindowDosboxTests`: `At(168, 73)` von 6 auf 4, dazu neue Ecken- und Nachbarpixel. Die Bildpunkte in `file40_08`/`file40_09`
  sind oben nachgemessen. Die Rückmutation N01 lässt den Wert fallen.
- `TransferVertraegeKomplettTeil3/5ScreenTests`: `CancelButton.X` hängt nicht mehr an `OfferBox.X + 10`, sondern ist fest 0x0B (Datei:
  Knopf bei 0x0B \| 0x53). Der Wert bleibt 11. Die Entkopplung ist nötig, weil `OfferBox.X` jetzt 2 ist.
- `KaderStartTeil4Tests.MarketLoan…`: Eingabe `Age` 25 → 32 (siehe R14KS-03). `LifecycleRegressionTests.R204`: Die Behauptungen sind
  strenger („in keinem Kader“, genau einmal vertragslos, genau einmal in `AllPlayers`).
- `TacticsClubScreenTests`: Konfliktlösung, siehe Merges.
- Keine Abdrücke, Saaten oder Schema-Abdrücke geändert. Schema bleibt 42/43 (`SaveGame.SchemaVersion` 43, `SaveGameTests` grün).

### AGENTS.md §4 und andere Spielarten

- **Saisonhaken:** Die Rückkehr läuft in `GameState.Integration.cs` (`OnNewSeasonManager`), die Heilung im Tageslauf `HealInjuries` →
  `OnDay…`. Es gibt keine eigenen Zähler.
- **Buchungen:** Das Delta fügt keine Buchung hinzu. Gehälter gelisteter Spieler laufen weiter über `MonthlyWages(this, club)`.
- **Bildschirme:** `DrawAccountBar` liest drei Pixel und schreibt sie zurück, nur beim Zeichnen. `TransferDialog`, `TeamUi` und
  `ContractDialog` zeichnen nur. Kein Spielzustand wird beim Öffnen geändert.
- **Mehrsaisonprobe:** `FourSeasonChanges_WithListedPlayers_SaveAndLoadEachSeason` läuft passiv und aktiv über 4 Wechsel und sichert
  je Saison zweimal (mitten in der Saison und am Ende). K01 macht den Test rot, er sichert also tatsächlich mit gelisteten Spielern.
  Grün sind auch die Mehrsaisonproben aus #669 und #670 im Lauf mit Daten, darunter alle 52 Stände über 4 Saisonwechsel.
- **„Nur Weltkugel“:** `ReturnListedPlayers` läuft in jedem Modus im selben Haken. Ohne gelistete Spieler macht es nichts. Die Tests
  für A056 (`WorldOnly_…`) sind grün.

### Akzeptierte `[APPROX]`

- Äußere Schattenecken der Kontoleiste: Hintergrund. Im Bild wechseln die Farben.
- `OfferBox`: kein DOSBox-Bild. Die Lage folgt aus dem Code, der denselben Aufruf wie `BuyBox` hat.
- Import: Torhüter-Gegentore ohne Wettbewerb und ohne Vereinswert.

### Annahmen (nicht geprüft)

- Europapokal-Felder des Imports: nur nach Offsets, kein Stand mit Europapokalspielen (R14KS9-01).
