# Review: Spur `saisonwechsel` (PR #419, #428, #431, #432)

Unabhängiges Review (Claude Code, geplanter Lauf, 02.11.2026; nicht an der Umsetzung beteiligt) nach
`docs/vorlagen/review-anfrage.md`. Geprüft am nachgewiesenen Checkout (Klon, `git rev-parse HEAD` = Head,
`git status --porcelain` leer) mit Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`).

| PR | Base | Head |
|---|---|---|
| #419 Teil 1 | `7332a0fd04628e1a63acde30d9131ecb2f2d2afb` | `4153da455a89309106ed1528dfb1af4a6cf54cd2` |
| #428 Teil 2 | `4153da455a89309106ed1528dfb1af4a6cf54cd2` | `6175a23bb3c8ccbe0b851ea938c8e4423ba58d48` |
| #431 Teil 3 | `6175a23bb3c8ccbe0b851ea938c8e4423ba58d48` | `8fd51c22c17483f130d77e7384ee84281be304e4` |
| #432 Teil 4 | `8fd51c22c17483f130d77e7384ee84281be304e4` | `853ebdfc0c3ecadfcb8f8f7feb4ba3c9168b2187` |

## 1. Merge-Empfehlung

- **#419 (Head `4153da4`): ja.** S-K1/S-K2 wie DOSBox-Beleg #389 nachgestellt (auch mit Originaldatenbank), alle
  Mutationen rot; nur P3 offen (SW-03).
- **#428 (Head `6175a23`): nein.** SW-01 (P1): Zwangsabstieg eines 2.-Liga-Vereins, der zugleich über die Aufstiegsrunde
  aufsteigt, ergibt Ligagrößen 12/13/11, die Folgesaison bricht mit `ArgumentOutOfRangeException` ab.
- **#431 (Head `8fd51c2`): nein, nur wegen des Stapels.** Eigener Inhalt (S-K4) richtig; enthält #428 und braucht nach dessen
  Fix eine Nachprüfung des Deltas.
- **#432 (Head `853ebdf`): nein, nur wegen des Stapels.** Eigener Inhalt (#329, #362, Modus-1-Probe) richtig; wie #431.

Nach dem Fix von SW-01 genügt eine Nachprüfung des Deltas `6175a23..<neu>` (und der neu gestapelten Heads).

## 2. Befunde

### SW-01 (P1) – Zwangsabsteiger aus der 2. Liga, der in der Aufstiegsrunde aufsteigt: Ligen 12/13/11, Absturz

[`SeasonFlow.Zwangsabstieg.cs:120–145`](https://github.com/kaaasi/eishockey-manager-redone/blob/6175a23bb3c8ccbe0b851ea938c8e4423ba58d48/src/EishockeyManager.Core/SeasonFlow.Zwangsabstieg.cs#L120-L145)
schreibt einen 2.-Liga-Verein in die Gruppe 2. Liga/Oberliga ein und trägt ihn in `RelegatedInto[Oberliga]` ein, lässt aber
`PromotedInto[Bundesliga]` der Gruppe Bundesliga/2. Liga unverändert. Steht er dort unter den ersten zwei, steht er in beiden Listen;
[`SeasonRecord.NextLeague`](https://github.com/kaaasi/eishockey-manager-redone/blob/6175a23bb3c8ccbe0b851ea938c8e4423ba58d48/src/EishockeyManager.Core/SeasonRecord.cs#L86-L92)
nimmt die erste Liga (Bundesliga). Der aus der Oberliga aufrückende Verein geht trotzdem in die 2. Liga, der Platz in der
Oberliga bleibt leer.

**Gegenbeispiel** (synthetische Datenbank, `SaisonwechselZwangsabstiegTests.Create(seed: 1, clubs: 12)`; alle 2.-Liga-Spiele von
Verein 12 als 9:0-Sieg umgeschrieben; Konto ab 13. April −700.000 DM, dann `PlaySeason` und `StartNextSeason`):

```
PromotedInto[Bundesliga]=17,12  RelegatedInto[ZweiteLiga]=10,5  PromotedInto[ZweiteLiga]=26,33  RelegatedInto[Oberliga]=12,22
Ereignisse Manager: ForcedRelegation; Verein 12 nächste Saison: Bundesliga; Ligagrößen 12/13/11
Folgesaison: ArgumentOutOfRangeException (Parameter 'count')
```

Nach dem Disassembly (Datei 0x55F93 ff., Gruppe nach Liga-Stufe 1; Austausch Gruppe 0 vor Gruppe 1, Gruppentabellen mit
Vereinsplätzen) bleibt im Original vermutlich der Zwangsabsteiger in der Bundesliga, und der Bundesligist, der durch den ersten
Tausch auf seinen Platz kam, wird mit der Gruppe 1 in die Oberliga getauscht (zwei Ligen tiefer); die Ligagrößen bleiben 12.
Das ist nicht belegt. **Erwartet:** eine bewusst gewählte Regel (am besten diese, `[APPROX]`), die 12/12/12 hält, und ein
Regressionstest für diesen Fall (rot am Head `6175a23`).

### SW-02 (P3) – Ewige Tabelle fehlt die letzte Saison, wenn am 29. April alle Manager entlassen werden

Seit #431 schreibt `FinishSeason` die Ewige Tabelle, Ewige Bilanz und „Früher“ fort
([`SeasonFlow.cs:292–295`](https://github.com/kaaasi/eishockey-manager-redone/blob/8fd51c22c17483f130d77e7384ee84281be304e4/src/EishockeyManager.Core/SeasonFlow.cs#L292-L295));
`ProcessDay` endet bei `GameOver` aus `CheckDebts` vorher. Vorher lief die Fortschreibung im Tageshook. Steht im PR als
Restlücke; nur aus dem Code gelesen, nicht ausgeführt. Kein Merge-Hindernis, in `docs/arbeitspakete.md` übernehmen.

### SW-03 (P3) – Test „keine Aufstiegsprämie beim Doppeltausch“ greift nicht

[`SaisonwechselOberligaTests.cs:83`](https://github.com/kaaasi/eishockey-manager-redone/blob/4153da455a89309106ed1528dfb1af4a6cf54cd2/tests/EishockeyManager.Core.Tests/SaisonwechselOberligaTests.cs#L83)
prüft `Premium % SinglePremium == 0`; die Aufstiegsprämie aus der Oberliga (300.000 DM) ist ebenfalls ein Vielfaches von 100.000 DM.
Mutation `SeasonPremium(league, rank, events)` statt `PremiumEvents(...)`: alle `SaisonwechselOberligaTests` grün, rot nur fremde
`SeasonEndStatusTests` (5 Fälle). Vorschlag: `Premium == SinglePremium · Anzahl Einzelprämien-Ereignisse`.

### SW-04 (P3) – Schwelle eines fremden Tests gelockert

[`TransferVertraegeKomplettTeil5Tests.cs:347`](https://github.com/kaaasi/eishockey-manager-redone/blob/4153da455a89309106ed1528dfb1af4a6cf54cd2/tests/EishockeyManager.Core.Tests/TransferVertraegeKomplettTeil5Tests.cs#L347)
`renamedClashes` 5 → 8 (Spur `kader-transfer`). Begründet (anderer Zufallsverbrauch, bekannte Lücke #357), der Kommentar darüber
nennt aber weiter „0–1 je Saison“. Leitstelle: in #357 vermerken, dass die Schwelle zurückgesetzt wird.

### Erwartet offen – bestätigt

- **Reihenfolge der Ewigen Tabelle nach dem Zwangsabstieg:** im Stapel gelöst (#431). `RecordSeasonStatistics` läuft in
  `FinishSeason` direkt nach `ApplyForcedRelegation`, der Aufruf in `GameState.Integration.cs` ist entfernt (nur Kommentar).
  Mutation „vor dem Zwangsabstieg“ wird rot. Kein Anschluss durch die Leitstelle nötig; Rest SW-02.
- **Schema 23 vorläufig:** bestätigt (`SaveGame.cs`, Migration ohne Datenänderung). Leitstelle nummeriert in der Integration.

### Besonders geprüft

- **#166:** in der Sache behoben (Managerverein bleibt, Platz nicht nachbesetzt, Konto 400.000 DM gesetzt, beide Pfeile, keine
  Aufstiegsprämie). **#329:** wie im Review R9W-D02 vorgeschlagen (`[APPROX]` in `SuccessorOrder`, Probe ab Schema 18).
  **#362:** neuer Test schaltet jedes Feld einzeln, Mutation „nur Oberliga“ rot (4 Fälle). Alle drei können mit `Fixes` schließen.
- **Migration/Rundlauf:** Stand mit Vorsaison auf Schema 22 zurückgesetzt → lädt, `OutsidePlaces` gleich, Neusicherung
  byte-gleich. Rundlauf (`SaveGameTests.AssertRoundTrip`) an jedem von 20 Saisonwechseln in vier Mehrsaisonproben grün.
  Hinweis: Ältere Stände, deren Nachrücker noch von den Plätzen 42–47 kamen, ergeben über `OutsidePlaces` Plätze 42–47 für
  den damaligen Absteiger; der fällt aus der Ewigen Tabelle (nur gelesen, ohne Wirkung auf die Regeln).
- **Geänderte Sollwerte:** je Fall am Test begründet und nachvollzogen (`GameModeTests.Mode4Reference`: Manager 1 hält jede
  Saison 400.000 DM; `FinanceCalibrationTests`: neutrale Setzung getrennt vom Finanzbuch; `StatisticsHistoryTests`: 42 Vereine,
  nächste Liga; `RngTwinProbeTests`, `SpielberechnungAnschlussTests`, `DebCupDrawFlowTests`: anderer Zufallsverbrauch).
- **Dateigrenzen:** eingehalten. Gemeinsame Dateien nur `GameState.Integration.cs` (Aufruf entfernt, begründet) und
  `SaveGame.cs` (Schema, Migrationseintrag). `BoerseStartTests`/`FinanceWerbungTests` unverändert.

## 3. Ausgeführte Prüfungen

**Zugriff:** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → `94bb25e…`. SDK 10.0.402 aus Release
`claude-werkzeuge`, Originaldaten über `tools/claude/fetch-reference.sh original`.

**`tools/pruefen.sh` (alle drei Läufe) am Head `853ebdf`:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1679 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1399 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1399 | 280 | 0 |

**`tools/pruefen.sh --schnell` je Teil** (eigene Worktrees, Head geprüft, porcelain leer):

| Head | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| `4153da4` (#419) | 1640 | 0 | 0 |
| `6175a23` (#428) | 1663 | 0 | 0 |
| `8fd51c2` (#431) | 1670 | 0 | 0 |

CI (`build`) auf allen vier Heads grün.

**Gegenbeispiele erneut ausgeführt** (eigene, nicht committete Probe am Head `853ebdf`):

| ID | Ablauf | Ergebnis |
|---|---|---|
| S-K1/S-K2 | Originaldatenbank, Manager auf Verein 27 (Krefelder EV), alle Oberligaspiele verloren, Konto −491.221 DM ab 13. April | bleibt Letzter in der Oberliga; Left 27,32 / In 27,40; Konto 600.000 DM = 400.000 + 200.000 Einzelprämien (Torjäger, Fair Play), keine Aufstiegsprämie; Pfeil hoch und runter; nur Verein 40 (Platz 36–41) rückt nach, 32 übernimmt dessen Platz; Ewige Tabelle 42 Vereine, Verein 27 1 Punkt – wie Beleg #389 |
| S-K3 | `Minus700k_OutsideRelegationRound_ForcedDown_Minus600k_Stays` (Bundesliga 5, 2. Liga 17), Einschubregel am Disassembly Datei 0x55FC8–0x560B4 nachgelesen | −700.000 DM steigt ab (Symbol 0, kein Pfeil), −600.000 DM nicht; `InsertForcedRelegation` entspricht dem Disassembly; Gegenbeispiel SW-01 |
| S-K4 | Saison 1, Seeds 1–29, Punkte am Spielablauf | Seed 1: Aufsteiger 14 (Platzfeld 1) 51, Absteiger 11 (Platzfeld 10) 22 |
| Mehrsaisonprobe | 20 Saisonwechsel, Originaldatenbank, Seed 11, Modus 2 und 4 × passiv/aktiv (aktiv: −700.000 DM ab 13. April, Vereine 30/14); je Wechsel Ligagrößen 12/12/12, Managervereine mit Liga, `OutsidePlaces` = Vereine ohne Liga, 42–47 nie in der Oberliga, Ewige Tabelle 42, Rundlauf | grün; aktiv je 1 Zwangsabstieg und 36 gehaltene Oberliga-Managersaisons. Modus 1: `WorldOnly_TwentySeasonChanges_…` (passiv/aktiv) grün |

**Gezielte Mutationen** (Worktree am Head `853ebdf`, je eine, danach zurückgesetzt) – alle rot:

| Mutation | rot |
|---|---|
| Nachrücker aus allen 12 Plätzen | `Newcomers_OnlyFromPlaces36To41_…` |
| Konto nicht auf 400.000 DM | 5 `SaisonwechselOberligaTests` |
| Managerverein wird ersetzt | 5 `SaisonwechselOberligaTests` |
| Aufstiegsprämie beim Doppeltausch | nur 5 `SeasonEndStatusTests` (SW-03) |
| Grenze −1.800.000 Drittel | `Limit_…(-666666)` |
| Einschub ab `count + 1` | 6 `SaisonwechselZwangsabstiegTests` |
| höchstens drei je Liga | `AtMostTwoPerLeague_InManagerOrder` |
| Meister nicht ausgenommen | `Champion_WithDebts_NotForced` |
| Pfeil trotz Zwangsabstieg | 2 × `Minus700k_…` |
| Ewige Tabelle mit gespielter Liga | 2 `SaisonwechselEwigeTabelleTests` |
| Ewige Tabelle vor dem Zwangsabstieg | `ForcedRelegation_CountsWithSecondLeaguePoints` |
| Ewige Tabelle mit 48 Vereinen | 3 (`StatisticsHistoryTests`, `SaisonwechselOberligaTests`) |
| Oberliga-Austausch auch im Modus 1 | 2 × `WorldOnly_TwentySeasonChanges_…` |
| #362: nur Oberliga-Feld | 4 `PlayoffWithoutManager_…` (1./2. Liga) |
| #329: Startordnung ohne `SuccessorOrder` | `LoadedGameInSecondSeason_KeepsStartOrderAndTables` |

**Akzeptierte `[APPROX]`:** gesamter Zwangsabstieg bis zum Beleg „FILE15“ (Einschub am Disassembly nachgelesen und
übereinstimmend); nur aktive Manager beim Zwangsabstieg; Prüfung nach dem Finanz-Tageswechsel des 29. April; Startordnung im
Sonderfall doppelter Platz; Zug am 13. statt 29. April; entlassene Manager zählen als Managerverein; Paarung beim Spielstart
mit mehreren Vereinen ohne Liga; Nachrücker 0 Punkte in der Ewigen Tabelle; #329 für Schema ≤ 17; Modus 1 ohne Austausch.

**Annahmen (nicht geprüft):** Verhalten von em.exe im Fall SW-01 (nur aus dem Disassembly abgeleitet); UI-Symbol für
`ForcedRelegation` (`SeasonEndSummaryScreen.cs`, Spur `ui-meldungen`) zeigt bis zur Integration den Rückfall – im PR benannt.
