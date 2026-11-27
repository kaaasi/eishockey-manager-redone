# Review PR #1005 (Spur statistik-anzeige: Saisonende, Historie, Stärken, Spielerfenster)

Reviewer: Claude Code (Opus 5.5), eigene Sitzung ohne Implementierungskontext, geplanter Lauf. Geprüft: Head
`e4158709adb49c1fae36c3f0fa5889fd57e84d3d` gegen Base `0c1811f` (= `main`). Checkout `git rev-parse HEAD` = Head,
`git status --porcelain` leer. Erstreview: alle drei Läufe von `tools/pruefen.sh`.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Kein P0–P2. Der Verzicht auf die geplante Migration ist belegt und richtig:

- B29: Die Bilanzzeichen leiten sich aus `Results` ab. An **81 Original-HOK-Ständen** stimmen sie mit den Zeichen des
  Originals überein (Prüfung 3): 2916 von 2916 Ligaplätzen, auch rechtsbündig bei weniger als acht Spielen.
- B33/A957: Das Original führt keinen Vereinswert der Gegentore. Es gibt also kein zweites Feld. Zählstellen in
  `MatchEngine.*` braucht es nicht.

Alle Rückschalt- und Mutationsproben werden rot. Im Pixelvergleich mit den Videos sind Meisterfoto und Historie deutlich
näher am Original als am Base-Stand; Spielerinfo-Balken (Zeilenprofil) und Stärken liegen positionsgleich.

## 2. Befunde

**SA-01 (P3) – Testlücke: Leerung beim Saisonwechsel und Abgleich mit dem Original sind nicht als Test abgesichert.**
[`GameState.TeamForm.cs:31`](https://github.com/kaaasi/eishockey-manager-redone/blob/e4158709adb49c1fae36c3f0fa5889fd57e84d3d/src/EishockeyManager.Core/GameState.TeamForm.cs#L31)
hängt nur an `Results` und an dessen Leerung in
[`SeasonFlow.cs:529`](https://github.com/kaaasi/eishockey-manager-redone/blob/e4158709adb49c1fae36c3f0fa5889fd57e84d3d/src/EishockeyManager.Core/SeasonFlow.cs#L529).
`StrengthScreen_DrawsFormMarksAndFace` spielt nur drei Spieltage, und die PR-Tabelle nennt keine Mehrsaisonprobe.

- **Probe (nicht committet):** `ReviewProbeTeamFormTests` mit den Startwerten 11 und 29, synthetisch, zwei Manager. Ablauf:
  fünf Saisons, also 4 Wechsel. In jeder Saison zehn Spieltage, Sichern und Laden, dann der Rest der Saison mit Play-offs,
  wieder Sichern und Laden, dann der Saisonwechsel.
- **Ergebnis:** grün. Nach jedem Wechsel sind alle Zeichen leer, nach dem Laden gleich. Play-off-Spiele bleiben draußen (je
  Verein so viele Spiele in `Results` wie Spieltage im Plan). Alle drei Gesichter kommen vor.
- **Probe gegen das Original (nicht committet):** `ReviewProbeHokFormTests` liest DGROUP 0x825E + 0x50 · Satz aus Block 0x8257
  jedes HOK in `~/referenz/original/spielstaende` und vergleicht mit `TeamForm(clubOf[slot])`.
  Ergebnis `files 81, slots ok 2916, bad 0` (z. B. FILE03 Satz 0: Original `-----nUn`, Remake `00000121`).
- **Vorschlag, nicht blockierend:** Beide Proben als Tests übernehmen, im Fixlauf oder in `pruefung-karte`.

**Erwartet offen – bestätigt:**

- **A958 [APPROX] unerreichbar.** `grep` findet als Schreiber von `SuspendedGames` nur Zuweisungen von 0
  (`OriginalImport.cs:611`, `Player.Factory.cs:126/143`). Den Zweig mit „Noch n Minuten“ erreicht das Spiel nie.
- **Neue [APPROX] in `HistoryScreen`.** Europacup-Stufe: Gruppe 1, Endrunde 2 wie `SeasonCodes.Of`. DEB 0x1E zeichnet keinen
  Balken. Beides ist begründet und markiert.
- **Bilanzzeichen der Vereine aus `Results` abgeleitet.** Belegt durch die HOK-Probe oben. Die Menge stimmt:
  - `Results.Add` gibt es nur in `GameState.cs:110` (Liga, `MatchCompetition.League`).
  - Play-off und Relegation (Code 1, `Playoffs.cs:298`) gehen in eigene Listen.
- **R25-H1 (Highscore) besteht weiter.**
  [`Highscore.cs:160`](https://github.com/kaaasi/eishockey-manager-redone/blob/e4158709adb49c1fae36c3f0fa5889fd57e84d3d/src/EishockeyManager.Core/Highscore.cs#L160)
  schreibt `Math.Clamp(h.Rank, 0, 0x7F)` ohne + 12 · Liga. Das liegt außerhalb der Spur, die Leitstelle verfolgt es als Issue.
  `HistoryScreen.Codes` ergänzt den Liga-Anteil selbst.

**Besonders geprüft:**

- **Ohne Migration.** Der Diff fügt kein Instanzfeld hinzu (`TeamForm`/`FormMood` sind Methoden, UI-Werte `static`/`const`);
  `SchemaVersion` bleibt 63, Schema 64 bleibt frei. Spielstandtests im Lauf mit Originaldaten grün; 4 Wechsel: SA-01.
- **Zählstellen der Gegentore.**
  - Torhüter-Gegentore zählen weiter `MatchEngine.Live.cs:530` (nur Liga), `MatchEngine.Statistics.cs:131/133` und
    `MatchEngine.National.cs:230`, unverändert.
  - Einen Vereinswert braucht es nicht. A624 (`OriginalImport.cs:583–588`) hält fest: In FILE01–FILE49A sind +0x22–0x27 bei
    jedem Torhüter 0. Das stützt A957.
  - Die Zelle zeigt „17( -)“ wie das Original. Der Test ist kein Regressionstest, denn er ist auch am Base-Stand grün; das ist
    richtig, weil es keine Abweichung gibt.

## 3. Ausgeführte Prüfungen

**Umgebung.** `fetch-reference.sh original oberflaeche folge02 folge08 folge23 folge25`, `setup-workspace.sh`, SDK 10.0.402.

**`tools/pruefen.sh`** (alle drei Läufe, Head `e415870`, Ergebnis: bestanden):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3871 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2729 | 0 | 630 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2730 | 1141 | 0 |

**Vorgaben aus `re-abweichungen.md` gegen die Umsetzung.** Die Skripte liefen mit `~/referenz/original/gamedata/em.exe`:

- `b28_historie_balken.py` und `b29_staerken.py`: Rückgabe 0, Ausgabe wortgleich mit dem Bericht (`diff` leer).
- Die Formel in `HistoryScreen.Bars` ergibt für alle 16 Liga-Bytes der B28-Tabelle die Füllungen des Skripts. Das ist von
  Hand nachgerechnet: v = 1, 2, 6, 12, 13, 18, 24, 25, 30, 36, 0x33–0x35, 0x80/0x81. DEB und Europacup sind über die
  Stichproben in `CupBars_LikeOriginal` belegt (DEB 1, 2 und Sieger; Europacup 9, 10 und Sieger 0x8D).
- Die B29-Zeichen und das Gesicht (8 px Raster ab x 80, Gesicht bei 240, Regel 2 + S − N) stimmen mit `FormMood` und
  `StrengthScreen` überein.

**Rückschaltprobe am Base-Stand** (`0c1811f`, alter Code; neuer `SeasonEndStatusUiTests` und `HistoryScreen_DrawsChampionBarInColor30`
isoliert): `EventIcon…` rot („Expected X = 120, Actual X = 72“), Historie rot („Expected 30, Actual 7“) – wie im PR.

**Gegenproben je neuem Test** (Mutation in temporärer Kopie, gefilterter `dotnet test`): **alle rot**.

| Mutation | Test | Ergebnis |
|---|---|---|
| M1 Meister → `Cell(3, 0)` | `SeasonEndStatusUiTests.EventIcon…` | rot |
| M2 `TitleColor` 30 → 7 | `HistoryScreen_DrawsChampionBarInColor30` | rot |
| M3 mittlerer Liga-Block entfällt | `LeagueBars_StackedBlocks_LikeOriginal` | rot |
| M4 Gesicht immer gelb | `StrengthScreen_DrawsFormMarksAndFace` | rot |
| M5 Bilanzzeichen linksbündig | `StrengthScreen_DrawsFormMarksAndFace` | rot |
| M6 Farbband 6 statt 5 Zeilen | `Bar_LikeFunction04C75` | rot |
| M7 Pokalteil nicht kopiert | `TrophyScreen_ComposesPartOverPhoto` (3 Fälle) | rot |
| M8 DEB 3 · (Byte + 1) | `CupBars_LikeOriginal` | rot |

**Pixelvergleich.** Gerendert über eigene Abläufe (nicht committet) bzw. `spielerinfo`, verglichen mit dem Videobild. Gemessen
ist die mittlere absolute RGB-Abweichung bei der besten Verschiebung um ±1:

| Bild | Bereich | Base | Head |
|---|---|---:|---:|
| `folge02_00928` (Deutscher Meister, DEG) | Pokal / Titel / Teilebereich / Verein | 28,6 / 45,8 / 21,0 / 43,7 | 9,8 / 14,9 / 6,9 / 19,1 |
| `folge02_00932` (DEB Pokalsieger) | Pokal / Titel / Teilebereich / Verein | 17,4 / 43,3 / 24,7 / 43,9 | 8,2 / 13,8 / 7,8 / 19,4 |
| `folge23_02229` (Früher, Verlauf aus dem Video nachgestellt) | Liga-Balken / DEB-Balken | 13,1 / 7,4 | 7,0 / 7,4 |
| `oberflaeche_1115` (Spielerinfo) | Balken und Beschriftung | 12,4 | 13,2 |

- **Spielerinfo.** Der Gesamtwert ist durch andere Zeigerstände verfälscht. Ein Spaltenprofil (x 180, y 170–189) zeigt am Head
  wie im Video 1 Zeile Rand, 2 Zeilen schwarz, 5 Zeilen Farbband, 2 Zeilen schwarz und 1 Zeile Rand. Am Base-Stand sind es
  6 Zeilen Band und zwei Randfarben.
- **Stärken nach 8 Spieltagen gegen `folge08_01735`.** Die Zeichen liegen bei y 36 + 15 n, x 80 + 8 i, das Gesicht bei x 240.
  Das Video zeigt in Zeile 11 Zeichen ab x 79 bis 135 im 8-px-Raster.

**Akzeptierte [APPROX].** A958, Europacup-Stufen 1/2, DEB 0x1E ohne Balken.

**Annahmen, nicht geprüft.** Pokalteil des Weltmeisters (kein Video; geprüft nur Meister und DEB). Farbe 1 der Balkenfläche
ist gegen das unscharfe Video nur plausibel, nicht exakt.
