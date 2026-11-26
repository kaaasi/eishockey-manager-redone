# Review PR #1034 – Spur tagesabschluss (Teil 1 + 2)

Erstreview, Claude Code (Opus 5.5, geplanter Lauf, eigene Sitzung ohne Implementierungskontext). Base `56b7f7c`, Head
[`f397c08`](https://github.com/kaaasi/eishockey-manager-redone/tree/f397c089f560793f4a96802c1a4320c36d91487e), Checkout
nachgewiesen (`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau `f397c089f560793f4a96802c1a4320c36d91487e`. Alle drei Läufe von `tools/pruefen.sh` sind grün, beide
Gegenbeispiele lassen sich reproduzieren, und die Wurfreihenfolge stimmt mit den Bytes von em.exe überein. Der Umbau hält
die Architekturregeln ein: Zeit nur über `Continue`, kein Geld außerhalb von `Book`, zwei begründete Zeilen in
`GameState.Integration.cs`. Offen sind nur P3.

## 2. Befunde

Keine P0–P2.

- **R1034-01 (P3, Testabdeckung)** – Die Reihenfolge des Baus je Manager in der WM-Schleife
  ([`GameState.Manager.cs` Z. 178–179](https://github.com/kaaasi/eishockey-manager-redone/blob/f397c089f560793f4a96802c1a4320c36d91487e/src/EishockeyManager.Core/GameState.Manager.cs#L178-L179))
  wird nur indirekt geprüft. Beleg: Als Mutation „Bau für alle nach der Schleife“ (`WeeklyFinances()` hinter der Schleife)
  bleiben alle `Tagesabschluss*`, `RngTwinProbe*` und `SpielberechnungAnschluss*` grün (43/43). Im ganzen Core-Testprojekt
  wird nur `GameModeTests.Mode4_ReferenceValues_Unchanged` rot (1/2927), also ein Abdruck. Bei der Sommerpause ist es
  ähnlich: Mit der Mutation „Bau nach `DayClose`“ in `RunSummerDay` werden nur Abdrücke rot (`RngTwinProbe` 6,
  `SpielberechnungAnschluss` 3), kein gezielter Test. Der Code stimmt mit `hauptweg.py` Teil 2 und der Fundstelle Datei
  0x564F5–0x56512 überein. Empfehlung: Je ein Orakeltest wie `MainPathBuildDay_DamagePerManagerAfterDailyRun` für einen
  Bautag der WM-Schleife und einen der Sommerpause, mit zwei Managern und Level 5.
- **R1034-02 (P3, bestätigt offen, #188)** – Die Börse (`AdvanceStocks`, würfelt) läuft einmal je Tag. Laut `hauptweg.py`
  Teil 3 liegt sie im Original in 0x3ED01 jedes Managers, zwischen den Baufirmen (Datei 0x3ED20) und den Werbeangeboten
  0x3FB47. Im Remake liegt sie im Hauptweg nach der Talentsuche
  ([Z. 183–185, 197](https://github.com/kaaasi/eishockey-manager-redone/blob/f397c089f560793f4a96802c1a4320c36d91487e/src/EishockeyManager.Core/GameState.Manager.cs#L183-L197))
  und in der WM-Schleife nach den Stadionschäden. Selbst mit nur einem Manager weicht die Zufallsfolge dadurch vom
  Original ab. Das ist durch die Entscheidung #188 gedeckt und im Code als Abweichung vermerkt. Hinweis an die Leitstelle:
  Wäre ein Abgleich mit Saaten für einen Manager je gewünscht, müsste #188 neu entschieden werden.
- **R1034-03 (P3, Doku)** – Der Kopf von
  [`stadionschaeden.py` Z. 4](https://github.com/kaaasi/eishockey-manager-redone/blob/f397c089f560793f4a96802c1a4320c36d91487e/tools/reverse-engineering/tagesabschluss/stadionschaeden.py#L4)
  sagt „game level 1 … 5 → 0 … 4“. Code und `re-geld.md` sagen dagegen d = 5 − Level (`TrainingRules.LevelValue`). Die
  Zählrichtung ist im Skript also nicht eindeutig. Das Ergebnis ändert sich nicht, weil das Skript [0xED8] direkt setzt.

Restlücken der PR-Beschreibung geprüft und übernommen: Platzmeldung ohne Mindestbreite 0x374; Plätze unter 0; Eisflächen-Werbung
nach einem Schaden; Bau ohne Tageslauf nur mit Ladeversatz (`GameState.LoadQuirk.cs` Z. 52, #703); Heilung in der WM-Schleife
getrennt.

## 3. Ausgeführte Prüfungen

**Zugriff:** `git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD` → `7412a50…`. Originaldaten über
`fetch-reference.sh original`, SDK 10.0.402.

**Gegenbeispiele:**
- **SG-01 (#1018):** `python3 tools/reverse-engineering/tagesabschluss/hauptweg.py`
  - Hauptweg: je Manager 0x3ED01 → 0x3F833, dann Talentsuche 0x06FA5 und Wanderung 0x09D1B, dann je Manager 0x07169 → 0x3D316.
  - WM-Schleife: je Manager 0x3ED01 → 0x3F833 → 0x3D316 → Heilung.
  - Angebotstag: 13 Würfe je Manager.
  - Roter Test: `TagesabschlussTests` gegen `56b7f7c` (Worktree, Testdatei vom Head) → **5 rot**, 2 grün
    (`OfferDay_InterleavesPerManager` ×2, `MonthEndBuildDay_BooksUpkeepBeforeConstruction` ×2,
    `TalentSearch_DrawsAfterDayCloseOfAllManagers`).
- **R26-E1:** `python3 tools/reverse-engineering/tagesabschluss/stadionschaeden.py`
  - Fälle 1–8 wie in der PR-Beschreibung.
  - Zusätzlich unabhängig mit `ndisasm` nachgelesen:
    - Datei 0x3D316–0x3D330: der Aufruf 0x3CFA0 ist der erste Schritt.
    - 0x3CFB8–0x3CFD0: Gate `random(0, 2·[0xED8]+8)`, ≠ 0 → Ende.
    - 0x3CFEB–0x3D00F: Plätze `random(0, 3000) < 20·(5−[0xED8])`.
    - 0x3D012–0x3D036: Liga `random(1, 3)`, Schaden nur bei Liga < Wurf.
    - 0x3D049: Plätze ≠ 0 erst danach geprüft.
    - 0x3D063–0x3D08A: `random(P/20, P/6) + 150`, vorzeichenlos geteilt.
    - 0x3D14C–0x3D1CC: Projekte 3 und 4 mit 2500, `dec byte` erst nach dem Ligawurf und der Prüfung „Stufe ≠ 0“.
    - 0x3D1F0–0x3D203: Projekte 2 und 5 nur bei [0xED8] = 0.
  - Das stimmt mit `StadiumDamageRules.Draw` überein.

**Mutationen** (temporärer Worktree des Heads):

| Mutation | Ergebnis |
|---|---|
| Ligawurf `<= league` → `< league` | `LeagueRollAndEmptyFeature_GoOn` rot |
| Hauptweg: Bau vor `ManagerDailyRun` | `MainPathBuildDay_DamagePerManagerAfterDailyRun` rot |
| Sommerpause: Bau nach `DayClose` | nur Abdrücke rot (R1034-01) |
| WM-Schleife: Bau nach der Schleife | nur `Mode4_ReferenceValues_Unchanged` rot (R1034-01) |

**Rückschaltprobe fremder Tests (R15-E2 = C, R21-D2 = A):** `src/` von `36323e2` plus die Tests des Heads, ohne
`TagesabschlussStadionTests`, weil der Typ dort fehlt.
- **Rot (14):** `RngTwinProbe` 6, `SpielberechnungAnschluss` 3, `Mode4_ReferenceValues_Unchanged`, `JugendTeams` 2,
  `TeamwertAufstellung` (passiv), `KaderplatzWerteFix` (passiv).
- **Grün:** die nur gelockerten Tests `FinanceCalibration`, `StadiumAusbau` und `FinanceWerbung`.
- Das bestätigt die Angaben im PR. Die Begründungen im Code stehen je Test und nennen die Ursache (Datei 0x3CFC5, 0x3D330).

**Geld und Integration:**
- `StadiumDamageRules.Draw` bucht nichts, und im Diff kommt keine neue Kontoänderung außerhalb von `Book` hinzu.
- Die zwei Zeilen in `GameState.Integration.cs` reichen den Bau des Tages von `AdvanceMainPathFinance` an
  `MainPathDailyRunAndBuild` durch. Das ist nötig, weil Wanderung und `HealUnmanaged` zwischen beiden liegen.
- Zähler ≡ 0 ist immer ein Takttag (`IsDailyRunDay`: ≡ 0 oder 4). Der Ersatzzweig ohne Tageslauf greift deshalb nur mit
  Ladeversatz.

**Mehrsaisonprobe:** `TagesabschlussTests.FourSeasonChanges_DayClosePerManager` und
`TagesabschlussStadionTests.FourSeasonChanges_StadiumDamage` – je passiv/aktiv, zwei Manager, 4 Wechsel; in allen Läufen grün.

**Erwartet offen, bestätigt:**
- `markerliste.py`: „2 unklassifiziert (erste: A1010, A1011)“.
- Börsentag einmal je Tag (#188, R1034-02).

**`tools/pruefen.sh`** (alle drei Läufe):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 4018 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2850 | 0 | 652 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2851 | 1167 | 0 |

Commit `f397c089f560793f4a96802c1a4320c36d91487e`, SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`:**
- A1010: Mindestbreite 0x374 der Zahl in der Platzmeldung.
- A1011: Plätze bei 0 gehalten statt 16-Bit-Überlauf.

Beide sind begründet und erst nach vielen Schäden bzw. nur in der Darstellung sichtbar.

**Annahmen (nicht geprüft):**
- `CostLeague` entspricht DGROUP 0x9746. Grundlage: Export nach Feld 0xEB und Pflege beim Saisonwechsel.
- Die Heilung 0x07C12 würfelt nicht. Davon hängt ab, ob die getrennte Heilung in der WM-Schleife für die Zufallsfolge
  gleichwertig ist.
