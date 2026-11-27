# Unabhängiges Review: wettbewerbe-spiel Teil 1 (PR #482)

Base `af907c5af899c58424de2d843b60952a8c76b460` · Head `65141fbef4d64c8e5986455d34e9d232ba627086` · Reviewer: Claude Code
(geplanter Lauf, Claude Opus 5.5), eigene Sitzung ohne Mitarbeit an PR #482.

## 1. Merge-Empfehlung

**Ja für `65141fb`.** Die Aussage des PR zu R11A-01 (Serien schreibt 0x50283 nur bei Code 7/8, also kein Anschluss für
DEB-Pokal, Play-offs, Aufstiegsrunde) ist im Disassembly bestätigt; das Remake verhält sich bereits so, die Änderung ist
Doku plus Probe. WS-04 ist gegen Datei 0x38ABE–0x38B0F geprüft und getestet. Alle drei Läufe von `tools/pruefen.sh` am
exakten Head bestanden; jede Mutation an Grenze 5 und am Abzug vom Sturm macht einen der neuen Tests rot. Dateigrenzen
eingehalten.

## 2. Befunde

Kein P0–P2.

- **P3 – Grenzfall „Gesamt = Grenze“ nicht im gezielten Test.** Mutation `total > limit` → `total >= limit` in
  [`MatchEngine.Managed.cs:168`](https://github.com/kaaasi/eishockey-manager-redone/blob/65141fbef4d64c8e5986455d34e9d232ba627086/src/EishockeyManager.Core/MatchEngine.Managed.cs#L168)
  lässt alle fünf Tests von `WettbewerbeSpiel12Tests` grün (bei f = 3 ist `3 − 0 = 3` gleich). Unterscheidbar ist der
  Fall erst bei weniger als drei gezählten Stürmern, z. B. Torhüter, 2 + 2 auf dem Eis, ein Strafbankplatz: 6 = Grenze →
  Original (`jng`, Datei 0x38AFC) Sturm 2, Mutante 3. In der vollen Suite fällt die Mutation über die Fingerabdrücke auf
  (`RngTwinProbeTests`, `SpielberechnungAnschlussTests`, `GameModeTests.Mode4`, `AutoLineupTeil8Tests` rot), also kein
  ungeschützter Code. Empfehlung: einen Fall `Team(2, 2, goalie: true)` + `Box.Add(0, 2)` → `[2, 2]` ergänzen
  (optional, nicht mergehindernd).
- Hinweis ohne Priorität: Die Code-Liste im Testkommentar
  ([`WettbewerbeSpiel12Tests.cs:18`](https://github.com/kaaasi/eishockey-manager-redone/blob/65141fbef4d64c8e5986455d34e9d232ba627086/tests/EishockeyManager.Core.Tests/WettbewerbeSpiel12Tests.cs#L18))
  nennt die beiden weiteren Schreiber von DGROUP 0xD0C2 mit Wert 0 nicht (Datei 0x44E32 Hauptmenü, 0x55003). Für die
  Aussage unerheblich (0 ≠ 7/8).

Gegenbeispiele des Auftrags:

| ID | Ergebnis |
|---|---|
| R11A-01 (Serien in DEB-Pokal, Play-offs, Aufstiegsrunde gezählt) | Als Gegenbeispiel widerlegt: Datei 0x5030D `cmp byte [0xd0c2],7` / 0x50314 `cmp …,8`, sonst 0x5031B `jmp 0x50B41` vor dem Serienblock 0x50443–0x50AB4. `--xref 0xD0C2`: Schreiber 6 (0x35E7C), 7 (0x35EC4), 8 (0x30F53), 1 (0x31AB7), 3 (0x31490), 0xA (0x31017), 0xD (0x30390), 0 (0x44E32, 0x55003). „Rot am Base“ entfällt, weil das Base-Verhalten bereits richtig ist: der neue Test ist am Base grün (5/5, Testdatei auf `af907c5` kopiert). Stattdessen rot: Serien in `RecordMeetings` (Pokal + Play-offs), nur DEB-Pokal (`CupsDeb.cs:173`), nur Play-offs, nur Aufstiegsrunde (`SeasonFlow.cs:248/250`) – je beide Seeds. |
| WS-04 (Neuzählung 38A36h: Grenze 5, Abzug vom Sturm) | Code am Base und Head gleich und originalgetreu (Datei 0x38ABE `mov bl,6`, 0x38AC7/0x38AD8 → `mov bl,5`, 0x38B09–0x38B0F `sub al,bl; mov dl,3; sub dl,al`). Testlücke geschlossen; am Base fehlten die Tests (dort grün, da kein Produktfehler). Mutationen siehe unten. |

## 3. Ausgeführte Prüfungen und akzeptierte `[APPROX]`

Klon: `git rev-parse HEAD` = `65141fbef4d64c8e5986455d34e9d232ba627086`, `git status --porcelain` leer. SDK 10.0.402
(Release `claude-werkzeuge`), Originaldaten aus `kaaasi/eishockey-manager-referenz` (`fetch-reference.sh original`,
`git ls-remote` erreichbar). Ausführung, nicht nur Codelesen.

`tools/pruefen.sh` (alle drei Läufe), Ergebnis „bestanden“:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1987 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1675 | 0 | 280 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1676 | 311 | 0 |

Disassembly: `tools/reverse-engineering/disasm.py em.exe 0x502F0 0x50330`, `… 0x38AA0 0x38B78`, `… --xref 0xD0C2 0xD0C2`.

Mutationen (temporärer Worktree, je `dotnet test tests/EishockeyManager.Core.Tests --filter WettbewerbeSpiel12Tests`):

| Mutation | rot |
|---|---|
| Grenze fest 6 | `Recount_NoGoalie_PenaltyBox_LimitFiveCutsForward` |
| ohne Torhüter immer 5 | `Recount_NoGoalie_SixSkaters_KeepsLimitSix` |
| `d + f < 6` → `≤ 6` | `Recount_NoGoalie_SixSkaters_KeepsLimitSix` |
| Torhüterbedingung der Grenze 5 entfernt | `Recount_TwoForwards_TwoInBox_ForwardsThreeMinusExcess` |
| Sturm = gezählt − Überschuss | `Recount_TwoForwards_TwoInBox_ForwardsThreeMinusExcess` |
| Konstante 3 → 4 | `…LimitFiveCutsForward`, `…ForwardsThreeMinusExcess` |
| Strafbank nicht mitgezählt | `…LimitFiveCutsForward` |
| `total > limit` → `≥` | keiner der gezielten Tests (P3 oben); volle Core-Suite rot |
| Serien in `RecordMeetings` / nur DEB-Pokal / nur Play-offs / nur Aufstiegsrunde | `Series_OnlyLeagueResults_NotCupsOrPostseason` (Seed 3 und 7) |

Dateigrenzen (Zuständigkeit Runde 11, `docs/archiv/runde-11.md`): `Core/StatisticsClub.cs` (nur Kommentar), neuer Test,
`docs/wettbewerbe.md`, eigener Abschnitt in `docs/arbeitspakete.md` – Spur. `docs/reverse-engineering.md`: eine Zeile,
im PR begründet. `docs/analyse/inventur.md` nicht geändert, Nachzug A070/A089 an `pixel-marker` im PR gemeldet.

Akzeptierte `[APPROX]`: f &lt; 0 bei mehr als drei über der Grenze hält das Remake bei 0 (Original Byte-Arithmetik,
Datei 0x38B0D; bestehend, nicht neu). Entfernt: das frühere `[APPROX]` zu Pokal-/Play-off-Serien – zu Recht.
Annahmen: keine.
