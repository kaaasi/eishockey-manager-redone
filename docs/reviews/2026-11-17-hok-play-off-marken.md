# Review PR #924 – Spur `hok-play-off-marken` (A612)

Reviewer: Claude Code (Opus 5.5, geplanter Lauf, frische Sitzung ohne Implementierungskontext) · 17.11.2026
Base `191f3ee` · Head `35dfce6` (`git rev-parse HEAD` = Head, `git status --porcelain` leer) · Erstreview

## 1. Merge-Empfehlung

**Ja** für Head `35dfce6`. Die Lesart der Marken ist an em.exe und an beiden Original-Ständen mit Play-offs (FILE22, FILE44)
bestätigt, auch bei klaren Serien. Import, Export der laufenden Runde, Laden/Speichern, Migration eines echten Schema-54-Stands
und ein Verlauf über fünf Saisons sind korrekt. Offen sind nur Testlücken (P3): drei Kernregeln überleben eine Mutation.

## 2. Befunde

**Gegenbeispiele (erneut ausgeführt, Probe `ZzReviewProbe` in temporärer Kopie, alle 82 HOK in `original/spielstaende/`):**

- Nur FILE22 und FILE44 enthalten Play-offs. Marken je Serie: FILE22 VF1 `[1,1,1]`, HF1 `[1,1,1,0]`, alle anderen 0. FILE44 VF1
  `[1,1,1,1,1]`, VF2 `[0,0,2,0,0]` (Spiel 3 „2:4“, Byte 0x40), HF1 `[1,1,1]`, HF2 `[0,0,2,0]` (0x3F), Endspiele 0.
- **Lesart, auch bei 4:2, gegen em.exe (`ndisasm -b16`):** 0x32CA0–0x32CCE vergleicht `Heim(0x5C4B+…) − 30 · Schalter` mit
  `Gast(0x5C4C+…)`, also Gleichstand. Bei Gleichheit (Plätze ≠ 0xFF, 0x32D34/0x32D4E) folgt + 30 aufs Heimbyte (0x32D72/0x32D84),
  Meldung über `[bp−2] = 1`. 0x376E1 springt nur bei Meldung nach 0x376F0: zweimal random(0, 2) (0x376F7/0x37708), Vergleich
  `Heim + x` mit `Gast + y` samt bereits gesetzter Marke (0x37746/0x37748), danach + 30 (0x37761). Das ergibt alle Datenpunkte:
  Nach VF1 stehen die 0:0 der übrigen Paarungen auf 30:0, also kein Gleichstand mehr, und VF2 bekommt nur bei eigenem Gleichstand
  Marken. HF1 Spiel 4 (FILE22) ist ohne Marke, weil HF2 entschieden war. Ein eigener Gleichstand gibt zwei Marken.
- **Lage:** 0x3168B–0x316A0 überspringt im fünften Spiel ohne dritten Gastsieg Tausch und Markenschub (Sprung 0x3173A). Sonst
  setzt 0x316BF das Siegbyte 0x14, 0x316C9 ruft 0x31505, 0x31701–0x31734 schiebt die Marken von 2k+1 nach 2k. Das passt zur
  Exportregel `onAway` ([OriginalExport.Cups.cs:328](https://github.com/kaaasi/eishockey-manager-redone/blob/35dfce6/src/EishockeyManager.Core/OriginalExport.Cups.cs#L328)).
- **Import → Export:** In FILE44 wird das Endspiel (laufende Runde, `CCB2` Plätze 12/13, alle fünf Spiele) bytegleich
  geschrieben. In FILE22 unterscheiden sich nur die getauschten Endspielplätze, wegen des fehlenden Siegbytes 0x14 (A871).
  Frühere Runden schreibt der Export nicht, das ist bekannt und steht im PR. Export → Import scheitert an FILE22/FILE44 mit
  „Tagesindex 217 (DGROUP 0xA969) vor Zähler 218 außerhalb des 13. April.“. Das passiert auf Base `191f3ee` genauso, und
  `OriginalExportRoundTripTests` überspringt beide Stände (`Skipped`). Es ist also **kein Befund dieses PRs**.
- **Laden/Speichern:** `SaveGame.Save` → `Load` der importierten FILE22/FILE44 gibt alle Marken gleich zurück.
- **Migration 54 → 55 mit Altstand:** Auf Base `191f3ee` wurde ein Spielstand mit Schema 54 erzeugt (Seed 1, passiv, Halbfinale
  Spiel 3, Endspiel 1 „2:2 p2“). Mit dem Head lädt er; `Marks` sind Endspiel `[2,0,0]`, sonst 0. Speichern → Laden ist gleich, und
  bis `SeasonOver` weitergespielt gilt `Marks.Count == Games.Count` in allen Serien.
- **Mehrsaisonverlauf:** Passiver Manager (Verein 30) und aktiver Manager (Head 6, Verein 2), jeweils 1993 → 1998, also fünf
  Saisonwechsel mit Play-offs in jeder Saison. `Marks.Count == Games.Count` gilt immer. 10 Speichern → Laden mitten in den
  Play-offs geben gleiche Marken zurück. Marken ≠ 0 kamen 16-mal (passiv) bzw. 114-mal (aktiv) vor.

**Erwartet offen (bestätigt):**

- **A873:** Zusatzwurf des schnellen Wegs fehlt im Remake. `FastCupResultWithTie` ([MatchEngine.Minutes.cs:603](https://github.com/kaaasi/eishockey-manager-redone/blob/35dfce6/src/EishockeyManager.Core/MatchEngine.Minutes.cs#L603)) würfelt nur bei eigenem Gleichstand und wiederholt bis ungleich. Laut em.exe
  0x37746 bleibt ein eigener Gleichstand (Marke im Vergleich) nach einem Wurf mit x = y bestehen. Das Remake erzeugt nie eine
  einzelne Marke. Geht an `wettbewerbe-regeln`.
- **A871:** Siegbyte 0x14 fehlt im Export (FILE22: Endspielplätze 12/13 getauscht exportiert).
- **Schemanummer 55** ist vorläufig ([SaveGame.cs:48](https://github.com/kaaasi/eishockey-manager-redone/blob/35dfce6/src/EishockeyManager.Core/SaveGame.cs#L48)).

**Neue Befunde:**

- **R924-01 (P3, Testlücke): Die Exportregel fürs fünfte Spiel ist ungetestet.** Mutation `var onAway = true;`
  ([OriginalExport.Cups.cs:328](https://github.com/kaaasi/eishockey-manager-redone/blob/35dfce6/src/EishockeyManager.Core/OriginalExport.Cups.cs#L328)): `HokPlayOffMarkenTests` 7/7 grün, ganz
  `EishockeyManager.Core.Tests` mit Originaldaten 2577/2577 grün. `Export_PlayoffMarks_RoundTrip` exportiert nach zwei Terminen,
  und an FILE22/FILE44 hat kein fünftes Spiel mit Heimsieg Marken. Vorschlag: einen synthetischen Stand mit 2:2 und Heimsieg im
  fünften Spiel samt Marken exportieren und das Heimbyte prüfen.
- **R924-02 (P3, Testlücke): `MarksFromReport` ist ungetestet.** Mutation `false && (…)`
  ([Playoffs.cs:118](https://github.com/kaaasi/eishockey-manager-redone/blob/35dfce6/src/EishockeyManager.Core/Playoffs.cs#L118)): Marken-Tests 7/7 grün, Core
  2577/2577 grün. `PlayDate_RecordsMarksPerGame` (Seed 7) enthält kein Spiel mit Gleichstand, die Prüfung `TieMarks` läuft leer.
  Vorschlag: Seed mit Penaltysieger wählen und `Assert.Contains(…, TieMarks)` ergänzen.
- **R924-03 (P3, Testlücke): Der Migrationszweig „Penaltysieger → 2“ ist ungetestet.** Mutation `? 0 : 0`
  ([SaveGame.Migration.HokPlayOffMarken.cs:28](https://github.com/kaaasi/eishockey-manager-redone/blob/35dfce6/src/EishockeyManager.Core/SaveGame.Migration.HokPlayOffMarken.cs#L28)): `HokPlayOffMarkenTests` 7/7 grün
  (Seed 7, zwei Termine, kein Penaltysieger). Core 2577/2577 grün. Das Verhalten selbst stimmt, siehe Altstand oben (Endspiel `[2,0,0]`).
  Vorschlag: in `SaveGame_Schema54_GetsPlayoffMarks` mindestens ein Spiel mit Penaltysieger verlangen.

Sonst keine Befunde. Nebenbei: Ein Gleichstand ohne Penaltysieger mit zwei Marken (Original, A873 b) exportiert das Remake als
Penaltysieg + 1 (bestehendes `[APPROX]`, `OriginalExport.Cups.cs:318`). Das gehört zu A873.

## 3. Ausgeführte Prüfungen

- `tools/claude/setup-workspace.sh …/original/gamedata`, `dotnet --version` = 10.0.402, `git rev-parse HEAD` = `35dfce6e47a5…`.
- `tools/pruefen.sh` (alle drei Läufe, Erstreview):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3590 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2512 | 0 | 588 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2513 | 1077 | 0 |

Ergebnis: bestanden (Commit `35dfce6`, SDK 10.0.402).

- Proben in einer temporären Worktree-Kopie (nicht committet): `Probe_AllSaves_ImportExport` (82 HOK, Marken, `CCB2`/`CCA4`/
  `5CFB`-Vergleich nach Export, Export → Import, Speichern → Laden) und `Probe_MultiSeason` (zweimal fünf Saisons). Dazu
  `ZzAltstand` auf Base (Schema-54-Datei) und `ZzMigProbe` auf dem Head. Gegenprobe auf Base: Export → Import FILE22/FILE44 mit
  derselben Ausnahme.
- Mutationen M1–M3 (siehe R924-01…03), jeweils mit `dotnet test tests/EishockeyManager.Core.Tests` und `EM_REQUIRE_GAMEDATA=1`.
- em.exe: `ndisasm -b16` ab Datei 0x32AA3, 0x31680 und 0x376E1.

**Akzeptierte `[APPROX]`:** A872 (Tor in der Verlängerung, zwei Marken wie beim Pokal), A873 (Zusatzwurf), A874 (Migration ohne
Verlängerung und ohne frühere Importmarken), A871 (Siegbyte 0x14), Export nur der laufenden Runde (unverändert).
**Annahmen:** Die Zieladresse von `call 0x38:0x847` ist als random(lo, hi) übernommen und nicht eigens disassembliert. Der
Markenzähler `[bp−3]` in 0x31701 ist nicht bis zu seiner Quelle verfolgt; die Daten (FILE22/FILE44) stützen die Lagerregel.
