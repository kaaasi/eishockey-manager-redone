# Nachprüfung: Integration Runde 9 – spielberechnung-komplett (PR #338)

- **Nachprüfung von:** `docs/reviews/2026-11-01-integration-runde-9-spielberechnung-komplett.md` (bisher geprüfter Head `3726747`)
- **Geprüft:** Head `4318f84c05e3010281e5f855d8b2ab0d53cfb53d`, nur Delta `3726747..4318f84` ohne die Merges von #335/#336
  (`9a5e0ea`, Eltern `3726747` + `4458299`; `git show --remerge-diff` leer), d. h. `9a5e0ea..4318f84` (1 Commit, 4 Dateien),
  dazu R9S-01 bis -03 (#349–#351).
- **Reviewer:** Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne Implementierungskontext.
- **Checkout:** Worktree, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich). Ausführung am nachgewiesenen Checkout, nicht nur Codelesen.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** R9S-02 ist behoben (WM-Endrunde ohne Manager immer schneller Weg, Regressionstest fängt die
Mutation), R9S-01 und R9S-03 sind wie erwartet nur als `[APPROX]` gekennzeichnet, Verhalten unverändert. Die Merges von
#335/#336 sind konfliktfrei; die alten Stände von Schema 18/19 laden auch an diesem Head (siehe Nachprüfung #336). Alle drei
Läufe grün. Neu nur P3 (Kennzeichnung).

## 2. Befunde

### R9S-02 (P2, #350) – behoben

[`MatchEngine.Live.cs` Z. 82](https://github.com/kaaasi/eishockey-manager-redone/blob/4318f84c05e3010281e5f855d8b2ab0d53cfb53d/src/EishockeyManager.Core/MatchEngine.Live.cs#L82):
`WorldCup && knockOut` → `minuteLoop = false`. Einziger Aufrufer mit `WorldCup, knockOut: true` ist `KnockoutMatch.Play` aus
`CupsWorld.cs:210` (WM-Endrunde); WM-Gruppe (`CupsWorld.cs:185`) und Freundschaftsspiele (`CupsNational.GameState.cs:178`)
übergeben `knockOut: false` und folgen weiter dem Schalter.

- Gegenbeispiel erneut: [`WorldCupKnockoutWithoutManager_AlwaysFastPath`](https://github.com/kaaasi/eishockey-manager-redone/blob/4318f84c05e3010281e5f855d8b2ab0d53cfb53d/tests/EishockeyManager.Core.Tests/IntegrationRunde9Tests.cs#L50)
  (`PlayWithEvents(…, WorldCup, knockOut: true, new Random(7))`, Schalter an/aus) → gleicher `random.Next()`, grün.
- Gegenprobe: Sonderfall entfernt (`minuteLoop = false && …` → immer Schalter) → dieser Test **rot**. Umgekehrt WorldCup
  immer schnell (auch Gruppe) → `CupMatchWithoutManager_FollowsDisplaySwitch(WorldCup)` **rot**. Beide Richtungen gesichert.

### R9S-01 (#349), R9S-03 (#351) – erwartet offen, bestätigt

Code-Delta in `MatchEngine.Live.cs` ist nur die Zeile oben; `GameOptions.cs` und `docs/spielberechnung.md` §11 Nr. 2 nur
Kommentar/Doku. Beide Abweichungen stehen jetzt als `[APPROX]` mit Fundstelle (0x36FEA bzw. 0x37058–0x37085) und Issue-Nummer;
der frühere Kommentar „für Play-offs ohne Bedeutung“ ist berichtigt. Verhalten unverändert – Entscheidung des Nutzers steht aus.

### R9S-07 (P3, neu) – `[ORIGINAL]` für Code 10 ohne den Vorbehalt des Reviews

[`MatchEngine.Live.cs` Z. 75–76](https://github.com/kaaasi/eishockey-manager-redone/blob/4318f84c05e3010281e5f855d8b2ab0d53cfb53d/src/EishockeyManager.Core/MatchEngine.Live.cs#L75-L76),
`docs/spielberechnung.md` §11 Nr. 2: „Die WM-Endrunde (Code 10, 0x31017) … nimmt immer den schnellen Weg“ steht unter
`[ORIGINAL]`. Das Review hatte als `[PLAUSIBEL]` vermerkt, dass 0x31017 denselben Spieltagsablauf 0x36B5D erreicht
(Überlagerungssprung über 0xF0:0x70 nicht aufgelöst). Der Vergleich in 0x3702E–0x37085 selbst ist belegt, der Weg dorthin
geschlossen. Vorschlag: Halbsatz „Erreichen von 0x36B5D aus dem Aufrufmuster geschlossen“ ergänzen.

### Erwartet offen – bestätigt

R9S-04 bis R9S-06 (P3): Delta berührt `FinanceCalibrationTests.cs`, `TransferOriginalVertragTests` und §15 von
`docs/spielberechnung.md` nicht, unverändert offen.

## 3. Ausgeführte Prüfungen

```
git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD   # erreichbar
git show --remerge-diff 9a5e0ea                                           # leer
git diff --stat 9a5e0ea 4318f84                                           # 4 Dateien, +45/−6
tools/pruefen.sh                                                          # alle drei Läufe (Integrations-PR)
dotnet test tests/EishockeyManager.Core.Tests --filter "FullyQualifiedName~IntegrationRunde9Tests"  # Mutation 1
dotnet test tests/EishockeyManager.Core.Tests                                                       # Mutation 2: 1 rot / 1047 grün
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1629 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1349 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1349 | 280 | 0 |

Commit `4318f84c05e3010281e5f855d8b2ab0d53cfb53d`, SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:** R9S-01 (Entscheidung je Paarung statt je Spieltag) und R9S-03 (Play-offs/Aufstiegsrunde immer
Minutenschleife) bis zur Entscheidung des Nutzers. **Annahmen:** Erreichen von 0x36B5D aus 0x31017 (`[PLAUSIBEL]`, s. R9S-07).
