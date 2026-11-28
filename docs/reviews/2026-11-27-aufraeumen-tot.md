# Review PR #1064 – Spur aufraeumen-tot (Erstreview)

Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext) · Base `f9c4846` · Head `358579d` · 27.11.2026

## 1. Merge-Empfehlung

**Ja** für genau `358579d`. Verhalten unverändert belegt: Zustandsabdruck (Harness #1059) Base = Head bytegleich in allen
8 Abdrücken, Markerprobe nur die 8 gelisteten IDs, Testliste nur die begründeten Fälle. Alle drei Läufe von `tools/pruefen.sh`
grün. Die vier Schwerpunkte halten (s. u.). Einziger Befund P3 (zwei veraltete Crefs in Kommentaren), kein Merge-Hindernis.

## 2. Befunde

**P3-1 – zwei Crefs auf entfernte Methoden übrig geblieben** (Kommentar, kein Verhalten)
- [`GameState.Integration.cs:155`](https://github.com/kaaasi/eishockey-manager-redone/blob/358579d/src/EishockeyManager.Core/GameState.Integration.cs#L155)
  `<see cref="CatchUpTraining"/>` (entfernt mit K5).
- [`CupsNational.WorldCupLoop.cs:227`](https://github.com/kaaasi/eishockey-manager-redone/blob/358579d/src/EishockeyManager.Core/CupsNational.WorldCupLoop.cs#L227)
  `Kosten wie <see cref="TrainOneDay"/>` (entfernt mit K5).
- Beleg: `grep -rn "CatchUpTraining\|TrainOneDay" src --include=*.cs` auf Head → nur diese zwei Kommentarzeilen, keine
  Definition. Der Build meldet das nicht, weil kein Projekt `GenerateDocumentationFile` setzt (CS1574 aus). Die PR hat die
  übrigen Crefs (`DailyRun.cs`, `MatchGate.cs`) umgestellt, diese zwei fehlen.
- Vorschlag: beim Doku-Lauf `aufraeumen-doku` (oder Fixlauf) auf Text umstellen, z. B. „Training ab Tag 0 im Tageslauf
  (`ManagerSquadRun`)“ bzw. „Kosten einmal je Lauf (`FinanceCategory.Training`)“.

Keine weiteren Befunde.

**Erwartet offen – bestätigt:**
- K15 bleibt (A4 = A): `GameState.LoadQuirk.cs` ist im Diff nicht berührt.
- Restliste/Abweichungsliste und Fachdoku nennen noch alte Namen bis `aufraeumen-doku`: bestätigt in
  `docs/analyse/restliste-v1.md`, `docs/spielberechnung.md` (`FastCupResult`, `HealInjuries`, `MatchStrength`),
  `docs/transfer.md` (`CatchUp`/`AdvanceDay`), `docs/wettbewerbe.md:234` (`CountPowerPlayGoal`), `docs/portierung.md:60`,
  `docs/reverse-engineering.md:139` (Poisson). Kein Befund dieser PR.

**Schwerpunkte (Plan §2.1 Nr. 6, Verhalten unverändert):**
1. **K6, 20 Testdateien:** Gezielt gelesen `TransferOriginalVertragTests`, `KaderTransferNachtraegeTests`, `TransferTests`,
   `KaderTransferTeil4Tests`, `TransferTransactionRegressionTests`, `SpielberechnungKomplett3Tests`, `TestSeason.cs`. `RunUntil`
   hat Abbruchzähler (400) und läuft nur in `SeasonPhase.Running`; Ergebnis wird von nachfolgenden `Assert.Single/NotNull`
   abgesichert. Wertanpassung `SponsorRating_SecondPassRecomputesValueWithOffer` ist begründet und nicht tautologisch:
   Mutation `FinanceSponsorRating.cs:142` auf `12L * (11 - 6)` → Test **rot** (`Assert.Equal() Failure`), d. h. der Rang ist
   bis zum Gebot ≠ 6 und der Test misst den Rangterm tatsächlich. Gegenprobe `MarketBid_Expires_WhileListingStays`:
   `TransferMarket.Decline(bid)` in `GameState.DailyRun.cs:120` abgeschaltet → **rot**; `Heal_Weekly_PermanentStays` bleibt
   dabei grün (erwartet, anderer Weg).
2. **A993 / `AdvanceBids`:** Auf Base war `AdvanceBids(…, expire: true, …)` nur aus `AdvanceDay` erreichbar, `AdvanceDay` und
   `CatchUp` hatten in `src/` keinen Aufrufer (grep über Core, UI, Tools). Der verbliebene Aufrufer `CatchUpManagerDay` übergab
   `expire: false` mit Manager ≠ null (`ThrowIfNull`); neuer Filter `l.Seller == seller` ist dafür identisch zu
   `Seller is not null && Seller == only`. `CreateBid` nach Saisonende über `CatchUpManagerDay` betrifft nur den Testaufbau.
   Kein Weg einer echten Partie ändert sich; Zustandsabdruck bestätigt das.
3. **`KnockoutMatch.FromReport` wirft bei Gleichstand ohne Shootout:** Aufrufer nur `PlayWithMarks` und `Playoffs` (beide
   `knockOut: true`). Berichtsquellen: Forfeit (immer 2:0/0:2), schneller Weg (`MatchEngine.Live.cs:191–215`), Minutenweg
   (`LiveMatch.Finish` → `FinishMinuteLoop`, `MatchEngine.LiveStep.cs:128`, `MatchEngine.Live.cs:71`). `FinishMinuteLoop`
   setzt bei `KnockOut` und Gleichstand immer `ShootoutHomeWins`. `FastCupResultWithTie` (`MatchEngine.Minutes.cs:676–703`):
   Play-off ohne eigenen Gleichstand und ohne Zusatzwurf → kein Gleichstand; mit Zusatzwurf schleift `while (!ownTie && h+px
   == g+py)` bis ungleich; eigener Gleichstand → `Shootout = true`, Bericht hält Gleichstand **und** `ShootoutHomeWins`; übrige
   K.-o.: Gleichstand → `true`. Der Rückfall A138 ist damit unerreichbar; der entfallene `random`-Zug lag nur im Rückfall, also
   kein verschobener Zufallsstrom. Alle Tests mit Originaldaten (4002) und der Harness laufen mit dem `throw` grün.
4. **#1063:** `SeatLoosePlayers` und der öffentliche Mantel entfernt; Base-Aufrufer des Mantels nur
   `TransferVertraegeKomplettTeil5Tests.cs:329` (Test), Spielstart rief schon `SeatStartSquads`. Umbenennung hält die Crefs
   (A414/A415, `OriginalExport.Squads.cs`, `CupsNational.GameState.cs`, `GameState.AddManager.cs`) gültig.
   **#980 P3-3:** `ndisasm -b16 -e 0x54910 -o 0x54910 gamedata/em.exe` → `00054918 cmp byte [es:bx+0xa],0x0` / `jnz` /
   `00054922 cmp byte [es:bx+0x15],0x0` / `jz 0x49a2` – Kommentar in `MatchEngine.Managed.cs:111` stimmt.

## 3. Ausgeführte Prüfungen

Checkout: `git rev-parse HEAD` = `358579d632f64a32c43124318188b55020d1b0bb`, `git status --porcelain` leer; Base als eigener
Worktree `f9c4846`. SDK 10.0.402 (Release `claude-werkzeuge`), Originaldaten aus `kaaasi/eishockey-manager-referenz`
(`git ls-remote … HEAD` erreichbar).

| Probe | Befehl / Ablauf | Ergebnis |
|---|---|---|
| Zustandsabdruck | Harness aus PR #1059 (unverändert, Saat 1993, 4 Saisonwechsel, passiv 1 / aktiv 2 Manager mit Rundreise) in `tests/EishockeyManager.UI.Tests/` beider Worktrees, `EM_REQUIRE_GAMEDATA=1 dotnet test … --filter Zz_MigrationHarness`, `cmp` je Datei | alle 8 Abdrücke + 2 Logs **bytegleich**; Hashes wie PR (passiv `427150EB…`–`1FD1AA54…`, aktiv `D9D7F4C4…`–`BBCD733F…`), Rundreise je Saison gleich |
| Markerprobe | `python3 tools/reverse-engineering/markerliste.py` Base/Head, `diff` | 495 → 487, 188 → 187 Dateien; entfallen genau A137, A138, A416, A426, A693, A710, A859, A993; keine neuen IDs, sonst nur Zeilenverschiebungen |
| Testliste | `dotnet test tests/EishockeyManager.{Core,UI,Formats,Audio}.Tests --list-tests` Base/Head, `diff` | Core 2870 → 2867: entfallen `MatchEngineTests.AverageGoals_AroundSixAndAHalf`, `.StrongerTeam_WinsMoreOften` (Poisson, K3), `StatistikenVereinTests.CountPowerPlayGoal_OnlyManagerClubs` (K7); umbenannt 2 (K6); 8 Datenzeilen nur `expected`; UI/Formats/Audio unverändert |
| Abdruckwerte | Base mit `"MatchStrength", "RoleName"` in `RngTwinProbeTests.Excluded` und den neuen 8 Sollwerten, Filter auf die 3 Abdrucktests | 8/8 **grün** → neue Werte nur durch die entfallenen Eigenschaften |
| Mutation Sponsor | s. Schwerpunkt 1 | rot |
| Mutation Verfall | s. Schwerpunkt 1 | rot |
| ndisasm 0x54918 | s. Schwerpunkt 4 | bestätigt |

`tools/pruefen.sh` (alle drei Läufe) auf `358579d`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4002 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2810 | 0 | 673 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2811 | 1191 | 0 |

Ergebnis: bestanden (SDK 10.0.402).

**Akzeptierte `[APPROX]`:** keine neuen. **Annahmen:** nicht alle 20 K6-Dateien einzeln gelesen (Stichprobe s. o.); deren
Gleichwertigkeit stützt sich zusätzlich auf den grünen Lauf mit Originaldaten und den bytegleichen Zustandsabdruck, der den
Produktcode abdeckt, nicht die Testaufbauten.
