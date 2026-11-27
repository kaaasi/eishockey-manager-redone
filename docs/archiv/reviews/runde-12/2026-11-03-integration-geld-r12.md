# Review: PR #505 – Integration Runde 12, Spur `geld`

- PR: #505 · Base `af907c5af899c58424de2d843b60952a8c76b460` (`main`) · Head `1cb0f3a8a2be1410fa58508167ebd3e195e34b64`
- Umfang: Teile 1–3 (#484, #487, #490) und Leitstellen-Commit `1cb0f3a` (14 Dateien, +227/−59)
- Reviewer: Claude (geplanter Lauf, frische Sitzung ohne Implementierungskontext)
- Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; gebaut und getestet (nicht nur Codelesen).
  Gegenproben in eigenen Worktrees von Base und Head, nach jedem Lauf zurückgesetzt.

## 1. Merge-Empfehlung

**Ja für genau diesen Head.** Alle drei Läufe von `tools/pruefen.sh` sind grün, mit denselben Zahlen wie im PR. Die Gegenbeispiele
R11G-01/-02/-03 sind am Base grün und am Head rot. Die Zuschauerstärke stimmt mit 0x4D316 überein, der rechte Stadionblock (G7) mit dem DOSBox-Bild.
Die Änderung an `StatisticsStrength.cs` ist begründet: Teil 2 ist genau die dort angekündigte Umstellung der Aufrufer. Sie lässt sich mit dem Stapel #482 → #503
ohne Konflikt in dieser Datei zusammenführen. Konflikte gibt es nur in den Abdruck-Konstanten (siehe R12G-03).
Offen sind drei P3-Punkte, keiner blockiert.

## 2. Befunde

### R12G-01 (P3) – Doku behauptet für die Monatseinnahme dieselben Felder, für Managervereine stimmt das nicht

- [spielberechnung.md:752](https://github.com/kaaasi/eishockey-manager-redone/blob/1cb0f3a8a2be1410fa58508167ebd3e195e34b64/docs/spielberechnung.md#L752)
  schreibt: „`StrengthTable.TeamValues` liest jetzt dieselben Vereinsfelder … die Monatseinnahme (`FinanceMonthly.TeamValueSum`, 0x3E657) las sie schon“.
- [FinanceMonthly.cs:57](https://github.com/kaaasi/eishockey-manager-redone/blob/1cb0f3a8a2be1410fa58508167ebd3e195e34b64/src/EishockeyManager.Core/FinanceMonthly.cs#L57)
  rechnet aber für **jeden** Verein `4 · club.Strength + Abweichungen` aus `TeamValuesOf`. Die Zuschauerstärke nimmt dagegen für Managervereine
  die Aufstellung (0x952C, `LineupTeamValues`). Im Original überschreibt 0x952C die Felder 0x18–0x1D eines Managervereins täglich. 0x3E657 sieht dann
  dieselben Aufstellungswerte wie 0x4D316.
- Gegenbeispiel: Managerverein mit einer Aufstellung, deren Kondition und Technik weit von der Datenbankstärke abweichen. `SpectatorRules.Strength`
  folgt der Aufstellung, `TeamValueSum` bleibt gleich. Beide lesen im Original dieselben Bytes.
- Altbestand, nicht durch diesen PR verursacht. Vorschlag (Folgespur geld): `TeamValueSum` für Managervereine an `StrengthTable.TeamValues`
  anschließen oder als `[APPROX]` markieren und den Satz in `spielberechnung.md` einschränken.

### R12G-02 (P3) – Zweig „Managerverein → Aufstellung“ der Zuschauerstärke nur über Abdrücke abgesichert

- `StadiumStrengthTests` prüft nur Computervereine. Die Gegenprobe „Managerverein liest Vereinsfelder statt Aufstellung“ wird zwar rot, aber nur in
  6 Abdrücken bzw. Strukturtests (`RngTwinProbeTests` aktiv ×3, `GameModeTests.Mode4`, `SpielberechnungAnschlussTests` aktiv,
  `WettbewerbeSpiel4Tests.ManagerClub_ShowsLineupValues`). Aus ihnen ist die Ursache nicht ablesbar; das ist dasselbe Muster wie bei R11G-03.
- Ebenso belegt die Mehrsaisonprobe die Geldwirkung von Teil 2 nur über geänderte Abdrücke (passiv und aktiv, 4 bzw. 5 Saisonwechsel, Zufallszustand R/C
  in `Mode4Reference` gleich). Eine Aussage zur Richtung, etwa „bessere Aufstellung → mehr Zuschauer“, fehlt. Die Abdrücke `RngTwinProbeTests` mit
  synthetischer Datenbank und **passivem** Manager (Seed 3/11) sind gegen die Zuschauerstärke unempfindlich: Auch „Form nicht mitzählen“ lässt sie grün.
- Vorschlag: In `StadiumStrengthTests` einen Fall für einen Managerverein ergänzen: `Strength` gleich `(Σ LineupTeamValues) / 6`, und er ändert sich mit
  der Aufstellung.

### R12G-03 (P3, Hinweis für die Leitstelle) – Abdruck-Konflikte mit dem Stapel `wettbewerbe-spiel`

- Probemerge von `1cb0f3a` in `paket/wettbewerbe-spiel-4` (`6a831a1`, enthält #482 ff.): `StatisticsStrength.cs`, `spielberechnung.md`,
  `WettbewerbeSpiel4Tests.cs` und `arbeitspakete.md` laufen ohne Konflikt zusammen. Konflikte gibt es nur in den Abdruck-Konstanten von
  `GameModeTests.Mode4Reference`, `RngTwinProbeTests` (6 Zeilen) und `SpielberechnungAnschlussTests` (2 Zeilen).
  Der Stapel ruft weder `PlayerValues` noch `StrengthTable.TeamValues` auf.
- Wer als Zweiter integriert, muss diese Abdrücke neu erheben. Das erfordert eine Begründung im PR und die Mehrsaisonprobe, nicht nur das Übernehmen einer Seite.

## 3. Ausgeführte Prüfungen

### Gegenbeispiele (Mutation → gezielter Testlauf, `EM_REQUIRE_GAMEDATA=1`, Base mit Base-Tests, Head mit Head-Tests)

| ID | Mutation | Base | Head |
|---|---|---|---|
| R11G-01 | `FinanceStadium.cs:295` `--_weeksLeft[p] > 0` → `> 1` (`~Stadium`) | grün (39/39) | **rot** (3, `ConstructionEnds_InTheWeekWhenWeeksLeftReachZero` alle Fälle) |
| R11G-02 | Bauzeit Anzeigetafel (12,17) → (12,18) | grün (5/5) | **rot** (`BuildTime_FromTable` Anzeigetafel) |
| R11G-02 | Bowlingbahn (10,15) → (10,16), Sitzkomfort (6,9) → (6,10) | – | **rot** je 1 |
| R11G-03 | `FinanceYouth.cs:208` `R(0, 8)` → `R(0, 7)` (`~Jugend\|~Youth`) | grün (41/41) | **rot** (`FourSeasonChanges_StrengthFollowsFunding` passiv und aktiv) |
| Teil 2 | neue `StadiumStrengthTests` am Base-Code | **rot** (2/2) | grün |
| Teil 2 | Zuschauerstärke ohne die beiden Formfelder | – | **rot** (8, inkl. beider `StadiumStrengthTests`) |
| Teil 2 | Managerverein über Vereinsfelder statt Aufstellung | – | **rot** (6, nur Abdrücke, R12G-02) |

- Bauzeittabelle gegen die Datei nachgelesen (DGROUP 0x16DD0 + 0xEC, Paare lo/hi): `(20,30) (12,17) (16,25) (6,9) (5,8) (40,60) (17,24) (14,18) (10,15)
  (12,17) (10,14)`. Das entspricht `FinanceStadium.cs:166` und allen 11 `InlineData`.
- **0x4D316** (`ndisasm -b16 -e 0x4D316 em.exe`): Für k = 0, 1 werden die Bytes [Verein·0x3D + 0x18 + k] + [0x1A + k] + [0x1C + k] (vorzeichenlos) in `si`
  summiert, danach folgt `idiv 6` (0x4D37A). Das stimmt mit `SpectatorRules.Strength` überein: ganzzahlige `PartValues`, Summe der sechs Werte / 6. Kein Zufallszug.
- **G7** gegen `original/spielstaende/bilder/file15_2_stadion.png` (Referenz-Repo; 642 × 507, 2× verdoppelt, Spielfläche ab (1 | 26), jedes zweite
  Pixel): Spaltenläufe gegen den Hintergrund mit denselben Fenstern wie im Test. Sitzplätze: Beschriftung 213, Doppelpunkt 267, Wert 271. Eintritt:
  Beschriftung 213, Wert 272, oberste Zeile y = 190. Konto: Beschriftung 213, Betragsende 310. Das stimmt mit den Konstanten in
  `StadiumScreen.cs` und mit `RightBlockColumnsMatchDosBoxImage` überein (der Test ist am Head grün).
- `AnswerConstruction`-Doku (R11G-04): Jetzt steht dort, dass keine Kontoprüfung stattfindet. Sie ist mit 0x3E389/0x3E107 belegt, das passt zum Code.

### `tools/pruefen.sh` (alle drei Läufe) am Head

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1994 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1681 | 0 | 281 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1682 | 312 | 0 |

Commit `1cb0f3a8a2be1410fa58508167ebd3e195e34b64`, SDK 10.0.402, Ergebnis: bestanden. CI am Head: `build` success.

### Akzeptierte `[APPROX]`

- ▲▼ im Stadion als gezeichnete Dreiecke (Grafik nicht gefunden). Der Hinweistext ab (213 | 201) ist unverändert. Beides steht so in `finanzen-rekonstruktion.md`.
- Managervereine aus der aktuellen Aufstellung statt dem Stand des letzten Tageslaufs (unverändert aus Runde 11, `spielberechnung.md`).

### Annahmen (nicht geprüft)

- Die Mehrsaisonprobe der Geldwirkung stützt sich nur auf neu erhobene Abdrücke. Ob die neuen Kontostände in `Mode4Reference` sachlich stimmen, habe ich nicht
  nachgerechnet. Geprüft ist nur, dass Meister, Pokalsieger und der Zufallszustand R/C gleich bleiben.
- G8 (Finanzübersicht) habe ich nicht nachgemessen; laut PR gibt es dort keine Abweichung und keine Code-Änderung.
