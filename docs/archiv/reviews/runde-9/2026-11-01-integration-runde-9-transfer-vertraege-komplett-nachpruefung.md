# Nachprüfung: Integration Runde 9 – transfer-vertraege-komplett (PR #336)

- **Nachprüfung von:** `docs/reviews/2026-11-01-integration-runde-9-transfer-vertraege-komplett.md` (bisher geprüfter Head `34eeec8`)
- **Geprüft:** Head `4458299625dccd56d22ee74c1b442728085307c9`, nur Delta `34eeec8..4458299` ohne den Merge von #335
  (`e4460fc`, Eltern `34eeec8` + `b1a8657`; `git show --remerge-diff` leer, also konfliktfrei), d. h. `e4460fc..4458299`
  (`6c4f50b`, `4458299`), dazu P1-01 (#345) und P2-01 (#346).
- **Reviewer:** Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne Implementierungskontext.
- **Checkout:** Worktree, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich). Ausführung am nachgewiesenen Checkout, nicht nur Codelesen.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** P1-01 ist behoben: Stände von Schema 18 (eigene, an `6504333` erzeugt) und 19 (Fixture)
laden, spielen einen Saisonwechsel weiter und sichern bytegleich; ohne die Korrektur ist der neue Regressionstest rot.
P2-01 ist behoben: Die Mutation „Tausch abgeschaltet“ macht `OriginalDatabase_FourChanges_NamesUniqueAndIdsDistinct` rot.
Die Fixture enthält keine Originaldaten (Regel 2.1 eingehalten, unten). Alle drei Läufe grün. Neu nur P3-03 (Schwelle der
P2-01-Prüfung knapp und an den Zufall gekoppelt), kein Merge-Hindernis.

## 2. Befunde

### P1-01 (#345) – behoben

[`SaveGame.Migration.TransferVertraegeKomplett5.cs` Z. 27](https://github.com/kaaasi/eishockey-manager-redone/blob/4458299625dccd56d22ee74c1b442728085307c9/src/EishockeyManager.Core/SaveGame.Migration.TransferVertraegeKomplett5.cs#L27)
legt `_databaseNames` leer an, `Renumber` jetzt auch, wenn nur dieses Feld fehlt.

- **Regressionstest rot → grün:** Migration auf `6c4f50b~1` zurückgesetzt → `Migration_RealSchema19Save_LoadsAndPlaysOn`
  und `Migration_FromSchema21_BuildsPoolOnFirstChange` rot („Feld _databaseNames in TransferMarket fehlt“); Head grün.
- **Eigene Stände von Schema 18:** Worktree an `6504333` (`SchemaVersion = 18`), temporärer Test (nicht committet):
  `TestGames.Create(8)` zu Beginn, nach 10 Spieltagen, nach `LifecycleRegressionTests.FinishSeason`, nach dem ersten
  Saisonwechsel; Originaldaten (Verein 2, Seed 5) nach 12 Spieltagen und am Saisonende. Am Head #336 **und** am Head #338
  je alle 6: `Load` erfolgreich, `ValidatePlayerIdentity()` leer, bis Saisonende weiter + `StartNextSeason`, Pool 199 Plätze
  mit 199 verschiedenen Namen, Sichern → Laden → Sichern bytegleich.
- **Fixture `schema19-synthetisch.ems.gz` (Regel 2.1):** entpackt 1,3 MB JSON, `schema` 19. Namen nur `VEREIN n` /
  `SPIELER n` (synthetische Datenbank `TestGames.CreateDatabase`), dazu Aktiennamen (`Herrenhäuser`, `Play Time`,
  `Software 2000`, `KRON Soft`), die schon im Quelltext stehen (`FinanceStocks.cs`, `FinanceSponsors.cs`). Keine Zeichenkette
  über 30 Zeichen außer der Kennung, keine Binär-/Base64-Blöcke. **Nachgebaut:** Am Head #335 (`b1a8657`)
  `TestGames.Create(8)` + 10 × `TestSeason.PlayMatchDay` + `SaveGame.Save` → **byteidentisch** zur entpackten Fixture. Die
  Herkunft im Testkommentar stimmt also, es steckt nichts aus `gamedata/` darin.
- `EmbeddedResource` mit `LogicalName="Fixtures.%(Filename)%(Extension)"` – Ressource wird in allen drei Läufen gefunden.

### P2-01 (#346) – behoben

Gegenprobe erneut: [`Transfer.Pool.cs:461`](https://github.com/kaaasi/eishockey-manager-redone/blob/4458299625dccd56d22ee74c1b442728085307c9/src/EishockeyManager.Core/Transfer.Pool.cs#L461)
`if (squads.Remove(name, out var holder))` → `if (false && …)` → `OriginalDatabase_FourChanges_NamesUniqueAndIdsDistinct`
**rot** („152 Poolspieler mit neuer ID tragen den Namen eines Kaderspielers (Saison 1)“), übrige 13 Tests der Klasse grün.

### P3-03 (neu) – Schwelle `renamedClashes <= 5` knapp, Kommentar „0–1 je Saison“ stimmt nicht

[`TransferVertraegeKomplettTeil5Tests.cs` Z. 342–344](https://github.com/kaaasi/eishockey-manager-redone/blob/4458299625dccd56d22ee74c1b442728085307c9/tests/EishockeyManager.Core.Tests/TransferVertraegeKomplettTeil5Tests.cs#L342-L344)

- Gemessen (temporäre Ausgabe, Saison 1–4): Head #336 **0 / 0 / 1 / 3**, Head #338 **0 / 4 / 1 / 3**. Am früheren Head
  `34eeec8` (vor dem Merge von #335) waren es laut Review 0 / 8 / 10 / 13 – dort wäre der Test rot gewesen.
- Die Resttreffer sind die bekannte Lücke `PlayerFactory.TalentName` und hängen am Zufallsverlauf. Eine fremde Änderung am
  Zufallsverbrauch kann den Test ohne Fehler im Pool rot machen (Abstand am integrierten Stand: 1).
- Vorschlag: Talente über ihre Herkunft ausnehmen (oder gegen Spieler prüfen, die nicht erst im Lauf entstanden sind) statt
  einer festen Schwelle; Kommentar auf die gemessenen Werte berichtigen. Die Mutation bleibt mit großem Abstand rot (152).

### Erwartet offen – bestätigt

P3-01 (Mutationen ohne roten Test) und P3-02 (Belegung Pool +0x15/+0x16): Delta berührt `Transfer.Pool.cs` und
`TransferVertraegeKomplettTeil4ScreenTests.cs` nicht, unverändert offen.

## 3. Ausgeführte Prüfungen

```
git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD   # erreichbar
git show --remerge-diff e4460fc                                           # leer
git diff --stat e4460fc 4458299                                           # 4 Dateien, +60/−5
tools/pruefen.sh                                                          # alle drei Läufe (Testprojekt geändert)
dotnet test tests/EishockeyManager.Core.Tests --filter "FullyQualifiedName~TransferVertraegeKomplettTeil5Tests.Migration"  # mit alter Migration
EM_REQUIRE_GAMEDATA=1 dotnet test tests/EishockeyManager.Core.Tests --filter "FullyQualifiedName~TransferVertraegeKomplettTeil5Tests"  # mit Mutation Tausch
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1588 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1311 | 0 | 248 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1311 | 277 | 0 |

Commit `4458299625dccd56d22ee74c1b442728085307c9`, SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:** keine neuen im Delta. **Annahmen:** keine; die Schema-18-Stände decken nur die genannten
Zeitpunkte ab (kein Stand mitten in einer Pokalrunde oder mit offenen Verhandlungen).
