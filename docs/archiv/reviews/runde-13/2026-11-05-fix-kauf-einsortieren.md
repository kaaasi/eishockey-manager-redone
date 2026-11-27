# Review PR #623 – Fix R13GN-01: Kauf sortiert nach Spielerart ein

Unabhängiges Review (Claude, geplanter Lauf, eigene Sitzung ohne Implementierungskontext) nach `docs/vorlagen/review-anfrage.md`.
PR [#623](https://github.com/kaaasi/eishockey-manager-redone/pull/623) · Base `49bbcaa6b2e9675bca7402b1b6fda102b27f9aba` · Head `0a731cf0e34b48ee267ddfbbad23cd9cb20039db`
(Branch `fix/r13-kauf-einsortieren`) · Befund R13GN-01 aus `docs/reviews/2026-11-05-runde-13-gesamt-nachpruefung.md`, Issue #622.
Geprüft: ganzes Delta (16 Dateien, +258/−37), Ausführung am nachgewiesenen Checkout.

## 1. Merge-Empfehlung

**Ja** für `0a731cf0e34b48ee267ddfbbad23cd9cb20039db`. R13GN-01 ist behoben: `PlayerFactory.InsertSlot` bildet em.exe Datei
0x42D55–0x42DA1 genau ab (erster leerer Platz oder erster Platz größerer Art, `ja` → Gleichstand der Art hinten), alle Wege
in Managerkader (Kauf/Leihe über die Transferliste, Talentsuche, Leihrückkehr) sortieren ein, Computervereine bleiben
hinten an (`[APPROX]`, begründet). Alle Gegenbeispiele rot am Base und grün am Head, jeder neue Test fällt unter einer gezielten
Mutation, die 4 geänderten Fingerabdrücke fallen einzeln per Rückmutation auf die alten Werte zurück. Alle drei Läufe von
`tools/pruefen.sh` am Head bestanden. Nur P3-Befunde (Testlücke Talentsuche, ungenaue Aufruferliste im Kommentar), kein offener
Prüfbereich außer den erwartet offenen Punkten.

## 2. Befunde

### R623-01 (P3): Einsortieren bei der Talentsuche ungetestet

[`FinanceTalent.cs:142`](https://github.com/kaaasi/eishockey-manager-redone/blob/0a731cf0e34b48ee267ddfbbad23cd9cb20039db/src/EishockeyManager.Core/FinanceTalent.cs#L142)
ruft `PlayerFactory.Join` mit der Vorgabe `managerSquad: true` – richtig, denn die Talentsuche ruft 0x42CB1 im Original für den
Managerkader auf (Aufruf Datei 0x06EB9 in 0x06D7F). Kein Test sichert das: Mutation `managerSquad: false` in `JoinScoutedTalent`
→ `EishockeyManager.Core.Tests` 1508/1508 und `EishockeyManager.UI.Tests` 602/602 grün (mit Originaldaten). Ein Talent-Torhüter
oder -Verteidiger würde unbemerkt wieder hinten angehängt. Vorschlag: ein Test wie `Buy_Goalkeeper_FromTransferList_SortedIn`
für `JoinScoutedTalent` (Saat mit Torhüter/Verteidiger aus dem Pool). Nicht merge-blockierend.

### R623-02 (P3): Aufruferliste im Kommentar ungenau

[`Player.Factory.cs:189–191`](https://github.com/kaaasi/eishockey-manager-redone/blob/0a731cf0e34b48ee267ddfbbad23cd9cb20039db/src/EishockeyManager.Core/Player.Factory.cs#L189-L191)
nennt „… Listenwechsel (0x4970A) … – alle für Managerkader (DGROUP 0x5DC3 + 0x618 · Manager)“. Der Listenwechsel 0x494EF setzt
bei Datei 0x494F9 `mov byte [0xa70e],0x4`, schreibt also Satz 4 (Transfermarkt), keinen Managerkader. Außerdem mischt die Liste
Funktionsköpfe (0x06D7F, 0x28165, 0x3CABF) und Aufrufstellen (0x2758B, 0x47066, 0x477E7, 0x4970A, 0x5B1CD); die direkten
Aufrufe `call 0x108:0x5c` stehen bei 0x2758B, 0x29996, 0x3CB09, 0x47066, 0x477E7, 0x4970A, 0x5B1CD, die Talentsuche ruft über
`call 0x14b7:0x5c` bei 0x06EB9. Auf das Verhalten des Remakes hat das keinen Einfluss (der Markt ist kein `Club.Players`).
Nebenbei folgt daraus, dass auch die Reihenfolge der Transferliste im Original nach Art einsortiert ist – im Remake nicht geprüft
(nicht Gegenstand von #623).

### Hinweis (kein Befund): Gleichstandstest unterscheidet Base/Head nur über die Nr.

`AutoLineup_AfterGoalkeeperPurchase_TieWonByLaterSlot` ist am Base nur wegen `Assert.Equal(3, bought.Number)` rot (Base: 26);
`Assert.Same(bought, …Goalkeeper)` ist am Base ebenfalls grün, weil der Neue in beiden Fällen der spätere Torhüterplatz ist.
In einem nach Art geordneten Kader ändert Einsortieren statt Anhängen die Folge innerhalb einer Art nicht. Die Gleichstandsregel
selbst sichert der Test trotzdem ab (Mutation M7 unten: rot); em.exe Datei 0x4292F `cmp [bp-6],si`/`jg` bestätigt „späterer
Platz gewinnt“.

## 3. Ausgeführte Prüfungen

**Zugriff und Stand:** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → `36aeca95…` (erreichbar).
Klon `git rev-parse HEAD` = `0a731cf0e34b48ee267ddfbbad23cd9cb20039db`, `git status --porcelain` leer; SDK 10.0.402 aus Release
`claude-werkzeuge`; Originaldaten `tools/claude/fetch-reference.sh original` (Referenz-Repo @ `36aeca95`).

**Disassembly (selbst nachgelesen, `python3 tools/reverse-engineering/disasm.py gamedata/em.exe 0x42CB1 0x42E30`, dazu 0x04180,
0x27540–0x27595, 0x42900–0x42950, 0x494EF, Aufrufstellen per Bytesuche `9a 5c 00 08 01`):**

- 0x42D46–0x42D50: Art des Neuen = 0x04180(Poolindex) nach `[bp-2]`. Schleife ab Platz 0 (0x42D53 → 0x42D58): Platz leer
  (`cmp byte [bx+0x5dd2],0` = +0x0F Poolindex 0) → einfügen; sonst Art des Platzes `cmp al,[bp-2]` / `ja` (0x42D96) → einfügen;
  sonst weiter bis Grenze `[bp+0x10]` (0x42D9E–0x42DA1). Ab 0x42DC3 memmove `(Grenze − i) · 0x34` von Platz i nach i + 1
  (0x42E01 `add dx,0x5df7`, 0x42E22 `call 0x0b830`). Entspricht `FindIndex(q => q.Role > p.Role)`, sonst hinten.
- 0x04180: Pool +0x18 / 25, höchstens 2 – mit den Rollenwerten (Torhüter 0, Abwehr 25–41, Sturm 75–91) also 0/1/2.
- Leihrückkehr 0x27553–0x2758B: Manager = Pool +0x1A, bei ≥ 4 (0x27576 `jnl`) kein Aufruf, sonst 0x42CB1 mit Grenze 0x1D –
  stimmt mit `managed.Contains(owner.Index)` in [`Player.Contract.cs:198`](https://github.com/kaaasi/eishockey-manager-redone/blob/0a731cf0e34b48ee267ddfbbad23cd9cb20039db/src/EishockeyManager.Core/Player.Contract.cs#L198).
- Aufrufer: Leihrückkehr (0x2758B), Spielstart (0x29996 in 0x28165; Remake `SquadSetup` legt T/V/S geordnet an), WM-Kader
  (0x3CB09), Transferliste Kauf/Leihe (0x47066, 0x477E7 → Remake `Complete`/`Move` mit `managerSquad: true`), Listenwechsel
  (0x4970A, Satz 4 = Markt, R623-02), Pool-Neuanlage 0x5AF50 (0x5B1CD), Talentsuche (0x06EB9, R623-01). Ein eigener Jugendweg in
  den Kader ruft 0x42CB1 nicht auf; im Remake gibt es keinen weiteren Einfügeweg in `Club.Players` (alle `Players.Add/Insert`
  und `Join`-Aufrufer gelesen: `Transfer.Squads.cs` Nachfolger/Auffüllen nur für Computervereine, `Transfer.Pool.cs` Tausch nur
  `!IsManaged`, `Transfer.Unsolicited.cs` und `Accept` nur Nicht-Managervereine).

**Annahme `Player.Role` = 0x04180 geprüft:** In allen 40 lesbaren Original-Spielständen (`emtool hok dump … 5DC3`, ohne
KAPUTT.HOK) ergibt Rollenwert/25 (max. 2) bei 1331 Kader- und Marktplätzen die Rolle der Originaldatenbank (`emtool vereine`) des
gleichnamigen Spielers; die 5 Abweichungen (ASLIN, DALPIAZ, STAUBER – Marktplätze) sind Namensgleichheiten mit Nationaltorhütern
auf Stürmer-Poolplätzen (bekannte Namensvergabe ab Pool 179), keine Rollenabweichung. Alle Managerkader aller Stände sind nach
Art geordnet (T…V…S) – vereinbar mit der Regel, kein Gegenbeleg.

**Gegenbeispiele (Test-Dateien des Heads auf den Base-Quellcode gelegt, `dotnet test tests/EishockeyManager.Core.Tests -c Release`,
`EM_REQUIRE_GAMEDATA=1`):**

| Gegenbeispiel / Test | Base | Head |
|---|---|---|
| Torhüter → Platz 2 hinter dem letzten Torhüter (`Join_Goalkeeper_LandsBehindLastGoalkeeper`, `TestGames.Create(3)`) | rot | grün |
| Verteidiger hinter dem letzten Verteidiger (`Join_Defender_LandsBehindLastDefender`) | rot | grün |
| Stürmer hinten (`Join_Forward_LandsLast`) | grün (erwartet) | grün |
| Ungeordneter Kader (`Join_UnsortedSquad_InsertsBeforeFirstGreaterRole`) | rot | grün |
| Nr. = Kaderplatz + 1 für alle Folgespieler (`AssertNumbersAreSlots` in allen obigen) | rot | grün |
| Auto-Aufstellung Gleichstand (`AutoLineup_AfterGoalkeeperPurchase_TieWonByLaterSlot`) | rot (Nr. 26 statt 3) | grün |
| Kauf Torhüter über die Transferliste (`Buy_Goalkeeper_FromTransferList_SortedIn`) | rot | grün |
| Leihrückkehr zu Managerverein (`LoanReturn_ToManagerClub_SortedIn`) | rot | grün |
| `KaderStartRunde13Teil4Tests.LiveSquad_FollowsSquadSlots_NotRoleAndNumber` (geändert) | rot | grün |
| `KaderStartRunde13Teil4Tests.MultiSeason_FourChanges_SlotOrderKept` active/passive (geändert) | rot / grün | grün / grün |
| 4 geänderte Fingerabdrücke (aktiv) | rot | grün |

Base gesamt 12 rot / 11 grün von 23 ausgewählten Tests.

**Mutationen am Head (Kopie, je ein Eingriff, Filter: `KaufEinsortierenTests`, `KaderStartRunde13Teil4Tests`, die 8
Fingerabdruck-Fälle):**

| Mutation | Ergebnis |
|---|---|
| M1 `InsertSlot`: immer hinten (`at = -1`, Regel zurück) | 12 rot: alle neuen Tests außer `Join_Forward_LandsLast`, beide geänderten Kadertests (aktiv), 4 aktive Abdrücke; Abdrücke exakt die alten Werte (`377126FABD83B898`, `C429365C16B41D63`, `FD69401E5D7397F7`, `98AD5EF36A0277C8`) |
| M2 `>` → `>=` (vor dem ersten gleicher Art) | 14 rot, u. a. `Join_Forward_LandsLast`, `Join_Goalkeeper…`, `Join_Defender…` |
| M3 hinter dem letzten Platz kleinerer/gleicher Art statt vor dem ersten größerer | 1 rot: `Join_UnsortedSquad_InsertsBeforeFirstGreaterRole` |
| M4 `Complete`: `managerSquad: false` (Kauf/Leihe Transferliste) | 6 rot: `Buy_Goalkeeper…`, `MultiSeason…(active)`, 4 aktive Abdrücke – alle 4 exakt auf den alten Werten |
| M5 `JoinScoutedTalent`: `managerSquad: false` | 0 rot – auch volle Core- (1508) und UI-Tests (602) grün → R623-01 |
| M6 Leihrückkehr `InsertSlot(…, false)` | 1 rot: `LoanReturn_ToManagerClub_SortedIn` |
| M7 Auto-Aufstellung `bestValue > value` → `>=` (früherer Platz gewinnt) | rot, u. a. `AutoLineup_AfterGoalkeeperPurchase_TieWonByLaterSlot` |
| M8 `InsertSlot` auch für Computervereine sortieren | 8 rot: alle 8 Abdrücke inkl. passiv – bestätigt die PR-Angabe zur `[APPROX]`-Wahl |

**Fingerabdrücke einzeln zurückgeführt:** `RngTwinProbeTests` Seed 3 aktiv (`57912317…` ↔ alt `377126FA…`), Seed 11 aktiv
(`FC146C17…` ↔ `C429365C…`), Originaldatenbank aktiv (`771D1BE3…` ↔ `FD69401E…`),
`SpielberechnungAnschlussTests.FourSeasonChanges_OriginalDatabase_CountersPerSeason` aktiv (`FB60D817…` ↔ `98AD5EF3…`): je unter
M1 und unter M4 exakt der alte Wert, unter M5/M6 unverändert → Ursache ist allein das Einsortieren beim Kauf über die
Transferliste. Passive Abdrücke unter M1/M4/M5/M6 unverändert.

**Mehrsaisonprobe:** `MultiSeason_FourChanges_SlotOrderKept` (4 Wechsel, passiv und aktiv) und die vier Abdrucktests über 4
Wechsel am Head grün. Die entfernte Zeile `Assert.Equal(stayed, own.Players.Take(stayed.Length))` ist mit dem Einsortieren
nicht mehr gültig; die Folge der Bleibenden prüft der Test weiter.

**`tools/pruefen.sh` am Head (alle drei Läufe):**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2273 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1908 | 0 | 327 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1909 | 364 | 0 |

Commit `0a731cf0e34b48ee267ddfbbad23cd9cb20039db`, SDK 10.0.402, Ergebnis: bestanden. CI (`build`) am Head grün.

**Akzeptierte `[APPROX]` / erwartet offen (bestätigt):**

- Computervereine hängen Neuzugänge hinten an (`InsertSlot(…, managerSquad: false)`): das Original führt für sie keine
  Kaderplätze (0x42CB1 schreibt nur DGROUP 0x5DC3 + 0x618 · Satz; Leihrückkehr 0x27576 überspringt Manager ≥ 4). Akzeptiert.
- WM-Kader-Reihenfolge (Aufruf 0x3CB09 in 0x3CABF) nicht geprüft – bestätigt offen, außerhalb von #623.
- Volle Kapazität (Grenze `[bp+0x10]`) nicht nachgebildet, Remake begrenzt vorher – akzeptiert.

**Annahmen:** Rollenzuordnung des Remakes für Poolspieler ohne Kaderplatz nur indirekt über Marktplätze geprüft (oben).
Die Reihenfolge der Transferliste (Satz 4) wurde nicht geprüft (R623-02, nicht Teil des Fixes).
