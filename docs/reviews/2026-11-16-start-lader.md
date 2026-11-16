# Review #895 – Spur start-lader (Erstreview)

Geprüft: Head `9f85e3bcd093515c6a262102208b7a436f8fe448`, Base `b73f9617e071045e9490dbbc6881f6f605a40818`; Checkout
`git rev-parse HEAD` = Head, `git status --porcelain` leer; SDK 10.0.402. Reviewer: Claude Opus 5.5, eigene Sitzung (geplanter
Lauf), ohne Implementierungskontext.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die Lesart von em.exe ist am Binärcode bestätigt, alle drei Läufe von `tools/pruefen.sh` sind
grün, jeder neue Regressionstest ist mit `src/` von `40f67fc` rot, die gezielten Mutationen der neuen Schritte werden erkannt.
Die nachgezogenen fremden Tests ändern nur Abdrücke oder Saaten. Kein fachlicher Sollwert aus Originalständen wurde neu
eingetragen. Es gibt nur P3.

## 2. Befunde

Keine P0–P2.

**P3-1 – doppelter Kommentarblock.** [RngTwinProbeTests.cs:196–203](https://github.com/kaaasi/eishockey-manager-redone/blob/9f85e3bcd093515c6a262102208b7a436f8fe448/tests/EishockeyManager.Core.Tests/RngTwinProbeTests.cs#L196-L203):
Der R21-D2-Kommentar steht zweimal hintereinander. Beleg: `grep -n "Runde 21, Spur start-lader" …RngTwinProbeTests.cs` → Zeilen 196
und 200. Nur kosmetisch.

**P3-2 – `UpdateManagerTeamValues` in `StartGame` ist im Endzustand nicht beobachtbar (äquivalente Mutation).**
[MatchEngine.TeamValues.cs:166](https://github.com/kaaasi/eishockey-manager-redone/blob/9f85e3bcd093515c6a262102208b7a436f8fe448/src/EishockeyManager.Core/MatchEngine.TeamValues.cs#L166). Mutation M2 (Zeile
`game.UpdateManagerTeamValues();` auskommentiert) → `StartLaderTests`, `SpielstaerkeManagerTests`, `RngTwinProbeTests`,
`GameModeTests`, `SpielberechnungAnschlussTests` und `JugendTeamsTests` bleiben grün (46 + 29 bestanden). Eine Probe zeigt nach
`NewGame.Create` dieselben Felder wie die Aufstellung (Saat 5: −19/−19/−19/−20, Form 55/52), mit oder ohne M2. Der Grund:
`BeginFirstTurn` → `OpenStartDay` (Tag 0 ist ein Ereignistag) → `MainPathEventDayLineups` → `UpdateManagerTeamValues`
überschreibt die Felder ohnehin. Die Wanderung zieht eine feste Zahl von Zufallszügen. Der Aufruf bildet also nur die Reihenfolge
des Originals ab (0x2A073 vor 0x43A73), und `A080_ManagerClubs_StartFromLineupThenDrift` kann ihn nicht unterscheiden. Das ist
kein Fehler. Der Test sollte das im Kommentar sagen statt „Gegenprobe über die Wanderung“.

**P3-3 – `WithSlot` (Teamwerte bleiben am Platz) nur indirekt getestet.**
[NewGame.cs:283](https://github.com/kaaasi/eishockey-manager-redone/blob/9f85e3bcd093515c6a262102208b7a436f8fe448/src/EishockeyManager.Core/NewGame.cs#L283). Mutation M3 (`TeamValues = club.TeamValues`) → `StartLaderTests` und
`SpielstaerkeManagerTests` bleiben grün (29/29). Rot werden nur Abdruck- und Ablauftests (`GameModeTests.Mode4_ReferenceValues_Unchanged`,
`EuropeanCupTests.FullCompetition…`, `PlayoffSemifinalTests.MultiSeason…`, `LivespielKernLiveStepTests.WholeDay…`). Ein direkter
Test fehlt, etwa „nach dem Starttausch tragen beide Plätze die Laderwerte ihres Platzes“. → `pruefung-karte`.

**Bestätigt offen (wie erwartet):**
- **A080** → `teamwert-aufstellung` (#891). In `MatchEngine.Live.cs` ist kein Code geändert (`git diff --stat`).
- **P3 `AddUnseatedSquadPlayers`.** Der alte Test `PruefungKarteRunde16Tests` (Saat 5) auf dem Head scheitert genau daran:
  Erwartet `…25, 26, 27, 28…`, tatsächlich `…25, 26, 28, 29, 30…`. Poolplatz 27 wird übersprungen, die Sollplätze FILE01 bleiben
  im neuen Test unverändert (2, 3, 22–31, 98–110). → `pruefung-karte`.
- **A100.** Die Zufallsfolge ist nicht bitgenau.
- **Neues `[APPROX]` (K):** Der Europapokal wird vor der Wanderung ausgelost. Akzeptiert, siehe Teil 3.
- Veralteter Kommentar `OriginalExport.cs:190` („noch nicht gezogen“). Er steht schon in den Restlücken des PR.

## 3. Ausgeführte Prüfungen

**em.exe** (`ndisasm -b16`, Stubs über Segmenttabelle 0x163C0 → `INT 3Fh`-Stub + Overlay-Offset):
- Datei 0x439BB `call 0xe0:0x75` → Stub 0x16680+0x75 = `cd3f 3b4d` → Overlay-Offset 0x4D3B (Ghidra 3810:4D3B „Teamwerte“), Datei
  0x2EF1B.
- 0x43A17 `call 0xf0:0x43` (Ligamischung) und 0x43A61 `call 0xd8:0x2f` → Offset 0x135 → 0x28165.
- 0x43A6E `jnz 0x3ae2` (Laden überspringt). 0x43A73 `push 0xa`, `call 0x28:0x1c5b` → 0x9D1B (Wanderung Modus 10).
- 0x43A8C `call 0xe8:0x89` → Offset 0x4C7B → 0x341AB (Europapokal).
- In 0x28165: Schleife 0x29B57 (`[0xa70e] = [bp−0x64]`, bis `[0x386]`) … 0x2A0A3. Dort 0x2A067 `call 0x108:0x4d`, dann 0x2A073
  `push 0`, `push m`, `call 0x28:0x146c` → 0x952C.

Die Lesart (1) ist damit bestätigt. Laden springt 0x43A6E → 0x43AE2, also keine Wanderung und kein Europapokal-Aufbau.

**`tools/pruefen.sh`** (alle drei Läufe, Commit 9f85e3b, „Ergebnis: bestanden“):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3384 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2439 | 0 | 574 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2440 | 944 | 0 |

**Gegenbeispiele (Rückschaltung):** Worktree am Head mit `git checkout 40f67fc -- src`. `src/` ist zwischen 40f67fc und b73f961
gleich. In A106 wurde der neue Überladungsaufruf durch `LoadTeamValues(new GameState{…})` ersetzt, sonst kompiliert der Test
nicht. Ergebnis `StartLaderTests`: **11/11 rot** (A106, A108, A418, A485, A505, A704, A519, A080, Laden vor dem ersten Spiel,
Mehrsaisonprobe passiv/aktiv). Mit dem Head alle grün.

**Mutationen am Head** (je `dotnet test --filter StartLaderTests|SpielstaerkeManagerTests`):

| Mutation | Ergebnis |
|---|---|
| M1 Wanderung in `StartGame` entfernt | rot: A108, `SpielstaerkeManagerTests.FourSeasonChanges_TeamValuesWithinBounds(False,True,False)` |
| M2 `UpdateManagerTeamValues` in `StartGame` entfernt | grün (äquivalent, P3-2) |
| M3 `WithSlot` ohne Teamwerte des Platzes | grün; volle Core-Suite rot (P3-3) |
| M4 Lader nach `AssignClubs` | rot: A106 |
| M5 `WriteLineupTeamValues` in `AddManager` entfernt | rot: A519 |

**Fremde Tests (Punkt 2, R21-D2 = A):**
- Alter Code + neue Testdateien (16 Core-Klassen, mit Daten): 18 rot, 176 grün. Rot sind genau die neu eingetragenen Abdrücke und
  Werte: `RngTwinProbeTests` (6), `SpielberechnungAnschlussTests` (3), `GameModeTests.Mode4…`, `JugendTeamsTests` (2),
  `SpielstaerkeManagerTests.TeamValues_DrawnOnceThenDrift`, `NationalCoachMode4Tests` (3), `SeasonEndStatusTests` (2). Die neuen
  Werte sind also Folge der Spur.
- Head + alte Saaten (10 Klassen): rot nur an Vorbedingungen. `MarketFull` statt `Accepted` gilt für `TransferTests`,
  `KaderRestTransferTests` (3), `KaderStartTeil4Tests`, `KaderTransferTeil4Tests(3)` und `WaveBReviewRegressionTests`. Dazu:
  Manager nicht unter den ersten fünf (`NationalCoachMode4`, `SeasonEndStatus`), `StadiumAusbau` 2 statt 3 Meldungen, Playoff ohne
  Fall, FILE01 (siehe oben).
- Keine Behauptung wurde abgeschwächt. Die Sollwerte aus Originalständen sind unverändert: FILE01-Plätze und `RngTwinProbeTests`
  sind reine Abdrücke. `KaderTransferTeil3Tests` prüft jetzt ausdrücklich den Altstand (`TeamValues = null`); den neuen Weg deckt
  A704 ab.

**Punkt 3:**
- `SpielstaerkeManagerTests.SaveGame_RoundTripAndMigration` (Schema 11 ohne `TeamValues` → null → beim nächsten Spiel gezogen)
  ist grün.
- `StartLaderTests.SaveBeforeFirstMatch…`: Speichern und Laden direkt nach dem Spielstart erhält die Werte, das erste Spiel lädt
  nicht neu.
- `EnsureTeamValues` ist für neue Spiele ein No-op: alle Vereine haben Werte (A106, A485), WorldOnly ausgenommen (#747, unverändert).

**Akzeptierte `[APPROX]`:**
- Europapokal-Auslosung vor der Wanderung (`StartGame`). Die Auslosung liest nur Gegner mit Index > 64, die nicht wandern, und zieht
  aus `CupRandom`.
- Rückfall `EnsureTeamValues` für Schema < 12.
- Sturm = Byte 0 (A078).

**Annahmen (nicht geprüft):**
- Nicht geprüft: die Zahl der Laderzüge („150 · 13“ laut PR).
- Nicht geprüft: ob `0x952C` Zufall zieht. Für P3-2 unerheblich, weil das Remake dort nicht zieht.
- Kein Pixelvergleich: die Spur ändert keinen Bildschirm.

