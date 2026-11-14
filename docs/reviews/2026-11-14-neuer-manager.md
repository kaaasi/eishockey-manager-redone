# Review PR #854 – Spur `neuer-manager` (Platztausch in die Start-Liga, #847)

Erstreview (Claude Opus 5.5, geplanter Lauf, eigene Sitzung ohne Implementierungskontext). Base `9f1994e5`, Head
`1c2c43d810c3f3edf84c2915f05e4d1a66fc6d51` (`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Der Tausch bildet 0x36169(v, r, 0x80) im belegten Umfang nach (nur Kennung, Spieler,
Managerzuordnung; Platzfelder bleiben am Index), die Zielplatzwahl folgt Datei 0x29C3B–0x29CF7, alle drei Läufe von
`tools/pruefen.sh` sind grün, die Gegenbeispiele zu #847 bestehen mit Originaldaten, 10 von 11 Mutationen werden von den neuen
Tests erkannt. Einziger Befund P3 (Testlücke Europapokal-Ausschluss). #857 bestätigt offen.

## 2. Befunde

**P3-1 – Europapokal-Ausschluss der Zielplatzwahl ist ungetestet.**
[`GameState.AddManager.Swap.cs` Z. 35](https://github.com/kaaasi/eishockey-manager-redone/blob/1c2c43d810c3f3edf84c2915f05e4d1a66fc6d51/src/EishockeyManager.Core/GameState.AddManager.Swap.cs#L35),
Test [`NeuerManagerTests.cs` Z. 103](https://github.com/kaaasi/eishockey-manager-redone/blob/1c2c43d810c3f3edf84c2915f05e4d1a66fc6d51/tests/EishockeyManager.Core.Tests/NeuerManagerTests.cs#L103).
Beleg: Mutation `|| europe.Contains(c)` entfernt → `NeuerManagerTests` 16/16 grün. Ursache: In `MidSeason` führt Manager 0
Verein 2, und der ist in allen Saaten der einzige deutsche Teilnehmer der Auslosung (Deutscher Meister zu Spielbeginn,
`EuropeanCup.InitialChampion = 2`) – der Ausschluss fällt mit dem Managerausschluss zusammen. Gegenprobe (Scratch,
`ReviewProbeEuropeTests`: Manager 0 auf Verein 5, Neuer mit Verein 20, Saaten 1–80, `Assert.DoesNotContain(r, Draw<64)`):
am Head grün (Plätze 0,1,3,4,6–11, nie 2), mit der Mutation rot. Der Code ist also richtig, nur nicht abgesichert.
Vorschlag: Test mit einem Managerverein ≠ 2 (z. B. wie die Probe), mitnehmen in einem ohnehin laufenden Fixlauf bzw. an
`pruefung-karte`. Issue (`befund`, `P3`) legt die Leitstelle an.

**Erwartet offen, bestätigt – #857:**
[`OriginalExport.League.cs` Z. 65–71](https://github.com/kaaasi/eishockey-manager-redone/blob/1c2c43d810c3f3edf84c2915f05e4d1a66fc6d51/src/EishockeyManager.Core/OriginalExport.League.cs#L65-L71)
schreibt Logo 0x34 und Farbcode 0x3B/0x3C weiter aus `database[club.Index]`, nach einem Tausch also die Werte des Platzes, nicht
der Kennung. Codesuche `ColorCode|LogoNumber|database[…Index]` in `src/`: einzige Stelle; die Oberfläche zeichnet über
`Club.LogoGraphic` (wandert mit dem Objekt) – sichtbar betroffen ist nur der HOK-Export.

**Geprüft ohne Befund:**

- **Umfang gegen em.exe** (`disasm.py` 0x36169–0x3672C): Titel nur bei `si & 0x7F` (0x3624D); Zweig 0x80 (0x3628F–0x363F6)
  tauscht Name (3 × Kopie), +0x16, +0x34, Wort +0x3B und springt über 0x3649C → 0x36728 ans Ende; Tabellensatz 0x8257 und
  Serien 0xB57B liegen hinter 0x364AE (`test si,0x80` → Ende). 0x45CD0 (Datei 0x45CD0–0x45D45): Zeile 1 von 0x5CFB, 2 ·
  [0x3B1 + [0x5CF4]] Einträge – Zeilenkompaktierung 0x3489D betrifft nur Wettbewerb 0 (Aufrufer 0x34B76 liest [0x5CF3]),
  Zeile 1 (Europapokal, Gruppen) bleibt in der Saison stehen. Lesart der PR stimmt.
- **Remake-Umfang:** Alle `Club`-Felder durchgesehen (`GameState.cs`, `FinanceSponsorRating.cs`, `StatisticsClub.cs`,
  `MatchEngine.TeamValues.cs`, `Squad.*.cs`, `StadiumStatistics.cs`): zurückgetauscht werden Index, Stärke, Liga, Teamwerte,
  Tabellenfeld, Platzverlauf, Strafminuten, Serien – das sind die Platzfelder; Rekordserien/Über-/Unterzahl liegen am Manager.
  Weitere Verweise per Index: `PoolSlot.Club` (getauscht), `Manager.ClubIndex`, `ManagerSetup.ClubIndex` inkl. offener Zeilen
  (getauscht); Spielplan, Ergebnisse, Satzfolge, Pokale, Historie bleiben am Index (gewollt). Keine Caches über `Club`/Index.
- **`UnsafeAccessor`** auf `<Index>k__BackingField`/`<Strength>k__BackingField`: keine Änderung an `GameState.cs`; Sichern/Laden
  nach dem Tausch bytegleich (eigene Probe und PR-Tests), Mutation „Stärke nicht zurücktauschen“ wird rot.
- **Fremder UI-Test** `AddManagerScreenTests.AddsManagerViaSetup` (R15-E2): Begründung im PR und Kommentar; Rückschaltprobe
  nachgestellt (Core auf Base, neuer Test): 2 rot (`NewTile_AddsManager_WhoTakesTheNextTurn`, `NewTile_AfterLoad_AddsManager`).
- **Mehrsaisonprobe:** beide Tests (synthetisch mit Geldfixierung, Originaldaten ohne) laufen grün; Mutation „Setup-Zeilen nicht
  tauschen“ macht beide rot.

## 3. Ausgeführte Prüfungen

`tools/claude/fetch-reference.sh original`, `setup-workspace.sh`, SDK 10.0.402; `tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3265 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2391 | 0 | 555 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2392 | 873 | 0 |

CI `build` am Head: success.

**Gegenbeispiele #847** (Scratch-Test `ReviewProbeTests`, Originaldaten, Saat 11, 10 Spieltage, danach 5 weitere und
Sichern/Laden bytegleich) – alle 6 grün:

- Start-Liga BL, Oberliga-Verein 30 → BL-Platz; ligaloser Verein 45 nach dem Laden → BL-Platz.
- Start-Liga 2. Liga, nach dem Laden, KÖLNER EC (5, BL) → Platz 16 (10 Spiele, 9 Punkte des Platzes), EC HANNOVER übernimmt
  Platz 5 in der BL.
- Start-Liga Oberliga, EHC ESSEN-WEST (15) → Platz 31, EA KEMPTEN übernimmt Platz 15 in der 2. Liga.
- Verein schon in der Start-Liga (2. Liga 17, Oberliga 30, je nach dem Laden): kein Tausch, `Clubs` unverändert.

**Mutationen** (temporärer Worktree, Filter `NeuerManagerTests` bzw. `AddManager`):

| Mutation | Ergebnis |
|---|---|
| `_tableRank` / `_rankSlots` / `SeriesCurrent` / `LeaguePenaltyMinutes` / Stärke nicht zurückgetauscht | je rot (Platzzustandstest) |
| offene Zeilen (`pending`) nicht mitgetauscht | rot (mehrere Zeilen) |
| `Setup.Managers` nicht getauscht | 4 rot (inkl. beider Mehrsaisonproben) |
| Bedingung ohne „≠ Bundesliga“ | 5 rot |
| Ausschluss Platz 0 entfernt | rot (Ausschlusstest) |
| Startwerte vor dem Tausch | rot (`AddManagerTests.AddManager_MidSeason_SeatsPoolSquadWithStartValues`) |
| Europapokal-Ausschluss entfernt | **grün** → P3-1 (eigene Probe rot) |

**Akzeptierte `[APPROX]`:** Zufallsverein ohne Wahl tauscht nicht (wie Spielstart, im Original ungeprüft); Taktik-Automatik-Zeile
wandert mit (Original schreibt sie vor dem nächsten Spiel neu); „alle Plätze ausgeschlossen → kein Tausch“ (nicht erreichbar).
**Annahmen, nicht geprüft:** Fenster 29. April – `StartNextSeason` (Platzwahl nach laufender Satzfolge, PR-Restlücke); kein
Pixelvergleich (keine Bildschirmänderung).
