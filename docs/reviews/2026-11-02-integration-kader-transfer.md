# Review: Integration Runde 11, Spur `kader-transfer` (PR #462)

Unabhängiges Review (Claude Code, geplanter Lauf, 02.11.2026; nicht an Spur oder Integration beteiligt) nach
`docs/vorlagen/review-anfrage.md`. Geprüft am nachgewiesenen Checkout (Klon, `git rev-parse HEAD` =
`ff7593437b121db31b24570b854c8661bb787308`, `git status --porcelain` leer) mit Originaldaten aus
`kaaasi/eishockey-manager-referenz` (`original/`). Mutationen und Hilfsproben nur in getrennten Worktrees, danach verworfen.

| PR | Base | Head |
|---|---|---|
| #462 | `606403065b72f7098edd1d57afa0cebda4ad1020` | `ff7593437b121db31b24570b854c8661bb787308` |

Die Teile #418 → #427 → #434 → #437 → #453 → #457 → #459 waren nicht einzeln reviewt; dieses Review deckt den Stapel
(`e871f0e`) und die drei Commits der Leitstelle ab.

## 1. Merge-Empfehlung

**Nein für genau diesen Head – allein wegen KT-01 (P2); nach Annahme durch den Nutzer oder Korrektur ja.** Alle
Gegenbeispiele der Spur werden mit gezielter Mutation rot (Ausnahme KT-03, nur über Abdrücke), die Konfliktlösung verliert
nichts, Migration 22 → 30 und 27 → 30 laufen samt Rundlauf und Weiterspielen, `tools/pruefen.sh` ist in allen drei Läufen
grün mit den Zahlen des PR. Die vier Nachführungen in `ff75934` verdecken keinen Fehler (Ursachen unten). Offen ist nur, dass
die Kalibrierprobe nicht die „strenge Fassung aus #352“ ist, wie PR und `arbeitspakete.md` sagen: Eine zweite Lockerung aus
der Spur bleibt stehen (KT-01). Der Rest ist P3.

## 2. Befunde

| ID | P | Kurz |
|---|---|---|
| KT-01 | P2 | Kalibrierprobe: passiver Bundesliga-Manager weiter von der Entlassungsgrenze ausgenommen, gegen #352 und gegen PR-Text |
| KT-02 | P3 | `TestSquadKeeper.KeepPlayable` zieht doch Zufall (Sponsorzuschuss), Kommentare sagen „kein Zufallszug“ |
| KT-03 | P3 | RN-2 (Restlaufzeit +0x0B = 1 beim Verlängerungsangebot) hat keinen eigenen Test |
| KT-04 | P3 | #364: Kommentar der Verhandlungsprobe nennt den falschen Annahmeweg |
| KT-05 | P3 | Veraltete oder falsch angehängte Kommentare (Mode4, Doc-Kommentar `AssertEnvelope`, Migration Teil 2) |

**KT-01 (P2).** [`FinanceCalibrationTests.cs` Z. 356](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L356):
`if (active || league != League.Bundesliga) Assert.True(lowest > Manager.DebtLimit, …)`. Auf `main` (#352, geld Teil 7)
steht die Prüfung ohne Bedingung. Die Ausnahme kommt aus der Spur (Teil 1, A170) und ist in `e242ce1`/`aacf3f1` geblieben;
ersetzt wird sie durch `maxDebtDays < DebtDaysUntilDismissal`. Die Zusammenfassung der Probe (Z. 152, „Entlassungsgrenze
nie erreicht“), der PR-Text („strenge Fassung aus #352 ohne Ausweg“) und `docs/arbeitspakete.md` nennen das nicht.
Gegenbeispiel: Bedingung entfernen → `FourSeasonChanges_AccountAndLedgerStayWithinOriginalEnvelope(Bundesliga, active: False)`
und `…_Synthetic_…(Bundesliga, active: False)` rot (Konto unter −1,5 Mio. DM; laut Kommentar Seed 41 −1.586.947 DM). Die
Ausnahme ist also nötig, nicht tot – aber sie lockert eine Abnahme der Spur `geld`. Entscheidungsbedarf (Nutzer): Ausnahme
als Teil von Entscheidung C annehmen und in Zusammenfassung, `arbeitspakete.md` und PR nennen – oder die Probe so aufbauen,
dass die Grenze wieder für alle gilt.

Zum Rest von Punkt (1) der Anfrage, bestätigt:
- `TestSquadKeeper.KeepPlayable` läuft nach `StartNextSeason`, das Gehaltssoll (`wagesPerMonth`) wird erst zu Beginn der
  nächsten Schleife aus dem dann gültigen Kader gelesen; die Probe prüft weiter `Gehaelter == −7 · wagesPerMonth` exakt und
  bleibt grün. Käufe zum Preis 0 buchen nichts (`Transfers == compensation`, alle anderen Kategorien 0, grün). Das Soll wird
  also nicht verfälscht. Ohne `KeepPlayable` werden drei passive Fälle rot (Bundesliga beide Datenbanken, Oberliga Originaldaten).
- `PassiveBundesligaManager_WithoutCare_IsDismissed` wird rot, wenn die Entlassung ausgeschaltet wird (`SeasonFlow.CheckDebts`:
  `m.Dismissed = true` übersprungen) und wenn die Strafe „Weniger als neun Spieler“ 0 bucht (`StadiumStatistics.cs` Z. 178);
  beide Seeds jeweils rot.

**KT-02 (P3).** `KeepPlayable` kauft über `TransferMarket.Buy` → `Sign` → `Complete` → `RollSponsorSubsidy`
([`Transfer.cs` Z. 425](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/src/EishockeyManager.Core/Transfer.cs#L425),
`Transfer.ComputerClub.cs` Z. 84: `random(0, 6)` aus `game.Random`, auch für Vertragslose). Seit Teil 3 stimmen
[`TestSquadKeeper.cs` Z. 11/15](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/tests/EishockeyManager.Core.Tests/TestSquadKeeper.cs#L11)
(„ohne eigene Zufallszüge“, „zieht keine Zahl“) und `FinanceCalibrationTests.cs` Z. 340 („kein Zufallszug“) nicht mehr. Das
Angebot wird verworfen (keine Buchung), deshalb kein Einfluss auf das Soll; die Proben mit `KeepPlayable` verbrauchen aber
Hauptzufall. Kommentare berichtigen.

**KT-03 (P3).** RN-2 (Review #446): Das Angebot „von 1 auf n“ nur bei Restlaufzeit +0x0B = 1. Umgesetzt in
[`Transfer.Squads.cs` Z. 161](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/src/EishockeyManager.Core/Transfer.Squads.cs#L161)
(Prüfung nach den Würfen, wie 0x07865). Mutation `|| p.ContractYears != 1` entfernt: `KaderTransferTeil7Tests` 7/7 grün;
in ganz `EishockeyManager.Core.Tests` werden nur die Abdrücke rot (`RngTwinProbeTests` 5, `SpielberechnungAnschlussTests` 1,
`Mode4`). Abdrücke werden bei jeder Integration neu ermittelt und halten die Regel nicht fest. Ein Test mit festem Zufall
(Spieler mit 2 Jahren, beide Würfe 0 → kein Angebot) fehlt. RN-3 betrifft nur die Doku (`re-nachtrag.md`), am Code nichts zu
prüfen; Kauf übernimmt den Platz samt +0x19 (Tendenz-Probe `SlotFields_RollTrendAfterEnergy` rot bei Mutation, siehe 3.).

**KT-04 (P3).** [`IntegrationRunde8Welle3Tests.cs` Z. 153–157](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/tests/EishockeyManager.Core.Tests/IntegrationRunde8Welle3Tests.cs#L153):
„angenommen … über `years == current && salary >= currentSalary`, nicht über die 120-%-Regel“. Mutation dieses Zweigs in
`Contracts.Decide` (`Player.Contract.cs` Z. 480 → `false && …`): Probe bleibt grün – 99.900 DM wird auch über
`ContractRules.Accepts` angenommen. Richtig ist der zweite Satz: Ohne Zurückschreiben `TooShort` (die Probe hält das
Zurückschreiben also fest). Den ersten Satz korrigieren.

**KT-05 (P3).**
- [`GameModeTests.cs` Z. 335–347](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/tests/EishockeyManager.Core.Tests/GameModeTests.cs#L335):
  Teil-7-Kommentar „Kader schrumpft …, in Saison 5 … entlassen (−4.592.736)“; der Abdruck am Head zeigt Manager 1 jede
  Saison mit 400.000 bzw. 500.000 DM (S-K1, Oberliga). Die Integrationszeile in der Zusammenfassung (Z. 316) nennt nur
  geld + saisonwechsel; anders als bei `DebCupDrawFlowTests` fehlt die Zeile für kader-transfer.
- [`FinanceCalibrationTests.cs` Z. 141–160](https://github.com/kaaasi/eishockey-manager-redone/blob/ff7593437b121db31b24570b854c8661bb787308/tests/EishockeyManager.Core.Tests/FinanceCalibrationTests.cs#L141):
  Der neue Test wurde zwischen den Doc-Kommentar von `AssertEnvelope` und die Methode eingefügt; der Test hat jetzt zwei
  `<summary>`, `AssertEnvelope` keinen.
- `KaderTransferTeil2Tests.cs` Z. 99: „Migration 22 → 23“ (Test heißt jetzt `Migration_FromSchema27`).

### Punkt (2): Nachführungen in `ff75934` – Ursachen nachvollzogen

- **`SaisonwechselDoppelwechselTests` (SW-01).** Hilfsprobe am Head ohne `winRound` (Seed 1, Verein 12, alle Ligaspiele 9:0):
  Verein 12 steht in der Aufstiegsrunde (Gruppe Bundesliga: 14, 6, 10, 12) und wird dort Letzter; Aufsteiger 14. Die Runde
  wird nicht umgeschrieben und ist Zufall – die Probe hing am Zufallsverlauf. Mit `winRound: true` wird das Gegenbeispiel
  fest herbeigeführt (wie in der Mehrsaisonprobe); das schärft die Probe, verdeckt nichts.
- **`PlayerFactoryTests.TalentSearch_TalentIsComplete`.** Hilfsprobe am Head: in der ersten Saison fünf Suchen, jede endet
  mit „Ihr Talentspäher gibt die Suche ohne Erfolg auf“ (Level 3, Chance 1/5 je Suche, `FinanceRandom`); Kader 25, also
  nicht die Grenze 28 (`TalentScout.MaxOwned`), die einen Erfolg ohne Spieler ergäbe. Die sechste Suche (Saison 1994) ist ein
  Erfolg, ein Spieler kommt. Reiner Zufallsverlauf; die Verlängerung über das Saisonende ist sachgerecht.
- **`SponsorBewertungTests`.** Der neue Zweig prüft `Konto = 400.000 + Prämien` am Saisonende genau, wenn
  `KeptInOberliga` gilt (S-K1, Datei 0x562D8), und zählt nur diese Differenz neutral; die tägliche Kontoprüfung bleibt
  sonst unverändert. Vorher rot mit Konto 500.000 statt Soll – der Zweig wird also betreten.
- **`LifecycleScreenRegressionTests.R204`.** Die Probe hält ausdrücklich den beobachteten Ausgang fest (#353); der neue
  (zurück im Poolplatz, Herkunft 59, ohne Vertrag, in keinem Kader) ist einer der beiden dokumentierten stimmigen Ausgänge,
  und die Probe schließt weiter jeden Kader aus.

### Punkt (3): Abdrücke und Konfliktlösung

- **Abdrücke an allen drei Spuren:** je eine Mutation pro Spur – kader-transfer A170 (Bundesliga 25 → 20), saisonwechsel
  S-K1 (`manager.Money = OberligaKeptMoney` aus), geld Teil 6 (Preiszug 0x3EEAB aus) – macht `RngTwinProbeTests`,
  `SpielberechnungAnschlussTests` und `Mode4` jeweils rot. `DebCupDrawFlowTests` bleibt bei A170 grün (kein
  Bundesliga-Managerverein in diesem Ablauf); dort tragen laut Kommentar Teil 4/5/7 – nicht weiter geprüft.
- **Konfliktlösung:** Merge `606403065` + `e871f0e` nachgestellt: dieselben 9 Konfliktdateien. Je Datei gegen beide Eltern
  verglichen: `SaveGame.cs` Kette 22–26 (geld) + 26 → 27 (saisonwechsel) + 27–29 → kader-transfer, nichts entfallen;
  `TransferDialog.cs` beide Seiten (Subsidy/Seller der Spur, Kommentar „gewürfelt Datei 0x3EEAB“ von geld);
  `TransferVertraegeKomplettTeil5Tests` = Fassung der Spur (Lösung #357 ohne Schwelle; die Schwelle 8 aus saisonwechsel
  entfällt mit Grund); Abdrücke neu; `SaveGameTests` Fingerabdruck neu mit allen Schemazeilen. Unterschiede außerhalb der
  Konfliktdateien gegen die automatische Zusammenführung: nur die drei Migrationskommentare, die Schemanummern in den
  Migrationstests der Spur, A360 (`PlayerStatisticsScreen`, `PlayerInfoPopup.Price`, neuer Test) und `arbeitspakete.md`.

### Erwartet offen – bestätigt

Doku-Anschlüsse (re-meldungen.md, re-geld.md, reverse-engineering.md, spielberechnung.md §15, Inventur) stehen als offen in
PR und `arbeitspakete.md`; Verletzungsart in `MatchEngine.Injuries.cs` → Integration `wettbewerbe-spiel`; U4/U5/U9/U15 →
Runde 12. Alle drei im PR unter „Restlücken“.

## 3. Ausgeführte Prüfungen

Setup: `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`, SDK 10.0.402.

`tools/pruefen.sh` (alle drei Läufe) am Head `ff75934`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1853 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1560 | 0 | 261 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1560 | 293 | 0 |

Gezielte Mutationen (je eine, `EM_REQUIRE_GAMEDATA=1`, `dotnet test --filter`; rot = erwartet):

| Teil | Mutation | Ergebnis |
|---|---|---|
| 1 A170 (0x29A3F) | Bundesliga (2, 10, 13) → (2, 8, 10) | rot: `KaderLigaGroesseTests` (u. a. FILE01/09/10) |
| 2 Kaufpreis +0x2C | `PurchasePrice = offer` entfernt | rot: 3 in `KaderTransferTeil2Tests` (Kauf, Leihe, Mehrsaison) |
| 2 Verletzungsart +0x17 | – | Setzen erst mit `wettbewerbe-spiel` (erwartet offen); Migration geprüft |
| 3 F005 (0x58DC) | Käufer nicht gestärkt | rot: 3 in `KaderTransferTeil3Tests` |
| 3 F006 (0x63BE) | kein Zuschuss | rot: `ComputerMarketLoan_OffersSubsidy_AnswerBooks` |
| 4 Takt (0x54E71) | `CatchUp` nie | rot: `Season_MarketRunsEveryCalendarDay`, Mehrsaison |
| 4 Rückfrage (0x47608–0x47743) | `askSeller` aus | rot: 3 × `SellerQuestion_AnswerDecides` |
| 4 Tendenz (0x4907A) | `RollTrend` aus | rot: `SlotFields_RollTrendAfterEnergy`, `TrendScore_RoundTripsAndMigrates` |
| 4 Kontoleiste (0x463CC) | Forderungen nicht in Anlagen | rot: `AccountBar_FollowsOriginal` |
| 5 A226 (+0x1C) | Übertrag 0 | rot: 4 in `KaderTransferTeil5Tests` |
| 5 0x57E3 | Umverteilung beim Start aus | rot: 3 in `KaderTransferTeil5PoolTests` (u. a. FILE05-Verteilung) |
| 5 0x7C7E | Umverteilung beim Wechsel aus | rot: `SeasonChange_RefillsManagersLeague_FourChanges` (beide) |
| 5 Drift 0x54D9 | `− 4` → `− 3` | rot: `Drift_StaysWithinThreeAndBounds` u. a. |
| 5 0x7EF1/0x5521 | Zähler bleibt stehen | rot: `NextPoolClub_CyclesFirstFiveTwiceAndSkipsManagedClub` |
| #347 | `random(0, 5)` → `random(0, 4)`; Farbe 201 → 202 | rot: `DrawIndex_NationalShareIsOneInSix`; `HoveredEntry_RecolorsSeparatorsAboveAndBelow` |
| #357 | Namenssperre ohne `held` | rot: `OriginalDatabase_FourChanges_NamesUniqueAndIdsDistinct` |
| #364 | Annahmezweig `years == current` aus | grün → KT-04 |
| 7 Ruhestand (0x07169) | Ankündigung aus | rot: `Retirement_AnnouncedRegardlessOfContract` |
| 7 RN-2 | `ContractYears != 1` entfernt | grün im Teil, nur Abdrücke rot → KT-03 |
| A360 (Leitstelle) | `context.MarketValue` statt Kaufpreis | rot: `PlayerStatisticsPurchasePriceTests` |
| Entscheidung C | Entlassung aus; Strafe 0 | rot: `PassiveBundesligaManager_WithoutCare_IsDismissed` (beide Seeds, je beide) |
| Entscheidung C | Ausnahme Bundesliga passiv entfernt; `KeepPlayable` entfernt | rot (2 bzw. 3 passive Fälle) → KT-01 |

#353 (Leihprobe, R204) ist eine reine Probe mit festem Ausgang; geprüft über den Ursachenabgleich in Abschnitt 2.

Migration (Hilfsprobe am Head): Spielstände an `02c4bd7` (Schema 22, vor geld) und am Base `6064030` (Schema 27) erzeugt,
je mitten in der Saison (12 Spieltage) und am Saisonende, Originaldatenbank, zwei Manager (Verein 2 und 30). Am Head:
Laden ohne Fehler, Speichern mit Schema 30, Rundlauf byte-gleich, zwei Saisons weitergespielt (Kader per `KeepPlayable`),
erneuter Rundlauf byte-gleich; 4/4 grün. Kaufpreis ≥ 0, Tendenz und Übertrag nach dem Weiterspielen gesetzt.

**Akzeptierte `[APPROX]`** (geprüft, Begründung trägt): Migration Teil 2/4/5 (Kaufpreis 0, Art unbekannt, Tendenz 0 → Stufe,
Übertrag 0, Zähler 0); eigene Zufallsquelle `TrendRandom`; Zuschuss auch für Vertragslose (im Original dieselbe Liste;
in `0x063BE`/`0x47A00` keine Preisprüfung gesehen); Fragefenster für Zuschuss und Verkäufer bis zum Baustein 0x4C246.

**Annahmen (nicht geprüft):** Originaltreue der übrigen Spur-Fundstellen über die Tests hinaus (keine Disassembly-Nachprüfung
außer 0x063BE/0x479F0 für KT-02); Bildschirme der Teile 2–4 nur über ihre Tests, kein Bildvergleich mit Videobildern.
