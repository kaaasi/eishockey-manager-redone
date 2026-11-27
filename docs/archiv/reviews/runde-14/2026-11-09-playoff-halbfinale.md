# Review PR #683 – Play-off-Halbfinale nach Tabelle DGROUP 0x144 (#676), R14HN-01..03, Import nach A380, R14KS9-01

**Reviewer:** Claude (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext (PR-Sitzung `…01GhACvSzq…`,
diese Sitzung `…01GhACvWQT…`). **Base:** `e5fe46f3f9e5c343366e505eab1cc5d48f863d47` · **Head:**
`84fe66c549ab4a2cc478477491aa620efa951bc7` · Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer. Ganzes Delta
gelesen (16 Dateien, +491/−82). Originaldaten aus `kaaasi/eishockey-manager-referenz` (`779a1c8`), SDK 10.0.402.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – aus zwei Gründen, die nicht am Kern liegen:
(a) Seit Base ist #672 auf `main` gemergt; der Head lässt sich nicht mehr konfliktfrei mergen (5 Sollwert-Dateien, R683-02), die
13 Sollwerte müssen auf dem gemeinsamen Stand neu ermittelt werden. (b) Spielstände (Schema 43), die vor diesem PR während
des Viertelfinales gesichert wurden, paaren nach dem Laden ein falsches Halbfinale (R683-01, P2).

Der Kern ist richtig: Tabelle 0x144, Schreiber 0x31BFF–0x31C07, Setzung, Heimrecht, Spielreihenfolge und Siegbyte 0x14 habe ich
selbst nachgelesen. Sie stimmen mit `Playoffs.cs` und dem Import überein. Alle neuen Tests werden durch die Mutationen rot.
Jeder der 13 geänderten Sollwerte fällt bei der Rückmutation auf das alte `Playoffs.cs`.

## 2. Befunde

### R683-01 (P2) – Alte Spielstände mit laufendem Viertelfinale: falsches Halbfinale nach dem Laden

`Playoffs._rounds` wird feldweise gesichert (`SaveGame.cs` L244, alle Instanzfelder). Am Base stehen die Viertelfinals in der
Folge 1–8, 4–5, 2–7, 3–6 (eigene Spiele). Aus importierten Ständen kommt die Folge `[0, 4, 2, 6]`, also 1–8, 3–6, 2–7, 4–5.
[`Playoffs.cs#L127`](https://github.com/kaaasi/eishockey-manager-redone/blob/84fe66c549ab4a2cc478477491aa620efa951bc7/src/EishockeyManager.Core/Playoffs.cs#L127)
paart jetzt Serie k mit k + 2 und setzt damit die neue Platzfolge voraus. Das Schema bleibt bei 43, eine Migration fehlt.

**Gegenbeispiel (ausgeführt):** Am Base `TestGames.Create(3)`, `Continue()` bis 4 Viertelfinalspiele gespielt sind, `SaveGame.Save`
→ Datei. Am Base weitergespielt ergibt das Halbfinale **1–4 | 2–3**. Dieselbe Datei am Head mit `SaveGame.Load` geladen und
weitergespielt ergibt **1–2 | 4–3**. Laut Tabelle 0x144 wäre bei diesen Siegern (1, 4, 2, 3) das Halbfinale **1–3 | 2–4** richtig.
Das Ergebnis am Head entspricht also weder dem Original noch dem alten Verhalten. Betroffen sind auch Stände, die am Base aus
einem HOK mit laufendem Viertelfinale importiert und dann gesichert wurden.

**Vorschlag:** Migration 43 → 44, die `_rounds[0]` in die Setzfolge (Platz 0, 2, 4, 6) bringt. Alternativ ordnet `Prepare` die
Vorrunde vor dem Paaren nach `Seeds`. Ein laufendes Halbfinale ist nicht betroffen, weil das Endspiel nur 2 Serien hat.

### R683-02 (P2, Wechselwirkung #672) – Head kollidiert mit `main`

#672 ist gemergt (`9cc7981`). `git merge-tree origin/main 84fe66c` meldet Konflikte in `GameModeTests.cs`, `JugendTeamsTests.cs`,
`RngTwinProbeTests.cs`, `SpielberechnungAnschlussTests.cs` und `DebCupDrawFlowTests.cs`. Beide PRs haben dieselben Mehrsaison-Abdrücke
neu gesetzt. Die Werte beider PRs gelten nur für den eigenen Base, deshalb lassen sich die Konflikte nicht einfach auflösen. Nach
dem Rebase müssen alle 13 Werte neu ermittelt werden. In den Kommentaren müssen beide Ursachen stehen (#672: WM-Schleife ohne
Bundestrainer; #676: Paarung). Dazu Rückmutation je Ursache.
`StatistikenPraemienTests` wird ohne Konflikt zusammengeführt. Der Fall hängt vom Zufall ab (S-K6-c), deshalb nach dem Rebase
erneut prüfen. Für den neuen Head ist eine Nachprüfung nötig (Delta `84fe66c..<neu>`).

### R683-03 (P3) – Mehrsaisonprobe: „aktiver“ Manager ändert nur den Eintrittspreis

[`PlayoffSemifinalTests.cs#L83`](https://github.com/kaaasi/eishockey-manager-redone/blob/84fe66c549ab4a2cc478477491aa620efa951bc7/tests/EishockeyManager.Core.Tests/PlayoffSemifinalTests.cs#L83):
Die Probe erfüllt AGENTS.md §5 (4 Wechsel, passiver Oberligist und aktiver Bundesligist, über `Continue()`). Sie paart in jeder
Saison nach 0x144, und mindestens einmal hat der schlechter Platzierte Heimrecht. Der aktive Manager ändert aber nur den
Eintrittspreis, ohne Transfers und ohne Verträge. Für diesen PR reicht das, weil nur Play-offs und Import betroffen sind. Hinweis,
kein Merge-Hindernis.

### R683-04 (P3) – Name `PlayoffSeries.HigherSeed`

Ab dem Halbfinale ist `HigherSeed` der erste Verein der Paarung (Platz 2k, Heimrecht) und kann schlechter platziert sein. Das ist
dokumentiert (Doc-Kommentar und PR „Restlücken“). Der Name ist irreführend für die Bildschirme, die ihn als „besser platziert“
lesen könnten. Eine Suche in `src/EishockeyManager.UI` ergab keinen solchen Leser, deshalb nur zur Kenntnis.

**Erwartet offen (bestätigt):** #678 und #679 sind offen (andere Läufe). #672 → R683-02.

## 3. Ausgeführte Prüfungen

### Gegenbeispiele aus dem Auftrag (selbst nachgelesen, `tools/reverse-engineering/disasm.py`)

| Punkt | Ergebnis |
|---|---|
| Tabelle Datei 0x16DD0 + 0x144 | `09 18 0B 18 0A 18 0C 18 0D 18 0E 18`: Platz i → 0x5D3A + T[i]. 0 → 8, 2 → 10, 4 → 9, 6 → 11, 8 → 12, 10 → 13, ungerade → 23 |
| Schreiber 0x31BED–0x31C07 | `mov al,[bx+0x5d3b]` → `mov dl,[bx+0x144]` → `mov [bx+0x5d3a],al`. Danach (0x31C24–0x31C46) Tausch zurück bei geradem i mit CCA4[i] = 0x14, **nach** der Kopie |
| Halbfinale 1–8 gegen 3–6, 2–7 gegen 4–5 | Mit der Setzung Platz 2i = 0x5CCB[i], 2i + 1 = 0x5CCB[7 − i] belegt. `Prepare` k ↔ k + 2 auf Platzfolge 0, 2, 4, 6 stimmt |
| Heimrecht beim kleineren Platz | `NextPairing` Spiel 1 beim ersten Verein = Platz 2k = Sieger der oberen Paarung (0x144) – stimmt |
| Endspiel | 8 → 12, 10 → 13, Heim = Sieger Platz 8 – stimmt (synthetisch: 3 vor 2) |
| Siegbyte 0x14 (0x3168B–0x316C9) | CCA3 < 4: immer Tausch. CCA3 = 4 und Gast-Siege ≤ 2: kein Tausch (→ 0x3173A). CCA3 = 4 und > 2: 0x14, dann Tausch. Daraus folgt, dass ein Sieger von Platz 2k + 1 nach 4 Täuschen immer 0x14 bekommt, auch bei 3:0. Der neue Text (R14HN-02) stimmt |
| Spielreihenfolge im Viertelfinale | Schleife 0x31B7D–0x31B9A: 0x315E9 für Platz CFFA, +2, … bis 0xFF – Platzfolge, wie jetzt `Rounds` |
| FILE22/FILE44 Paarungen und Siegbyte im Import | `Playoffs_OwnRounds_PairLikeTheOriginal`, `Import_Playoffs_FromOriginal` grün. Mutation N-e (0x14 ignoriert) → FILE22 rot |
| Laufende Runde, Tausch bei ungerader CCA3 | `Import_RunningQuarterFinal_…` k = 1–4 grün. Mutation N-d → k = 1, 3 rot |
| Aufstiegsrunde 3/6 | grün. Mutation N06 (immer 6) → rot |
| FILE26 HOCK / FILE41 HOHENADL nur auf dem Markt-Platz | grün. Mutationen N-f und N-g → rot (auch `Import_MarketListingOfManager`) |
| Überträge ab der zweiten Saison | `Import_CarriesPerCompetition_AllStands` grün. Mutation I3 → rot |
| `ImportConstruction` (R14HN-01) | `s + 2p + 1` = `0x117 + 2p`, `s + 0x12 + p` = `0x128 + p` (s = 0x116), also ohne Wirkung auf das Verhalten. N30 (Zeile entfernt) → Feldkartenprüfung rot |

### Mutationen (temporärer Worktree, `dotnet test --filter`, Originaldaten)

| Mutation | rote Tests |
|---|---|
| N-a Paarung k/k + 1 statt k/k + 2 | `NextRound_…`, `OwnRounds` (FILE44, FILE22), `MultiSeason_…` |
| N-b Heimrecht beim besser Platzierten | dieselben 4 |
| N-c Import `pairSlots [0, 4, 2, 6]` | `OwnRounds` ×2, `Import_Playoffs_FromOriginal` ×2 |
| N-d (N08) kein Tausch in laufender Runde | `RunningQuarterFinal` k = 1, 3 |
| N-e Siegbyte 0x14 ignoriert | `OwnRounds` FILE22, `Import_Playoffs_FromOriginal` FILE22 |
| N-f Markt-Spieler wieder im Kader | `OnlyOnMarket` ×2, `CarriesPerCompetition`, `Import_MarketListingOfManager` |
| N-g alte Angebotsbedingung (nur Kader) | `OnlyOnMarket` ×2, `Import_MarketListingOfManager` |
| N30 `ImportConstruction` entfernt | `FieldMap_ImportedRows_…` |
| N06 Aufstiegsrunde immer 6 Termine | `ThreeOfSixDates` |
| I3 `ClubCupGamesCarry = 0` | `CarriesPerCompetition`, Feldkartenprüfung |
| `StripComments` nicht angewandt | grün. Das ist erwartet, solange die Codezeile steht. Nur zusammen mit N30 deckt der Abzug den Fall ab, und N30 ist rot |

### Rückmutation der 13 Sollwerte (altes `Playoffs.cs` von Base)

- Altes `Playoffs.cs` mit den neuen Sollwerten: alle 13 rot. Das sind `RngTwinProbeTests` (4 synthetische und 2 mit
  Originaldatenbank), `CountersPerSeason` (2), `JugendTeamsTests` (2), `Mode4_ReferenceValues_Unchanged` (1) und
  `DebCupDrawFlowTests.MultiSeason_…` (1). Rot wurden außerdem die erwarteten neuen Tests (Paarung, MultiSeason, OwnRounds).
- Altes `Playoffs.cs` mit den alten Sollwerten (Testdateien von Base): Core 30/30 und UI 6/6 grün. Damit hängen alle Werte allein an
  `Playoffs.cs`, also an Paarung und Serienfolge. Beide Ursachen einzeln habe ich nicht getrennt.
- `StatistikenPraemienTests` (kein Sollwert, Strafminuten 0 → 1): Mit neuem `Playoffs.cs` und altem Test (Rivale 0) ist der Test
  rot, mit altem `Playoffs.cs` grün. Die Begründung (Gleichstand per Zufallsmischung) ist schlüssig.

### `tools/pruefen.sh` am Head (alle drei Läufe, Ergebnis: bestanden)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2555 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2022 | 0 | 438 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2023 | 532 | 0 |

Darin sind enthalten: die Mehrsaisonprobe `MultiSeason_FourChanges_PlayoffsPairByTable0x144` (4 Wechsel, passiv und aktiv) und
`Import_AllStands_FourSeasonChanges` (alle Stände, 4 Wechsel). Beide sind grün.

### Regeln

- AGENTS.md §4: Zeit nur über `Continue()`, also ohne eigene Zähler. Es gibt keine Buchungen und keine Bildschirme.
- Dateigrenzen: `CupsNational.WorldCupLoop.cs`, `DayCounter.cs` und `GameState.Integration.cs` sind unberührt, es gibt keine
  gemeinsamen Dateien nach §2.5. Das Schema bleibt 43 (zur Folge siehe R683-01).
- **Akzeptierte `[APPROX]`:** unverändert die Termine der Aufstiegsrunde (Punktsumme / 4) und die Marken in CCB2, die nicht gedeutet
  werden. Neue `[ORIGINAL]` (0x144, 0x319AC–0x319D4, 0x316BF, 0x4A6C7) habe ich nachgelesen; 0x4A6C7 nur als Fundstelle,
  ohne eigene Ausführung.
- **Annahme:** Kein Bildschirm wertet `HigherSeed` als „besser platziert“ aus (Suche in `src/EishockeyManager.UI`, R683-04).
