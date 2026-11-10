# Nachprüfung PR #726 – Spur kader (Review `2026-11-10-kader.md`)

Reviewer: Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext · 10.11.2026
Geprüfter Stand: Base `2f9cc99`, Head `68a976635136f1919fbf74adf2a2488c6fce23d8` (`git rev-parse HEAD` = Head,
`git status --porcelain` leer). Bisher geprüft: `bebf704` auf Base `3af4ec9`. Delta: Rebase (`git range-diff
3af4ec9..bebf704 2f9cc99..68a9766`: Teile 1, 2, 4 nur in Kommentaren/Abdrücken geändert, Teil 3 gleich) plus `ab1747f`
(P3-1, P3-4) und `68a9766` (R15-E3 = C, P3-2, P3-3, Abdrücke). CI `build` auf dem Head: success.

## 1. Merge-Empfehlung

**Ja** für genau `68a9766`. Das Gegenbeispiel R15-E3 ist mit `src/` von `fd83ed1` rot und am Head grün; Speichern/Laden und
eine weitere Saison nach dem Verlust bleiben stabil. Alle angepassten fremden Tests bestehen die Rückschaltprobe gegen
`src/` von main, und jede Anpassung ist am Head nötig (Base-Fassung rot). P3-1…P3-4 erledigt; offen bleibt nur ein P3
in der Doku.

## 2. Befunde

**P3-5 – Teilebeschreibung in `docs/arbeitspakete.md` widerspricht dem Head.**
[docs/arbeitspakete.md#L79-L80](https://github.com/kaaasi/eishockey-manager-redone/blob/68a976635136f1919fbf74adf2a2488c6fce23d8/docs/arbeitspakete.md#L79-L80)
sagt noch „Vertragslose aus Managervereinen werden freie Poolspieler (Besitzer 6) und kommen nur über den Listenwechsel
zurück“. Am Head (und im Original, FILE46 Pool 196 `+0x1A = 0`) bleiben sie verloren, ihr Platz bleibt vergeben
(`Transfer.Pool.cs` `IsLost`; Status Z. 81–84 sagt es richtig). Beleg: `grep -n "Besitzer 6) und kommen nur"
docs/arbeitspakete.md` → Z. 80. Die Zeile steht schon so auf main (Text der Leitstelle) → Leitstelle beim Merge berichtigen.

**Vorbefunde:** P3-1 (Abschwächung `SpielstaerkeManager2Tests` zurück, Datei nicht mehr im Diff gegen main), P3-2
(Kommentar `JugendTeamsTests` nennt Teil 1), P3-3 (`Teil5`: vergebene Plätze im Kader = Kaderspieler außerhalb des
Startkaders, verlorene ausgenommen), P3-4 (Status) – alle erledigt.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh --schnell` (Head `68a9766`, SDK 10.0.402, Originaldaten aus dem Referenz-Repo):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2661 | 0 | 0 |

Alle folgenden Proben in einer Arbeitskopie (`git worktree`), Filter über die 13 fremden Testklassen, alle `Kader*`-Tests
und `DebCupDrawFlowTests` (Core 390–408 Tests, UI 6), `EM_REQUIRE_GAMEDATA=1`.

**R15-E3 = C (Gegenbeispiel):** `git checkout fd83ed1 -- src/`, Head-Tests →
`KaderVertragsloseTests` rot 3 von 5 (`ExpiredContractInManagedClub_NotOnTransferList`, `File44_SeasonChange_Kjellberg_LostLikeFile46`,
`File44_FourSeasonChanges_FreeAgentsNeverListed(passiv)`); Head grün. Mutation `IsLost` in `StartPoolSeason` aus → rot
`Kjellberg_LostLikeFile46`, `SquadLifecycleTests.SixChanges_OriginalDatabase` (2), Abdrücke RngTwin (1) und
Spielberechnung (2). Zusatzprobe (temporärer Test, nicht committet): FILE44 → Saisonwechsel → `SaveGameTests.AssertRoundTrip`
→ Platz 196 KJELLBERG `Taken`, Spieler nicht in `AllPlayers`; eine weitere Saison und erneuter Roundtrip grün, nicht auf
der Liste, Alter bleibt 33 (das erwartete `[APPROX]`). Achtung beim Nachstellen: Ein zweiter KJELLBERG (Nationalmannschaft
Schweden, 24) steht in `AllPlayers` – nur per Referenz prüfen; die Tests des PR tun das.

**Rückschaltprobe (R15-E1/E2):**

| Stand | Ergebnis |
|---|---|
| A: `src/` main + fremde Tests main (neue Kader-Tests entfernt) | 390/390 + 6/6 grün |
| B: `src/` Head + fremde Tests main | rot: `TransferVertraegeKomplettTests` (5), `LifecycleRegressionTests` R204 (1), `SquadLifecycleTests` (6, auch über `RunProbe` in `KaderStart*Tests` 6), `Teil5` (2), `PlayoffSemifinalTests` (1: „kein Heimrecht beim schlechter Platzierten in fünf Saisons“), `FinanceCalibration` (4), `HeimspielStatistik` (1), alle Abdrücke (RngTwin 6, Spielberechnung 2, GameMode Mode4, JugendTeams 2, DebCupDrawFlow 1) → jede Anpassung nötig |
| C: `src/` main + Head-Tests (ohne `KaderJugendSaisonendeTests`, `SeasonEndYouth.MaxCount` → 2) | grün: `SquadLifecycle`, `Teil5`, `PlayoffSemifinal` (Saat 8), `FinanceCalibration`, `HeimspielStatistik`; rot nur die neuen Erwartungen (`TransferVertraegeKomplettTests` R15-E3 4, `Lifecycle` R204 1, `TagestaktHauptweg` Vollliste, `KaderTendenz`/`KaderVertragslose`) und die Abdrücke |

Die gelockerten Prüfungen sind eng gefasst: verlorene Spieler müssen aus `expiringManaged`/`squadBefore` stammen
(Vertrag ≤ 1, keine Leihe); `PlayoffSemifinal` Saat 7 → 8 nur wegen Abdeckung (Paarungsregel selbst grün).

**Rebase-Abdrücke:** Werte von main mit `src/` von main grün (Stand A); mit Head-`src/` rot (Stand B) → neue Werte folgen
aus der Spur, nicht aus dem Rebase.

**Akzeptierte `[APPROX]` / erwartet offen (bestätigt):**

- Poolplatz eines verlorenen Spielers altert nicht weiter (`Transfer.Pool.cs` `StartPoolSeason`, Probe: 33 nach zwei
  Wechseln; Original 0x274A7 altert alle Plätze).
- Ereignissymbol 9 im Fenster „Saisonende“ fehlt (`FinanceTalent.cs` Z. 176, braucht `SeasonFlow.cs`).
- Startenergie neues Spiel weiter offen, nicht Teil dieser Spur.

**Annahmen (nicht geprüft):** Disassembly-Lesart 0x27DE3–0x27E9D aus dem Erstreview übernommen, im Delta nicht erneut
verfolgt; dass außer Pool-Platz keine Referenz auf verlorene Spieler bleibt, nur über `AllPlayers`/Roundtrip geprüft.
