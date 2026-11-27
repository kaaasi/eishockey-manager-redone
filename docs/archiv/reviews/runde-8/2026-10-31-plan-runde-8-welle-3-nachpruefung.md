# Nachprüfung Review PR #255 – Plan Runde 8, Welle 3

PR #255 (`plan/runde-8-welle-3`) · Base `9fab85493372587d7c936ac906544ac974ffba16` · Head `53f82c0e6d5d63ad6f33e6fc5917b4b629292798`
Nachprüfung von [`2026-10-31-plan-runde-8-welle-3.md`](2026-10-31-plan-runde-8-welle-3.md) (PR #258, geprüfter Head `c60d0cc`).
Reviewer: Claude (geplante Sitzung, an #255 nicht beteiligt). Delta `c60d0cc..53f82c0`: ein Commit, nur
`docs/arbeitspakete.md` (+12/−8, Z. 260–263 und Tabelle Z. 274–285).

`A` = `https://github.com/kaaasi/eishockey-manager-redone/blob/53f82c0e6d5d63ad6f33e6fc5917b4b629292798/docs/arbeitspakete.md`

## 1. Merge-Empfehlung

**Ja** für genau diesen Head – **nur Codelesen** (reiner Doku-PR, kein Build, kein `tools/pruefen.sh`, kein Testnachweis
nach `zusammenarbeit.md` §6 Nr. 4). Beide P2 (R8W3-01, R8W3-02) und alle P3 (R8W3-03 bis R8W3-07) sind umgesetzt; die
Dateigrenzen der Welle-3-Spuren bleiben überschneidungsfrei. Ein neuer P3 (R8W3N-01) blockiert nicht.

## 2. Befunde

### Gegenbeispiele erneut ausgeführt

- **R8W3-01 (#256) – behoben.** [A#L276](https://github.com/kaaasi/eishockey-manager-redone/blob/53f82c0e6d5d63ad6f33e6fc5917b4b629292798/docs/arbeitspakete.md#L276):
  `statistiken-verein` führt `UI/Screens/Team/LineupScreen.cs` (nur Aufruf) und übernimmt den Aufruf aus der Aufstellung
  (0x3FFE0/0x40364) mit `PlayerDataContext.Create`. `grep LineupScreen docs/arbeitspakete.md`: sonst nur Welle 1
  `spielerinfo` (Z. 58, 117) ohne Folgespur → kein zweiter Bearbeiter in Welle 3. Die Ausnahme „`spielerinfo` darf den Aufruf
  selbst setzen“ (Z. 56–58) gilt für Welle 1; für Welle 3 regelt Z. 260 (Spur, der die Datei gehört) – kein Widerspruch.
- **R8W3-02 (#257) – behoben.** [A#L277](https://github.com/kaaasi/eishockey-manager-redone/blob/53f82c0e6d5d63ad6f33e6fc5917b4b629292798/docs/arbeitspakete.md#L277):
  `nationalteam-2` führt `UI/Screens/Competitions/EuropeanCupScreen.cs` (nur Anmeldung bzw. Aufruf der Paarungsansicht).
  Die Kachel `[MenuEntry("Europapokal")]` (`EuropeanCupScreen.cs` Z. 173) kann damit umgehängt oder ein Knopf gesetzt
  werden, ohne doppelte Anmeldung. Keine andere Welle-3-Spur nennt die Datei (`tabellen-original-2` „wie
  `tabellen-original`“ = `TableScreen.cs`, `CupTableMenuEntries.cs`; `UI/Screens/Competitions/*` in Z. 330 gehört zum
  abgeschlossenen Paket `wettbewerbe`).

### P3 des Vorreviews

- **R8W3-03 – umgesetzt.** R8-A03 → `spielstaerke-manager-2` (Z. 275; `MatchEngine*.cs` liegt in deren Grenzen, Z. 150);
  Goldtest aus Original-Spielständen → `transfer-original-vertrag` (Z. 278); W51-01 „erledigt“, F46-01 „entfällt“ (Z. 285).
- **R8W3-04 – umgesetzt.** `Core/Player.Value.cs` (nur Kommentar, SW-03) bei `leitstelle-r8` (Z. 280); keine Welle-3-Spur
  führt die Datei (`transfer-original` hat nur `Player.Contract.cs`, Z. 99).
- **R8W3-05 – umgesetzt, Zeilen stimmen am Head:** `docs/wettbewerbe.md` Z. 366/373 (Abschnitt „Bundestrainer im Modus 4“,
  0x5CCB), `docs/saison.md` Z. 90 (`[APPROX]` Sortierung 0x5CCB), `docs/wettbewerbe.md` Z. 161 („Nicht umgesetzt …“).
- **R8W3-06 – umgesetzt.** `nationalteam-2`: Hinweiszeile `NationalLineupScreen.cs` auf `PlayerHintLine`, Fenster nur mit
  Beleg; `ContractsScreen.cs` als Hinweiszeile bei `transfer-original-vertrag`.
- **R8W3-07 – umgesetzt.** Ausnahme #193 in Z. 260–262 (Leitstelle nach Integration von `nationalteam-2`, passt zur
  Reihenfolge Z. 268–269); `BoerseStartTests.cs`/`FinanceWerbungTests.cs` nur Leitstelle; R8-C01-Testdateien
  (`TransferTests.cs`, `PlayerFactoryTests.cs`, `LifecycleRegressionTests.cs`, `WaveBReviewRegressionTests.cs`) nur bei
  `transfer-original-vertrag`, sonst nirgends genannt.

### R8W3N-01 · P3 – Spurbranch kann an den Seed-Proben rot werden, ohne sie ändern zu dürfen

[A#L262-L263](https://github.com/kaaasi/eishockey-manager-redone/blob/53f82c0e6d5d63ad6f33e6fc5917b4b629292798/docs/arbeitspakete.md#L262-L263).
Ändert `spielstaerke-manager-2` oder `transfer-original-vertrag` den Zufallsverbrauch, bevor `leitstelle-r8` R8-A02
(robuste Proben) auf `main` hat, können `BoerseStartTests`/`FinanceWerbungTests` auf dem Spurbranch rot werden (in der
Integration Runde 8 genau so geschehen, Seeds 7 → 8, 15 → 16). Die Spur darf sie nicht ändern, `tools/pruefen.sh` ist vor
dem Push aber grün verlangt (AGENTS.md §6). Abhilfe (wahlweise): R8-A02 vor den Spurbranches auf `main` bringen, oder in
Z. 263 ergänzen, dass ein dadurch roter Fall im PR als bekannt genannt wird und nicht blockiert. Kein Issue (P3).

## 3. Ausgeführte Prüfungen

Nur Codelesen am exakten Head: `git rev-parse HEAD` = `53f82c0…`, `git status --porcelain` leer.
`git diff --stat c60d0cc 53f82c0` (1 Datei), `git diff c60d0cc 53f82c0`; `grep -n` in `docs/arbeitspakete.md` nach
`LineupScreen`, `EuropeanCupScreen`, `Player.Value`, den vier R8-C01-Testdateien, `BoerseStartTests`, `FinanceWerbungTests`,
`Competitions/`, `Team/`, `Statistics/`, `MatchEngine`; Zeilen in `docs/wettbewerbe.md` (161, 366, 373) und
`docs/saison.md` (90) am Head; `[MenuEntry` in `EuropeanCupScreen.cs`; R8-A03/R8-C01 im Review
`2026-10-31-integration-runde-8.md`. Kein Build, kein `tools/pruefen.sh` (reiner Doku-PR).

**Akzeptierte `[APPROX]`:** keine (Doku-PR). **Annahmen:** wie im Vorreview (Spalte „Dateigrenzen“ gilt zusammen mit dem
Welle-1-Abschnitt; „übernimmt“ erweitert sie nicht); die Welle-1-Regel Z. 56–58 wird für Welle 3 durch Z. 260 ersetzt.
