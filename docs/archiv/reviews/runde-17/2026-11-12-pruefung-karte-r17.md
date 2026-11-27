# Unabhängiges Review: PR #794 (pruefung-karte R17, Teil 1: #784, #785)

- Geprüfter Head: `0869d23a294217804f11197ddf310f66219c1c4a` (`git rev-parse HEAD`, `git status --porcelain` leer), Base `e1476e65e721a2266752949d798e2d1539194e61`
- Reviewer: Claude Code, geplanter Lauf (eigene Sitzung, nicht an #794 beteiligt), 12.11.2026
- Umgebung: SDK 10.0.402 aus Release `claude-werkzeuge`, Originaldaten aus `kaaasi/eishockey-manager-referenz` (`original/`)

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Der Produktfix in `MatchEngine.Preparation.cs` stimmt mit em.exe überein (Aufrufreihenfolge und
Schreibziel von 0x8365 nachgelesen), der neue Test auf FILE30→31 vergleicht exakt gegen den Originalsatz und wird bei
Rückschaltung bei allen drei Saaten rot. Beide #785-Tests fangen die genannten Rückmutationen. Die 18 angepassten fremden Tests
sind begründet: Abdrücke werden bei Rückschaltung rot, die drei Saatwechsel scheitern mit der alten Saat an der Vorbedingung.
Die drei Läufe von `tools/pruefen.sh` sind grün. Offen bleiben nur P3-Hinweise.

## 2. Befunde

Keine P0–P2.

**P3-1 – #730 verliert Pflichteinträge.** In
[`AbgleichOriginalPaareTests.cs` Z. 87–98](https://github.com/kaaasi/eishockey-manager-redone/blob/0869d23a294217804f11197ddf310f66219c1c4a/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L87-L98)
sind HOCK (FILE02→03) und KUHNEKATH (FILE13→14) nicht mehr Pflicht, sondern nur noch Sägezahn (optional). Die Begründung ist
belegt (Probe 4): Mit den alten Einträgen scheitert nur Saat 2 (HOCK) bzw. Saat 1 (KUHNEKATH). Die Abweichung zum Original
(50 bzw. 96) besteht bei 5 von 6 Saaten aber weiter. Damit hält kein Pflichteintrag mehr das #730-Symptom in diesen beiden Paaren
fest, und ein weiteres Abdriften fällt nicht auf. Vorschlag für Teil 2 oder die Leitstelle: einen Pflichteintrag nur für die
treffenden Saaten oder einen Bereich (z. B. „≥ 5 von 6“) und `docs/analyse/abgleich-original.md` Z. 54 nachziehen. Der PR
nennt die veraltete Doku selbst.

**P3-2 – Die genannte Mehrsaisonprobe erkennt den Fix nicht.**
`AbgleichRestPreparationTests.MultiSeason_PreparedFieldsStayInRange` ist mit `Preparation.cs` von e1476e6 ebenfalls grün
(Probe 1, nicht unter den Fehlschlägen); sie prüft nur Wertebereiche. Über mehrere Saisons wird die Änderung tatsächlich durch die
Abdrücke über 4 Saisonwechsel (passiv und aktiv) abgesichert: `RngTwinProbeTests`,
`SpielberechnungAnschlussTests.FourSeasonChanges_…`, `JugendTeamsTests.FourSeasonChanges_…` und
`DebCupDrawFlowTests.MultiSeason_…`, die bei Rückschaltung alle rot werden. Die Pflicht nach AGENTS.md §5 ist damit erfüllt; im
PR-Text sollten aber diese Tests als Mehrsaisonprobe stehen, nicht die Bereichsprobe.

**P3-3 – Gelockerte Gastprüfung.**
`ComputerMatch_KeepsFieldsAcrossSaveAndLoad`
([Z. 181](https://github.com/kaaasi/eishockey-manager-redone/blob/0869d23a294217804f11197ddf310f66219c1c4a/tests/EishockeyManager.Core.Tests/AbgleichRestPreparationTests.cs#L181))
prüft für den Gast nur noch {40, 60} und ist deshalb auch bei Rückschaltung grün. Hinnehmbar: Der exakte Gastwert ist in
`Prepare_ComputerClubs_…` (synthetisch, verlangt mindestens eine asymmetrische Paarung) und in `File30To31_…` (Original)
abgedeckt.

**Hinweis, nicht belegt:** 0x8365 setzt beim Heimverein vor dem zweiten Aufruf auch 0x2C Bit 7 (Favorit) und, ohne Manager,
Bit 3 zurück (Datei 0x83DD/0x83EC, 0x844E). Ob 0x977F diese Bits beim Gastvergleich liest, habe ich nicht geprüft. Das Remake
hält diese Bits nicht im `LiveTeam`. FILE30→31 stimmt bei allen 34 Computervereinen exakt, ein Einfluss ist dort also nicht
sichtbar.

**Erwartet offen bestätigt:** #730 und #773 sind offen (`gh api …/issues/730|773` → `open`). Die Wirkung des Fixes auf beide
untersucht der PR nicht (so in Restlücken vermerkt).

## 3. Ausgeführte Prüfungen

**em.exe** (`ndisasm -b16`, MZ-Kopf 0x1E00, Basissegment 0x62C ab Datei 0x80C0 → Ziel `0x28:0x2A5` = Datei 0x8365):

- Datei 0x36D12: `push [0x5D7C]` (Gast), `push [0x5D7B]` (Heim), `call` → erstes Argument ist der Heimverein.
- Datei 0x36D49: umgekehrte Reihenfolge → erstes Argument ist der Gast. Reihenfolge Heim vor Gast bestätigt.
- 0x8384–0x839A: `[bp-0x8]` = Satz von `[bp+6]` (erstes Argument). 0x839D–0x83D6: zwei Aufrufe 0x368B1 für `[bp+6]` und
  `[bp+8]`, jeweils `shl ax,1`. Alle Schreibzugriffe (0x2C, 0x27–0x2B, 0x26/0x2D bei 0x8479/0x847D, 0x35 ff.) gehen über
  `[bp-0x8]`. Der zweite Aufruf liest also 0x26 des Heimvereins nach dem ersten Schreiben. Das entspricht dem Remake: Gast mit
  altem eigenem Feld, Heim mit `homeTeam.Effort`, wenn geschrieben, sonst `MatchEffort`.

**Gegenproben** in einer eigenen Arbeitskopie (`git worktree`, Head 0869d23), jeweils mit `EM_REQUIRE_GAMEDATA=1`:

| # | Mutation | Ergebnis |
|---|---|---|
| 1 | `MatchEngine.Preparation.cs` von e1476e6, Core-Tests der Liste | 15 rot: `File30To31_…` (Saat 1, 2, 3), `Prepare_ComputerClubs_…`, `RngTwinProbeTests` (6), `SpielberechnungAnschluss…` (2), `GameModeTests.Mode4_ReferenceValues_Unchanged`, `JugendTeamsTests…` (2); grün: `ComputerMatch_KeepsFields…`, `MultiSeason_…`, Saat-Tests |
| 1b | wie 1, UI: `DebCupDrawFlowTests.MultiSeason_CupResultsUnchanged_WithAndWithoutDisplay` | rot |
| 2 | Fix behalten, alte Saaten (71 statt 84 ×2, 3 statt 4) | 3 rot (`SeasonEndStatus…`, `NationalCoachMode4…`, `PlayoffSemifinal…Schema43…`): Vorbedingung |
| 3 | `SeasonEndSummaryScreen` Z. 116: `&& result.Manager == 0` | `SeasonEndEvents_SecondManager_…` rot |
| 3b | dort `&& Game.NationalCoach is null` | `…_NationalCoach_…` rot (der Zweimanager-Test ebenfalls) |
| 4 | HOCK/KUHNEKATH wieder als `StrainOf` | rot nur FILE02→03 Saat 2 bzw. FILE13→14 Saat 1 – Begründung bestätigt |
| 5 | LEER4-Eintrag entfernt | rot nur FILE02→03 Saat 3 (`AttackCondition 12 → −4`) |

**`tools/pruefen.sh`** (alle drei Läufe, Head 0869d23, `Ergebnis: bestanden`):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2966 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2174 | 0 | 505 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2175 | 791 | 0 |

**Akzeptierte `[APPROX]`:** keine neuen. Die bestehenden zu 0x8365 (Reihe 0 statt 0x2E/0x30; SEMI/MANUELL behalten die eigenen
Werte) sind unverändert und werden durch diesen PR nicht berührt.

**Annahmen (nicht geprüft):** Der #785-Bildbeleg (`duell_s2c_03225/03443`, `eisbaeren_f02_02121–02139`) wurde nicht am Videobild
nachgemessen; die Tests prüfen nur die Remake-Logik (Konto vor der Prämie). Die Einstufung des LEER4-Ausschlags (16 Punkte) als
Zufall übernehme ich aus dem PR.
