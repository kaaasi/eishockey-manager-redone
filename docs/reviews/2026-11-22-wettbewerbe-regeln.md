# Review PR #933 – Spur wettbewerbe-regeln (Erstreview)

Reviewer: Claude (Opus 5.5), geplanter Lauf, frische Sitzung ohne Implementierungskontext. Geprüft: Head
`3ca753a7cf4272cdc24c50ef78173297cc1aabd2`, Base `41ad124b` (Merge-Basis), Checkout verifiziert (`git rev-parse HEAD` = Head,
`git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja für den Code dieses Heads** – kein P0–P2. Die geprüften Lesarten von em.exe (unten) stimmen, jede neue Regel hat einen Test,
der unter gezielter Mutation rot wird, alle drei Läufe von `tools/pruefen.sh` sind grün.

Vor dem Merge offen (erwartet, bestätigt): Base ist `41ad124b` mit Merge-Commit `3531aa8`; `main` steht auf `f7f0410` mit
Schema 61, `git merge-tree --write-tree origin/main 3ca753a` meldet Konflikte (Rückgabe 1) → Rebase-Fixlauf mit nächster freier
Schemanummer nötig. Da der Rebase **Konflikte** hat, greift die Ausnahme „nur Rebase ohne Konflikt“ (`zusammenarbeit.md` §6 Nr. 1)
nicht automatisch; die Leitstelle entscheidet per `git range-diff`, ob die Auflösung rein mechanisch ist (Schemanummer,
Migrationstabelle, erwartete Werte nach R21-D2 = A), sonst Nachprüfung nur dieses Deltas.

Restlücken laut PR bestätigt (Code entspricht der Beschreibung): Bundestrainer ohne A808 (`NationalLineupScreen` fragt
`PenaltyBoxMinutes` nicht ab) und ohne A549 (`MatchEngine.National.cs` unverändert); #925 DEB-Pokal unverändert (Zweig
`!knockOut || h != g` bis ungleich); Marken des Zusatzwurfs (`PlayoffSeries.MarksFromReport`) für `pruefung-karte`.

## 2. Befunde

**P3-1 – Play-off-Gleichstand, der nach dem einen Wurf bleibt: Endstand ohne die Zusatztore.**
[`MatchEngine.Live.cs` Z. 189–191](https://github.com/kaaasi/eishockey-manager-redone/blob/3ca753a7cf4272cdc24c50ef78173297cc1aabd2/src/EishockeyManager.Core/MatchEngine.Live.cs#L189-L191)
setzt `ShootoutGoals = (x, y)` mit x = y; [`Cups.Tiebreak.cs` Z. 49–56](https://github.com/kaaasi/eishockey-manager-redone/blob/3ca753a7cf4272cdc24c50ef78173297cc1aabd2/src/EishockeyManager.Core/Cups.Tiebreak.cs#L49-L56)
nimmt dann den Rückfall und speichert nur den Gleichstand. Original: 0x376F0–0x377A3 schreibt Heim + x + 30 (Datei 0x3775D–0x37774)
und Gast + y (0x3779F); die Wertung 0x315E9 zieht nur die Marken (30er) ab (Datei 0x31622–0x31631) – die Zusatztore bleiben im
Ergebnisbyte. Sieger (Gast) stimmt, nur der gespeicherte Endstand weicht ab (Anzeige/HOK-Export). Neu durch diesen PR (vorher
blieb nie ein Gleichstand).
Beleg (Probe in temporärer Kopie, `tests/EishockeyManager.Core.Tests/ZzProbeTieStays.cs`: `FastCupResultWithTie(…, playoff: true,
knockOut: true)` über Saaten, Bericht wie `BeginMatch`, dann `CupMatch.FromReport`):
`dotnet test tests/EishockeyManager.Core.Tests --filter FullyQualifiedName~ZzProbe` → rot:
`seed 1: Wurf 5:5 (Gleichstand 3), Endstand 3:3, Sieger 1` (erwartet 5:5). Vorschlag: im Rückfallzweig `hg + goals.Home`,
`ag + goals.Away`; zusammen mit den Marken (#925) an `pruefung-karte`.

**P3-2 (Hinweis, kein Fehler) – A838: Löschen des Bits ohne Test.** Mutation „`_goaliePulledAtEnd = false;` entfernt“
([`Squad.Tactics.GoaliePulled.cs` Z. 35](https://github.com/kaaasi/eishockey-manager-redone/blob/3ca753a7cf4272cdc24c50ef78173297cc1aabd2/src/EishockeyManager.Core/Squad.Tactics.GoaliePulled.cs#L35))
bleibt grün (`WettbewerbeRegelnTests.PulledGoalie…`, auch die Fingerabdrücke laufen über `KeepMatchFields`, das das Bit am
Spielende neu setzt). Wirkungsgleich, solange jedes Spiel mit `KeepMatchFields` endet; nur eine Spielwertung (0:2) zwischen zwei
Spielen ließe das Bit stehen. Kein Handlungsbedarf außer optional einer Testzeile.

Gegenbeispiele erneut ausgeführt (je mit Fundstelle selbst nachgelesen, `tools/reverse-engineering/disasm.py gamedata/em.exe`):

| ID | Fundstelle geprüft | Test | Mutation → Ergebnis |
|---|---|---|---|
| #897 Gegentore nur Liga | 0xA3FD `cmp [0xd0c2],7 / jnz` | `GoalsAgainst_OnlyInLeague` grün | `if (league)` entfernt → 3/4 rot |
| #925/A873 Zusatzwurf | 0x376E1 → 0x32AA3 (Arg. 0: Gleichstand ohne Marke, +30 bei 0x32D72), Schleife 0x376F0–0x37748, Wertung 0x31622/0x3166E `jnc` → Gast | `FastPath_*`, `FirstPlayoffPairingOfDate_*` grün | eigener Gleichstand „bis ungleich“ → rot; `FirstPlayoffPairingOfDate` = false → rot |
| A384/A670 Jugend | (Lesart PR, Zeilen 0x55003–0x5502C) | `Youth_*` grün | Jugend nur Tag 0 → 2/2 rot; Migrationsflag false → 1/2 rot |
| `SlotSeed` Strom | 0x35EE3 (Länder, 20 × 2 Würfe 0x38:0x7E5) vor 0x36013/0x3602E (Ligen) | `Preparing_NextSlots_DrawsTheSwapsFromGameRandom` grün | Ligen vor Ländern → 10/17 Fingerabdrücke rot (gezielte Tests grün) |
| A553 `OnTacticsLeft` | 0x48E19 → 0x48E62; 0x4902B–0x4903E (0x2D = 0x26, 0x26 = 0x2D + 0x29) | `TacticsLeft_*`, UI `TacticsScreen_Leave_*` grün | Ereignis leer → UI rot; Verlassen ohne Änderung schreibt → 2 rot (`Passive…`) |
| A101 Penaltyschießen | (Lesart PR) | `Shootout_*` grün | Markierung ignoriert → rot; „Runde fertig“ entfernt → rot |
| A808 Strafbank | 0x40DAF `and 3`, 0x40E01/0x40E07 Status 1/2 → Meldung, Abbruch | `PenaltyBox_*`, UI `LineupInMatch_*`, `HintLine_*` grün | Kernprüfung entfernt → 2/3 rot; Platzprüfung `LineupScreen` entfernt → rot |

Weitere Mutationen: A677 Aufstellen im Spiel zählt nicht → 3/3 rot; A456 Schütze nur bei Tor → rot; A071 Strafe schreibt
Merker nicht → rot.

**Besonders geprüft:**
- *Spielrechnung/Zufallsfolge:* Neue Würfe nur an belegten Stellen (Zusatzwurf, Schussfolge nur bei Markierung, Ligamischung
  aus dem Strom); A553/A456/A677/A808 ziehen nichts. Angepasste erwartete Werte (RngTwin, Anschluss, Mode4, WorldOnly) sind
  jeweils mit Ursache und Rückschaltprobe kommentiert; die Mutation der Mischreihenfolge zeigt, dass sie die Lage der Würfe
  festhalten. Saatwechsel fremder Tests einzeln begründet (R15-E2 = C).
- *Bildschirme ändern durch Öffnen nichts:* `TacticsScreen.cs` schreibt nur in `Leave` (Verlassen nach Änderung, nicht im
  Spiel, nicht der Bundestrainer – `[APPROX]` gegen das Original, das immer schreibt, dokumentiert); Konstruktor/`Update` nehmen
  nur einen Vergleichsstand. `LineupScreen.cs` liest nur `MatchEngine.PenaltyBoxMinutes` (nur `InMatch`). Andere Ausgänge des
  Taktikbildschirms (`_lineup` des Bundestrainers/Live) laufen nicht über `Leave` – richtig. Keine weiteren Zustandsänderungen.
- *Mehrsaisonverlauf:* `RngTwinProbeTests.FourSeasonChanges_*`, `SpielberechnungAnschlussTests.FourSeasonChanges_*`,
  `JugendTeamsTests.FourSeasonChanges_*` (passiv/aktiv, synthetisch und Originaldatenbank) grün; Ligamischung am 29. April nach
  den 40 Länderwürfen (Disassembly oben). Jugend: `ProcessDayCore` ruft sie vor dem Spielplan auch an Nicht-Ligatagen
  (`DailyYouth` filtert selbst), nicht im Modus „nur Weltkugel“ und nicht doppelt nach Migration.

## 3. Ausgeführte Prüfungen

- Einrichtung: `tools/claude/fetch-reference.sh original`, `tools/claude/setup-workspace.sh ~/referenz/original/gamedata`,
  `dotnet --version` = 10.0.402.
- `tools/pruefen.sh` (alle drei Läufe, Erstreview):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3706 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2602 | 0 | 605 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2603 | 1103 | 0 |

  Commit 3ca753a7cf4272cdc24c50ef78173297cc1aabd2, SDK 10.0.402, Ergebnis: bestanden.
- Gezielt mit Originaldaten: Core `WettbewerbeRegeln|SaisonSatzfolgeSaisonendeTests|IntegrationRunde7NationalCountersTests|
  SpielberechnungKomplett5Tests` 46/46, UI `WettbewerbeRegeln|Passive` 12/12.
- 16 Mutationen in einer temporären Arbeitskopie (`git worktree`, Skript ersetzt je eine Stelle, `dotnet test --filter`, stellt
  zurück); 15 rot, 1 grün (P3-2). Probe P3-1 wie oben, danach entfernt.
- Disassembly: 0xA3F7, 0x32C8E–0x32D96, 0x315E9–0x3168B, 0x376E1–0x377C8, 0x36FD0–0x37360 (Minutenschleife ist ein eigener
  Zweig für den ganzen Termin, keine Mischung mit dem schnellen Weg), 0x35EDD–0x36040, 0x48E10–0x48E1F, 0x49022–0x49042,
  0x40DA0–0x40E2E.

**Akzeptierte `[APPROX]`:** A553 Schreiben nur nach Änderung (AGENTS.md §4); A101 ohne Anzeige/Szenenzufall; A677 Zählzeitpunkt
Spielende; A808 nur Vereine, Nachspielabzug nicht abgebildet; A549 Zähler nicht im Spielstand; A071 Merker ohne Bundestrainer;
A838 ohne HOK-Import/-Export; Ligamischung ohne lrandom (N3 = B), Teamwert-Wanderung nach der Mischung; A105 Rückfall Byte 0.

**Annahmen (nicht selbst nachgelesen):** Zeilen 0x55003–0x5502C (Jugend), 0x39280–0x39547 (Schussfolge), 0x415B7–0x41622
(Wettbewerbsfeld beim Aufstellen), 0x35BFD–0x35C0C/0x35791 (Strafbankstatus), 0x2F004 (Datenbankbyte 1), 0x544C0 (A191) – nur
über die Tests und die PR-Begründung geprüft. Kein Pixelvergleich (keine neue Grafik; Meldetexte aus bestehenden Bausteinen).
