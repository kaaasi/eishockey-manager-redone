# Nachprüfung: Spur `saisonwechsel` (PR #449 auf #419 → #428 → #431 → #432)

Unabhängige Nachprüfung (Claude Code, geplanter Lauf, 02.11.2026; nicht an der Umsetzung beteiligt) des Reviews
`docs/reviews/2026-11-02-saisonwechsel.md` (PR #441) nach `docs/vorlagen/review-anfrage.md`. Geprüft nur das Delta
`853ebdf..dbbde55` und die dort genannten Befunde, am nachgewiesenen Checkout (Klon, `git rev-parse HEAD` =
`dbbde559549e010e08b7061dde3dcdb3418192bc`, `git status --porcelain` leer) mit Originaldaten aus
`kaaasi/eishockey-manager-referenz` (`original/`).

| PR | Base | Head |
|---|---|---|
| #449 Teil 5 | `853ebdfc0c3ecadfcb8f8f7feb4ba3c9168b2187` | `dbbde559549e010e08b7061dde3dcdb3418192bc` |

## 1. Merge-Empfehlung

**Ja für den ganzen Stapel #419 → #428 → #431 → #432 → #449 (Head `dbbde55`), nur als Ganzes.** SW-01 ist behoben:
Gegenbeispiel ergibt 12/12/12, die Folgesaison läuft durch, die Mutationen in `ResolveDoubleMoves` werden rot. SW-03 ist
behoben (Mutation rot im eigenen Test). Die Heads von #419/#428/#431/#432 sind unverändert (`4153da4`, `6175a23`,
`8fd51c2`, `853ebdf`; Basen wie im Review #441), deren Prüfung gilt weiter. Neu nur P3 (SW-05, SW-06), kein Merge-Hindernis.

**Merge-Hinweis für die Leitstelle:** `main` ist seit der Stapelbasis `7332a0f` um 26 Commits weiter (`3901e36`).
`git merge-tree origin/main dbbde55` meldet Konflikte in `SaveGame.cs`, `SaveGameTests.cs`, `GameModeTests.cs`,
`RngTwinProbeTests.cs`, `SpielberechnungAnschlussTests.cs` (Schema-Nummer und Sollwerte aus anderem Zufallsverbrauch).
Den Stapel daher über einen Integrations-Branch zusammenführen, Schema neu nummerieren, geänderte Sollwerte neu
begründen und `tools/pruefen.sh` (alle drei Läufe) am Integrations-Head wiederholen. Diese Nachprüfung belegt nur den
Head `dbbde55`, nicht das Ergebnis der Konfliktlösung.

## 2. Befunde

### SW-01 (P1) – behoben

[`SeasonFlow.Zwangsabstieg.cs:144–178`](https://github.com/kaaasi/eishockey-manager-redone/blob/dbbde559549e010e08b7061dde3dcdb3418192bc/src/EishockeyManager.Core/SeasonFlow.Zwangsabstieg.cs#L144-L178):
`ResolveDoubleMoves` lässt den Zwangsabsteiger, der zugleich in die Bundesliga aufsteigt, oben, schickt seinen
Tauschpartner (gleicher Paarindex in `RelegatedInto[ZweiteLiga]`) in die Oberliga und entfernt ihn aus der 2.-Liga-Liste.
Regel wie im Review vorgeschlagen, als `[APPROX]` ohne DOSBox-Beleg markiert.

Gegenbeispiel (eigene, nicht committete Probe, Ablauf wie im Review: `Create(seed: 1, clubs: 12)`, alle Spiele von
Verein 12 als 9:0, Konto ab 13. April −700.000 DM, Saison zu Ende, `StartNextSeason`, dann eine weitere Saison):

```
vorher (853ebdf): PromBL=17,12  RelZL=10,5  PromZL=26,33  RelOL=12,22  → 12/13/11, Folgesaison ArgumentOutOfRangeException
jetzt  (dbbde55): PromBL=17,12  RelZL=10    PromZL=26,33  RelOL=5,22   Ereignis ForcedRelegation
                  Verein 12 → Bundesliga, Partner 5 → Oberliga, Ligagrößen 12/12/12; Folgesaison durch, wieder 12/12/12
```

### SW-03 (P3) – behoben

[`SaisonwechselOberligaTests.cs:83–86`](https://github.com/kaaasi/eishockey-manager-redone/blob/dbbde559549e010e08b7061dde3dcdb3418192bc/tests/EishockeyManager.Core.Tests/SaisonwechselOberligaTests.cs#L83-L86)
prüft jetzt `Premium == SinglePremium · Anzahl Einzelprämien`. Mutation `SeasonPremium(league, rank, events)` statt
`PremiumEvents(...)` (`SeasonFlow.cs:338`): `ManagerClubLast_StaysInOberliga_…` rot.

### SW-05 (P3, neu) – Test legt den Partner nicht fest

[`SaisonwechselDoppelwechselTests.cs:85`](https://github.com/kaaasi/eishockey-manager-redone/blob/dbbde559549e010e08b7061dde3dcdb3418192bc/tests/EishockeyManager.Core.Tests/SaisonwechselDoppelwechselTests.cs#L85)
nimmt „den einzigen Bundesligisten in `RelegatedInto[Oberliga]`“, nicht den Paarpartner. Mutation in `ResolveDoubleMoves`
`down[k]` → `down[0]` (Partner immer der erste Absteiger): alle 51 `Saisonwechsel*`-Tests grün, obwohl im Gegenbeispiel dann
Verein 10 statt 5 zwei Ligen tiefer geht. Vorschlag: im Gegenbeispiel `Assert.Equal(5, partner)` bzw. den Partner aus den
Listen vor `ResolveDoubleMoves` herleiten (Paarindex von Verein 12).

### SW-06 (P3, neu) – Paarindex weicht im Fall „doppelter Platz“ von der eigenen Platzlogik ab

Die Begründung in `ResolveDoubleMoves` (em.exe bewegt im zweiten Tausch den *Platz* des Zwangsabsteigers) und die Umsetzung
(Partner = Absteiger mit gleichem Listenindex) stimmen nur, solange `RelegationExchange` keinen Platz doppelt hat.
`RelegationExchange([A, B, X1, X1])` (Bundesliga-Zwangsabsteiger X1 hat X2 auf Platz 3 überschrieben) liefert
`Promoted = [B]`, `Relegated = [X1]` (ausgeführt); nach der Platzlogik steht auf Platz B danach aber A. Ist zusätzlich B ein
Zwangsabsteiger der 2. Liga (zwei verschuldete Manager), schickt das Remake X1 in die Oberliga, die Platzlogik ergäbe A. Ist
stattdessen A der Zwangsabsteiger, steht A gar nicht in `PromotedInto` und geht in die Oberliga (Platzlogik: X1). Ligagrößen
bleiben in beiden Fällen 12 (nur aus dem Code gelesen, nicht durchgespielt). Sehr selten, ohnehin `[APPROX]`; Vorschlag:
im Kommentar als bekannte Abweichung nennen oder die Partner über die Platzabbildung von `RelegationExchange` bestimmen.

### Erwartet offen – bestätigt

- **SW-02 (P3):** als Restlücke in `docs/arbeitspakete.md` (eigener Statusabschnitt) eingetragen, Runde 12; nicht geändert.
- **Doppelfall-Regel:** `[APPROX]` ohne DOSBox-Beleg, mit Fundstellen (Datei 0x55F93 ff., 0x56137–0x5636D) – akzeptiert.
- **Schema 23 vorläufig:** im Delta unverändert; Leitstelle nummeriert in der Integration (siehe Merge-Hinweis).

### Besonders geprüft

- **#428/#431/#432 unverändert:** PR-Heads und Basen per REST identisch mit Review #441; `853ebdf..dbbde55` ist ein Commit.
- **Mehrsaisonprobe mit Doppelfall:** `MultiSeason_DoubleMoveRecurs_LeaguesStay12` (Modus 2 und 4, je 8 Saisonwechsel,
  aktiver und passiver Manager, ≥ 3 Doppelfälle, je Wechsel 12/12/12, jeder Verein höchstens einmal bewegt, Rundlauf) grün;
  wird mit entferntem `ResolveDoubleMoves` und mit nicht entferntem Partner rot.
- **Dateigrenzen:** Delta nur `SeasonFlow.Zwangsabstieg.cs`, zwei Testdateien der Spur und die eigene Statuszeile in
  `docs/arbeitspakete.md`. Keine gemeinsamen oder fremden Dateien.

## 3. Ausgeführte Prüfungen

**Zugriff:** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → `94bb25e…`. SDK 10.0.402 aus Release
`claude-werkzeuge`, Originaldaten über `tools/claude/fetch-reference.sh original`.

**`tools/pruefen.sh` (alle drei Läufe) am Head `dbbde55`:** Ergebnis bestanden.

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1682 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1402 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1402 | 280 | 0 |

CI (`build`) am Head `dbbde55` grün.

**Gezielte Mutationen** (Worktree am Head, je eine, danach zurückgesetzt; Filter `Saisonwechsel*` bzw. `SaisonwechselOberligaTests`):

| Mutation | Ergebnis |
|---|---|
| Aufruf `ResolveDoubleMoves(record)` entfernt | rot: 3 `SaisonwechselDoppelwechselTests` |
| Partner nicht aus `RelegatedInto[ZweiteLiga]` entfernt | rot: 3 `SaisonwechselDoppelwechselTests` |
| Partner `down[0]` statt `down[k]` | **grün** (SW-05) |
| SW-03: `SeasonPremium(league, rank, events)` | rot: `ManagerClubLast_StaysInOberliga_…` |

**Weitere Probe:** `RelegationExchange([100, 101, 1, 1], c => c < 50)` → `Promoted = [101]`, `Relegated = [1]` (SW-06).
Merge-Probe `git merge-tree --write-tree origin/main dbbde55` (main `3901e36`): 5 Konfliktdateien (Merge-Hinweis).

**Akzeptierte `[APPROX]`:** Doppelfall-Regel (Zwangsabsteiger mit Aufstieg bleibt oben, Partner zwei Ligen tiefer) und
Startordnung der beiden Vereine in der Folgesaison (hinten angehängt); alle in Review #441 akzeptierten bleiben.

**Annahmen (nicht geprüft):** Verhalten von em.exe im Doppelfall (nur Disassembly); Fälle aus SW-06 nicht durchgespielt.
