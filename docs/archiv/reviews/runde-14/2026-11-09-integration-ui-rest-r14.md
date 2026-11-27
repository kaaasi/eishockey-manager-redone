# Review: Integration Runde 14, Spur `ui-rest` (PR #666)

Base `6e1bf447ab1db1f346fccf536d0ae5a20dcb561f` · Head `85405f5777a70a3f295ff9efd1509e6e8b7aba83` · Reviewer: Claude (geplanter Lauf,
frische Sitzung ohne Implementierungskontext) · 09.11.2026 · Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer;
Base und Head zusätzlich als Worktrees. Vollprüfung des ganzen Deltas (20 Dateien, +539/−154; enthält #634, #647, #657). Codelesen
**und** Ausführung am nachgewiesenen Checkout mit Originaldaten. Fundstellen selbst mit `tools/reverse-engineering/disasm.py` gelesen.
Pixelvergleich gegen die DOSBox-Bilder aus `kaaasi/eishockey-manager-referenz` (`original/spielstaende/bilder/`, Stand `779a1c8`;
642 × 507 → Spielpixel (1 + 2x | 26 + 2y), Farben auf 6 Bit verglichen).

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83`

## 1. Merge-Empfehlung

**Nein** für diesen Head. Stadionbild (A323/A325) und Bauanzeige (A326) sind pixelgleich mit den DOSBox-Bildern (0 abweichende Pixel in
allen vier Ausbaustufen, Tabelle in em.exe nachgelesen). Gleiches gilt für #597, A382, A373 (Richtung, kein Umlauf), A313 und die
Farben und die Marke von A357. Zwei Änderungen machen das Spiel aber schlechter als auf Base:

- **R14UR-01 (P1):** Das neue Kauf-/Leihfenster zeichnet die beiden Knöpfe in die linke obere Bildschirmecke (0 | 0).
- **R14UR-02 (P1):** Die Ligabalken der Vereinshistorie sind um eine Stufe zu niedrig. Der Kern speichert den Rang **1-basiert**,
  der Test setzt ihn 0-basiert. Mit echten Daten hat der 12. Platz der Oberliga keinen Balken mehr; auf Base hatte er die richtigen
  2 Pixel.

Dazu kommen die Texte der Ausbau-Frage (R14UR-03), das 1 Pixel zu kleine Kauffenster (R14UR-04) und die Dateigrenze `inventur.md`
(R14UR-05). Der Hotspot Kino/Gastronomie ist mit dem Original belegbar (R14UR-06): Das Original prüft die Treffer mit derselben Tabelle.

## 2. Befunde

### R14UR-01 (P1) – A377: Kauf und Leihe zeichnen „Kaufen“/„Abbruch“ bei (0 | 0)

[`TransferDialog.cs` Z. 39–41](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Team/TransferDialog.cs#L39-L41) setzt für Kauf und Leihe `ConfirmButton`
und `CancelButton` auf `new Rect(0, 0, 0, 0)`. [`Draw` Z. 237–238](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Team/TransferDialog.cs#L237-L238)
zeichnet die Knöpfe danach trotzdem, mit `TeamUi.DialogButton` an `ConfirmButton.X/Y` = (0 | 0). Die Knopfgrafik aus kb\2 hat feste Größe.

- Gegenbeispiel: neues Spiel → Transfer → „kaufen“ bei einem Spieler ohne Verkäufer. Oben links steht ein Knopf „Abbruch“ (darunter
  „Kaufen“). Gegen `file40_08` weichen im Bereich (0 | 0)–(59 | 11) 677 von 720 Pixeln ab. Im Original gibt es dort keinen Knopf.
- Der Test [`TransferBuyWindowDosboxTests`](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/tests/EishockeyManager.UI.Tests/TransferBuyWindowDosboxTests.cs) prüft nur
  `ConfirmButton == Rect(0,0,0,0)`, nicht die Pixel. Darum fällt der Fehler nicht auf.
- Fix: bei `IsBuyOrLoan` keine Knöpfe zeichnen. Test: Bereich (0 | 0)–(60 | 12) gleich dem Hintergrund ohne Fenster.

### R14UR-02 (P1) – A357: Balkenhöhe Liga mit 0-basiertem Rang; der Kern speichert 1-basiert

[`HistoryScreen.cs` Z. 180](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Statistics/HistoryScreen.cs#L180) rechnet jetzt
`2 · (36 − (Rank + 12 · Liga))` statt `2 · (37 − …)`. Begründung im Kommentar: „Rang 0-basiert; 12. Platz der Oberliga = 2 Pixel“.
[`StatisticsHistory.cs` Z. 185](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.Core/StatisticsHistory.cs#L185) speichert aber `IndexOf(club) + 1`. Die Anzeige
schreibt `{h.Rank}. Platz` ([Z. 78](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Statistics/HistoryScreen.cs#L78)).

- Der Test [`HistoryBarsDosboxTests` Z. 22](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/tests/EishockeyManager.UI.Tests/HistoryBarsDosboxTests.cs#L22) setzt `Rank = 11`. Der
  Bildschirm zeigt dann „11. Platz der Oberliga“. Im Bild `file14_5` steht „12. Platz der Oberliga“.
- Gegenbeispiel mit dem Wert, den der Kern wirklich schreibt (`Rank = 12`, Oberliga, DEB erste Runde): Am Head fehlt der Ligabalken ganz
  (4 abweichende Pixel bei x 117–118, y 171–172 gegen `file14_5`). Am Base ist die Höhe richtig (2 Pixel). Am Base falsch waren nur Farbe
  und Marke (33 Pixel).
- Folge: Jeder Ligaplatz ist um 2 Pixel zu niedrig, der letzte Platz der Oberliga hat keinen Balken mehr.
- Fix: `37` zurück, Kommentar berichtigen, im Test `Rank = 12` setzen (und den Text „12. Platz der Oberliga“ prüfen). Farbe 29/27,
  Marke 2/4/6 + Stiel und DEB 3 Pixel/Runde bleiben. Diese Teile sind mit dem Bild pixelgleich (0 abweichende Pixel im Grafikbereich).

### R14UR-03 (P2) – A326: Ausbau-Frage, Zeilen „Rabatt“ und „Bauzeit“ nicht wie im Original

[`StadiumScreen.cs` Z. 242–243](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Manager/StadiumScreen.cs#L242-L243) schreibt
`$"Rabatt: {q.Discount}%"` und `$"Bauzeit: ca. {q.Weeks} Wochen"`. Nachgelesen in Datei 0x3E238–0x3E37A:

- Datei 0x3E24D setzt `DGROUP 0x374 = 2`, die Mindestbreite der Zahlenausgabe 0x0B954. Das ist dieselbe Routine wie
  `ClubStatisticsScreen.Number(value, width, …)`: fehlende Stellen werden als Ziffernabstand aufgefüllt.
- Zeile 3 setzt sich zusammen aus DGROUP 0x2F31 „Bauzeit: ca. “, der Zahl mit Breite 2 und DGROUP 0x2F1F „ Wochen“.
- Zeile 2 setzt sich zusammen aus DGROUP 0x43B7 „Rabatt:“ (ohne Leerzeichen), der Zahl mit Breite 2 (0x3E326/0x3E335) und
  DGROUP 0x502F **„#“**. Das ist das Prozentzeichen der Schrift, wie bei „einzahlen (1#)“ im Finanzbildschirm.
- Das Remake zeichnet „Rabatt: 8“: Das „%“ fehlt in der Mini-Schrift und bleibt unsichtbar, und die Auffüllung fehlt. Gegen `file43_5`
  weichen im Fenster 723 Pixel ab (Zeilen 145–158 und die Knöpfe, siehe R14UR-08). Rahmen, Titel, Symbole und Zeile 1 sind gleich.
- Der Test [`StadiumPartsTests` Z. 102](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/tests/EishockeyManager.UI.Tests/StadiumPartsTests.cs#L102) schreibt den falschen Text fest.

### R14UR-04 (P2) – A377: Kauf-/Leihfenster 1 Pixel zu schmal und zu niedrig

[`TransferDialog.cs` Z. 25](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Team/TransferDialog.cs#L25): `BuyBox = (2, 29, 164, 42)`.
`TeamUi.DialogFrame` setzt damit die dunkle Kante auf x 165 / y 70 und den Schatten bis (167 | 72). In `file40_08`, `file40_09` und
`file35_2` liegt die dunkle Kante bei x 166 / y 71, der Schatten reicht bis (168 | 73). Das steht auch so im Kommentar und in
`re-runde-14.md` („Rahmen (2 | 29)–(166 | 71), Schatten bis (168 | 73)“). Richtig wäre `(2, 29, 165, 43)`. Der Test prüft nur
(167 | 40) und (60 | 72), beide sind in beiden Fällen Index 6. Die Rückmutation auf 165 × 43 fällt nur an der Konstanten.
Hellere Linienfarbe (36 | 44 | 48) statt Weiß und Ecke (2 | 29) in Füllfarbe: siehe die erwarteten Hinweise aus #663 (Teil 3).

### R14UR-05 (P2) – Dateigrenzen: `docs/analyse/inventur.md` und fremde Testdateien

- `docs/analyse/inventur.md` gehört allein `pruefung-karte` ([`arbeitspakete.md` Z. 40–42](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/docs/arbeitspakete.md#L40-L42)). Die anderen
  Spuren nennen ihre Zeilen nur im PR. Commit `051f30f` (Teil 3) ändert die Zeilen A323, A325 und A326 direkt
  ([Z. 619–622](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/docs/analyse/inventur.md#L619-L622)). #657 nennt unter „Dateigrenzen“ nur `StadiumScreen.cs`, Tests und Doku. #666
  schreibt „Inventur-Zeilen (→ `pruefung-karte`)“. Fix: die drei Zeilen auf den Stand von Base zurücksetzen und im PR nennen, oder die
  Ausnahme von Leitstelle/Nutzer freigeben lassen.
- Commit `6766462` passt `NationalLineupScreenTests.cs` (Datei der Spur `nationalteam`) und `TeamScreenTests.cs` an (Klick in die untere
  Pfeilhälfte, Folge von A373). Fachlich nötig, aber in #647/#666 nicht genannt. Regel „Tests nur in den Testdateien der eigenen Spur“:
  im PR begründen.

### R14UR-06 (P2) – Hotspot Kino/Gastronomie: Das Original prüft die Treffer mit derselben Tabelle

Der Kommentar [`StadiumScreen.cs` Z. 91–92](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Manager/StadiumScreen.cs#L91-L92) begründet das `[APPROX]`
mit „Treffer im Original nicht belegt“. In 0x3D7F8 steht die Trefferprüfung direkt nach dem Zeichnen, in Datei 0x3DB83–0x3DC4B:

- Schleife i = 0 … 4 in der Folge DGROUP 0x4F97 (5, 6, 8, 0, 7). Es zählt immer der Eintrag der **Stufe 1** (`[bp−9] = 0`), mit den
  Grenzen einschließlich: Ziel x ≤ Maus-x ≤ Ziel x + Breite und Ziel y ≤ Maus-y ≤ Ziel y + Höhe. Der erste Treffer gewinnt.
- Daraus folgen diese Flächen:

  | Bereich | Original (x, y, Breite+1, Höhe+1) | Remake `Hotspots` |
  |---|---|---|
  | Verkehrsanbindung | 244, 50, 67, 119 | 250, 50, 70, 118 |
  | Gastronomie | 25, 112, 51, 31 | 4, 110, 90, 44 |
  | Bowlingbahn | 201, 94, 69, 56 | 194, 110, 80, 44 |
  | Überdachung | 79, 57, 145, 61 | 90, 63, 126, 39 |
  | Kino | 29, 44, 53, 22 | 18, 53, 72, 30 |

- Danach (Datei 0x3DC59 ff.) folgen die Sonderflächen und die Bau-Tabelle (y > 167, Datei 0x3DB62). Anzeigetafel und Sitzkomfort
  (A322) sind nicht Teil dieses Deltas.
- Das `[APPROX]` lässt sich also durch `[ORIGINAL]` ersetzen (Hotspots aus `PartTable[f][0]`, Reihenfolge `PartOrder`). Nicht
  blockierend, wenn Leitstelle/Nutzer es als Folgebefund annehmen.

### R14UR-07 (P3) – A321: Nachrichtenzeilen 1 Pixel zu weit links, Test zu großzügig

Gemessen gegen `file37a_1`: Remake-Zeile 1 x 94–220, Zeile 2 x 132–182. Im Bild: x 95–221 und 133–183. Der Kommentar
([`NewsBar.cs` Z. 90](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/src/EishockeyManager.UI/Screens/Manager/NewsBar.cs#L90)) nennt selbst 95–221. Die Zeilenlage y (208–212,
220–224, 227–231) stimmt. Der Test prüft die Mitte mit `InRange(156, 160)`
([Z. 55](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/tests/EishockeyManager.UI.Tests/NewsBarDosboxTests.cs#L55)): Die Rückmutationen `center = 157` und `= 159` bleiben grün.
Zusatz zum `[APPROX]`-Umbruch: Längere Meldungen (z. B. „Ein Werbepartner mit Verlängerungsoption bittet Sie an den
Verhandlungstisch.“) bekommen eine dritte Zeile bei y 234. Die liegt auf dem unteren Rand der Leiste (Area endet bei y 236).

### R14UR-08 (P3, außerhalb des Deltas) – Knöpfe der Fragefenster laut DOSBox bei x 95/165

`OriginalDialog` zeichnet die Knöpfe nach dem Video bei 94/164 (`[APPROX]`, Code 95/165). In `file43_5`, `file44_3` und `file44_5`
beginnt die Knopfkante bei x 95 (Zeile 171: Remake hell bei 94–95, Bild bei 95–96). Die DOSBox-Bilder belegen also den Code-Wert.
Das betrifft alle Aufrufer. Hinweis an die Leitstelle; nicht dieser Spur anzulasten.

### R14UR-09 (P3) – Testlücken (Gegenproben bleiben grün)

- Ausbau-Frage: Die rechte Maustaste wird nicht geprüft. Der Kommentar im Test behauptet es, geklickt wird aber nur links (5 | 5).
  Mutation „rechte Taste schließt die Frage“ bleibt grün.
- A373: Die Richtung des Pfeils oben links (`GoalieArrows`) ist ungetestet. Umgedreht bleiben alle 615 UI-Tests grün.
- A357: Die oberste Zeile der Marke um 1 Pixel verschoben bleibt grün (geprüft wird nur die Breite je Zeile).
- `TeamScreenTests.Tactics_ModesBlocksAndSwitchToLineup` hängt bei falscher Pfeilrichtung in der `while`-Schleife
  ([Z. 109–110](https://github.com/kaaasi/eishockey-manager-redone/blob/85405f5777a70a3f295ff9efd1509e6e8b7aba83/tests/EishockeyManager.UI.Tests/TeamScreenTests.cs#L109-L110)), statt rot zu werden. Besser mit Obergrenze.

### R14UR-10 (P3, Hinweis) – A382: Länge der normalen Trennlinie

Farbe Index 31 einzeilig und Markierung Index 16 stimmen (Zeile 132 gegen `file40_05`: 0 Pixel Abweichung). Die normale Trennlinie
reicht in `file40_04` von x 181 bis 313, im Remake von x 180 bis 314. In `file40_05`–`07` reichen einige Linien bis 314. Das sind
vermutlich Reste der Markierung. Nicht entscheidbar; als Hinweis für `pruefung-karte`.

### Erwartet offen – bestätigt

- **Hinweise aus #663 zu `TeamUi.DialogFrame`/`DialogButton`:** weiter offen. Im Kauffenster (`file40_08`) ist die obere/linke Linie
  (36 | 44 | 48), Index 1/34, nicht Weiß. Die dunkle Linie ist (8 | 12 | 20). Die Ecke (2 | 29) hat die Füllfarbe. Der Schatten Index 6
  stimmt (A374 belegt).
- **Inventurzeilen trägt `pruefung-karte` nach:** für A373, A374, A377, A382, A313, A321 und A357 ja (unverändert). A323, A325 und A326
  hat die Spur selbst geändert, siehe R14UR-05.

## 3. Ausgeführte Prüfungen

### `tools/pruefen.sh` (alle drei Läufe, Head `85405f5`, SDK 10.0.402, Originaldaten aus dem Referenz-Repo)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2321 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1946 | 0 | 337 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1947 | 374 | 0 |

Ergebnis „bestanden“, gleich den Zahlen im PR. Schema bleibt 41: kein Diff in `src/EishockeyManager.Core` und
`src/EishockeyManager.Formats`, `SaveGameTests.SchemaFingerprint_MatchesSchemaVersion` grün mit unverändertem Abdruck.

### Gegenprobe je neuem oder geändertem Test

Base: Testdateien des Heads in den Base-Worktree kopiert (`EM_REQUIRE_GAMEDATA=1`). Mutation: einzelne Änderung im Head-Worktree, danach
zurückgesetzt.

| Test | Gegenprobe | Ergebnis |
|---|---|---|
| `FinanceBedienungTests.AmountBar_DisappearsWhenMouseLeavesStrip` (neu) | Base | rot |
| `FinanceBedienungTests.HeldButton_Repeats_AndDrawsBar`, `SelectionFrames_AndDeselect`, `Hint_ShownForActiveSide` (geändert: Maus im Streifen) | Base grün (erwartet, nur angepasst); Mutation `ActionBar` Höhe 28 → 7 | rot |
| `HistoryBarsDosboxTests.Bars_ColorsHeightsAndMarker_LikeOriginal` (neu) | Base | rot (Rang-Basis falsch, R14UR-02) |
| `NationalLineupScreenTests.Lineup_Clicks_…` (geändert) | Base; Mutation Effort-Richtung | rot / rot |
| `NewsBarDosboxTests.Message_ShowsDateAndTwoCenteredLines` (neu) | Base | rot |
| `StadiumArrowTests.TicketArrows_AreTenPixelsWide_FromKb4BaseGraphic` (neu) | Base; Mutation Grundgrafik weg | rot / rot |
| `StadiumPartsTests.EveryPartIsDrawnFromItsTableEntryWithMask0xC0` (neu) | Mutation Maske 0xC0 → 0; Ziel x + 1 | rot / rot |
| `StadiumPartsTests.HallCoversRestaurantInDrawOrder` (neu) | Mutation Folge (Gastronomie nach Überdachung); Ziel x + 1 | rot / rot |
| `StadiumPartsTests.ProjectUnderConstruction…OriginalDialog` (neu) | Mutation „(Noch 1 Woche)“ zurück; Klick daneben = Nein | rot / rot; rechte Taste schließt: **grün** (R14UR-09) |
| `TacticsClubScreenTests.Tactics_OnlySemiEditable` (geändert) | Base; Mutation Attack-Richtung | rot / rot |
| `TacticsClubScreenTests.Tactics_ModeArrows_LowerHalfIsNext_NoWrap` (neu) | Base; Mutation Umlauf; Mutation Richtung LineChange | rot / rot / rot |
| `TeamScreenTests.Tactics_ModesBlocksAndSwitchToLineup` (geändert) | Base; Mutation Effort-Richtung | rot / rot (LineChange-Richtung: hängt, R14UR-09) |
| `TransferBuyWindowDosboxTests.BuyWindow_…` (neu) | Mutation `BuyBox` 165 × 43; Knöpfe zurück; altes Fenster `Box` | rot / rot / rot (der erste nur an der Konstanten, R14UR-04) |
| `TransferVertraegeKomplettTeil4ScreenTests.HoveredEntry_…` (geändert) | Base; Mutation 16 → 96; 31 → 63; Linie 198 | rot / rot / rot / rot |

### Rückmutation je geändertem Sollwert

| Sollwert | Rückmutation | Test fällt? |
|---|---|---|
| `HoverEntryColor` 16 | 96 (gleiche Farbe) | ja |
| `SeparatorColor` 31 | 63 (gleiche Farbe) | ja |
| `BarColorNormal` Liga 29 / DEB 27 | 147 / 200 | ja / ja |
| Ligahöhe `36 − …` | `37 − …` | ja, aber 37 ist richtig (R14UR-02) |
| DEB 3 Pixel/Runde | 4 | ja |
| Marke Stiel 2 × 3 | 1 × 3 | ja; erste Zeile x + 1: nein (R14UR-09) |
| NewsBar `center` 158 | 157 / 159 | **nein / nein** (R14UR-07) |
| NewsBar `DateY` 207, `TextY` 220, Zähler nur bei > 1 | 208 / 221 / > 0 | ja / ja / ja |
| Taktik `Next` ohne Umlauf, Richtung | Umlauf; Richtung LineChange/Effort/Attack/Goalie | ja / ja / ja / ja / **nein** |
| `PartTable`, `PartOrder`, `PartMask` | Ziel x + 1, Folge, Maske 0 | ja |
| `BuyBox` | 165 × 43 | ja (nur Konstante) |
| Pfeil-Grundgrafik #597 | entfernt | ja |
| A313 Streifen y 73–100 | Höhe 7 | ja |

### Pixelvergleich mit den Referenzbildern

Bildschirmfotos mit einem eigenen, nicht eingecheckten Ablauf `review-r14` (`emtool screens`) am Head und am Base. Zustand jeweils wie
im Spielstand (`emtool hok dump`: FILE43 Gastronomie 1, Kino 0; FILE44 2/1; FILE44A 2/2; FILE44B 3/3; Überdachung 2, Straße).

| Bildschirm / Befund | Referenz | Bereich | Abweichende Pixel Head | Base |
|---|---|---|---:|---:|
| Stadionbild A323/A325 (Imbiß) | `file43_2`, `file43_7` | (0 \| 0)–(319 \| 167) | 0 / 0 | 1969 |
| Stadionbild Kneipen + Kino klein | `file44_7` | dto. | 0 | 4315 |
| Stadionbild Kneipen + Kino mittel | `file44a_10` | dto. | 0 | 4501 |
| Stadionbild Restaurant + Kino groß | `file44b_6` | dto. | 0 | 5562 |
| Eintrittspfeile #597 | `file43_2` | (188 \| 186)–(213 \| 198) | 5 (nur x 213 = „E“ von „Eintritt“) | 15 |
| Stufenpfeile #597 | `file43_4` | (188 \| 207)–(213 \| 219) | 22 (nur x 212–213 = Rahmen LevelBox, nicht im Delta) | 32 |
| Bauanzeige A326 (Maus auf Gastronomie im Bau) | `file44_6` | (205 \| 198)–(319 \| 231) | 0 | – (Base-Ablauf abweichend) |
| Ausbau-Frage A326 | `file43_5` | (70 \| 100)–(249 \| 184) | 723 (R14UR-03, -08); Rahmen, Titel, Zeile 1: 0 | 12997 |
| Vereinshistorie A357 (Rang 11, Test) | `file14_5` | (60 \| 20)–(319 \| 199) | 0 | 37 |
| Vereinshistorie A357 (Rang 12, Kernwert) | `file14_5` | dto. | 4 (Ligabalken fehlt, R14UR-02) | 33 |
| Nachrichtenleiste A321 | `file37a_1` | (88 \| 203)–(299 \| 235) | 614 (anderes Datum; Text −1 px, R14UR-07) | 658 |
| Transferliste A382, Trennlinien | `file40_04` | Zeilen 52 + 16k, x 180–314 | 2 je Zeile (Enden, R14UR-10) | 135 je Zeile (2 Zeilen) |
| Transferliste A382, Markierung | `file40_05` | Zeile 132 | 0 | 135 |
| Kauffenster A377/A374 | `file40_08` | Rahmen/Schatten | Kante und Schatten 1 px zu früh (R14UR-04); Schattenfarbe gleich | – |
| Kauffenster, linke obere Ecke | `file40_08` | (0 \| 0)–(59 \| 11) | 677 von 720 (R14UR-01) | 0 |

### Fundstellen in em.exe (selbst gelesen)

- Teiltabelle Datei 0x3D904–0x3D9EB: Ziel DGROUP 0x4F3D, Größe 0x4F5B, Quelle 0x4F79, Folge 0x4F97 = 5, 6, 8, 0, 7, Index
  6 · i + 2 · (Stufe − 1), Maske 0xC0 (Datei 0x3D92D). Alle 15 Einträge gelesen. Sie stimmen Byte für Byte mit `PartTable` überein;
  die leeren Einträge (Größe 0) fehlen im Remake, sind aber durch die Stufenzahl nicht erreichbar.
- #597: Datei 0x3DAE0–0x3DB29 bzw. 0x3DFAB–0x3DFF4: Quelle kb\4 (20 | 9j), 10 × 8, Ziel (192 + 10j | 189 bzw. 210), Maske 0xC0.
  Das Remake blittet ohne Maske; kb\4 enthält im Bereich keinen Wert 0xC0, also gleichwertig.
- A373: Datei 0x487A8–0x487B8 (untere Hälfte → `[bp−0xC] = 1`), 0x48BD2–0x48C25 (Schritt +1 bis Anzahl − 1, −1 bis 0, kein Umlauf). Die
  genaue Grenzzeile hängt an der Tabelle `[bp−0xBE]`. Die Pfeil-Rechtecke sind im Delta unverändert; nicht nachgeprüft.
- A313: Datei 0x59531–0x5955F: y ≤ 0x48 oder y ≥ 0x65 → 0x59ADD (Löschen), unabhängig von einem Klick; y aus Wort 0 der Mausdaten
  0xC147 (Datei 0x5903A, x = Wort 2 → `si`). Belegt.
- A326: Fragefenster Datei 0x3E37A → 0x4C246, Texte wie in R14UR-03; Ja Datei 0x3E389, Nein 0x3E482. Buchung über `GameState.Book`
  in `AnswerConstruction`, nur nach „Ja.“.
- Hotspots: Datei 0x3DB83–0x3DC4B (R14UR-06).

### AGENTS.md §4

- Kein Bildschirm ändert beim Öffnen den Spielstand. `StadiumScreen`, `HistoryScreen`, `NewsBar.Draw`, `TransferDialog` und
  `FinanceScreen` lesen nur. `FinanceScreen.Deactivate` setzt nur Bildschirmzustand.
- Ausbau-Frage: `QuoteConstruction` (Zufall, Merker „beleidigt“) nur nach dem Klick auf die Stufenbox (unverändert gegenüber Base).
  `AnswerConstruction` und `Book(…, Bau, …)` nur nach einem Klick auf „Ja.“. Nur linke Maustaste, Klick daneben wirkt nicht (getestet).
  Escape und Enter wirken nicht mehr; das deckt sich mit 0x4C246 und der Entscheidung #205 (Komforttasten sind Sache des Aufrufers).

### Akzeptierte `[APPROX]`

- NewsBar: Umbruch längerer Texte; Hinweis/Zähler bei Fragen und mehreren Nachrichten (Hinweis zu Zeile 3 in R14UR-07).
- TransferDialog: Vorgabebetrag vor der Eingabemarke; Lage der Eingabezeile (x 14, y 60).
- HistoryScreen: Europapokal-Farbe wie DEB, DEB 3 Pixel je Runde (ein Messpunkt).
- FinanceScreen: Betrag wird beim Verlassen mit gelöscht (vorher schon so).
- Nicht akzeptiert: Hotspots Kino/Gastronomie (belegbar, R14UR-06).

### Annahmen und Grenzen

- Die Rabatt-/Bauzeit-Zeilen sind nur aus dem Code und einem Bild (Rabatt 6, 8 Wochen) abgeleitet. Ob „Rabatt:“ bei 0 % eine eigene
  Zeile ohne Zahl zeigt (Zweig random(0, 2) = 0, Datei 0x3E314), ist nicht verfolgt.
- Die Lage des Kauffensters ist nur am Kauf (`file40_08`) und an der Leihe (`file40_09`, `file35_2`) gemessen, nicht am Verkauf.
- Kein offener Prüfbereich. Alle Punkte des Auftrags sind geprüft, die Grenzen stehen oben.
