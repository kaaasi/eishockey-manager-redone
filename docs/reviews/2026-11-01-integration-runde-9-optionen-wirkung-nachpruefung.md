# Nachprüfung: Integration Runde 9 – optionen-wirkung (PR #335)

- **Nachprüfung von:** `docs/reviews/2026-11-01-integration-runde-9-optionen-wirkung.md` (bisher geprüfter Head `4f8cbc3`)
- **Geprüft:** Head `b1a8657abedc034a2544e985237e5b6faaded050`, nur Delta `4f8cbc3..b1a8657` (1 Commit, nur
  `tests/EishockeyManager.Core.Tests/MeetingHistoryTests.cs`) und R9O-01 (#343).
- **Reviewer:** Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne Implementierungskontext.
- **Checkout:** Worktree, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Originaldaten aus
  `kaaasi/eishockey-manager-referenz` (`ls-remote` erfolgreich). Ausführung am nachgewiesenen Checkout, nicht nur Codelesen.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** R9O-01 ist behoben: Der neue Test geht über den echten Spieltag, die Mehrsaisonprobe zählt
nur noch Vereinsplätze; das Gegenbeispiel ist jetzt in beiden Tests rot. Alle drei Läufe grün. Offen bleibt nur R9O-02 (P3).

## 2. Befunde

### R9O-01 (P2, #343) – behoben

- Gegenbeispiel erneut ausgeführt: [`GameState.cs:109`](https://github.com/kaaasi/eishockey-manager-redone/blob/b1a8657abedc034a2544e985237e5b6faaded050/src/EishockeyManager.Core/GameState.cs#L109)
  (`RecordMeeting(home, away, hg, ag);`) auskommentiert → **2 rot**:
  `MeetingHistoryTests.Meetings_SurviveFourSeasonChanges_AndSaveGame` und
  [`LeagueMatchDay_WritesMeeting_SeenByInfoBarAndOpponentWindow`](https://github.com/kaaasi/eishockey-manager-redone/blob/b1a8657abedc034a2544e985237e5b6faaded050/tests/EishockeyManager.Core.Tests/MeetingHistoryTests.cs#L137)
  (vorher: alle grün). Zurückgesetzt → grün.
- [`ClubEntries`](https://github.com/kaaasi/eishockey-manager-redone/blob/b1a8657abedc034a2544e985237e5b6faaded050/tests/EishockeyManager.Core.Tests/MeetingHistoryTests.cs#L153)
  nimmt die ersten `FirstNation × Slots × 2` Bytes (Vereinsplätze) – entspricht dem Vorschlag des Reviews. Der neue Test
  prüft Infoleiste (`MeetingEntries`) und Gegner-Fenster (`HeadToHead(0, …)`) gegen das Ergebnis aus `Results`, Wert
  `own + (against << 4)` wie `MeetingHistory.RecordAt`.

### R9O-02 (P3) – erwartet offen, bestätigt

Delta berührt `MeetingHistory.cs` nicht; die Bundestrainer-Zweige (`TableOwner`, `MeetingEntries` im Bundestrainermenü)
haben weiter keinen Test.

### Neue Befunde

Keine.

## 3. Ausgeführte Prüfungen

```
git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD   # erreichbar
tools/claude/fetch-reference.sh original && tools/claude/setup-workspace.sh ~/referenz/original/gamedata
git diff --stat 4f8cbc3 b1a8657                                           # 1 Datei, +26/−2
tools/pruefen.sh                                                          # alle drei Läufe
dotnet test tests/EishockeyManager.Core.Tests --filter "FullyQualifiedName~MeetingHistory|FullyQualifiedName~GegnerFenster"  # mit Mutation
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1522 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1256 | 0 | 237 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1256 | 266 | 0 |

Commit `b1a8657abedc034a2544e985237e5b6faaded050`, SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:** keine neuen im Delta. **Annahmen:** keine.
