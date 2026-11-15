# Review PR #880 – Spur `pruefung-karte` (Runde 20, Welle 1)

Erstreview, unabhängige Sitzung (keine Mitarbeit am PR), unbeaufsichtigter Lauf. Base `e5262e89b6834f1c737cd410ef551fd31a555cdb`
(= `main`), Head `5e5c52997f7faa6f23b2d5b410398af1122d3520` (Checkout nachgewiesen: `git rev-parse HEAD` = Head,
`git status --porcelain` leer; `git merge-tree origin/main` konfliktfrei). CI auf dem Head: `build` grün.

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Alle drei Läufe von `tools/pruefen.sh` grün. Die #872-Lesart von 0x84D7 stimmt mit dem
Disassembly überein, die Spielstandbytes 0x2E/0x30 aus FILE50/61/63 bestätigen die Testwerte unabhängig, Schema 51 samt
Migration und Laden/Speichern ist abgesichert. Die Rückschaltprobe der zwölf Fremdtests liefert exakt die alten Abdrücke; die
Saat- und Grenzänderungen sind durch breitere Saatproben gedeckt (kein systematischer Zuwachs). Fünf P3, keiner blockiert.

## 2. Befunde

**P3-1 Drift-Wächter ZIMMERMANN: angegebene Mutation nicht reproduzierbar, Bereich weit.**
[AbgleichOriginalPaareTests.cs:232](https://github.com/kaaasi/eishockey-manager-redone/blob/5e5c52997f7faa6f23b2d5b410398af1122d3520/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L232)
Die PR-Beschreibung sagt „Mutation Bereich 90–100 ergibt ZIMMERMANN 1 von 6, also rot“. Probe: InlineData auf `90, 100` →
`Strain_DeviatesAtMostSeeds_DriftGuard` **grün** (4/4). Ausgabe der Abweichungen je Saat: 1993 → 97, 4711 → 98, 1 → 96, 2 → 96,
3 → 99 (77 keine) – alle in 90–100. Der Wächter selbst wirkt (Untergrenze 100 → „nur bei 0 von 6 Saaten“, rot), aber 90–127
lässt eine Drift bis zum Sägezahn-Maximum unbemerkt. Vorschlag: Bereich auf etwa 90–110 und PR-Text berichtigen.

**P3-2 Export 0x2E/0x30 ohne Test.**
[OriginalExport.League.cs:67](https://github.com/kaaasi/eishockey-manager-redone/blob/5e5c52997f7faa6f23b2d5b410398af1122d3520/src/EishockeyManager.Core/OriginalExport.League.cs#L67)
Mutation `r[0x30] = 0;` → alle 97 Tests mit Filter `Export|Hok|ReiheBeiSpielbeginn` grün. Import (Mutation: FILE50/FILE63 rot)
und `.ems` (Schema-Test) sind abgedeckt, der neue Exportweg nicht. Vorschlag: Rundlauf Export → Import mit gesetzten Reihen.

**P3-3 Fundstellen-Text unvollständig.**
[Squad.Tactics.Lines.cs:9](https://github.com/kaaasi/eishockey-manager-redone/blob/5e5c52997f7faa6f23b2d5b410398af1122d3520/src/EishockeyManager.Core/Squad.Tactics.Lines.cs#L9),
[MatchEngine.Live.cs:310](https://github.com/kaaasi/eishockey-manager-redone/blob/5e5c52997f7faa6f23b2d5b410398af1122d3520/src/EishockeyManager.Core/MatchEngine.Live.cs#L310)
(a) „Geschrieben werden sie nur …“ nennt 0x91CF/0x91E3, 0x38A2A, 0x857D; Suche aller Schreibzugriffe auf `[es:bx+0x2e]`/`+0x30`
(`ndisasm -b16 em.exe | grep`) findet zusätzlich **Datei 0x38FAA** (`mov [es:bx+0x2e+2·Teil],al`, Eingriffsmenü 0x38D96; im
Remake `MatchEngine.Intervention.cs:75`, landet über `KeepMatchFields` richtig im Feld – nur Doku). (b) Das `[APPROX]` zu Bit 3
sagt „nur Computervereine“: Die Abfrage 0x855D–0x857D ist nicht an 0x16 gebunden (nur das `and 0xf8` bei 0x8521 ist es), und
Bit 3 setzt auch die Neuzählung der Managervereine (0x38A36, `RecountSkaters`). Gilt also für jeden Verein mit gesetztem Bit 3.

**P3-4 Bestenliste am Spielende: Zeilenfarbe weicht von `file54_4` ab (vorbestehend, nicht durch den PR).**
[HighscoreScreen.cs:181](https://github.com/kaaasi/eishockey-manager-redone/blob/5e5c52997f7faa6f23b2d5b410398af1122d3520/src/EishockeyManager.UI/Screens/Statistics/HighscoreScreen.cs#L181)
Pixelvergleich (Bildschirm aus `GameEnd_DrawsRowOne_WithoutTitleColumn_LikeFile54`, temporär als RGB ausgegeben, gegen
`spielstaende/bilder/file54_4_highscore_spielende.png`, 2:1 ab (1|26)): Form und Lage von „Spieler – DEUTSCHLAND – 5“
deckungsgleich, aber die Schrift ist im Remake Farbe 7 (81,162,195; „Eintrag dieser Sitzung“), im Original Farbe 10
(130,211,227). Ebenso in `highscore-weltkugel_2`/`_4` (12 996 Pixel Farbe 10). Die Kennung DGROUP 0x2C4 ist bereits `[APPROX]`;
die neuen Tests vergleichen nur Remake gegen Remake und hätten das nicht gefunden. Außerdem enthält `file54_4` einen zweiten
Eintrag „fsdfsd – DEUTSCHLAND – 1“ (aus einer früheren Sitzung); „einziger Eintrag“ in Test-Doku/PR ist daher ungenau. Vorschlag:
Issue zur Kennung/Farbe; kein Fixlauf für diesen PR nötig.

**P3-5 Angaben zu Fremdtests teils unvollständig (kein Codefehler).** Pool-Probe FILE44 (max. |Δ| Kondition/Technik über vier
Wechsel, Saaten 1–10, 4711, 1993): passiv Head Mittel 1,92 gegen alter Code 1,87; über 5 auch Saat 8 (Head 6,39, **alter Code
5,71**) – der Ausreißer ist also nicht auf 4711 beschränkt und nicht neu. Saat 4 liegt am Head bei 3,75 (PR nennt nur 1–3, 5, 6).
Aktiv 3,57 gegen 2,98 (innerhalb der Streuung, s ≈ 1).

### Gegenbeispiele

| ID | Ablauf | Ergebnis |
|---|---|---|
| #872 | Spielstand entschlüsselt (`emtool hok dump … far:9657`, Satz 0x3D): FILE02 alle 0; FILE50 Köln 0x2E/0x30 = 0/2, Ratingen 0/0; FILE61 Köln 1/1, Kaufbeuren 0/0; FILE63 Köln 0/0, Kaufbeuren 1/1. Disassembly 0x84D7: Schleife Verein × Teil schreibt nur 0x2F+2·Teil = 0 (0x853F), `and 0x2C,0xF8` nur bei 0x16 = 0 (0x8521), bei Teil 0 und Bit 3 0x2E = 2 (0x857D); 0x8589 ff. nur Energie 0x1E. Mutation `LiveTeamOf` ohne die zwei Zuweisungen → `NextMatch_StartsWithLinesOfSave` FILE50 (Köln „(0, 2) erwartet, (0, 0)“), FILE61, FILE63 und `ManagerMatch_KeepsEndLines` rot (4/6), Head grün. | behoben |
| #874 | Mutation Punkte = 1 (`Highscore.cs:277`) → `GameEnd_RecordsAndShowsManager_LikeFile54` rot; `_showTitles = true` → `GameEnd_DrawsRowOne_…` rot. | abgesichert (siehe P3-4) |
| #871 P3-2 | `Strain_DeviatesAtMostSeeds_DriftGuard` ZIMMERMANN grün (5/6, nicht 77); Untergrenze 100 → rot. | abgesichert (siehe P3-1) |

### Erwartet offen

- **A093:** offen, bestätigt (`abgleich-dosbox.md` §4.5 „weiter nicht belegbar“).
- **`[APPROX]` 0x2C Bit 3 zwischen den Spielen:** vorhanden (`MatchEngine.Live.cs:309–311`, Migration), Wortlaut siehe P3-3b.

### Besonders prüfen

1. **0x84D7, 0x2E/0x30, Schema 51:** siehe Gegenbeispiel #872. Mutationen: Migrationseintrag `[50]` entfernt →
   `Schema50_WithoutMatchLines_LoadsWithZero` rot; Import ohne `RecordMatchLines` → FILE50/FILE63 rot (FILE61 bleibt grün, die
   Reihen 1/1 entstehen dort auch beim Nachspielen des Imports); `KeepMatchFields` ohne `RecordMatchLines` → 2 rot. Speichern/Laden
   bytegleich im Mehrsaisontest.
2. **Fremde Tests (R15-E2 = C):** Rückschaltprobe (Zuweisung in `LiveTeamOf` entfernt): RngTwin (6), Spielberechnung (2),
   Mode4, Jugend (2), DebCup → alle 12 rot, **Actual exakt die alten Werte** aus Base (z. B. `AE2951A985F29E9C`,
   `0A6DB7826EA09AC8`, „1993: DEB 2, DM 6, Tab 15406“). Damit ist auch P3-3 aus #871 (Umbau `UpdateManagerTeamValues`) wirkungsgleich.
   Alte Testfassungen am Head: MENDE Saat 3 → 126; Pool 4711 passiv 45,85 außerhalb ±5; Finanzen Saat 41 −1 639 310; AddManager
   Platz 11 statt 7 (Name gleich); TeamValueDrift Managerverein 68 < 69 – je wie im PR. Breitere Proben (temporäre Tests, nicht
   committet), Head gegen alten Code: MENDE ≤ 70 bei 37/40 gegen 40/40 (Saaten 3, 25, 28 bei 125–126, also letzter Zahn vor 128);
   Finanzen passiv Oberliga Saaten 1–17, 41: Mittel des tiefsten Kontostands −155 524 gegen −154 559, unter −1,5 Mio. nur Saat 41;
   aktiv Oberliga 169 136 gegen 202 386, keine Entlassung; Pool siehe P3-5. Kein systematischer Zuwachs; Begründungen tragen.
   GoalScene und PruefungKarte17 sind mit altem Code ebenfalls grün (geprüft).
3. **Mehrsaisonprobe und Abgleich-Paare:** `MultiSeason_FourSeasonChanges_PassiveAndActive_LinesCarryOver` grün (4 Wechsel,
   passiv/aktiv); `AbgleichOriginalPaareTests` im Lauf mit Originaldaten grün; Funktionskarte am Head frisch
   (`funktionskarte.py --write` ohne Diff).

## 3. Ausgeführte Prüfungen

- Einrichtung: `fetch-reference.sh original dosbox`, `setup-workspace.sh`, `dotnet --version` = 10.0.402.
- `tools/pruefen.sh` (alle drei Läufe) am Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3373 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2428 | 0 | 574 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2429 | 944 | 0 |

- Mutationen in eigenem Worktree (`dotnet test --filter …`, je zurückgesetzt): siehe oben, zusätzlich Export (P3-2) und Bereich
  des Drift-Wächters (P3-1).
- Disassembly: `tools/reverse-engineering/disasm.py em.exe 0x84D7 0x8750`; `ndisasm -b16 em.exe` mit Suche aller Schreiber auf
  `+0x2e`/`+0x30` mit Faktor 0x3D bzw. Vereinszeiger (0x857D, 0x91E3, 0x38A2A, 0x38FAA).
- Pixelvergleich Bestenliste gegen `file54_4` (P3-4).

**Akzeptierte `[APPROX]`:** Bit 3 zwischen den Spielen (Wirkung: ein Computerverein, der mit leerem Tor endet, beginnt mit der
gespeicherten statt Abwehrreihe 2); Migration alter Stände mit 0/0; `WriteLineupTeamValues` vor dem ersten Spiel.
**Annahmen (nicht geprüft):** Platztausch beim Hinzufügen (0x36169) trägt 0x2E/0x30 mit dem Satz wie das Remake mit `Tactics`;
kein Rücksetzen der Felder beim Saisonwechsel außer den gefundenen Schreibern (Suche statisch, indirekte Kopien ganzer Sätze nicht
ausgeschlossen).
