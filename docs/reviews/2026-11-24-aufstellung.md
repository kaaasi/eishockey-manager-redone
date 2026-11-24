# Review PR #1004 – Spur aufstellung (B31, K07, K2, S07, B13)

Erstreview, unabhängige Sitzung (geplanter Lauf, Claude Opus 5.5). Base `3d6f792`, Head `f0eb0eb`
(`git rev-parse HEAD` = `f0eb0eb5e1aab8ad8ad38aa811fb979cd845f6de`, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für Head `f0eb0eb`. Alle genannten roten Regressionstests sind gegen den alten Code rot (Rückschaltprobe), die Vorgaben
aus `docs/analyse/re-abweichungen.md` zu K07, K2 und B13 sind umgesetzt. Das gilt auch für K07 Fall (d): Der sechste
Feldspieler bleibt eingesetzt, zählt sein Spiel beim Spielbeginn und spielt Position 1 (eigene Probe unten). Migration 64,
Speichern/Laden und Mehrsaisonprobe halten. Alle drei Läufe von `tools/pruefen.sh` sind grün. Es gibt keine Befunde P0–P2.

## 2. Befunde

- **P3 (Testlücke) – Bindung beim Hinzufügen eines Managers ungetestet.**
  [`GameState.AddManager.cs:161`](https://github.com/kaaasi/eishockey-manager-redone/blob/f0eb0eb/src/EishockeyManager.Core/GameState.AddManager.cs#L161)
  Mutation `BindLineupOwners();` → `;`: `AufstellungK07Tests` bleibt 9/9 grün. Ohne die Zeile wäre ein Manager, der mitten im
  Spiel hinzukommt, nicht an seinen Verein gebunden. Ein Umschalten schriebe dann nur das Vereinsfeld, und das nächste Laden
  (`BindLineupOwners` setzt den Verein auf den Wert des Managers) setzte es zurück. Der Code ist richtig. Es fehlt nur ein Test.
  Vorschlag: `AddManager` im laufenden Spiel, Automatik aus, Speichern → Laden → aus.
  Die anderen Stellen, an denen `Manager.ClubIndex` gesetzt wird, sind abgedeckt:
  - Spielstart: `InitializeManagerSystems` aus `NewGame`.
  - HOK-Import: `InitializeManagerSystems` aus `OriginalImport.cs:245`; `OriginalImport.Rest.cs` setzt zusätzlich den Manager.
  - Laden: `SaveGame.Reader.cs:57`. Mutation → `SaveLoad_KeepsManagerSettingAndLastLine` wird rot.
  - Platztausch `SwapClubPlaces`: läuft nur in `AddManagers` vor Zeile 161. Der Manager folgt dabei seinem Vereinsobjekt.

  Eine Codesuche (`ClubIndex\s*=`) findet in `Core` keine weitere Stelle. Einen Manager zu entfernen gibt es nicht.
- **P3 (Hinweis, kein Fehler) – Überschneidung der Dateigrenzen.** `GameState.AddManager.cs` und `OriginalExport.Squads.cs`
  gehören zur parallelen Spur `saison-geld`. Hier ändert sich je eine Zeile. Die Leitstelle sollte das beim Rebase von
  `saison-geld` beachten.

Außerhalb der Dateigrenzen geprüft und minimal: `MatchEngine.Live.cs` (zwei Aufrufe `CountAtKickOff`, zwei Bedingungen;
Freigabe der Leitstelle), `MatchEngine.National.cs` (Parameter `countGames`), `SaveGame.cs`/`SaveGame.Reader.cs` (Schema 64,
Bindung nach dem Laden), `OriginalImport*`/`OriginalExport*` (je eine Zeile), `CupsNational.GameState.cs` und
`MatchEngine.PenaltyBoxQuery.cs` (nur Kommentare); dazu fünf neue Dateien (`MatchEngine.LiveCredit.cs`, `Squad.Lineup.Grid.cs`,
`Squad.LastLine.cs`, `Squad.AutoLineup.Manager.cs`, `SaveGame.Migration.Aufstellung.cs`).

**B31 / AGENTS.md §4:** `LineupScreen.Enter` baut nur das Gitter (`LineupGrid.Build`, nur lesend). Das Kern-Ereignis kommt nur
beim Verlassen: `Leave` → `OnLineupLeft`, `TacticsScreen.Close` → `Leave` vor der Neuaufstellung (Reihenfolge A960).
`PassiveScreenTests.OpeningEveryTile_…` ist mit altem und neuem Code grün. Die angepassten fremden Tests (R15-E2 = C) haben wir
nachvollzogen:

- `LeavingUnchangedLineup_WritesClubFields` und `TacticsScreen_Leave_WritesMatchEffort`: mit altem Code rot.
- `PassiveScreenTests`, `IntegrationRunde4Tests.NationalLineup_SurvivesSaveLoad…`, `CoachMenu_Opens…`, `Lineup_Clicks_…`,
  `FeldspielerSechsScreenTests.ClubLineup_SixthOpen…`: mit altem Code grün. Sie prüfen weiter nur das Öffnen; das
  vorgezogene `OnNationalTacticsLeft` ist dabei herausgenommen.

## 3. Ausgeführte Prüfungen

**Volle Prüfung** `tools/pruefen.sh` (alle drei Läufe, Head `f0eb0eb`, SDK 10.0.402, Ergebnis: bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3921 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2771 | 0 | 638 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2772 | 1149 | 0 |

**Rückschaltprobe** (eigener Worktree: `src/` von `3d6f792`, neue Dateien unter `src/` entfernt, Tests vom Head):

- Bau rot wie in der PR-Beschreibung angegeben (neue API):
  - `AufstellungK07Tests`: 50 Baufehler.
  - `AufstellungK07GridTests`, `AufstellungK07GridScreenTests`, `AufstellungS07NationalTests` (4-Argument-`ClickPlayerInMatch`).
  - `AufstellungTeil3Tests.B13_…` (`NationalLineupScreen.Info`).
- Danach wurden diese Dateien entfernt, die zwei `Info`-Zeilen und die zwei vorgezogenen `OnNationalTacticsLeft` auskommentiert.
- UI, `EM_REQUIRE_GAMEDATA=1`: 7 rot.
  - `AufstellungB31Tests` (3)
  - `LeavingUnchangedLineup_WritesClubFields`
  - `TacticsScreen_Leave_WritesMatchEffort`
  - `AufstellungTeil3Tests.NationalLineupInMatch_PlayerInPenaltyBox_OnlyMessages` (K2)
  - `B13_…` (ohne Spielerdaten-Fenster)

  Die sechs fremden Öffnungs- und Klicktests sind grün.
- Core: 12 rot.
  - `AufstellungS07Tests` (2)
  - `Games_StartLineupAndPlacedInMatch`
  - `SchemaFingerprint_MatchesSchemaVersion`
  - `RngTwinProbeTests` (6)
  - `SpielberechnungAnschlussTests` (2)

**Gegenproben** (Mutation in temporärer Kopie, danach zurückgesetzt):

| Mutation | Ergebnis |
|---|---|
| `Lineup.Clear(cell, grid)`: `grid.SixthOpen = false` vor dem Sturmzweig entfernt | `AufstellungK07GridTests` CaseA, CaseB, CaseC, InMatch rot |
| `LineupGrid.Build` ohne `Block(0).Count() == 5` | CaseD rot |
| Migration: Default `true` statt Wert des Vereins | `Migration_63To64_ManagerTakesClubSetting` rot |
| `SaveGame.Reader`: `BindLineupOwners` entfernt | `SaveLoad_KeepsManagerSettingAndLastLine` rot |
| `KeepLastLines`: `LastLine = 0` statt Position | 7 von 9 `AufstellungK07Tests` rot, darunter die Mehrsaisonprobe (passiv/aktiv) und A939 Reihe 2 |
| `AddManager`: `BindLineupOwners` entfernt | alle grün (Befund P3) |

**K07 Fall (d) – Abrechnung (eigene Probe, nicht committet):**

- Aufbau: Managerverein, Automatik aus, Torhüter heraus, sechster per Gitter geöffnet, Sturmzelle 0 geleert.
- Neuer Aufbau: Kennzeichen 0. Danach Torhüter eingesetzt (Fall c).
- `LiveMatchDay.Begin` + `Step()`:
  - der sechste Feldspieler: `Games` + 1, im Spielkader `Played` und `Position = 1`.
  - der Herausgenommene: `Games` unverändert.
- Nach `Finish()`: der sechste Feldspieler weiter + 1, also nicht doppelt gezählt.
- Ergebnis: bestanden (1/1). Das entspricht Datei 0x54918/0x54922 (+0x0A ≠ 0 und +0x15 ≠ 0).

**Migration 64:**

- `Migration_63To64_ManagerTakesClubSetting`: Laden mit Schema 63; Speichern → Laden → Speichern byte-gleich.
- `MultiSeason_FourChanges_…`: 4 Wechsel, passiv und aktiv; Speichern/Laden je Saison.
- Beide sind grün und durch die Mutationen gestützt. Die Vereinsliste steht in Platzfolge (`clubs[i].Index == i` auch nach dem
  Tausch), der Zugriff der Migration über `ClubIndex` ist also richtig.

**Akzeptierte `[APPROX]`** (wie erwartet offen, bestätigt):

- Siebter Feldspieler bei Sturmzelle: „Kein / Platz mehr !“ in `Lineup.TryOpenSixthSkater`.
- Länder-Freundschaftsspiel zählt im Liga-Feld (`NationalSlot`).
- Von Hand eingesetzter Spieler mit stehen gebliebenem +0x15 gilt im Remake als Ersatz (Klassenbemerkung in `Squad.Lineup.cs`).
- A893 im Spiel: `LiveTeam`; das Spielende schreibt 0x26.
- A165 Computervereine: Ergänzung, das Original hat keine Kaderplätze.
- Nationalaufstellung leitet das Kennzeichen ab (`CupsNational.Lineup.cs`).
- `LastLine` alter Stände ist 0.

**Annahmen** (nicht selbst nachgeprüft): Die Fundstellen in em.exe haben wir nicht neu disassembliert. Grundlage sind die
Ausgaben in `re-abweichungen.md` (K07, K2, B13), die Kommentare und die PR-Beschreibung.
