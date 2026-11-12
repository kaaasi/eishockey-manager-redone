# Review PR #790 – Spur wettbewerbe-rest (Erstreview)

Reviewer: Claude Opus 5.5, geplanter Lauf (unabhängige Sitzung, nicht an der Umsetzung beteiligt).
Geprüft: Head `6319b7eaf8ac62ee6ce040f75f199531b25fb049`, Base `e1476e65e721a2266752949d798e2d1539194e61`
(Checkout verifiziert: `git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja für genau diesen Head** – mit einer Bedingung für den Merge: Der PR ist gegen den aktuellen `main` (`a62ab21`, #794/#796)
**nicht konfliktfrei** (GitHub `mergeable_state: dirty`, s. u.). Inhaltlich: alle acht Rückmutationen der Spur werden rot, alle
drei Läufe von `tools/pruefen.sh` wie gefordert, die em.exe-Lesarten für A113/A142 und A004 an der Disassembly nachvollzogen
und A142 zusätzlich am Video bestätigt. Keine P0–P2.

Konflikte (nur gemeldet, nicht rebased; `git merge-tree --write-tree --name-only origin/main <head>`), alle in Abdruck-/Saattests,
die #794 ebenfalls angefasst hat:
`GameModeTests.cs`, `JugendTeamsTests.cs`, `NationalCoachMode4Tests.cs`, `RngTwinProbeTests.cs`, `SeasonEndStatusTests.cs`,
`SpielberechnungAnschlussTests.cs`, `UI.Tests/DebCupDrawFlowTests.cs`. Nach dem Rebase müssen diese Abdrücke/Saaten auf dem
gemeinsamen Stand neu gezogen werden; das ist kein „Rebase ohne Konflikt“ (§5 Punkt 4) – die Leitstelle entscheidet, ob die
Delta-Prüfung reicht oder eine Nachprüfung allein dieser Testdateien nötig ist.

## 2. Befunde

Keine P0, P1 oder P2.

- **P3 (Codepflege):** `MatchEngine.LastRules` ist ein öffentliches, veränderliches `[ThreadStatic]`-Feld im Produktionscode,
  das nur Tests lesen ([MatchEngine.Live.cs:105/156](https://github.com/kaaasi/eishockey-manager-redone/blob/6319b7eaf8ac62ee6ce040f75f199531b25fb049/src/EishockeyManager.Core/MatchEngine.Live.cs#L156)).
  Beleg: `grep -rn LastRules src tests` → einziger Schreiber `PlayWithEvents`, einzige Leser `WettbewerbeRestTests.cs`
  Z. 40–79. Kein Spielzustand, wirkt nicht aufs Ergebnis; besser `internal` (InternalsVisibleTo) oder Rückgabe über den
  `MatchReport`. Issue legt die Leitstelle an (Sammel-P3).
- **Hinweis (kein Befund):** `NationalCoachMode4Tests.Mode4_TwoManagersInTopFive_…`/`SeasonEndStatusTests.Full_…` hängen laut
  Kommentar an einer von 40 Saaten (14). Nach R15-E2 = C zulässig und begründet, aber sehr spröde; beim Neuziehen nach dem
  Rebase wird die Saat erneut wandern.

### Gegenbeispiele (Rückmutation des Fixes → zugehöriger Test rot)

Je Mutation nur die genannte Zeile im Head geändert, `EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests -c Release --filter …`:

| ID | Rückmutation | Ergebnis |
|---|---|---|
| A008 | Freundschaftsspiel wieder `MatchCompetition.WorldCup` (`CupsNational.GameState.cs`) | `Friendly_UsesRulesWithoutWorldCupFlag` (2) rot |
| A110 | `Friendly` aus dem schnellen Weg der Pokale genommen (`MatchEngine.Live.cs`) | `FriendlyWithoutCoach_TakesFastWay_…` rot |
| A084 | `PenaltyManagerValue = 20` (`MatchEngine.National.cs`) | `NationalPenaltyManagerValue_IsHomeGoalsOfBundesligaDay13Pairing6` rot |
| A024 | Ersatzaufstellung: alle Verfügbaren eingesetzt | `EmptyFormationWithAutoLineup_PlaysLineupOfMenuRebuild` rot |
| A004 | Auslosung wieder `Club.Strength < 70` (`CupsEurope.cs`) | `EuropeanCupDraw_ComparesMeanOfClubFields` rot |
| A045 | `LeagueCode` wieder „ligalos = Bundesliga“ | `SpectatorLeagueCode_OfClubWithoutLeague` (40, 52) rot |
| A077 | Penaltyschießen in jedem Nicht-Ligaspiel statt nur mit K.-o.-Merker | `MinuteLoop_ShootoutOnlyWithKnockOutFlag(EuropeanCup, false)` rot |
| A113 | Spielstart wieder `Schedule.Rounds(members)` (`NewGame.cs`) | `NewGame_LeaguePlan_FollowsSlotOrder` (3, 17) rot |
| A113 | Saisonwechsel wieder `Schedule.Rounds(members)` (`GameState.Spielplan.cs`) | `MultiSeason_FourChanges_PlanFollowsSlotOrder` (passiv, aktiv) rot |
| A142 | `DateIndices = [0, 2, 5, 7, 9, 10]` | `RelegationDates_AsOriginal` rot |
| A142 | Aufstiegsrunde wieder `Schedule.DoubleRoundRobin` | `RelegationGroup_UsesFourTeamTableInParticipantOrder` rot |

### Besonders geprüft

1. **Liga-Spielplan (A113) und Aufstiegsrunde (A142) an em.exe** (`tools/reverse-engineering/disasm.py gamedata/em.exe …`):
   - 0x4E7C7: Buchstabe + 0x9F (= − 'a'), danach + `[bp+0x10]` (Basis) nach DGROUP 0x5D7B/0x5D7C (Datei 0x4E810, 0x4E837,
     0x4E862–0x4E885); Spieltag d ≥ n → d − (n − 1) mit Heimtausch (0x4E7D3–0x4E7EA). Weil der Tausch 0x36169 ganze
     Vereinssätze bewegt, ist der Buchstabe der Platz in der Satzfolge – `Schedule.ForSlots(LeagueSlots(…))` ist die richtige
     Abbildung. `OriginalRounds` (12er, d − 11, Runde 3 = Runde 1) und `CupGroup.Pairings` (4er, ab Spieltag 4 getauscht)
     entsprechen dieser Schleife. Import (`OriginalImport.cs:219`) nutzte schon die Satzfolge; Export erkennt beide Folgen.
   - 0x31A2C–0x31A5A: Aufstiegsrunde nur bei `[0xCCA3]` = 0 oder 2, Gruppenspieltag `2 · [0x7B0] + ([0xCCA3] = 2)` →
     Play-Off-Termine 5 · Runde + {0, 2} = 0, 2, 5, 7, 10, 12. Bestätigt am Video `folge07/folge07_03307.png` („Aufstieg
     2.Liga“): 6.2./25.2. Berliner SC – EA Kempten, ECD Sauerland – SC Riessersee; 10.2./22.3. SC Riessersee – Berliner SC,
     EA Kempten – ECD Sauerland; 20.2./29.3. Berliner SC – ECD Sauerland – genau Tabelle 0x324 mit Rückrunde.
   - Mehrsaisonprobe vorhanden (`MultiSeason_FourChanges_PlanFollowsSlotOrder`, 4 Wechsel, passiv/aktiv, 66 Paarungen je Runde).
2. **Länderspiele/Nationalteam (A008/A110, A084) und Europapokal (A004):** A084 – 0xA165 − 0xA0CB = 0x9A = 12 · 12 + 2 · 5 →
   Spieltag 13, Paarung 6, Heimseite; das Remake liest dieselbe Paarung aus dem (nun satzfolgentreuen) Plan inkl. getauschtem
   Heimrecht, `Results` wird erst im Saisonwechsel geleert, die WM endet am 28.4. – kein veralteter Wert. A004 – Datei
   0x34401–0x3441C: `call 0x4D316`, `cmp ax,0x46`, `jnl`, sonst `random(0,3)` ≠ 0 → neu ziehen; umgesetzt mit
   `SpectatorRules.Strength` (= 0x4D316). Vorab-`[APPROX]` (keine geladenen Teamwerte vor dem ersten Spiel) akzeptiert.
3. **Fremde Tests nach R15-E2 = C:** Begründungen stehen je Test mit Fundstelle. Rückschaltprobe (A113 Start + Saisonwechsel,
   A142 Plan + Termine zurückgedreht): 13 der 21 umgezogenen Fälle rot – alle sechs `RngTwinProbeTests`-/beide
   `SpielberechnungAnschlussTests`-Abdrücke, `GameModeTests.Mode4_ReferenceValues_Unchanged`, beide `JugendTeamsTests`,
   `StadiumAusbauTests.SeveralProjects_…`, `KaderTransferTeil4Tests.Season_MarketRunsEveryCalendarDay` – also an den Fix
   gebunden, wie im PR angegeben. Die Saatwechsel mit „alter Code grün“ (Mode4/SeasonEnd 71 → 14, Runde14 3 → 4,
   Teil7 3 → 1) ändern nur die Saat, nicht Prüfung oder Grenze. `KaderTransferTeil7`: `Game()` → `TestGames.Create(1)` ist
   gleichwertig (Helfer ruft nur `TestGames.Create()`).

### Erwartet offen (bestätigt)

- **A037:** bleibt begründetes `[APPROX]` (`FinanceSponsorRating.cs`: Verhalten von 0x31CD5 nach dem letzten Termin nicht gelesen).
- **#730** (P2, Belastung der Managerspieler) und **#773** (P3, Liga-Strafminuten FILE02→03): offen, vom PR nicht berührt.

## 3. Ausgeführte Prüfungen

- SDK 10.0.402 (`tools/claude/setup-workspace.sh`), Originaldaten aus `kaaasi/eishockey-manager-referenz`
  (`ls-remote` HEAD `a529b60`).
- `tools/pruefen.sh` (alle drei Läufe) auf dem Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2989 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2202 | 0 | 502 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2203 | 786 | 0 |

- CI auf dem Head: `build` success.
- Rückmutationen und Rückschaltprobe: Tabellen oben (je Mutation eigener Build in Arbeitskopie, danach zurückgesetzt).
- Disassembly: `disasm.py` für Datei 0x4E7C7–0x4E8A3, 0x31A0A–0x31A70, 0x343F0–0x34420.
- Konflikte: `git merge-tree --write-tree --name-only origin/main 6319b7e`.

**Akzeptierte `[APPROX]`:** Zufallszüge der früheren Mischung bleiben (Spielstart, Saisonwechsel, Aufstiegsrunde) – erhalten nur
den Zufallsverlauf; A084 Ergebnismarken nicht geführt; A024 Zeitpunkt (beim Lesen statt beim Menüaufbau); A004 vor dem ersten
Spiel; A077 Paarungen ohne Spiel; A037 (s. o.).
**Annahmen (nicht selbst geprüft):** Lesarten der Freundschaftsspiel-Adressen 0x30365/0x36B5D/0x544C0 (A008) und 0x35883 (A084)
nur über die Tests und die Dokumentation in `docs/reverse-engineering.md` nachvollzogen, nicht einzeln disassembliert.
