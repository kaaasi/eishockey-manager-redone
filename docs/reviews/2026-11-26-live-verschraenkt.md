# Review PR #1032 – Spur live-verschraenkt (Erstreview)

Geprüft: Head `f4ac7fa` (Base `9d9ae39`; `main` = `6d916d8`, seither nur Doku: `docs/arbeitspakete.md`, eine Review-Datei).
Checkout nachgewiesen: `git rev-parse HEAD` = `f4ac7fa4af3e7e36b1302d635dfd07d0934af6e3`, `git status --porcelain` leer.
Reviewer: Claude Code (Opus 5.5), geplanter Lauf, frische Sitzung ohne Implementierungskontext.

## 1. Merge-Empfehlung

**Ja** für genau `f4ac7fa`. Alle drei Läufe von `tools/pruefen.sh` grün, CI `build` grün auf dem Head. Die Gegenbeispiele
S01/S04/S05 lassen sich mit den Nachbildungen nachvollziehen, und der Code folgt ihnen: Liga 0 → 2 und Paarungsreihenfolge,
Abschluss erst nach der Minute aller Paarungen, Verlängerung nur mit Gleichstand, Leertaste ohne Rechnung, Seitenfilter 0x3A88A.
Jede der fünf Mutationen in den neuen Kernstellen macht mindestens einen Test rot. Laden und Speichern an jedem Tag rund um
Live-Spieltage ergibt denselben Spielstand (eigene Probe, unten). Die Stichprobe der neu begründeten Sollwerte bestätigt die
Rückschaltangaben der PR-Tabelle. Die Dateigrenzen-Erweiterungen sind minimal (nur Anschluss an `PlayDate`). Es gibt keinen
offenen P0–P2.

## 2. Befunde

**P3-1 (Testabdeckung):** Die Regel „Penaltyschießen bzw. Abschluss erst nach Minute 70 **aller** Paarungen“ (0x32AA3 nach 0x37A62,
[`MatchEngine.LiveStep.cs` Z. 226–233](https://github.com/kaaasi/eishockey-manager-redone/blob/f4ac7fa/src/EishockeyManager.Core/MatchEngine.LiveStep.cs#L226-L233))
hat keinen benannten Test. E4 (`KnockOutDay_OnlyTiedPairingsPlayOvertime`) prüft nur `LastMinute`. Rot werden bei der Mutation
nur interne Fingerabdrücke.
Beleg: Z. 228 `completed.Add(match);` → `match.Complete();`, dann
`dotnet test tests/EishockeyManager.Core.Tests --filter "LiveVerschraenkt|RngTwinProbe|SpielberechnungAnschluss|LiveTasten|DebCupDrawFlow|JugendTeams"`.
Ergebnis: 9 rot / 34 grün. Alle `LiveVerschraenkt*`-Tests bleiben grün, die roten sind Abdrücke (`RngTwinProbe` u. a.).
Vorschlag: In E4 nach `Finish` prüfen, dass Penaltyschießen erst nach Minute 70 aller Paarungen beginnen (z. B. Zufallszähler
oder Reihenfolge der `Complete`-Aufrufe).

Keine weiteren Befunde.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe) auf `f4ac7fa`, SDK 10.0.402:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4044 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2866 | 0 | 662 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2867 | 1177 | 0 |

Ergebnis: bestanden.

**Gegenbeispiele (erneut ausgeführt):**
- `python3 tools/reverse-engineering/live/minutenschleife.py`, Fall 1: Reihenfolge in jeder Minute
  `[0, 2, 4, …, 50]`, Liga 0 → 2. Je Reihenpaarung kommt die Eingriffsabfrage unmittelbar vor der Rechnung, Verletzung
  0x38B8F vor den Chancen. Fall 4: in 61–65 rechnen nur `[100, 104]`, keine Abfrage für die entschiedene Reihenpaarung.
  Fall 2: Leertaste ohne Rechnung und ohne Tafel. Fall 6: Nebenbefund Gasttor (nicht Spur).
  Das stimmt mit `PlayMatchDay`/`LiveMatchDay.Step`/`Skip` und `GameState.PlayNextMatchDay` (Ligen sortiert) überein.
- Teil 2b: `PlayDate` in `CupsDeb.cs`, `Playoffs.cs` (Play-off, Aufstiegsrunde), `CupsGroup.cs`, `CupsWorld.cs` (Gruppen,
  Endrunde) gelesen. `RatedNation` hat keine Nebenwirkung (vorgezogen unkritisch). `FromReport` entspricht dem alten
  `PlayWithMarks`/`CupMatch.Play`.
- `drittelseiten.py`, `pokalseiten.py`: `LivePages.After` gegen alle Matrixzeilen verglichen. Geprüft: Liga (Index 3·L + (m = 60)),
  DEB-Pokal, Play-off/Aufstiegsrunde (Index 3/4), Europapokal, WM-Gruppe/-Endrunde und nach dem Penaltyschießen. Die
  Seitentexte („       n.Minute“) entsprechen `pokalseiten.py`. Die Ligaseite mit Minute und F076 0x35CBE decken
  `LiveSeitenUiTests`/`LivePenaltyUiTests` ab (grün).
- S05: Leertaste 0x2CD1C (`LiveTastenUiTests.Space_…`), A456 Reporterreihenfolge, A448 Pause (`PauseFrames_OriginalTickWord`).

**Mutationen (temporäre Kopie, je danach zurückgesetzt):**

| Mutation | rot |
|---|---|
| `LiveMatchDay.Step`: `Complete` sofort statt nach allen Paarungen | 9 (nur Abdrücke, P3-1) |
| `LiveMatch.Play`: `skip` wirkungslos | `LiveTastenTests.Skip_NoPairingComputed_NoRandomDrawn` |
| `PlayDate`: schneller Weg vor der Minutenschleife | `PlayDate_MinuteLoopFirst_ThenFastPathInOrder` |
| `LivePages`: Liga-Index `minute >= 40` statt `== 60` | 1 von 6 `LivePagesTests` |
| `LiveMatchScreen.Messages`: Verletzung nach den Strafen | `ReporterOrder_InjuryBeforeChancesAndPenalties` |

**Laden/Speichern (eigene Probe, nicht committet):** Synthetische Datenbank, zwei Manager (BL/2. Liga), Live-Treiber an, Saat 3
und 17, zwei Saisons. Spiel b wird vor jedem `Continue` und vor `StartNextSeason` gesichert und neu geladen, Spiel a läuft
durch. Nach jedem Tag gilt `SaveGame.Save(a) == SaveGame.Save(b)`, mehr als 20 Treiberläufe → **grün**.

**Mehrsaisonprobe:** `FourSeasonChanges_TwoManagersLive_Plausible` (4 Wechsel, passiv/aktiv, mit = ohne Treiber) ist im Lauf oben grün.

**Rückschaltprobe-Stichprobe** (`src/` von `9d9ae39` + Tests des Heads ohne die neuen Spurtests):

| Test | erwartet laut PR | Ergebnis |
|---|---|---|
| `TeamwertAufstellungTests` (passiv) | rot | rot |
| `TagesabschlussStadionTests` (passiv) | rot | rot |
| `KaderplatzWerteFixTests` | grün | grün |
| `WorldCupTests` | grün | grün |

**Tagesablauf mit `tagesabschluss` (#1034):** Stadionschäden laufen im Tageswechsel (`OpenClubSeasonDay`), getrennt vom
Spieltag. Der Rebase ändert nur den Zufallsverlauf (Sollwerte siehe Stichprobe).

**Hängeschutz:** Die Suchschleifen der neuen Tests sind begrenzt (`seed < 40`, `guard < 1000`).

**Erwartet offen (bestätigt):**
- Aufstiegsrunde/Europapokal im Spielablauf → `live-seiten-rest` (R26-E2 = C, `[APPROX]` `LivePageScreens.cs` Z. 21).
- Schüsse des Penaltyschießens T7 → `torszene-dauer` Teil 2 (`[APPROX]` `LivePenaltyScreen.cs` Z. 22).
- R26-D2 = A: A449 Enter/Escape, A453, A541/A542, Zufallsfolge nicht bitgenau.

**Akzeptierte `[APPROX]`:**
- Weg je Paarung statt je Tag, Minutenschleife vor dem schnellen Weg (`PlayDate`).
- Maske 0xC und Europapokalgruppen je Wettbewerb.
- A454 Klick zwischen Minuten (0x38D96 ohne Zufall).
- Tafel nach der Leertaste.
- Escape im Penalty-Bildschirm.
- Breite der ersten Ligaseite.
- Tormeldungsziel an Pokalterminen.

**Annahmen (nicht geprüft):** kein Pixelvergleich mit `duell_s2a_01126` (im PR als offen genannt). Die Lesart der Fundstellen ist
nur über die mitgelieferten Nachbildungen belegt, die Disassembly wurde nicht unabhängig gelesen.
