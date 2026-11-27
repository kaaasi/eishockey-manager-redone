# Nachprüfung: Integration Runde 14, Spur `ui-rest` (PR #666)

Nachprüfung des Reviews `docs/reviews/2026-11-09-integration-ui-rest-r14.md`, bisher geprüfter Head `85405f5`. Delta
`85405f5777a70a3f295ff9efd1509e6e8b7aba83..c5a2787b5fff724d165afdad2b9c826159c03648` · Reviewer: Claude (geplanter Lauf, frische
Sitzung ohne Implementierungskontext) · 09.11.2026 · Checkout: Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer;
Head und `85405f5` zusätzlich als Worktrees. Codelesen **und** Ausführung am nachgewiesenen Checkout mit Originaldaten
(`kaaasi/eishockey-manager-referenz`, Stand `779a1c8`). Pixelvergleich wie im Review: DOSBox-Bilder `original/spielstaende/bilder/`,
642 × 507 → Spielpixel (1 + 2x | 26 + 2y), Farben auf 6 Bit.

H = `https://github.com/kaaasi/eishockey-manager-redone/blob/c5a2787b5fff724d165afdad2b9c826159c03648`

## 1. Merge-Empfehlung

**Ja** für Head `c5a2787`. R14UR-01 bis -09 sind behoben und am Bild nachgewiesen. Jede Rückmutation auf den alten Wert macht einen
Test rot (Ausnahme: gedrückter Knopf im Vertragsfenster, R14N-03). Die gemeinsamen Dateien wirken auf allen geprüften Bildschirmen
gleich oder besser als auf `85405f5`. Kein Bildschirm wird schlechter. Der Merge von `main` bringt nur die Review-Datei von #668.
Die drei neuen Befunde sind P3 und blockieren nicht.

## 2. Befunde

### Status der Befunde aus dem Review

| Befund | Status | Nachweis (selbst ausgeführt) |
|---|---|---|
| R14UR-01 | behoben | Kauf (`file40_08`), Leihe (`file40_09`) und `file35_2`: Bereich (0 \| 0)–(59 \| 11) 0 von 720 Pixeln abweichend (vorher 677) |
| R14UR-02 | behoben | Vereinshistorie mit Kernwert `Rank = 12`, Oberliga, DEB erste Runde gegen `file14_5`, (60 \| 20)–(319 \| 199): 0 Pixel (vorher 4). Ligabalken (117–118 \| 171–172) vorhanden |
| R14UR-03 | behoben | Ausbau-Frage im Fenster (75 \| 107)–(242 \| 182) gegen `file43_5` (Rabatt 6, 8 Wochen): 2 Pixel (vorher 723). Die 2 Pixel sind die Knopfgrafik (149 \| 167) und (219 \| 167), siehe unten. Zeilen 2/3 gegen `file44_3` (3 %, 17 Wochen) und `file44_5` (6 %, 18 Wochen): 0 Pixel außerhalb des Mauszeigers |
| R14UR-04 | behoben, Rest siehe R14N-01 | Rahmen und Schatten des Kauf-/Leihfensters gegen `file40_08`, `file40_09`, `file35_2`: Kanten x 166 / y 71, helle Linie Index 1 = (36 \| 44 \| 48), dunkle Linie Index 29 = (8 \| 12 \| 20), Ecken wie im Bild. Von 832 Rahmen- und Schattenpixeln weichen nur die 3 äußeren Schattenecken ab (R14N-01). Vertragsfenster (`file46_3`, `file40_12`, `file35_3`): In Knopfband und Unterkante (Bildzeilen 70–95) weichen nur 4 Pixel ab, und zwar Text der Zeile „Versicherung“ (anderer Spieler). Vorher waren es 534. Nur die linke Kante x 1 statt x 2 weicht ab, das ist der bekannte Hinweis zu `ContractsScreen.DialogRect` |
| R14UR-05 | behoben | `docs/analyse/inventur.md` am Head gleich `main` (`git diff 1e9a233 c5a2787` leer). Die Begründung für `NationalLineupScreenTests`/`TeamScreenTests` steht im PR-Kommentar 6730242141 |
| R14UR-06 | behoben | Datei 0x3DB83–0x3DC4B selbst gelesen: Index 6 · i + 2 · `[bp−9]` mit `[bp−9] = 0`. Grenzen: x ≤ Maus-x ≤ x + Breite, y ≤ Maus-y ≤ y + Höhe. Treffer = DGROUP 0x4F97[i], danach erst 0x3DC59. DGROUP-Bytes nachgelesen: Ziel (244,50) (25,112) (201,94) (79,57) (29,44), Größe (66,118) (50,30) (68,55) (144,60) (52,21), Folge 5, 6, 8, 0, 7. Das entspricht genau `Hotspots` am Head |
| R14UR-07 | behoben | Nachrichtenleiste gegen `file37a_1`, Textzeilen (88 \| 217)–(299 \| 233): 0 Pixel (das Datum weicht wie im Review ab). 0x4B6F0–0x4B76D gelesen: Bereich 0x10 … 0x12F, Grundlinie 0xD4 (212), danach +12, dann +7. Die lange Werbepartner-Meldung steht in zwei Zeilen innerhalb des weißen Felds (Bild angesehen), y 234–236 bleibt frei |
| R14UR-08 | behoben | `OriginalDialog` x 95/165: Knöpfe gegen `file43_5`, `file44_3`, `file44_5` gleich, ebenso gegen `file11_10` (Sicherheitsfrage „Und tschüss“): Knopfband 0 Pixel (vorher 285). Alle Aufrufer geprüft: `StadiumScreen`, `AdvertisingScreen`, `QuitQuestion`, `DebCupDrawLive` und `TransferDialog` (Fragen) nutzen `OriginalDialog.LeftButton/RightButton`. Kein Aufrufer hat eine eigene Lage |
| R14UR-09 | behoben | Rechte Taste, Torwartpfeil, alle Markenzeilen, Obergrenze der Schleife: Rückmutationen rot (Tabelle unten) |

**Erwartet offen, bestätigt:** R14UR-10 (Trennlinie A382) → `pruefung-karte`, `TransferScreen.cs` ist im Delta unverändert.
Die Inventurzeilen trägt `pruefung-karte` nach. `ContractsScreen.DialogRect`/`TransferDialog.OfferBox` X = 1 statt 2 ist
nachgemessen: Linke Kante im Bild bei x 2 (Index 1), im Remake bei x 1. Andere Spur.

**Knopfgrafik (149 \| 167)/(219 \| 167):** Die Pixel weichen nur in den Stadionbildern ab (`file43_5`, `file44_3`, `file44_5`).
In `file11_10` stimmen sie mit dem Remake überein. Vermutlich liegt der Unterschied an der Stadionpalette (kb\14), nicht an der Lage.
Restlücke, nicht blockierend.

### R14N-01 (P3) – Schattenecken der Dialoge haben im Original die Füllfarbe

[`TeamUi.DialogFrame` Z. 90–91](https://github.com/kaaasi/eishockey-manager-redone/blob/c5a2787b5fff724d165afdad2b9c826159c03648/src/EishockeyManager.UI/Screens/Team/TeamUi.cs#L90-L91)
füllt den Schatten als volle Rechtecke. Im Original sind die drei äußeren Ecken des Schattens (20 | 28 | 32), also Füllfarbe Index 4,
nicht Index 6:

- Kauffenster `file40_08`, `file40_09`, `file35_2`: (168 | 31), (4 | 73), (168 | 73). Ohne Fenster (`file40_04`) liegt dort der
  orange Rand bzw. (16 | 24 | 28). Es ist also nicht der Hintergrund.
- Vertragsfenster `file46_3`, `file40_12`, `file35_3`: (168 | 31), (4 | 97), (168 | 97). Gleiches Muster.
- Der neue Test [`TransferBuyWindowDosboxTests` Z. 40](https://github.com/kaaasi/eishockey-manager-redone/blob/c5a2787b5fff724d165afdad2b9c826159c03648/tests/EishockeyManager.UI.Tests/TransferBuyWindowDosboxTests.cs#L40)
  schreibt `At(168, 73) == 6` fest. Das widerspricht dem Bild. Auch der Kommentar „Schatten bis (168 | 73)“ in `TransferDialog.cs` Z. 22 stimmt
  nicht ganz.
- Betrifft alle Nutzer von `DialogFrame` (Vertrag, Kauf/Leihe, Verkauf/Gebot). Es geht nur um 3 Pixel je Fenster. Folgebefund für
  `ui-rest` (A374).

### R14N-02 (P3, nicht durch das Delta verursacht) – Kontoleiste der Transferliste: Linie y 238 und „Saldo“

`DialogDark` = 29 wirkt auch auf [`TransferScreen.DrawAccountBar`](https://github.com/kaaasi/eishockey-manager-redone/blob/c5a2787b5fff724d165afdad2b9c826159c03648/src/EishockeyManager.UI/Screens/Team/TransferScreen.cs#L483-L498).
Der Knopf „Talent“ ist jetzt gleich `file40_04` (Schrift 47 + 35 Pixel, Farben gleich; vorher (8 | 12 | 16)). Zwei Teile weichen
weiter ab, am Base wie am Head:

- Die Linie y 238 ist im Bild (12 | 20 | 24) = Index 6/47. Im Remake ist sie Index 29 (vorher 200).
- Die Schrift „Saldo:“ und der Betrag sind im Bild (6 | 9 | 18) = Index 240. Im Remake ist sie Index 29 (vorher 200).

Durch das Delta wird nichts schlechter. Der Kommentar „unten 6/200/6“ ist veraltet. Hinweis an die Spur, die `TransferScreen.cs` besitzt.

### R14N-03 (P3) – Testlücke: Beschriftung des gedrückten Knopfs im Vertragsfenster

Die Rückmutation [`ContractDialog.cs` Z. 205](https://github.com/kaaasi/eishockey-manager-redone/blob/c5a2787b5fff724d165afdad2b9c826159c03648/src/EishockeyManager.UI/Screens/Team/ContractDialog.cs#L205)
`+ 1` → `+ 2` lässt alle 624 UI-Tests grün. Für den gedrückten Knopf gibt es kein DOSBox-Bild. Die Änderung folgt nur der Angleichung an
`DialogButton`. Sie ist schlüssig, aber nicht belegt und nicht getestet.

## 3. Ausgeführte Prüfungen

### Merge von `main` (`53285da`)

Eltern: `85405f5` und `1e9a233`. Ich habe mit `diff` verglichen:
`git diff $(merge-base) 1e9a233` = `git diff 85405f5 53285da` (nur `docs/reviews/2026-11-09-integration-ui-rest-r14.md`, +262).
Außerdem `git diff $(merge-base) 85405f5` = `git diff 1e9a233 53285da`. Beide sind identisch: Der Merge bringt nur bereits gemergte
Inhalte. Das inhaltliche Delta `53285da..c5a2787` umfasst 14 Dateien. Kein Diff in `src/EishockeyManager.Core` und
`src/EishockeyManager.Formats`. `SchemaVersion = 41`, `SaveGameTests.SchemaFingerprint_MatchesSchemaVersion` ist grün.

### `tools/pruefen.sh` (alle drei Läufe, Head `c5a2787`, SDK 10.0.402, Originaldaten aus dem Referenz-Repo)

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2330 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1952 | 0 | 340 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1953 | 377 | 0 |

Ergebnis „bestanden“. Die Zahlen stimmen mit dem PR-Kommentar überein.

### Rückmutationen (je Befund, einzeln im Head-Worktree, danach zurückgesetzt; `EM_REQUIRE_GAMEDATA=1`)

| Befund | Rückmutation | Rot |
|---|---|---|
| R14UR-01 | `if (IsBuyOrLoan) return;` entfernt | `TransferBuyWindowDosboxTests` |
| R14UR-02 | `37` → `36` | `HistoryBarsDosboxTests` |
| R14UR-03 | Rabatt-Zeile `$"Rabatt: {d}%"` | `ConstructionQuestion_TextRowsAndButtons_LikeDosbox`, `ProjectUnderConstruction…OriginalDialog` |
| R14UR-03 | Bauzeit-Zeile `$"Bauzeit: ca. {w} Wochen"` | `ConstructionQuestion_…` |
| R14UR-03 | `DrawLine` → alte `TeamUi.LabelCentered` (ohne Ziffernabstand) | `ConstructionQuestion_…` |
| R14UR-04 | `BuyBox` 164 × 42 (auch mit angepasster Testkonstante) | `TransferBuyWindowDosboxTests` |
| R14UR-04 | `DialogLight` = Weiß | `TransferBuyWindowDosboxTests` (sonst 623 grün) |
| R14UR-04 | `DialogDark` = 200 | `TransferBuyWindowDosboxTests` (sonst 623 grün) |
| R14UR-04 | schwarze Ecke rechts unten entfernt | `TransferBuyWindowDosboxTests` |
| R14UR-04 | Linien wieder bis in die Ecken | `TransferBuyWindowDosboxTests` |
| R14UR-06 | Flächen ohne +1 (Grenzen exklusiv) | `Hotspot_EdgesAreInclusive_LikeOriginal` (5 Fälle), `StadiumScreenTests.SelectLevelConfirmAndAdjustTicketPrice` |
| R14UR-06 | Anzeigetafel/Sitzkomfort vor den Teilen geprüft | `Hotspot_EdgesAreInclusive_…` (2), `Hotspot_FirstHitWins_InOrderOfTheOriginal` |
| R14UR-07 | `center` 158 | `Message_ShowsDateAndTwoCenteredLines` |
| R14UR-07 | `MaxTextLines` 9 | `LongMessage_ShowsOnlyTwoLines_NothingOnTheBottomEdge` |
| R14UR-08 | Knöpfe 94/164 | `ConstructionQuestion_…` |
| R14UR-08 | `DialogButton`-Schrift `+ 2` | `ConstructionQuestion_…` |
| R14UR-08 | `ContractDialog` gedrückt `+ 2` | **keiner** (R14N-03) |
| R14UR-09 | rechte Taste schließt die Frage | `ProjectUnderConstruction…OriginalDialog` |
| R14UR-09 | Torwartpfeil umgedreht | `Tactics_GoalieArrows_LowerHalfIsNext_NoWrap` |
| R14UR-09 | oberste Markenzeile x + 1 | `HistoryBarsDosboxTests` |
| R14UR-09 | Linienwechsel-Pfeil umgedreht | `TeamScreenTests.Tactics_ModesBlocksAndSwitchToLineup` rot (keine Endlosschleife mehr) |

Geänderte Sollwerte in den Tests (BuyBox, Rang 12 und Text, exakte Zeilenlagen der Nachricht, Zeilenausmaße der Ausbau-Frage,
Hotspot-Rechtecke, Knopfkanten 95/165, Schrift „Ja.“ x 120 / „Nein.“ x 185): Jeder dieser Werte geht auf eine der Regeln oben zurück.
Die zugehörige Rückmutation lässt ihn fallen. Fingerabdrücke oder Saaten hat das Delta nicht geändert.

### Gemeinsame Dateien: Bildschirmfoto-Abläufe am Head und am Base (`85405f5`)

Ausgeführt: `stadion`, `vertraege`, `transfer`, `transfer-angebot`, `transfer-vertrag`, `finanzen`, `speichern`, `und-tschuess`, dazu ein
eigener, nicht eingecheckter Ablauf (Kauf, Leihe, Historie Rang 12, drei Ausbau-Fragen, Nachrichtenleiste kurz/lang). Die Bilder habe
ich angesehen (Kauffenster, Ausbau-Frage, Vertragsverhandlung, Verkaufsmeldung, Nachrichtenleiste).

| Ablauf | Unterschied Head ↔ Base | gegen DOSBox |
|---|---|---|
| `finanzen` (9 Bilder) | 0 Pixel | – |
| `stadion` (8 Bilder) | 0 Pixel. Der Ablauf zeigt keine Frage, weil das Geld fehlt (Kosten 530.000 > 500.000 DM). Die Frage habe ich im eigenen Ablauf geprüft | `file43_5`, `file44_3`, `file44_5` siehe oben |
| `vertraege` 02/03 | Rahmen und Knöpfe | `file46_3`/`file40_12`/`file35_3` Knopfband: Head 4 (nur Text „Versicherung“), Base 534; nur die linke Kante x 1 (bekannt) |
| `speichern` (alle) | Knopfschrift A:–G:, LÖSCHEN | `a338_1` (50 \| 207)–(290 \| 234): Head **0**, Base 384 |
| `und-tschuess` 01 | Knöpfe | `file11_10` Knopfband: Head **0**, Base 285; Text 0 / 0 |
| `transfer` (alle) | Kontoleiste (Index 29), Kauffenster, Knopf „Aha !“ | „Talent“ gleich Bild. Linie y 238 und „Saldo“ abweichend wie am Base (R14N-02) |
| `transfer-angebot`, `transfer-vertrag` | Rahmenfarben, Ecken, Knöpfe | kein DOSBox-Bild. Gleiche Bausteine wie oben, angesehen |

### Fundstellen in em.exe (selbst gelesen, `tools/reverse-engineering/disasm.py`)

- Datei 0x3DB83–0x3DC59: Trefferprüfung der Stadionteile (siehe R14UR-06). DGROUP 0x4F3D/0x4F5B/0x4F79/0x4F97 gelesen
  (Basis Segment 0x14FD).
- Datei 0x4B6F0–0x4B76D: Zeilen der Nachrichtenleiste an „^“, zentriert in 0x10 … 0x12F, Grundlinien 212 / +12 / +7.

### AGENTS.md §4

Das Delta ändert keinen Spielzustand beim Öffnen. `HotspotAt` und `ConstructionDiscountLine/WeeksLine` sind reine Funktionen.
`NewsBar.Draw` liest nur. Die Ausbau-Frage antwortet weiter nur auf die linke Taste auf einem Knopf (getestet).

### Akzeptierte `[APPROX]`

- NewsBar: Remake-Meldungen ohne „^“ werden umbrochen, höchstens zwei Zeilen (zuerst breiter, dann abgeschnitten). Der volle Text
  steht im Posteingang.
- StadiumScreen: Anzeigetafel und Sitzkomfort bleiben angenommen und werden nach den Teilen geprüft.

### Annahmen und Grenzen

- Für den gedrückten Knopf im Vertragsfenster, den Verkaufs-/Gebotsrahmen (`OfferBox`) und den Knopf „Aha !“ gibt es kein DOSBox-Bild.
  Ich habe sie nur angesehen.
- Die Ursache der 2 Knopfpixel im Stadion (Palette) ist nicht verfolgt.
- Kein offener Prüfbereich.
