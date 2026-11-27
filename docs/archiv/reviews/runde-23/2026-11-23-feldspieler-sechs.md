# Review: feldspieler-sechs (#963)

Unabhängiges Erstreview (Claude Opus 5.5, geplanter Lauf, keine Mitarbeit am PR). Geprüft: Base `70a09fc`, Head
`b1c73d27b1c3683c22933cd5c3119ca56b19052a` (`git rev-parse HEAD` = Head, `git status --porcelain` leer). `main` steht inzwischen auf
`3c374eb` (#966, 3 Dateien); `git merge-tree` ohne Konflikt.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head, wegen eines P2: Der PR öffnet den sechsten Feldspielerplatz auch in der Nationalaufstellung (Teil 3).
Der HOK-Export schreibt ihn mit +0x15 = 1, der HOK-Import des Satzes 5 verwirft ihn aber. Der Vereinsteil ist sauber: Öffnen,
Meldung, Nachrücken, Spielrechnung, Eingriff im Live-Spiel, Migration und Import/Export im Verein stimmen mit em.exe überein, und
alle Rückschaltproben werden rot. Nach dem Fix von R963-01 genügt ein Fixlauf mit `--schnell` und der Delta-Prüfung der Leitstelle.

## 2. Befunde

### R963-01 (P2): Nationalaufstellung, HOK-Export → Import verliert den sechsten Feldspieler

- Stelle: [`OriginalImport.National.cs` Z. 81–87](https://github.com/kaaasi/eishockey-manager-redone/blob/b1c73d27b1c3683c22933cd5c3119ca56b19052a/src/EishockeyManager.Core/OriginalImport.National.cs#L81-L87).
  Dort gilt weiter `PlayerRole.Defender when n < Lineup.DefendersPerBlock`. Der dritte Spieler der Abwehrhälfte in Reihe 1 bekommt
  also keinen Platz und bleibt nur eingesetzt. Den Vereinsimport hat der PR angepasst
  ([`OriginalImport.cs` Z. 521/546](https://github.com/kaaasi/eishockey-manager-redone/blob/b1c73d27b1c3683c22933cd5c3119ca56b19052a/src/EishockeyManager.Core/OriginalImport.cs#L546)).
  Die Datei gehört laut `docs/arbeitspakete.md` („`OriginalImport.National.cs` (`LineupSlot`) nur `feldspieler-sechs`“) zu dieser
  Spur. Der Export
  ([`OriginalExport.Cups.cs` Z. 124–125](https://github.com/kaaasi/eishockey-manager-redone/blob/b1c73d27b1c3683c22933cd5c3119ca56b19052a/src/EishockeyManager.Core/OriginalExport.Cups.cs#L124-L125))
  schreibt +0x15 = 1 über `FindSlot`.
- Ein Stürmer auf dem sechsten Platz (+0x15 = 1 ohne Bit 7) wird zudem als Stürmer der Reihe 1 gelesen
  (`s[0x15] & 0x0F`). Bei vollem Sturm fällt er ebenfalls weg.
- Beleg: Probe-Test (temporär, `[GameDataFact]`, in `tests/EishockeyManager.Core.Tests/`):

  ```csharp
  var raw = OriginalExportTests.RawDatabase();
  var game = OriginalExportTests.NewGameForExport(ClubDatabase.Parse(raw));
  game.AppointNationalCoach(game.Managers[0]); game.AutoNominate();
  var coach = game.NationalCoach!; coach.AutoFormation(); coach.ClearSlot(LineupSlot.Goalkeeper);
  int? line = 0;
  var sixth = coach.Squad.Select(m => m.Player).First(p => p.Role == PlayerRole.Defender && p.Available && !coach.Formation.Contains(p));
  Assert.Equal(LineupResult.Assigned, coach.ClickPlayer(sixth, ref line, right: false));
  var again = GameState.ImportOriginal(OriginalSaveGame.Read(OriginalExport.Export(game, raw)), ClubDatabase.Parse(raw), 4711);
  // Ergebnis: vorher Formation.Count = 16, SixthSkater = SEBEK; nachher Count = 15, SixthSkater = null (nur eingesetzt)
  ```

  `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter ZzReviewProbe` → `before: count=16 sixth=SEBEK |
  after: count=15 sixth= gk= dressed=True`.
- Vorschlag: dieselbe Zuordnung wie im Verein (dritter Spieler der Abwehrhälfte in Reihe 1 → `LineupSlot.SixthSkater`, Art nach
  +0x15 Bit 7 statt nach `Role`, soweit `Formation.Assign` das zulässt). Dazu der Probe-Test als Regressionstest.

### R963-02 (P3): Mehrsaisonprobe passiv belegt nicht, dass der sechste Platz erhalten bleibt

- Stelle: [`FeldspielerSechsTeil3Tests.cs` Z. 97–154](https://github.com/kaaasi/eishockey-manager-redone/blob/b1c73d27b1c3683c22933cd5c3119ca56b19052a/tests/EishockeyManager.Core.Tests/FeldspielerSechsTeil3Tests.cs#L97-L154).
  `AssertConsistent` kehrt bei `SixthSkater is null` sofort zurück, und `pulled` wird nur bei `active` geprüft.
- Beleg: Mutation `_sixthSkater = null;` am Ende von `Lineup.RemoveUnavailable` (läuft an jedem Spieltag) →
  `MultiSeason_…(active: False)` bleibt grün, nur `active: True` wird rot.
- Vorschlag: Passiv festhalten, ob und wann der Platz schließt (z. B. Spieler gesperrt oder verletzt), oder mindestens bis zum
  ersten Spieltag `SixthSkater` ≠ null prüfen.

### Geprüft, ohne Befund

- (1) em.exe, nachgelesen mit `disasm.py`:
  - 0x40EC4–0x40EFF: Gruppe 0 (Torhüter), keine Meldung vorher, Reihe 4, `[bp−0xE]` ≠ 0, nur linke Taste. Das liegt vor dem
    Herausnehmen (0x40F0D) und nach der Grenze 22 (0x40E2A). So ist es auch in `ClickPlayer`/`ClickPlayerInMatch` (Z. 615 bzw.
    `CupsNational.Lineup.cs` Z. 248) geordnet.
  - 0x413BA–0x413E8: Geöffnet wird erst nach dem zweiten Versuch (`[bp−0xD]` = 1) oder in Reihe 4, dann nur Reihe 0,
    `[bp−0xE]` = 0 und `[bp−0x2A]` = 0. Zelle 5 liegt bei x 0x12A, y 0x2A, die Art wird nicht geprüft. Die drei Knöpfe
    (0x102 + 0x12·i, y 0x23) entsprechen `SlotButton(…, sixthOpen)`.
  - 0x404E8–0x404FD: Kennzeichen beim Aufbau. 0x40B95/0x41043: Nachrücken.
  - Die Halbe aus Gruppe < 2 (0x412A1) gilt auch für einen Torhüter mit Rechtsklick im Spiel. Das ist die bestehende `[APPROX]`
    „Art-Prüfung wie beim Linksklick“, die der PR beibehält.
- (2) Rechtsklick auf einen Torhüter im Spiel bei offenem Platz (Probe): Ergebnis `Assigned`. Danach stehen Torhüter und sechster
  Feldspieler, `SkatersPerPart` = [3, 2], 6 auf dem Eis, `GoaliePulled` = false. Das stimmt mit der Grenze 6 in 0x38A36 überein, wie
  im PR beschrieben.
- (3) Migration 62 → 63 (reines Umbenennen samt `$ref`), Vereins-Export/-Import, Schemaabdruck. Dateigrenzen: `ShortMessageTexts.cs`
  ändert nur einen Kommentar. Außerhalb der Liste stehen `HokCompareClasses.cs` und `spielstand-original.md`, beide wegen der
  Feldumbenennung begründet.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe, Head `b1c73d2`, SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3811 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2694 | 0 | 615 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2695 | 1116 | 0 |

CI auf dem Head: `build` success.

Rückschaltproben (temporärer Worktree, `dotnet test --filter FeldspielerSechs`, mit Originaldaten):

| Probe | rot |
|---|---|
| Teil 1: `TryOpenSixthSkater` → immer false | 18 Tests aus Teil 1–3 (u. a. `ClubClick_Line1Full…`, `SaveGame_RoundTrip…`, `OriginalExportImport…`) |
| Teil 1: Abweisung „Schon 6“ im Verein entfernt / `MoveSixthSkaterInto` aus / Migration [62] entfernt / Import-Kapazität alt | `ClubClick_GoalieWhileSixthOpen…`, `FreedDefenceSlot…`, `Migration_62To63…`, `OriginalExportImport…` (+ MultiSeason aktiv) |
| Teil 1: `ClearOrder` → `All` | `ClearLineup_ClearsSixthSkaterToo` |
| Teil 2: `Squad.Lineup.InMatch.cs` von `8c6eb76` | alle 7 Tests in `FeldspielerSechsTeil2Tests` |
| Teil 3: `CupsNational.Lineup*.cs`, `NationalLineupScreen.cs` von `59b9b9f` | 3 Tests in `FeldspielerSechsTeil3Tests` + `NationalLineup_SixthOpen…` (UI) |
| Fremde Tests (R15-E2 = C): `src/` von `70a09fc`, neue Testdateien entfernt | `EinzelpunkteTests.AutoLineup_WithoutGoalie…`, `…Migration_58To61…`, `RngTwinProbeTests…` Saat 11 (beide), `SaveGameTests.SchemaFingerprint…`; Saat 3 grün |

**Akzeptierte `[APPROX]`** (bestätigt, wie im PR):

- `Lineup.SixthSkaterOpen` wird aus dem Zustand abgeleitet statt aus `[bp−0xE]` (Z. 116). Das weicht nur ab, wenn in Reihe 1 ein
  Stürmer herausgenommen oder ausgefallen ist.
- `MoveSixthSkaterInto`: Wird in Reihe 1 eine Sturmzelle geleert, rückt niemand nach. Das Original setzt bei 0x40B95 jede Zelle.
- Import: kein dritter Abwehrplatz in den Reihen 2–3.
- `NationalLineup.LineValue` ohne den sechsten Feldspieler.
- Schema 63 ist vorläufig (`main` steht auf 62), die Nummer wird beim Rebase festgelegt.
- Ob 0x54910 über +0x15 zählt, ist offen (A899, D).

**Annahmen:** Kein Videobild mit offenem Platz (`docs/referenz-videos.md` ohne Treffer). Deshalb gibt es keinen Pixelvergleich; der
UI-Test prüft nur Knopf und Klick.
