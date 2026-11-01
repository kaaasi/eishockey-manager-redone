# Review #360 – Play-offs/Aufstiegsrunde ohne Manager über den schnellen Weg (#351)

- **PR:** #360 (`fix/playoffs-schneller-weg`)
- **Base:** `0a435823733d49c5bca2363b4959357826d69850` (Head von #335, auf `main` enthalten) · **Head:** `b80c1a82dd6d65da6a10be9ea8742c6dc143768e`
- **Geprüft:** nur Delta `0a43582..b80c1a8` (11 Dateien, +102/−31)
- **Reviewer:** Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Mitarbeit an #360
- **Art:** Klon am Head (`git rev-parse HEAD` = Head, `git status --porcelain` leer), Build und `tools/pruefen.sh` (alle drei
  Läufe) mit Originaldaten; Mutationen in einem getrennten Worktree.

## 1. Merge-Empfehlung

**Ja, für genau diesen Head.** Die Umsetzung entspricht 0x37058–0x37085: Code 1 prüft die drei Felder „Ergebnisse“
0x21E/0x221/0x224 mit ODER, und Code 1 deckt Play-offs und Aufstiegsrunde ab (einziger Schreiber 0x31AB7). Der Regressionstest
wird rot, wenn die Umsetzung zurückgenommen wird. Die drei gelockerten Proben verdecken keinen Fehler: Jede bleibt gegen
ihre eigene Mutation rot. Die Lockerungen gleichen nur knappe Schwellen aus, an denen der neue Zufallsverlauf gerade vorbeiläuft.
Die Referenzwerte hängen nachweislich nur an dieser Änderung. Neu sind drei P3 (PSW-01 bis -03), alle ohne Wirkung auf das
Verhalten.

Erwartet offen und bestätigt: #349 (Entscheidung je Paarung statt je Spieltag, Merker 0x36FEA). Das bleibt `[APPROX]` und ist
im Code und in `docs/spielberechnung.md` mit der Entscheidung des Nutzers vermerkt.

## 2. Befunde

### Gegenbeispiele erneut ausgeführt

`PlayWithEvents(game, A, B, …)` auf `TestGames.Create(3)`, zwei Vereine ohne Manager mit Index < 12, `new Random(7)`. Gemessen
wurden der Endstand, die Anzahl der Ereignisse und `random.Next()` danach. Ausgeführt als temporärer Diagnosetest, nicht committet.

| Fall | Matrix „Ergebnisse“ aus | an (1. Liga / 2. Liga / Oberliga, je einzeln) | nur „Drittelpause“ 1. Liga an | DEB-Pokal, Schalter aus |
|---|---|---|---|---|
| `Playoff, knockOut: true` | 0:1, 0 Ereignisse, 1495332452 | 4:3, 19 Ereignisse, 227950298 | 0:1, 0, 1495332452 | 1495332452 |
| `Relegation, false` | 0:1, 0 Ereignisse, 1495332452 | 4:3, 19 Ereignisse, 227950298 | 0:1, 0, 1495332452 | 1495332452 |

Der Zufallsverbrauch ist verschieden. „Aus“ hat dieselbe Zugfolge wie der schnelle Weg des DEB-Pokals mit ausgeschaltetem
Schalter, also den schnellen Weg (0x3731F prüft den Code nicht). Jedes der drei Felder schaltet allein um, andere Zeilen der
Matrix tun das nicht. **Bestätigt.**

### Besonders geprüft

**(1) 0x37058–0x37085 gegen die Umsetzung.** Mit `ndisasm -b16 -e 0x36FE0 -o 0x36FE0 em.exe` nachgelesen:

```
3702C jnz 7088            ; [bp-5] ≠ 0 (Managerverein spielt) → Minutenschleife
3702E cmp [D0C2],8 / 3703A [229]≠0 → 7088      ; WM-Gruppe
3703C cmp [D0C2],7 → 7088                      ; Liga immer
37043 3/5 → [228]≠0 → 7088                     ; Europapokal
37058 cmp [D0C2],1 ; jnz 7074
3705F cmp [21E],0 ; jnz 7088
37066 cmp [221],0 ; jnz 7088
3706D cmp [224],0 ; jnz 7088                   ; Code 1: ODER über die drei Felder
37074 cmp [D0C2],6 ; jz 707E ; jmp 731F        ; sonst schneller Weg
3707E cmp [227],0 ; jnz 7088 ; jmp 731F        ; DEB-Pokal
```

- Felder: 0x21E/0x221/0x224 = Matrixindex 1/4/7 = `ReportIndex(Results, league)` für alle drei Ligen (DGROUP 0x21D + Liga · 3 + 1).
  `Enum.GetValues<League>().Any(…)` bildet das ODER genau ab, unabhängig davon, welche Liga spielt (wie im Original).
- Code 1 für beide Wettbewerbe: Der einzige Schreiber von `[0xD0C2] = 1` ist 0x31AB7 nach der Schleife über 2 × 4 Plätze aus
  DGROUP 0xCF20 (Byte-Suche nach `C6 06 C2 D0`: 13, 8, 10, 3, **1**, 6, 7, 0, 0 an 0x30390, 0x30F53, 0x31017, 0x31490, 0x31AB7,
  0x35E7C, 0x35EC4, 0x44E32, 0x55003; dazu nur `mov [D0C2],al` 0x314A1 für Code 3 → 5). Das stimmt mit
  `docs/finanzen-rekonstruktion.md` Z. 328 überein. `Playoff` und `Relegation` gemeinsam auf Code 1 abzubilden ist belegt.
- Die WM-Endrunde (Code 10) behält den Vorrang vor dem Schalter (`competition == WorldCup && knockOut`). Play-offs sind davon
  nicht betroffen.
- Mutationen: Umsetzung zurück (Playoff/Relegation nicht im schnellen Zweig) → 9 rote Tests (Regressionstest beide Fälle,
  Mode4, RngTwinProbe). „Drittelpause“ statt „Ergebnisse“ → Regressionstest rot. Nur das Oberliga-Feld statt aller drei → Test
  **grün**, siehe PSW-01.

**(2) Die drei gelockerten Proben.** Je eine Mutation des Fehlers, den die Probe fangen soll, und eine Diagnose der Ursache:

- `Negotiate_AfterSeason_WritesBackOnEveryLevel` (R8W3I-12): Die Forderung nach dem Saisonende für den Prüfspieler (Level 5)
  liegt bei 23.041 DM (Base 21.993 DM), bei Level 3 bei 19.768 DM (Base 17.281 DM). Sie wurde per Reflexion auf
  `ComputedDemand` gemessen. Das alte Gebot 23.000 lag also 41 DM unter der Forderung, und die Annahme fiel an den Zufallszug.
  Die alte Probe war messerscharf. Mit 99.900 nimmt `Decide` über `years == current && salary >= currentSalary` an, und das gilt
  nur, wenn zurückgeschrieben wurde. Mutation „kein Rückschreiben“ → beide Level rot (`TooShort`). Mutation „Rückschreiben nur
  bis Level 4“ → genau Level 5 rot. **Die Lockerung verdeckt nichts.** Der Kommentar nennt als Grund „über 120 % jeder
  Forderung“. Entscheidend ist aber „Gebot ≥ zurückgeschriebene Forderung“ (PSW-03).
- `FinanceCalibrationTests` (Zinsspanne): Ohne die Tagesstände der letzten 31 Tage scheitert am Head genau
  `FourSeasonChanges_AccountAndLedgerStayWithinOriginalEnvelope(Bundesliga, passiv)` mit Zinsen −14.799 bei erwarteter Spanne
  (0 – 0). Mechanismus im Code: `AccrueDailyInterest` sammelt täglich 10 % eines negativen Kontos / (Monatstage + 1),
  `BookAccruedInterest` bucht erst am Monatsletzten (0x3F02C). Liegt der Saisonwechsel vor dem Monatsletzten, stehen die Zinsen
  der letzten Tage der Vorsaison im neuen Buch. Das hat schon Teil 5 so begründet (Kommentar Z. 175 f.). Die Lockerung erweitert
  nur die Untergrenze. Die Obergrenze 0 (keine Habenzinsen ohne Guthaben) und alle übrigen Kategorien bleiben unverändert.
  **Kein verdeckter Fehler.** Annahme: Das Original leert das Zinskonto beim Saisonwechsel nicht. Das ist nicht neu belegt,
  sondern aus Teil 5 übernommen.
- `SquadLifecycleTests` (Auffüllen bis 32): Ohne den neuen Block scheitert nur `MultiSeason_SixChanges_ComputerSquadsStayPlayable(aktiv, 3)`
  an „aktiver Manager hat nur 16 Spieler“. Das ist eine Vorbedingung des Testtreibers (der Manager kauft zu wenig), kein
  Verhalten des Spiels. Dass der Pool altert und die Liste älter wird, ist belegt und gewollt (0x274A7, `zusatzfunktionen.md`
  „Verjüngung des Spielerpools“). Alle Prüfungen der Computerkader (Mindestbesetzung, 20 bis Max + 3, Alter ≤ 36,
  Altersverteilung) bleiben unverändert. **Kein verdeckter Fehler.** Die Abhängigkeit vom Zufall gehört zur Klasse #353
  (Probenrobustheit, Runde 11).

**(3) Referenzwerte.** Geändert sind Mode4 (`GameModeTests`), RngTwinProbe (4 synthetisch, 2 Originaldaten),
SpielberechnungAnschluss (2) und der DEB-Pokal-Ablauf (UI). Jede Datei trägt die Begründung „Fix #351 … anderer Zufallsverbrauch
und andere Meister“. Nimmt man die Umsetzung zurück, werden die Referenzen rot. An der Base mit den alten Werten waren sie grün.
Die neuen Werte folgen also nur aus dieser Änderung. Plausibilität: Im DEB-Pokal-Ablauf bleibt 1993 gleich bis auf die Tabelle,
weil die Liga vor den Play-offs liegt. Erst ab den Play-offs 1993 weichen Zufall und Meister ab, wie erwartet. Gegenprobe zu den
neuen Meistern („DM 21“ 1995): Mit einem Diagnoselauf über 6 Saisons und den Seeds 3, 11 und 1993 war der Deutsche Meister
in jeder Saison ein Verein der Abschlusstabelle der 1. Liga auf Platz 1–3. **Begründet.**

### PSW-01 (P3) – Regressionstest schaltet nur das Oberliga-Feld

Stelle: [`IntegrationRunde9Tests.cs` Z. 101–103](https://github.com/kaaasi/eishockey-manager-redone/blob/b80c1a82dd6d65da6a10be9ea8742c6dc143768e/tests/EishockeyManager.Core.Tests/IntegrationRunde9Tests.cs#L101-L103).
Die Mutation `ShowsReport(ReportKind.Results, League.Oberliga)` statt `Any` über alle Ligen bleibt grün. Die Felder 0x21E
und 0x221 sind damit ungesichert, obwohl die Felder der 1. Liga das häufigste Bedienziel sind. **Vorschlag:** Den Wechsel
an/aus über alle drei Ligen als `foreach` oder `[InlineData]` führen. Das ist eine Zeile, und der Diagnoselauf oben zeigt,
dass jedes Feld allein umschaltet.

### PSW-02 (P3) – Zwei Doku-Stellen beschreiben noch den alten Stand

- [`GameOptions.cs` Z. 23–25](https://github.com/kaaasi/eishockey-manager-redone/blob/b80c1a82dd6d65da6a10be9ea8742c6dc143768e/src/EishockeyManager.Core/GameOptions.cs#L23-L25):
  „Spielwirkung haben nur `ShowDebCup`, `ShowEuropeanCup` und `ShowWorldCup`“. Mit #351 haben auch die Felder „Ergebnisse“
  (Index 1/4/7) Spielwirkung: Weg und Zufallsverbrauch der Play-offs ohne Manager.
- [`MatchEngine.Live.cs` Z. 39–41](https://github.com/kaaasi/eishockey-manager-redone/blob/b80c1a82dd6d65da6a10be9ea8742c6dc143768e/src/EishockeyManager.Core/MatchEngine.Live.cs#L39-L41)
  (Summary von `PlayWithEvents`): „Pokale (DEB-Pokal, Europapokal, WM) ohne Managerverein … DGROUP 0x227–0x229“. Play-offs und
  Aufstiegsrunde sowie die Matrix fehlen. Der Kommentar im Rumpf Z. 77 ist richtig.
- Nur Form: Der Name `CupMatchUsesMinuteLoop` deckt jetzt auch Code 1 ab. Ein Umbenennen ist nicht nötig, aber die Summary
  sagt es schon („Weg eines Spiels ohne Managerverein“).

### PSW-03 (P3) – Begründung im Kommentar der Verhandlungsprobe ungenau

[`IntegrationRunde8Welle3Tests.cs` Z. 153–155](https://github.com/kaaasi/eishockey-manager-redone/blob/b80c1a82dd6d65da6a10be9ea8742c6dc143768e/tests/EishockeyManager.Core.Tests/IntegrationRunde8Welle3Tests.cs#L153-L155):
„Forderung … über 23.000 / 1,2 DM“ und „sicher über 120 % jeder Forderung (sofortige Annahme)“. Gemessen sind 23.041 DM
Forderung gegen 23.000 Gebot. Angenommen wird, weil das Gebot bei gleicher (zurückgeschriebener) Laufzeit mindestens die
Forderung erreicht (`Decide`, `years == current && salary >= currentSalary`), nicht über die 120-%-Regel von `ContractRules.Accepts`.
Das hat keine Wirkung, nur der Kommentar sollte den tatsächlichen Pfad nennen.

## 3. Ausgeführte Prüfungen

```
git ls-remote https://github.com/kaaasi/eishockey-manager-referenz HEAD   → 0bc38fba0e3e7d0c41b122e8a3087a933a88ea13 HEAD
git rev-parse HEAD → b80c1a82dd6d65da6a10be9ea8742c6dc143768e ; git status --porcelain → leer
dotnet --version → 10.0.402
tools/pruefen.sh   (alle drei Läufe, Originaldaten aus ~/referenz/original/gamedata)
ndisasm -b16 -e 0x36FE0 -o 0x36FE0 em.exe ; ndisasm … -e 0x31A70 ; -e 0x31480 ; Byte-Suche C6 06 C2 D0 / A2 C2 D0
Mutationen (Worktree, je zurückgesetzt): M1 Umsetzung zurück · M2 nur Oberliga · M3 Drittelpause · M4 kein Rückschreiben ·
M4b Rückschreiben nur bis Level 4 · Squad-Block aus · Tagesstände aus der Zinsspanne entfernt
Diagnosetests (nicht committet): Gegenbeispiel-Matrix, Forderung je Level (Head/Base), Meister ∈ 1. Liga über 6 Saisons
```

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1631 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1351 | 0 | 251 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1351 | 280 | 0 |

Commit `b80c1a82dd6d65da6a10be9ea8742c6dc143768e`, SDK 10.0.402, Ergebnis: bestanden. Die Zahlen stimmen mit der PR-Beschreibung
überein.

| Mutation | Ergebnis |
|---|---|
| M1 Play-offs/Aufstiegsrunde wieder immer Minutenschleife | 9 rot (Regressionstest ×2, Mode4, RngTwinProbe) |
| M2 nur Feld Oberliga statt aller drei | grün → PSW-01 |
| M3 Zeile „Drittelpause“ statt „Ergebnisse“ | 2 rot |
| M4 kein Rückschreiben nach dem Saisonende | 2 rot (Level 3 und 5) |
| M4b Rückschreiben nur bis Level 4 | 1 rot (Level 5) |
| Squad-Auffüllen bis 32 entfernt | 1 rot (aktiv, Seed 3: 16 Spieler) |
| Tagesstände aus der Zinsspanne entfernt | 1 rot (Bundesliga passiv, Zinsen −14.799) |

Zusätzlich: `git merge-tree origin/main pr360` konfliktfrei, Base `0a43582` ist in `main` enthalten (#335 gemergt).

**Akzeptierte `[APPROX]`:** #349, die Entscheidung je Paarung statt je Spieltag (0x36FEA). Es bleibt laut Entscheidung des
Nutzers so, die Einstellung kommt nach v1.0. Ebenso übernommen: Die Darstellung des Penaltyzugs des schnellen Wegs (Gleichstand
plus `ShootoutHomeWins`) und der WM-Schalter im Bundestrainer-Abschnitt. Beide sind durch #360 nicht verändert.

**Annahmen (nicht neu belegt):** Das Zinskonto wird beim Saisonwechsel im Original nicht geleert (aus Teil 5). Die Saisonverteilung
§15 wurde nicht neu erhoben (#354, Runde 11).
