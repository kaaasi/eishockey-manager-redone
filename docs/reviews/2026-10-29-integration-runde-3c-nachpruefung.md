# Nachprüfung: Integration Runde 3, Welle C (#89)

Reviewer: ChatGPT (neue Sitzung, ohne Beteiligung an der Umsetzung). Abgelegt von der Leitstelle, weil der Reviewer keinen Branch anlegen konnte; Inhalt unverändert.

## 1. Merge-Empfehlung

**Ja für Head `2f27307e5245da1b703efc58849dd66bc931d3b5`**, Base `f642ef80ef450b0e79c3513429a04dc91b50078f`, PR [#89](https://github.com/kaaasi/eishockey-manager-redone/pull/89).

R3C-01 ist im Delta behoben. Der neue Hook-Aufruf liegt nur im direkten Managerwechsel; letzter Manager, Saisonende und Rückkehr aus Kacheln laufen über jeweils einen Main-Menu-Eintritt. `OnMainMenu` dedupliziert zusätzlich denselben Zug über `LastTurn`. Der neue Test bildet den beschriebenen Hot-Seat-Vergleich mit Seed 3 und zwei Managern ab.

## 2. Befunde

**Keine offenen Befunde. R3C-01 bestätigt behoben.**

- [`MainMenuScreen.Continue_`](https://github.com/kaaasi/eishockey-manager-redone/blob/2f27307e5245da1b703efc58849dd66bc931d3b5/src/EishockeyManager.UI/Screens/MainMenuScreen.cs#L92-L112): Bei `!Game.IsLastTurnOfDay` wird `Game.Continue()` und unmittelbar danach genau einmal `AutoSave.OnMainMenu(Host)` aufgerufen; der Bildschirm bleibt stehen. Die Methode kehrt danach zurück. Der Kachelweg zeigt einen Dialog; dessen Rückkehr ruft `Enter` des Menüs auf. `AutoSave.OnMainMenu` verwirft diesen zweiten Aufruf, solange `(Game, Season, Day, CurrentManager)` unverändert ist.
- Beim letzten Manager wird dieser neue Aufruf nicht ausgeführt. `Host.Show(MatchDayScreen)` führt dessen `Enter` aus und ruft `Game.Continue()` einmal für den Tagesablauf auf. Bei normaler Rückkehr zeigt `MatchDayScreen` das Menü; dessen `Enter` meldet den dann aktuellen Zug einmal. Bei Saisonende führt der Pfad über Saisonende und Zusammenfassung; erst nach `StartNextSeason()` wird das Menü gezeigt. Der neue Saisonzug wird daher einmal gemeldet. Spielende führt zum Titelbildschirm. `AutoSave.Write` überspringt außerdem Zustände `SeasonOver` und `GameOver`.
- [`HotSeat_AutoSave_DoesNotDependOnTileVisits`](https://github.com/kaaasi/eishockey-manager-redone/blob/2f27307e5245da1b703efc58849dd66bc931d3b5/tests/EishockeyManager.UI.Tests/IntegrationRunde3cTests.cs#L105-L145) verwendet Seed 3, zwei Manager (A: Kopf 0/Verein 2; B: Kopf 1/Verein 30), aktiviertes Autosave und jeweils Abläufe mit/ohne Laden-Kachelbesuch. Pro Schritt vergleicht er Tag, Manager und die geschriebenen Autosave-Bytes; mindestens zwei Schreibstände müssen vorhanden sein. Das prüft den konkreten R3C-01-Befund. Der Test zählt einen Schreibvorgang pro Schritt nur als Ja/Nein; ein doppeltes Schreiben identischer Bytes würde er isoliert nicht erkennen. Die Einmaligkeit des neuen Aufrufs ergibt sich hier aus dem Codepfad und der `LastTurn`-Deduplizierung.

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

### Prüfungen

- GitHub-Compare `1d52a63...2f27307`: bestätigt genau die drei angefragten Dateien: `MainMenuScreen.cs`, `IntegrationRunde3cTests.cs`, `docs/arbeitspakete.md`.
- Gelesen auf exakt `2f27307e5245da1b703efc58849dd66bc931d3b5`: Delta, Test, `MatchDayScreen`, `SeasonEndScreen`, `SeasonEndSummaryScreen`, `SeasonEndGameOverScreen`, `SeasonEndTrophyScreen`, `AutoSave` und `SaveContext`; früherer Bericht `docs/reviews/2026-10-29-integration-runde-3c.md` von `main`.
- Kein Build und keine Testläufe ausgeführt (wie beauftragt). Leitstellenkommentar in #89 weist für Head `2f27307` die drei Läufe nach: **661/0/0 · 561/0/85 · Pflichtlauf 561/100/0 wie gewollt**.

### Akzeptierte `[APPROX]`

Die bereits dokumentierte Abbildung des Hauptmenü-Aufrufs auf einen neuen Zug sowie die Unterdrückung am Saison-/Spielende bleiben im Umfang dieses Deltas akzeptiert. Der Hot-Seat-Zählfehler R3C-01 ist dadurch nicht mehr offen.

### Annahmen

Keine zusätzlichen Annahmen für die geprüften Aufrufpfade.
