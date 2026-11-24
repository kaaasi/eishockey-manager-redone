# Belege aus em.exe für die Abweichungen der Runde 25 (Spur `re-abweichungen`)

Welle 0 der Runde 25 (R25-D3 = B, `docs/arbeitspakete.md`). Je Thema: Beleg als **Nachbildung des Originalablaufs** – die
Skripte in `tools/reverse-engineering/abweichungen/` führen den Code von `em.exe` mit dem Interpreter `emu16.py` aus (Speicher
vorbereitet, Grafik- und Eingabefunktionen durch Stubs ersetzt) und geben aus, was das Original berechnet bzw. wohin es
verzweigt (`zusammenarbeit.md` §5 Punkt 7). Urteil „Abweichung bestätigt“ oder „keine Abweichung“, Vorgabe für die
umsetzende Spur. Kein Produktcode.

Aufruf aller Belege: `python3 tools/reverse-engineering/abweichungen/<skript>.py [~/referenz/original/gamedata/em.exe]`
(ohne Pfad: `~/referenz/original/gamedata/em.exe`). Die Ausgaben unten sind wörtlich kopiert.

## Werkzeug

`tools/reverse-engineering/abweichungen/emu16.py`: 16-Bit-Interpreter (8086/186-Befehlssatz ohne FPU) über die Bytes von
em.exe. Ladebild ab linear 0 (Basissegmente und DGROUP 0x14FD behalten ihre Segmentwerte), Overlays ab Segment 0x3000,
Fernaufrufe über die Segmenttabelle von `funktionskarte.py`. Je Skript ist festgelegt, welche Bereiche echt laufen
(`allowed`: Overlay des Themas, C-Laufzeit Basis 0x0/0x238, Knopffunktionen 0x0B0E6–0x0B446, Zahl→Text 0x0B954–0x0BAC7);
alles andere ist ein Stub (Rückgabe 0) und wird mitgeschrieben – Zeichenfunktionen werden so mit ihren Argumenten
protokolliert. Maus: `hauptmenue.py` ersetzt die Ereignisfunktion 0x0C068 (DGROUP 0x5A16 = 1 links, 3 rechts; Lage über
den Zeiger DGROUP 0xC147, +0 = y, +2 = x). Grenzen: Der Interpreter rechnet nur, was die Skripte vorbereiten; Stubs
liefern 0, Bilddateien werden nicht geladen. Eine Lesart gilt hier erst, wenn der Ablauf mit Ausgabe belegt ist.

## Teil 1 – Bedienung/Anzeige

### B12 – Rechte Maustaste im Hauptmenü (A296)

`python3 tools/reverse-engineering/abweichungen/b12_hauptmenue_rechtsklick.py` (Hauptmenü Datei 0x44E22, Schleife
0x4535B–0x4592F, je Zeile ein frischer Lauf; „Ende“ = Rückkehr über 0x4592F, [bp-7] = auslösende Taste, 0 = Bildschirm
geöffnet):

```
Taste  Klick                              Ergebnis [bp-7] Auswahl 0x1DB Bildschirme
links  Logo (Tag weiter)                  Ende     1      -           0 -
rechts Logo (Tag weiter)                  Ende     3      -           0 -
links  Kachel 1                           Schleife -      0x0         0 -
rechts Kachel 1                           Schleife -      0x0         0 -
links  Kachel 1 erneut                    Schleife -      0x0         0 -
rechts Kachel 1 erneut                    Schleife -      0x0         0 -
links  Eintrag 1 nach Kachel 1            Ende     0      -           0 0x58a57 (Ov 0x138)
rechts Eintrag 1 nach Kachel 1            Ende     0      -           0 0x58a57 (Ov 0x138)
links  Leere Fläche nach Kachel 1         Schleife -      0x0         0 -
rechts Leere Fläche nach Kachel 1         Schleife -      0x0         0 -
links  Kopfzeile y < 0x15 (Credits)       Schleife -      0xffff      0 0x399d0 (Ov 0xf8)
rechts Kopfzeile y < 0x15 (Credits)       Schleife -      0xffff      0 0x399d0 (Ov 0xf8)
links  Fläche x 0xE9–0x10F, y 0x22–0x47   Schleife -      0xffff      0 0x4b5f4 (Ov 0x120, Meldungen)
rechts Fläche x 0xE9–0x10F, y 0x22–0x47   Schleife -      0xffff      0 0x4b5f4 (Ov 0x120, Meldungen)
links  Gegnername x 0x8A–0xE6 (0xE1A6 = 5) Schleife -      0xffff      0 0x4d385 (Ov 0x128)
rechts Gegnername x 0x8A–0xE6 (0xE1A6 = 5) Schleife -      0xffff      0 -
links  Feld x 0x13E, y 0xEF               Ende     1      -           1 -
rechts Feld x 0x13E, y 0xEF               Ende     1      -           2 -
```

- **Urteil: Abweichung bestätigt.** Die rechte Taste wirkt im Original wie die linke: Logo → Tag weiter, Kachel → Auswahl,
  Eintrag → Bildschirm, Titelzeile → Credits, Fläche x 0xE9–0x10F → Nachrichtenleiste. Es gibt **kein Abwählen**: Ein
  Rechtsklick auf eine leere Fläche lässt die Auswahl stehen. Nur die linke Taste: Gegner-/Vereinsname (Datei 0x45375–0x453E5,
  Vereinsfenster 0x4D385) und das Infofile-Feld (319 | 0). Nebenbefund: Feld (0x13E | 0xEF) setzt DGROUP 0x1DB = 1 (links)
  bzw. 2 (rechts) und verlässt das Menü wie „Tag weiter“ (Datei 0x45407–0x4544B; 0x1DB = „Durchlauf ohne Halt“, im Remake
  nicht geführt).
- **Vorgabe (`bedienung`):** In `MainMenuScreen.HandleInput` den Zweig „Escape oder Rechtsklick → abwählen“ für den
  Rechtsklick entfernen; Logo, Kacheln, Einträge und Nachrichtenleiste nehmen `LeftClick || RightClick` an, Gegner-/
  Vereinsname und Infofile nur links. Roter Test: Rechtsklick aufs Logo → Tag geht weiter (wie Linksklick); Rechtsklick auf
  Kachel 1 → Auswahl 0; Rechtsklick auf leere Fläche bei Auswahl 0 → Auswahl bleibt 0. Escape (Tastatur) ist nicht Teil
  von B12.

### B13 – Spielerinfo per Rechtsklick in der Nationalaufstellung (A757)

`python3 tools/reverse-engineering/abweichungen/b13_nationalaufstellung_rechtsklick.py` (Aufstellung Datei 0x40364, je
y ein Rechtsklick bei x 0x3C):

```
Verein (Manager 0)         Rechtsklick x 0x3C → Spielerinfo 0x041AC: Platz 0: y 24–34, Platz 1: y 35–40, Platz 2: y 41–46, Platz 3: y 47–52, Platz 4: y 53–58
Nationalteam (Manager 5)   Rechtsklick x 0x3C → Spielerinfo 0x041AC: Platz 0: y 24–34, Platz 1: y 35–40, Platz 2: y 41–46, Platz 3: y 47–52, Platz 4: y 53–58
```

- **Urteil: Abweichung bestätigt.** Vereins- und Nationalaufstellung sind dieselbe Funktion; die Abfrage Datei
  0x4084E–0x40872 kennt keinen Sonderfall für Manager 5, und 0x041AC liest den Kaderplatz des Managers am Zug (DGROUP 0xA70E,
  Datei 0x041B4–0x041CD) – beim Bundestrainer den WM-Kader.
- **Vorgabe (`aufstellung`):** Rechtsklick in die Namensspalte (0x22 < x < 0x60) über Zeile k (k < Zahl der Kaderplätze)
  öffnet die Spielerinfo des WM-Kaderplatzes k, wie `LineupScreen` (Zeilen aus der Hinweiszeilen-Rechnung 0x41683).
  Escape bleibt.

### B15/B16 – Gegner- und Vereinsfenster (A286–A290, A495, A753)

`python3 tools/reverse-engineering/abweichungen/b15_gegnerfenster.py`:

```
1. Gesicht 0x51AAF (Zeichen +7…+14, das jüngste zuletzt)
   --------: 0 Zeichen, Gesicht bei (249 | 89): gelb (Unentschieden)
   s-------: 1 Zeichen, Gesicht bei (249 | 89): grün (Sieg)
   ss------: 2 Zeichen, Gesicht bei (249 | 89): grün (Sieg)
   sss-----: 3 Zeichen, Gesicht bei (249 | 89): grün (Sieg)
   n-------: 1 Zeichen, Gesicht bei (249 | 89): rot (Niederlage)
   nn------: 2 Zeichen, Gesicht bei (249 | 89): rot (Niederlage)
   uuuuuuuu: 8 Zeichen, Gesicht bei (249 | 89): gelb (Unentschieden)
   snsnsnsn: 8 Zeichen, Gesicht bei (249 | 89): gelb (Unentschieden)
   ssssnnnn: 8 Zeichen, Gesicht bei (249 | 89): gelb (Unentschieden)
   nnnnnsss: 8 Zeichen, Gesicht bei (249 | 89): rot (Niederlage)
   nnnnnnnn: 8 Zeichen, Gesicht bei (249 | 89): rot (Niederlage)
   ssssssss: 8 Zeichen, Gesicht bei (249 | 89): grün (Sieg)
2. Tore / Spiel 0x2ECA0 (Seite 2 = Gesamt) gegen TeamInfoWindow.Average
   Spiele   0 Tore   0:  0 → Original „0.0:0.0“, Remake „0.0:0.0“
   Spiele   1 Tore   2:  3 → Original „2.0:3.0“, Remake „2.0:3.0“
   Spiele   2 Tore   0:  1 → Original „0.0:0.5“, Remake „0.0:0.5“
   Spiele   9 Tore   6:  6 → Original „0.6:0.6“, Remake „0.6:0.6“
   Spiele 120 Tore   3:  7 → Original „0.0:0.0“, Remake „0.0:0.0“
   Abweichungen: 0 von 205
3. Ergebnis 0x50283(52, 53, 3, 1): geänderte Bytes der Teamsätze je Wettbewerbscode 0xD0C2
   Code  0: nichts
   Code  1: nichts
   Code  2: nichts
   Code  3: nichts
   Code  4: nichts
   Code  5: nichts
   Code  6: nichts
   Code  7: H+0x03=2, H+0x0e=115, H+0x19=3, H+0x1d=1, H+0x21=1, H+0x29=1, G+0x0e=78, G+0x1a=1, G+0x1e=3, G+0x22=1, G+0x2e=1
   Code  8: H+0x03=2, H+0x0e=115, H+0x19=3, H+0x1d=1, H+0x21=1, H+0x29=1, G+0x0e=78, G+0x1a=1, G+0x1e=3, G+0x22=1, G+0x2e=1
   Code  9: nichts
   Code 10: nichts
   Code 11: nichts
   Code 12: nichts
   Code 13: nichts
4. Fenster 0x4D385(52, 2, 0, 0): Zeile „Historische Ergebnisse“ und dritter Knopf je Code 0xD0C2
   Code  0: Texte ab y 167: 2 [(98, 167), (98, 173)], Knöpfe [(103, 215), (243, 215)]
   Code  7: Texte ab y 167: 0 [], Knöpfe [(103, 215)]
   Code  8: Texte ab y 167: 0 [], Knöpfe [(103, 215)]
   Code 13: Texte ab y 167: 0 [], Knöpfe [(103, 215)]
```

- **Gesicht (A287): Abweichung bestätigt.** „0x110:0x5C“ ist die Tabellenkurve 0x445E0 (Datei 0x4D588; für Länder ohne
  Wirkung, siehe B18), **nicht** das Gesicht. Das Gesicht zeichnet 0x51AAF (Datei 0x4D59E–0x4D5C6) bei (249 | 89) aus den acht
  Bilanzzeichen des Teamsatzes (DGROUP 0x825E + 0x50 · Team): Stimmung = 2 + Siege − Niederlagen (Zeichen s/S bzw. n/N,
  DGROUP 0x7CE–0x7D3), **erst nach** der Summe auf 1 … 3 begrenzt; Bild wie das Zeichen: 1 rot, 2 gelb, 3 grün. Das Remake
  zeigt immer gelb. Roter Test: Bilanz „s-------“ → grün, „nnnnnsss“ → rot, „ssssnnnn“ → gelb.
- **Tore / Spiel (A288) und Schnitt (A290): keine Abweichung.** 0x2ECA0 formatiert wie `TeamInfoWindow.Average`
  (Zehntel abgeschnitten, 0 Spiele → 0.0); 205 Fälle gleich. Quelle im Original: Teamsatz +0x21/+0x22 Spiele, +0x19/+0x1A
  Tore, +0x1D/+0x1E Gegentore (Heim/Auswärts, Seite 2 = Summe).
- **Bilanz und Teamsatz (A495, A753; B16): keine Abweichung in der Quelle.** 0x50283 schreibt den Teamsatz (Bilanzzeichen,
  Spiele, Tore, Punkte) nur bei Wettbewerbscode 7 (Liga) und 8 (WM-Gruppe); bei Pokal, Play-off und Freundschaftsspiel
  bleibt er unverändert. Das Remake rechnet Vereine aus den Ligaergebnissen und Länder aus den WM-Gruppenspielen – dieselbe
  Menge. Offen bleibt nur, wann das Original den Teamsatz leert (nicht nachgebildet; Saisonwechsel).
- **Zeile „Historische Ergebnisse“ und dritter Knopf (A286): Abweichung bestätigt.** Nur bei Code DGROUP 0xD0C2 = 0
  (Datei 0x4D7AA, 0x4E07A); bei 7, 8 und 13 fehlen Zeile, Liste und Knopf. Code 0 setzt das Hauptmenü (Datei 0x44E32); ≠ 0
  nur aus dem Spieltagsablauf bzw. nach dem Freundschaftsspiel (0xD, Datei 0x30390) bis zum nächsten Hauptmenü.
- **Vorgabe (`wettbewerbe-anzeige`):** Gesicht nach der Regel oben (Bilanzzeichen braucht das Remake dafür je Team – für
  Länder liegen sie als `NationalForm` vor; für Vereine siehe B29). Zeile und dritter Knopf nur, wenn das Fenster nicht aus
  einem Spieltags- bzw. Freundschaftsspielablauf geöffnet wird. A288/A290 auf `[ORIGINAL]` mit Fundstelle 0x2ECA0.

### B17 – Reihenwerte der Nationalteam-Anzeige (A285, A419, A961)

`python3 tools/reverse-engineering/abweichungen/b17_reihenwerte_national.py` (0x977F wie in Datei 0x3B65C, Anzeige
(Byte 2 + Byte 1) / 2 wie Datei 0x3B664–0x3B691):

```
0x977F(Team, Reihe, Teil, 0, 0) ohne Bundestrainer, 300 Zufallsfälle
   Team 58 Reihe 0 Sturm: Felder K 107 T   1 F  39 → 0x977F K 100 T   5 F  43, angezeigt  52; Remake (100, 5, 43) →  52 gleich
   Team 52 Reihe 2 Abwehr: Felder K  39 T  54 F  75 → 0x977F K  35 T  50 F  71, angezeigt  42; Remake (35, 50, 71) →  42 gleich
   Team 60 Reihe 2 Sturm: Felder K  79 T 105 F  70 → 0x977F K  75 T 100 F  66, angezeigt  87; Remake (75, 100, 66) →  87 gleich
   Team 60 Reihe 1 Sturm: Felder K 106 T  86 F  76 → 0x977F K 100 T  86 F  76, angezeigt  93; Remake (100, 86, 76) →  93 gleich
   Team 55 Reihe 2 Abwehr: Felder K  86 T  79 F   9 → 0x977F K  82 T  75 F   5, angezeigt  78; Remake (82, 75, 5) →  78 gleich
   Team 60 Reihe 2 Abwehr: Felder K  81 T  93 F  69 → 0x977F K  77 T  89 F  65, angezeigt  83; Remake (77, 89, 65) →  83 gleich
   Team 58 Reihe 1 Abwehr: Felder K  71 T  55 F  63 → 0x977F K  71 T  55 F  63, angezeigt  63; Remake (71, 55, 63) →  63 gleich
   Team 59 Reihe 2 Sturm: Felder K  21 T  52 F 104 → 0x977F K  17 T  48 F 100, angezeigt  32; Remake (17, 48, 100) →  32 gleich
   Abweichungen zur Regel des Remakes: 0 von 300
```

- **Urteil:** `GameState.NationalPanelLineValue` trifft 0x977F für Länder ohne Bundestrainer (300 von 300). Die Anzeige
  nimmt im Remake aber `NationalWidgets.LineValue` ((K + T) / 2 der Spieler) – **Abweichung bestätigt**.
- **Vorgabe (`wettbewerbe-anzeige`):** `NationalWidgets.LineValue` → `GameState.NationalPanelLineValue(nation, line,
  attack)`. Erwartete Werte für den roten Test (Teamfelder ohne Bundestrainer): Abwehr K 39 / T 54, Reihe 2 → 42; Sturm
  K 79 / T 105, Reihe 2 → 87. Land des Bundestrainers (Feld +0x16 ≠ 0 → Spielerweg 0x98F8): hier nicht nachgebildet, bleibt
  bei `MatchEngine.ManagedLineValues` (A494, Runde 13).

### B18 – Hauptmenüaufbau (A297, A301, A302, A331)

`python3 tools/reverse-engineering/abweichungen/b18_hauptmenue_aufbau.py`:

```
1. Markierung der großen Kachel (Umriss 0x0B5A7: x1, y1, x2, y2, Farbe)
   Kachel 1: (12|84)-(75|132) Farbe 24
   Kachel 2: (89|84)-(152|132) Farbe 24
   Kachel 3: (166|84)-(229|132) Farbe 24
   Kachel 4: (243|84)-(306|132) Farbe 24
   Kachel 5: (12|144)-(75|192) Farbe 24
   Kachel 6: (89|144)-(152|192) Farbe 24
   Kachel 7: (166|144)-(229|192) Farbe 24
   Kachel 1, dann Kachel 2: (12|84)-(75|132) Farbe 30; (89|84)-(152|132) Farbe 24
2. Klick ins Leistenfeld neben dem Kopf (250 | 40) bei gewählter Kachel 1
   Kachel 1     Schleife Auswahl 0x0     
   Leistenfeld  Schleife Auswahl 0x0    Leiste 0x4B5F4 
   Eintrag 1    Ende     Auswahl -       0x58a57
3. Tabellenkurve 0x445E0(x 3, y -2, eigen, Gegner, …): Zeichenaufrufe
   Verein 0 gegen Verein 1           15 Aufrufe {'Linie': 2, 'Farbe': 2, 'Stift': 2, 'Linie2': 8, 'Text': 1}
   Nationalteam 52 gegen Verein 1     0 Aufrufe {}
   Verein 0 gegen Nationalteam 53     0 Aufrufe {}
   Nationalteam gegen Nationalteam    0 Aufrufe {}
   Infoleiste Manager 5: 0x445E0(x 3, y -2, eigen 52, Gegner 255, …)
4. Lampen 0x42996 bei leerem Kader (rot/grün je Lampe 0–5)
   Manager 5: 0:rot 1:grün 2:grün 3:grün 4:grün 5:grün
   Manager 0: 0:rot 1:grün 2:grün 3:grün 4:rot 5:grün
```

- **Markierung (A301): Abweichung bestätigt.** Umriss 0x0B5A7 (alle vier Seiten, Ecken eingeschlossen) von (X + 4 | Y + 4)
  bis (X + 67 | Y + 52) der großen Kachel (X = 8 + 77 · (i % 4), Y = 80 + 60 · (i / 4)) in Farbe 24; die abgewählte Kachel
  bekommt denselben Umriss in Farbe 30 (Datei 0x45579–0x45659). Das Remake zieht nur die Unterkante.
- **Feld neben dem Kopf (A297): Abweichung bestätigt.** Klick in x 0xE9–0x10F zeigt die Nachrichtenleiste, die Auswahl
  bleibt, und ein Eintrag der Kachelreihe öffnet danach weiter seinen Bildschirm. Das Remake wählt ab.
- **Kurve des Bundestrainers (A302): keine Abweichung.** 0x445E0 zeichnet nichts, sobald einer der beiden ein Land ist
  (Index ≥ DGROUP 0x6AD); die Infoleiste übergibt beim Bundestrainer das Land 52 → leeres Feld, auch ohne Achsen.
- **Lampe 0 (A331): keine Abweichung.** Mit leerem WM-Kader ist Lampe 0 rot, 2–5 grün (Datei 0x42C4B). Ein Menü mit Zug 5
  vor der Ernennung gibt es im Original nicht (Datei 0x5557F–0x55587, [ORIGINAL] aus Runde 13, hier nicht neu nachgebildet).
- **Vorgabe (`bedienung`):** Markierung als Umriss wie oben; Klick ins Feld x 233–271 / y 34–71 ohne Abwählen. A302 und
  A331: nur Marker mit Beleg nachziehen.

### B28 – Balken der Historie (A356, A805, A806)

`python3 tools/reverse-engineering/abweichungen/b28_historie_balken.py` (Bildschirm Datei 0x2C1EE, Füllungen 0x0B620):

```
Liga (Byte +0x18; 0x80 = Meister-Kennzeichen, 0x33–0x35 Play-off)
   Byte 0x01 (  1): y 148–172 Farbe 30 (25 px) + y 124–148 Farbe 30 (25 px) + y 101–124 Farbe 30 (24 px)
   Byte 0x02 (  2): y 148–172 Farbe 29 (25 px) + y 124–148 Farbe 22 (25 px) + y 103–124 Farbe 23 (22 px)
   Byte 0x06 (  6): y 148–172 Farbe 29 (25 px) + y 124–148 Farbe 22 (25 px) + y 111–124 Farbe 23 (14 px)
   Byte 0x0c ( 12): y 148–172 Farbe 29 (25 px) + y 124–148 Farbe 22 (25 px) + y 123–124 Farbe 23 (2 px)
   Byte 0x0d ( 13): y 148–172 Farbe 29 (25 px) + y 125–148 Farbe 22 (24 px)
   Byte 0x12 ( 18): y 148–172 Farbe 29 (25 px) + y 135–148 Farbe 22 (14 px)
   Byte 0x18 ( 24): y 148–172 Farbe 29 (25 px) + y 147–148 Farbe 22 (2 px)
   Byte 0x19 ( 25): y 149–172 Farbe 29 (24 px)
   Byte 0x1e ( 30): y 159–172 Farbe 29 (14 px)
   Byte 0x24 ( 36): y 171–172 Farbe 29 (2 px)
   Byte 0x33 ( 51): y 148–172 Farbe 29 (25 px) + y 124–148 Farbe 22 (25 px) + y 111–124 Farbe 23 (14 px)
   Byte 0x34 ( 52): y 148–172 Farbe 29 (25 px) + y 124–148 Farbe 22 (25 px) + y 107–124 Farbe 23 (18 px)
   Byte 0x35 ( 53): y 148–172 Farbe 29 (25 px) + y 124–148 Farbe 22 (25 px) + y 103–124 Farbe 23 (22 px)
   Byte 0x80 (128): y 148–172 Farbe 30 (25 px) + y 124–148 Farbe 30 (25 px) + y 101–124 Farbe 30 (24 px)
   Byte 0x81 (129): y 148–172 Farbe 30 (25 px) + y 124–148 Farbe 30 (25 px) + y 101–124 Farbe 30 (24 px)
DEB-Pokal (Byte +0x1A; 0x80 = Sieger)
   Byte 0x00 (  0): y 76–75 Farbe 27 (y1 > y2)
   Byte 0x01 (  1): y 73–75 Farbe 27 (3 px)
   Byte 0x02 (  2): y 70–75 Farbe 27 (6 px)
   Byte 0x03 (  3): y 67–75 Farbe 27 (9 px)
   Byte 0x04 (  4): y 64–75 Farbe 27 (12 px)
   Byte 0x05 (  5): y 61–75 Farbe 27 (15 px)
   Byte 0x06 (  6): y 58–75 Farbe 27 (18 px)
   Byte 0x80 (128): y 58–75 Farbe 30 (18 px)
   Byte 0x81 (129): y 58–75 Farbe 30 (18 px)
Europacup (Byte +0x1B; Bit 3–6 = im Wettbewerb, Bit 0–2 = Runde, 0x80 = Sieger)
   Byte 0x00 (  0): kein Balken
   Byte 0x01 (  1): kein Balken
   Byte 0x08 (  8): y 30–29 Farbe 27 (y1 > y2)
   Byte 0x09 (  9): y 27–29 Farbe 27 (3 px)
   Byte 0x0a ( 10): y 24–29 Farbe 27 (6 px)
   Byte 0x0b ( 11): y 21–29 Farbe 27 (9 px)
   Byte 0x0c ( 12): y 18–29 Farbe 27 (12 px)
   Byte 0x10 ( 16): y 30–29 Farbe 27 (y1 > y2)
   Byte 0x88 (136): y 12–29 Farbe 30 (18 px)
   Byte 0x80 (128): kein Balken
```

- **Urteil: Abweichung bestätigt.** Liga: Byte v = Platz + 12 · Liga (Datei 0x55C24–0x55C52 schreibt Platz und addiert
  DGROUP 0x7C0 + Liga; 0x33/0x34/0x35 = Play-off → 6/4/2; Bit 7 → 1). Drei gestapelte Blöcke zu je 12 Plätzen (je 2 Pixel)
  mit eigener Farbe: unten 29, Mitte 22, oben 23, Gesamthöhe 2 · (37 − v) zuzüglich der doppelt gezeichneten Blockgrenzen;
  v = 1 (Bit 7 gesetzt oder erster Gesamtplatz) → alle drei Blöcke voll in Farbe 30. Das Remake zeichnet einen Balken in
  Farbe 29, Meister in Farbe 7. DEB-Pokal: 3 · Byte Pixel in Farbe 27 (Byte = erreichte Runde, Datei 0x55C87–0x55CC4), Sieger
  (Bit 7) 18 Pixel in Farbe 30. Europacup: nur wenn Bit 3–6 ≠ 0, Höhe 3 · (Byte & 7), Farbe 27 (A805: **keine**
  Abweichung der Farbe), Sieger 18 Pixel Farbe 30.
- **Grenzen des Belegs:** Bedeutung der Bytes aus dem Schreiber Datei 0x55C0E–0x55DA0 nur gelesen, nicht nachgebildet;
  Byte 0 bzw. Runde 0 erzeugt eine umgekehrte Füllung (y1 = y2 + 1), deren Wirkung 0x0B620 nicht nachgebildet ist.
- **Vorgabe (`statistik-anzeige`):** `HistoryScreen.BarHeight/BarColor` nach der Tabelle oben; Erwartung für den roten
  Test: Liga-Byte 6 → Blöcke 25/25/14 Pixel in 29/22/23; Byte 0x80 → 25/25/24 Pixel in 30; DEB Runde 2 → 6 Pixel Farbe 27.

### B29 – Stärken-Bildschirm (A361, A699, A700)

`python3 tools/reverse-engineering/abweichungen/b29_staerken.py` (Datei 0x51206, Liga 0):

```
1. Zeichen und Gesicht je Zeile (Zeile n: y = 36 + 15 n)
   Zeile 0 (ssss----): Zeichen 80:grün 88:grün 96:grün 104:grün; Gesicht bei x 240: grün
   Zeile 1 (nnn-----): Zeichen 80:rot 88:rot 96:rot; Gesicht bei x 240: rot
   Zeile 2 (suusnnnu): Zeichen 80:grün 88:gelb 96:gelb 104:grün 112:rot 120:rot 128:rot 136:gelb; Gesicht bei x 240: rot
   Zeile 3 (--------): Zeichen -; Gesicht bei x 240: gelb
   Zeile 4 (nnnnnnss): Zeichen 80:rot 88:rot 96:rot 104:rot 112:rot 120:rot 128:grün 136:grün; Gesicht bei x 240: rot
   Zeile 5 (ssssssss): Zeichen 80:grün 88:grün 96:grün 104:grün 112:grün 120:grün 128:grün 136:grün; Gesicht bei x 240: grün
   Zeile 6 (ssss----): Zeichen 80:grün 88:grün 96:grün 104:grün; Gesicht bei x 240: grün
   Zeile 7 (nnn-----): Zeichen 80:rot 88:rot 96:rot; Gesicht bei x 240: rot
   Zeile 8 (suusnnnu): Zeichen 80:grün 88:gelb 96:gelb 104:grün 112:rot 120:rot 128:rot 136:gelb; Gesicht bei x 240: rot
   Zeile 9 (--------): Zeichen -; Gesicht bei x 240: gelb
   Zeile 10 (nnnnnnss): Zeichen 80:rot 88:rot 96:rot 104:rot 112:rot 120:rot 128:grün 136:grün; Gesicht bei x 240: rot
   Zeile 11 (ssssssss): Zeichen 80:grün 88:grün 96:grün 104:grün 112:grün 120:grün 128:grün 136:grün; Gesicht bei x 240: grün
2. Aufrufe 0x952C(Manager, 0) beim Öffnen: [(0, 0), (1, 0), (5, 0)]
```

- **Smiley (A361): Abweichung bestätigt, aber anders als vermutet.** Kein „Wertverlauf“: 0x51206 ruft dieselbe Funktion
  0x51AAF wie das Gegnerfenster mit dem Teamsatz des Vereins (Datei 0x518AB–0x518ED). Sie zeichnet die **acht
  Bilanzzeichen** bei x = 80 + 8 i, y = 36 + 15 n (unter dem Namen) und das Gesicht bei x 240 nach der Regel aus B15.
  Das Remake zeichnet die Zeichen gar nicht und das Gesicht immer gelb. Video `folge08_01734` („STÄRKEN Zweite Liga –
  Abwehr“) zeigt beides. Benötigtes Feld: die Bilanzzeichen je Verein (DGROUP 0x825E, acht Zeichen, geschoben von 0x4F4F4;
  nur Liga und WM-Gruppe, siehe B15) – das ist die Migration der Spur.
- **Managervereine (A699): keine Abweichung.** Beim Öffnen ruft 0x51206 0x952C(Manager, 0) für alle Manager und den
  Bundestrainer (Datei 0x51217–0x5123A) – die Werte kommen also aus der aktuellen Aufstellung, wie im Remake.
- **Vor dem ersten Spiel (A700): Abweichung bestätigt (Bild, Lesart).** Der Lader 0x2EF1B läuft beim Programmstart vor der
  Spielvorbereitung (Datei 0x439BB vor 0x43A61, gelesen, nicht nachgebildet); DOSBox `nur-weltkugel/27` zeigt vor dem ersten
  WM-Spiel Form 46 … 54 statt 50.
- **Vorgabe (`statistik-anzeige`):** Bilanzzeichen je Verein führen (Schema 64), Zeichen und Gesicht wie oben; Teamwerte vor
  dem ersten Spiel aus dem Lader statt Streuung 0/Form 50.

### C07 – Pokal- und Play-off-Marken, Klick auf Verein (A098, A277, A435, A482, A759, A924, A925)

`python3 tools/reverse-engineering/abweichungen/c07_marken.py` (Minutenweg wie im Spieltag 0x36B5D, Datei 0x37176–0x37295:
0x32AA3 mit Schalter 0 nach Minute 60, Verlängerung 0x37A62(61–65) und 0x37A62(66–70), 0x32AA3 mit Schalter 1 und
Penaltyschießen 0x3901B; Chancenwurf 0x09F5C und Schuss 0xA484 gesetzt, Würfe random(2, 5) des Penaltyschießens ohne
Zuschauer vorgegeben):

```
Minutenweg nach Minute 60 (Stand 3:3 bzw. 4:2), Verlängerung 61–70, Chance = Tor
  DEB-Pokal (Zeile 0), 3:3, Heimtor Minute 63
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 34, Gast 3
     nach 0x37A62(66–70): Heim 34, Gast 3
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 0: Heim 34, Gast 3
     gewertete Spiele 0xA928/0xA918: [(34, 3)]
     → Endstand 4:3, Marken 1
  DEB-Pokal (Zeile 0), 3:3, Gasttor Minute 68
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 33, Gast 3
     nach 0x37A62(66–70): Heim 33, Gast 4
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 0: Heim 33, Gast 4
     gewertete Spiele 0xA928/0xA918: [(33, 4)]
     → Endstand 3:4, Marken 1
  DEB-Pokal (Zeile 0), 3:3, kein Tor
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 33, Gast 3
     nach 0x37A62(66–70): Heim 33, Gast 3
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 1: Heim 66, Gast 5
     0x3901B(Zeile, Paarung) gerufen bei Stand Heim/Gast (63, 3); Würfe random(2, 5) Heim/Gast: [3, 2]
     → Endstand 6:5, Marken 2
  DEB-Pokal (Zeile 0), 4:2 nach 60
     nach 0x32AA3(Schalter 0) Rückgabe 0: Heim 4, Gast 2
     → Endstand 4:2, Marken 0
  Play-off (Zeile 2), 3:3, Heimtor Minute 63
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 34, Gast 3
     nach 0x37A62(66–70): Heim 34, Gast 3
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 0: Heim 34, Gast 3
     gewertete Spiele 0xA928/0xA918: [(34, 3)]
     → Endstand 4:3, Marken 1
  Play-off (Zeile 2), 3:3, Gasttor Minute 68
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 33, Gast 3
     nach 0x37A62(66–70): Heim 33, Gast 4
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 0: Heim 33, Gast 4
     gewertete Spiele 0xA928/0xA918: [(33, 4)]
     → Endstand 3:4, Marken 1
  Play-off (Zeile 2), 3:3, kein Tor
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 33, Gast 3
     nach 0x37A62(66–70): Heim 33, Gast 3
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 1: Heim 66, Gast 5
     0x3901B(Zeile, Paarung) gerufen bei Stand Heim/Gast (63, 3); Würfe random(2, 5) Heim/Gast: [3, 2]
     → Endstand 6:5, Marken 2
  Play-off (Zeile 2), 4:2 nach 60
     nach 0x32AA3(Schalter 0) Rückgabe 0: Heim 4, Gast 2
     → Endstand 4:2, Marken 0
  WM-Endrunde (Zeile 3), 3:3, Heimtor Minute 63
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 34, Gast 3
     nach 0x37A62(66–70): Heim 34, Gast 3
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 0: Heim 34, Gast 3
     gewertete Spiele 0xA928/0xA918: [(34, 3)]
     → Endstand 4:3, Marken 1
  WM-Endrunde (Zeile 3), 3:3, Gasttor Minute 68
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 33, Gast 3
     nach 0x37A62(66–70): Heim 33, Gast 4
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 0: Heim 33, Gast 4
     gewertete Spiele 0xA928/0xA918: [(33, 4)]
     → Endstand 3:4, Marken 1
  WM-Endrunde (Zeile 3), 3:3, kein Tor
     nach 0x32AA3(Schalter 0) Rückgabe 1: Heim 33, Gast 3
     nach 0x37A62(61–65): Heim 33, Gast 3
     nach 0x37A62(66–70): Heim 33, Gast 3
     nach 0x32AA3(Schalter 1) mit Penaltyschießen 0x3901B, Rückgabe 1: Heim 66, Gast 5
     0x3901B(Zeile, Paarung) gerufen bei Stand Heim/Gast (63, 3); Würfe random(2, 5) Heim/Gast: [3, 2]
     → Endstand 6:5, Marken 2
  WM-Endrunde (Zeile 3), 4:2 nach 60
     nach 0x32AA3(Schalter 0) Rückgabe 0: Heim 4, Gast 2
     → Endstand 4:2, Marken 0
```

- **Urteil: Abweichung bestätigt – Marken vertauscht.** Minutenweg: Tor in der Verlängerung → **eine** Marke, Penaltyschießen
  → **zwei** Marken, in DEB-Pokal (Zeile 0), Play-offs (Zeile 2) und WM-Endrunde (Zeile 3) gleich. Ablauf: 0x32AA3 setzt
  nach Minute 60 bei Gleichstand Heim + 30 (DEB Datei 0x32B9D, Zeilen 2/3 Datei 0x32D72); die Verlängerung liest den Stand
  im Pokalzweig (Datei 0x380D0 ff., nur bei Pokal ≠ 0 – der Zweig mit DGROUP 0xA0BF, Datei 0x37E2D, ist die Liga) und zieht
  die Marken ab (Datei 0x381FF–0x3824B); ein Tor schreibt 0x36AAA(Stand ohne Marken + Tor, Heim + 30) als gewertetes Spiel
  (Datei 0x38402, Byte 0x5A, Liste DGROUP 0xA928/0xA918), am Ende jedes Abschnitts zurück in die Zeile (0x369C7(2, 1),
  Datei 0x38616). Nach der Verlängerung setzt 0x32AA3 bei noch gleichem Stand (Heim − 30 = Gast) die zweite Marke; 0x3901B
  nimmt eine ab (Datei 0x39088), setzt die Bytes auf die Treffer und schreibt Heim = Stand mit einer Marke + Treffer + 30
  (Datei 0x39770–0x397B3) – Endstand = Gleichstand + Treffer mit zwei Marken. Das Remake macht es umgekehrt:
  `CupMatch.FromReport` 2 Marken nach Verlängerungstor, 1 nach Penaltyschießen; `Playoffs.MarksFromReport` 2 in beiden
  Fällen. Das Video `eisbaeren_f44_01330` („4: 3..“ neben „4: 3.“, WM-Endrunde) widerspricht nicht: zwei Marken stammen dort
  aus einem Penaltyschießen bzw. dem schnellen Weg, nicht aus einem Verlängerungstor.
- **Schneller Weg (A925, Lesart, nicht nachgebildet):** Datei 0x376E1 ruft 0x32AA3 vor dem Zusatzwurf; für Zeile 3 setzt
  0x32AA3 bei Gleichstand dieselbe Marke (Datei 0x32C8E–0x32D84) und 0x3775D die zweite – auch die WM-Endrunde hat im
  schnellen Weg zwei Marken (`Cups.Tiebreak.cs` nennt eine, „Zeile 3 in 0x32AA3 nicht gelesen“). Der Zusatzwurf der ersten
  offenen Paarung bleibt wie in `Playoffs.cs` (A925) beschrieben.

`python3 tools/reverse-engineering/abweichungen/c07_klick.py` (Bildschirm 0x32E23 mit Paarungen in DGROUP 0x5CFB,
Heim 10 + k, Gast 40 + k; je Fall ein Klick, danach Rechtsklick):

```
DEB-Pokalspiele (Art 0): Kopfzeile bis y 30, Zeilen (Grundlinie) [73, 88, 95, 102, 109, 139, 146, 153, 160, 167, 174, 181, 188, 195, 202, 209]
   links, Name links, Zeile 0       → Fenster 0x4D385(Verein 23, Seite 0); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Name rechts, Zeile 0      → Fenster 0x4D385(Verein 53, Seite 1); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Name links, Zeile 3       → Fenster 0x4D385(Verein 24, Seite 0); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Name rechts, Zeile 3      → Fenster 0x4D385(Verein 54, Seite 1); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Zeile 0, Grundlinie + 1   → kein Fenster; Bildschirm bleibt, Rechtsklick danach: zurück
   links, Kopfzeile                 → kein Fenster; Bildschirm bleibt, Rechtsklick danach: zurück
   rechts, Name links, Zeile 0      → kein Fenster; Bildschirm zurück
Play-Off Spiele (Art 2): Kopfzeile bis y 28, Zeilen (Grundlinie) [55, 62, 69, 76, 90, 97, 111, 118, 125, 132, 139, 146, 153, 160, 167, 174]
   links, Name links, Zeile 0       → Fenster 0x4D385(Verein 10, Seite 0); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Name rechts, Zeile 0      → Fenster 0x4D385(Verein 40, Seite 1); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Name links, Zeile 3       → Fenster 0x4D385(Verein 13, Seite 0); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Name rechts, Zeile 3      → Fenster 0x4D385(Verein 43, Seite 1); Bildschirm bleibt, Rechtsklick danach: zurück
   links, Zeile 0, Grundlinie + 1   → kein Fenster; Bildschirm bleibt, Rechtsklick danach: zurück
   links, Kopfzeile                 → kein Fenster; Bildschirm bleibt, Rechtsklick danach: zurück
   rechts, Name links, Zeile 0      → kein Fenster; Bildschirm zurück
```

- **Klick auf einen Verein (A277): Abweichung bestätigt.** Linksklick unterhalb der Kopfzeile (y > Kopf + 10) auf eine
  gezeichnete Zeile (Grundlinie − 6 < y ≤ Grundlinie, Datei 0x33AE4–0x33B03) öffnet das Vereinsfenster 0x4D385(Verein, Seite,
  0, 0): x ≤ 0x63 linker Verein (Seite 0), sonst rechter (Seite 1; Art 3 = WM: Seite 2, Datei 0x33AD9). Der Bildschirm
  bleibt danach stehen; nur der Rechtsklick (DGROUP 0x5A16 = 3) verlässt ihn (Datei 0x33B33), ein Linksklick daneben oder in
  die Kopfzeile tut nichts. Das Remake: `DebCupGamesScreen` verlässt bei jedem Klick, `WorldCupKnockoutScreen` bei
  Linksklick; `PlayoffGamesScreen` öffnet schon ein Vereinsfenster.
- **Vorgabe (`wettbewerbe-anzeige`):** Marken Minutenweg: Verlängerungstor 1, Penaltyschießen 2 (`CupMatch.FromReport`,
  `Playoffs.MarksFromReport`, `MatchReport.Overtime`); schneller Weg WM-Endrunde 2. Roter Test: 3:3 nach 60, Heimtor in
  der Verlängerung → 4:3 mit einer Marke; 3:3 ohne Tor, Penaltyschießen 3:2 → 6:5 mit zwei Marken. Klick wie oben in
  `DebCupGamesScreen` und `WorldCupKnockoutScreen` (Zeilen aus der Zeichenrechnung, Seite nach x > 0x63); Rechtsklick
  verlässt. A098, A924, A925 nur Marker mit Beleg nachziehen bzw. nach der Regel oben.

## Teil 2 – Kern

### Aufräumkonzept K2 – Strafbank-Abfrage beim Bundestrainer (`Squad.Lineup.InMatch.cs` ↔ `CupsNational.Lineup.InMatch.cs`)

`python3 tools/reverse-engineering/abweichungen/k2_strafbank_bundestrainer.py` (Aufstellung 0x40364 wie in b13, Status
+0x09 & 3 des Kaderplatzes 0, ein Klick in seine Zeile):

```
Verein (Manager 0)        Status 0 links : Meldung „Keine Reihe“ / „ausgew{hlt !“
Verein (Manager 0)        Status 0 rechts: Meldung -
Verein (Manager 0)        Status 1 links : Meldung „Spieler ist“ / „Gesperrt.“
Verein (Manager 0)        Status 1 rechts: Meldung -
Verein (Manager 0)        Status 2 links : Meldung „Spieler ist“ / „Verletzt.“
Verein (Manager 0)        Status 2 rechts: Meldung -
Nationalteam (Manager 5)  Status 0 links : Meldung „Keine Reihe“ / „ausgew{hlt !“
Nationalteam (Manager 5)  Status 0 rechts: Meldung -
Nationalteam (Manager 5)  Status 1 links : Meldung „Spieler ist“ / „Gesperrt.“
Nationalteam (Manager 5)  Status 1 rechts: Meldung -
Nationalteam (Manager 5)  Status 2 links : Meldung „Spieler ist“ / „Verletzt.“
Nationalteam (Manager 5)  Status 2 rechts: Meldung -
```

- **Urteil: Abweichung bestätigt.** Die Abfrage Datei 0x40DAF–0x40E27 liest den Status des Kaderplatzes des Managers am Zug
  (DGROUP 0x5DC3 + 0x618 · DGROUP 0xA70E + 0x34 · Platz, +0x09 & 3) und meldet bei 1 „Spieler ist / Gesperrt.“, bei 2
  „Spieler ist / Verletzt.“ (Linksklick) – für Manager 5 genauso wie für einen Verein. Den Status 1 setzt die Strafe
  0x35818 im Satz des Managers der Mannschaft (Teamfeld +0x16 = Manager + 1, beim Land des Bundestrainers 6 → Satz 5;
  Datei 0x35AB8–0x35AC6, Kaderplatz Datei 0x35B3C–0x35B5F, `or [es:bx+0x09],1` Datei 0x35C02; gelesen, nicht
  nachgebildet). Ein Rechtsklick in die Namensspalte meldet nichts (dort öffnet das Original die Spielerinfo, B13).
- **Vorgabe (`aufstellung`):** `NationalCoach.ClickPlayerInMatch` prüft die Strafbank wie `Club.ClickPlayerInMatch`
  (`MatchEngine.PenaltyBoxMinutes` für das Land) und gibt `LineupResult.Suspended` zurück. Roter Test: Bundestrainer im
  Live-Spiel, Spieler seines Landes auf der Strafbank, Linksklick → „Gesperrt.“, Aufstellung unverändert.

### Aufräumkonzept K11 – Rollenwert +0x18 (`OriginalExport.Squads.cs` 0/30/70 ↔ `MatchEngine.Statistics.cs` 0/33/83)

`python3 tools/reverse-engineering/abweichungen/k11_rollenwert.py` (Poolaufbau Datei 0x5B391–0x5B4A9, Zufall 0x0C037 einmal
auf die untere, einmal auf die obere Grenze gesetzt):

```
Zufallsaufrufe 0x0C037: 175, Grenzen [(0, 16)]
   Plätze   0–  0: nicht geschrieben min 238 max 238  Beispiel [238]
   Plätze   1– 20: 0 (Torhüter)      min   0 max   0  Beispiel [0, 0, 0, 0]
   Plätze  21– 96: Abwehr            min  25 max  41  Beispiel [28, 41, 25, 34]
   Plätze  97–179: Sturm             min  75 max  91  Beispiel [78, 91, 75, 84]
   Plätze 180–183: 0 (Torhüter)      min   0 max   0  Beispiel [0, 0, 0, 0]
   Plätze 184–190: Abwehr            min  25 max  41  Beispiel [34, 28, 41, 25]
   Plätze 191–199: Sturm             min  75 max  91  Beispiel [75, 84, 78, 91]
```

- **Urteil: Abweichung bestätigt (beide Werte).** Der Rollenwert ist eine Zahl je Poolplatz, einmal beim Poolaufbau
  gewürfelt und danach nie geändert: Torhüter 0, Abwehr 25 + random(0, 16) = 25 … 41, Sturm 75 + random(0, 16) = 75 … 91
  (Platz 0 bleibt unbeschrieben). Einziger Schreiber im Spielcode; die übrigen Treffer von `+0x18` sind Kaderplätze
  (Verhandlungssperre). Leser: Torschützen/Vorlagen 0x5077–0x52CB (Rollenwert + random(5, 25) > random(10, 99)) und die
  Gruppe 0x04180 (Rollenwert / 25). Das Remake würfelt den Wert schon (`TransferPoolRules.RollRoleValue`, verbraucht nur
  den Zufall), führt ihn aber nicht; Export (30/70) und Statistik (33/83) nehmen verschiedene feste Werte.
- **Vorgabe (Aufräumen #936, Verhaltensänderung):** ein Feld `Player.RoleValue`, gesetzt in `RollRoleValue` (Poolplätze)
  bzw. mit derselben Regel für Spieler außerhalb des Pools; Export `e[0x18]` und Statistik lesen das Feld. Braucht eine
  Spielstandmigration. Roter Test: Poolaufbau mit festem Startwert → Rollenwerte der Verteidiger in 25 … 41, Stürmer in
  75 … 91, Export Byte +0x18 = Feld, Statistik-Kandidatenwurf mit dem Feld.

### Aufräumkonzept K13 – Platzwahl 0x29C3B (`NewGame.PlaceInStartLeague` ↔ `GameState.DrawStartLeaguePlace`; A463)

`python3 tools/reverse-engineering/abweichungen/k13_platzwahl.py` (Spielvorbereitung 0x28165 ab Datei 0x29C3B mit ihren
lokalen Werten bis Datei 0x29CFA; 0x34FD5 und 0x45CD0 echt, Zufall und Tausch 0x36169 ersetzt; „Start“ = Pokalrunde
DGROUP 0x5CF4 = 0 und leere Zeilen, „Zug“ = Hinzufügen nach der Europapokal-Auslosung mit acht Plätzen in Zeile 1):

```
   Start, L = Ober, Verein 30 (Oberliga)            kein Wurf; kein Tausch (behalten)
   Start, L = Ober, Verein 40 (ohne Liga)           random(24, 35) = 26; 0x36169(40, 26, 0x80)
   Start, L = Zweite, Verein 30 (Oberliga)          random(12, 23) = 15; 0x36169(30, 15, 0x80)
   Start, L = Bundesliga, Verein 4 (Bundesliga)     random(0, 11) = 4; 0x36169(4, 4, 0x80)
   Start, L = Bundesliga, Verein 4, Wurf 0          random(0, 11) = 0, random(0, 11) = 0, random(0, 11) = 6; 0x36169(4, 6, 0x80)
   Start, Manager 1, Wurf = Verein von Manager 0    random(24, 35) = 26, random(24, 35) = 27; 0x36169(40, 27, 0x80)
   Start, Manager 0, Wurf = Verein von Manager 1    random(24, 35) = 26; 0x36169(40, 26, 0x80)
   Zug, L = Bundesliga, Runde 1, Wurf Europapokal   random(0, 11) = 3, random(0, 11) = 7, random(0, 11) = 0, random(0, 11) = 8; 0x36169(30, 8, 0x80)
   Zug, L = Bundesliga, Runde 1, Zeile 2 belegt     random(0, 11) = 2; 0x36169(30, 2, 0x80)
   Start, L = Bundesliga, Zeile 2 belegt            random(0, 11) = 2, random(0, 11) = 8; 0x36169(30, 8, 0x80)
   Zug, L = Bundesliga, Runde 3, Wurf Europapokal   random(0, 11) = 3, random(0, 11) = 1, random(0, 11) = 8; 0x36169(30, 8, 0x80)
   Zug, L = Bundesliga, Runde 1, Wurf 0             random(0, 11) = 0, random(0, 11) = 8; 0x36169(30, 8, 0x80)
   Zug, L = Bundesliga, Runde 3, Wurf 0             random(0, 11) = 0; 0x36169(30, 0, 0x80)
   Zug, L = Bundesliga, Runde 4, Wurf Europapokal   random(0, 11) = 3, random(0, 11) = 1; 0x36169(30, 1, 0x80)
```

- **Urteil: ein Ablauf, Ergebnis bis auf einen Randfall gleich.** Spielstart und Hinzufügen laufen durch denselben Code:
  behalten, wenn L ≠ Bundesliga, Liga(v) / 2 = L (0x34FD5: Satz < 12 → 1, < 24 → 2, sonst bis 52 → 4) und v ≤ 35; sonst
  r = random(Basis L, Basis L + 11), neu gezogen, wenn r der Verein eines Managers mit **kleinerer** Nummer ist (Datei
  0x29C9B–0x29CC3) oder 0x45CD0(r, 1) ihn in den ersten 2 · DGROUP (0x3B1 + 0x5CF4) Bytes ab DGROUP 0x5CFB + 32 findet;
  dann 0x36169(v, r, 0x80), auch bei r = v. Beide Umsetzungen des Remakes treffen das für ihren Zeitpunkt (Start:
  leere Zeile → nur Satz 0 gesperrt; Zug: Europapokal-Plätze und Satz 0); `Next(12)` und `Random16(0, n − 1)` sind
  dieselbe Verteilung. **Randfall (Abweichung, Lesart):** Die Zahl der durchsuchten Bytes hängt an der Runde des
  Europapokals (DGROUP 0x5CF4, erhöht in 0x3489D, Datei 0x348DB/0x34AE3): ab Runde 3 (8 Bytes = die acht Plätze) ist
  Satz 0 wieder möglich, ab Runde 4 (4 Bytes) auch die hinteren Europapokal-Plätze; beim Start (Runde 0, 64 Bytes) zählt
  auch Zeile 2. Das Remake sperrt immer die acht Plätze und Satz 0. Was 0x3489D in späteren Runden in Zeile 1 schreibt,
  ist nicht nachgebildet.
- **A463 (Verein ohne Wahl): keine Abweichung im Original.** Jeder Manager wählt im Original einen Verein (Datei 0x2964C,
  `NewGame.cs`); die Zufallszuteilung ist eine Erweiterung des Remakes und bleibt `[APPROX]`.
- **Vorgabe (Aufräumen #936):** eine gemeinsame Abfrage für beide Wege: Ausschluss = Vereine früherer Manager ∪ die ersten
  2 · (32 ≫ Runde) Einträge von Zeile 1 (bei Runde 0 auch Zeile 2; leere Einträge = Satz 0), Zufall `Random16` aus dem
  gemeinsamen Strom; Tausch bleibt je Weg (Start: Platzfelder, Zug: `SwapClubPlaces`). Roter Test nur für den Randfall:
  Hinzufügen in Europapokal-Runde 3 mit Wurf 0 → Tausch mit Satz 0.

### F06 – Werbeangebot und Bande (A888)

`python3 tools/reverse-engineering/abweichungen/f06_bande.py` (Annahme Datei 0x2BAD4–0x2BB80 mit Angebotssatz, Kopie 0x105C4
= Borland `movmem(Quelle, Ziel, n)` nachgebildet; Monatseinnahme 0x3E657 mit Teamanteil 0):

```
1. Annahme eines Bandenangebots (Betrag 900, Partner 6); Felder vorher: [0: 300 Nr 1 k] [1: 300 Nr 1 k] [2: 500 Nr 2] [3: -] [4: -] [5: -] [6: -] (Nr 1 = kündbarer Vertrag über zwei Felder)
   1 Bande(n): [0: 900 Nr 3] [1: 300 Nr 1 k] [2: 500 Nr 2] [3: -] [4: -] [5: -] [6: -]; DGROUP 0x17C danach 4
   2 Bande(n): [0: 900 Nr 3] [1: 900 Nr 3] [2: 500 Nr 2] [3: -] [4: -] [5: -] [6: -]; DGROUP 0x17C danach 4
   3 Bande(n): [0: 900 Nr 3] [1: 900 Nr 3] [2: 500 Nr 2] [3: 900 Nr 3] [4: -] [5: -] [6: -]; DGROUP 0x17C danach 4
2. Monatseinnahme 0x3E657 (Teamanteil 0): Summe der Felder +0
   vorher      : Rückgabe 1100
   ein Feld    : Rückgabe 900
   zwei Felder : Rückgabe 1800
   drei Felder : Rückgabe 2700
```

- **Urteil: Abweichung bestätigt, und zwar im Geld.** Jede Bande ist ein eigener Satz (DGROUP 0xC15B + 0x6E · Manager +
  10 · Feld): die Annahme kopiert das Angebot je Bande in das erste freie oder kündbare Feld (Feld 0 … 6 der Reihe nach)
  und setzt dort +4 = Vertragsnummer DGROUP 0x17C (danach + 1). Ein Angebot über eine Bande überschreibt also **ein** Feld
  eines kündbaren Vertrags, der Rest des alten Vertrags bleibt mit **seinem Betrag je Feld** stehen. Die Einnahme
  0x3E657 zählt +0 aller 11 Felder (Datei 0x3E70C–0x3E734): ein Vertrag über n Banden bringt n · Betrag. Das Remake führt
  eine Bande als einen `SponsorContract` mit `Boards` und einem `MonthlyAmount`; `Sponsoring.MonthlyIncome` zählt den Betrag
  je Vertrag **einmal**, der Export (`OriginalExport.Rest.cs`) schreibt ihn je Feld. Das Aufteilen beim Überschreiben
  (`OverwriteCancellable`, Rest mit vollem Betrag) stimmt dann nur, wenn der Betrag als „je Feld“ gilt.
- **Vorgabe (`saison-geld`):** `MonthlyAmount` als Betrag je Feld festschreiben; `Sponsoring.MonthlyIncome` = Σ Betrag ·
  Zahl der Felder (Banden), übrige Arten · 1; Überschreiben wie bisher feldweise. Roter Test: Bandenvertrag über zwei
  Felder zu je 300 → Werbeanteil der Monatseinnahme 600 (Werte wie oben in Drittel-DM); kündbarer Vertrag über zwei Felder,
  Angebot über eine Bande → ein Feld neu, ein Feld alt, Einnahme alt + neu. Mehrsaisonprobe (Geld).

### K07 – Aufstellung: Automatik je Manager, Verletzte, Meldung, sechster Feldspieler (A165, A807, A936, A939, A971, A972)

`python3 tools/reverse-engineering/abweichungen/k07_automatik.py` (Neuaufstellung 0x426BC mit Auswahl 0x42838 und Rolle
0x04180 echt; Kader von Manager 1: 3 Torhüter, 9 Verteidiger, 12 Stürmer, Werte 40 + Platz, Platz 4 verletzt und vorher
aufgestellt mit +0x15 = 2; alle übrigen +0x15 = 0x55 als Merker):

```
Einstellung 0x375 (Manager 0) = 1, 0x377 (Manager 1) = 0; Stubs: -
   Platz:    0    1    2    3    4    5    6    7    8    9   10   11   12   13   14   15   16   17   18   19   20   21   22   23
   Art  :    T    T    T    A   Av    A    A    A    A    A    A    A    S    S    S    S    S    S    S    S    S    S    S    S
   +0x0A:    0    0    0    0    1    0    0    0    0    0    0    0    0    0    0    0    0    0    0    0    0    0    0    0
   +0x15: 0x55 0x55 0x55 0x55  0x2 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55 0x55
Einstellung 0x375 (Manager 0) = 0, 0x377 (Manager 1) = 1; Stubs: -
   Platz:    0    1    2    3    4    5    6    7    8    9   10   11   12   13   14   15   16   17   18   19   20   21   22   23
   Art  :    T    T    T    A   Av    A    A    A    A    A    A    A    S    S    S    S    S    S    S    S    S    S    S    S
   +0x0A:    1    1    1    1    0    1    1    1    1    1    1    1    0    1    1    1    1    1    1    1    1    1    1    1
   +0x15:  0x0  0x0  0x1  0x0  0x2  0x0  0x3  0x3  0x2  0x2  0x1  0x1 0x55  0x0  0x0 0x83 0x83 0x83 0x82 0x82 0x82 0x81 0x81 0x81
```

`python3 tools/reverse-engineering/abweichungen/k07_meldung.py` (Aufstellung 0x40364 wie in k2, Klick in die Zeile von
Platz 0; alle Meldungen des Klicks der Reihe nach):

```
Einstellung 0x375 vorher 1, Status 0, links : Meldungen „Auto-Aufstellung“ / „ist deaktiviert !“ → „Keine Reihe“ / „ausgew{hlt !“; 0x375 nachher 0
Einstellung 0x375 vorher 1, Status 1, links : Meldungen „Auto-Aufstellung“ / „ist deaktiviert !“ → „Spieler ist“ / „Gesperrt.“; 0x375 nachher 0
Einstellung 0x375 vorher 1, Status 2, links : Meldungen „Auto-Aufstellung“ / „ist deaktiviert !“ → „Spieler ist“ / „Verletzt.“; 0x375 nachher 0
Einstellung 0x375 vorher 1, Status 0, rechts: Meldungen -; 0x375 nachher 1
Einstellung 0x375 vorher 1, Status 1, rechts: Meldungen -; 0x375 nachher 1
Einstellung 0x375 vorher 1, Status 2, rechts: Meldungen -; 0x375 nachher 1
Einstellung 0x375 vorher 0, Status 0, links : Meldungen „Keine Reihe“ / „ausgew{hlt !“; 0x375 nachher 0
Einstellung 0x375 vorher 0, Status 1, links : Meldungen „Spieler ist“ / „Gesperrt.“; 0x375 nachher 0
Einstellung 0x375 vorher 0, Status 2, links : Meldungen „Spieler ist“ / „Verletzt.“; 0x375 nachher 0
Einstellung 0x375 vorher 0, Status 0, rechts: Meldungen -; 0x375 nachher 0
Einstellung 0x375 vorher 0, Status 1, rechts: Meldungen -; 0x375 nachher 0
Einstellung 0x375 vorher 0, Status 2, rechts: Meldungen -; 0x375 nachher 0
```

- **A936 – Einstellung je Manager: Abweichung bestätigt (Datenmodell).** 0x426BC liest das Byte DGROUP 0x375 + 2 · Manager
  (Datei 0x426C4–0x426E4; Manager 1 → 0x377), bei 0 kehrt es ohne Änderung zurück. Auch das Abschalten in der Aufstellung
  schreibt 0x375 + 2 · Manager (Datei 0x40ADE, 0x40DE4). Das Remake führt `Club.AutoLineupEnabled`; verschieden wird es,
  sobald ein Manager den Verein wechselt (Einstellung bleibt im Original beim Manager).
- **A165 – erste Aufstellung: für Managervereine keine Abweichung, für Computervereine kein Gegenstück.** Die
  Spielvorbereitung 0x28165 ruft 0x426BC nach dem Kaderaufbau (Datei 0x2A067); 0x426BC setzt zuerst +0x0A aller 29
  Kaderplätze auf 0 (Datei 0x426E7–0x42706) und stellt dann nach seiner Regel auf – die vorige Aufstellung wirkt nur über
  stehen gebliebene +0x15 (siehe A939). Das Remake ruft nach `Lineup.AutoFill` für Managervereine `ApplyAutoLineup`
  (`GameState.Integration.cs`, `GameState.AddManager.cs`); Ergebnis gleich. Computervereine haben im Original keine
  Kaderplätze (Sätze nur für die Manager 0–3, Markt 4, Bundestrainer 5); ihre Aufstellung nach Spielwert ist eine Ergänzung
  des Remakes.
- **A939 – Reihe eines Verletzten: Abweichung bestätigt.** 0x426BC schreibt +0x15 nur für ausgewählte Spieler
  (Reihe = Block + 1, Bit 0x80 Sturm; Ersatz 0, Datei 0x42744–0x42777, 0x427FD–0x42816). Nicht Ausgewählte behalten ihr
  altes +0x15 – der Verletzte (Platz 4) steht danach mit +0x0A = 0 und +0x15 = 2 da, ebenso der nicht gebrauchte Platz 12
  (0x55). Die Spielabrechnung kopiert +0x15 & 0x7F jedes Platzes (Datei 0x548A8–0x548E7, `Squad.SlotValues.cs`), der
  Verletzte spielt also seine alte Reihe ein; im Remake (Spieler aus der Aufstellung genommen) Reihe 0.
- **A807 – zwei Meldungen: keine Abweichung.** Ein Linksklick bei eingeschalteter Automatik schaltet sie ab und meldet
  „Auto-Aufstellung / ist deaktiviert !“, danach die Meldung des Klicks (keine Reihe, gesperrt, verletzt) – auch wenn der
  Klick nichts ändert. Ein Rechtsklick in der Namensspalte schaltet nichts ab (dort Spielerinfo, B13). Das Remake macht
  genau das (`LineupScreen` stellt die zweite Meldung hinter „ist deaktiviert !“, `DisableAutoLineup` vor den Prüfungen in
  `Squad.Lineup.cs`/`Squad.Lineup.InMatch.cs`); nur der `[APPROX]`-Satz in `LineupScreen.cs` („gewinnt diese Meldung“) ist
  überholt.
- **A971/A972 – sechster Feldspieler: Abweichung bestätigt.** Nachbildung:

  `python3 tools/reverse-engineering/abweichungen/k07_sechster.py` (Aufstellung 0x40364, Manager 0, Automatik aus; Reihe 1
  mit drei Stürmern +0x15 = 0x81 und drei Verteidigern +0x15 = 1, Platz 5 = sechster Feldspieler, Platz 6 Torhüter
  außerhalb; Rolle über 0x04180 echt; Gitter [bp−0x46] = 5 Blöcke × 6 Zellen, Zelle = Platz + 1, Zellen 0–2 Sturm, 3–5
  Abwehr, Block 4 Zelle 4 Torhüter):

  ```
  a) Klick auf Sturmzelle 0 der Reihe 1
    nach dem Aufbau            Reihe 1 Zellen 0–5:  1  2  3  4  5  6 | Torhüterzelle 0 | [bp-0x0E] 1 | +0x15 Plätze 0–6: 0x81 0x81 0x81 0x01 0x01 0x01 0x00
    nach dem Klick             Reihe 1 Zellen 0–5:  6  2  3  4  5  0 | Torhüterzelle 0 | [bp-0x0E] 0 | +0x15 Plätze 0–6: 0x00 0x81 0x81 0x01 0x01 0x01 0x00
    Meldungen: -
  b) Klick auf Abwehrzelle 3 der Reihe 1
    nach dem Aufbau            Reihe 1 Zellen 0–5:  1  2  3  4  5  6 | Torhüterzelle 0 | [bp-0x0E] 1 | +0x15 Plätze 0–6: 0x81 0x81 0x81 0x01 0x01 0x01 0x00
    nach dem Klick             Reihe 1 Zellen 0–5:  1  2  3  6  5  0 | Torhüterzelle 0 | [bp-0x0E] 0 | +0x15 Plätze 0–6: 0x81 0x81 0x81 0x00 0x01 0x01 0x00
    Meldungen: -
  c) wie a), dann Reihe 5 (Torhüter) und Torhüter (Platz 6) aus der Liste
    nach dem Sturmklick        Reihe 1 Zellen 0–5:  6  2  3  4  5  0 | Torhüterzelle 0 | [bp-0x0E] 0 | +0x15 Plätze 0–6: 0x00 0x81 0x81 0x01 0x01 0x01 0x00
    nach dem Torhüterklick     Reihe 1 Zellen 0–5:  6  2  3  4  5  0 | Torhüterzelle 7 | [bp-0x0E] 0 | +0x15 Plätze 0–6: 0x00 0x81 0x81 0x01 0x01 0x01 0x01
    Meldungen: -
  d) wie a), Bildschirm neu aufgebaut
    Zellen mit Nummer beim ersten Aufbau (0x0B651, x/y): 0x106/0x1d, 0x118/0x1d, 0x12a/0x1d, 0x106/0x2a, 0x118/0x2a, 0x12a/0x2a
    nach dem neuen Aufbau      Reihe 1 Zellen 0–5:  2  3  0  4  5  6 | Torhüterzelle 0 | [bp-0x0E] 0 | +0x15 Plätze 0–6: 0x00 0x81 0x81 0x01 0x01 0x01 0x00
    Zellen mit Nummer beim neuen Aufbau (0x0B651, x/y): 0x106/0x1d, 0x118/0x1d, 0x10f/0x2a, 0x121/0x2a
  ```

  - Aufbau (Datei 0x403DB–0x404FD): [bp−0x0E] = 1 genau dann, wenn Block 0 sechs Plätze zählt ([bp−0x0F], vor der
    Zellsuche gezählt) und die Torhüterzelle [bp−0x2A] leer ist; dann drei Abwehrzellen (x 0x106/0x118/0x12A).
  - Leeren eines Platzes der Reihe 1 bei gesetztem Kennzeichen (Datei 0x40B69–0x40C98): **jede** Zelle der Reihe 1 – auch eine
    Sturmzelle (a) – bekommt Zelle 5, Zelle 5 = 0, [bp−0x0E] = 0; der Herausgenommene +0x15 = 0, der sechste Feldspieler
    behält +0x15 = 1 (Abwehr). Bei einer Abwehrzelle (b) wie im Remake (`MoveSixthSkaterInto`).
  - **A971 – Kennzeichen ist Bildschirmzustand:** Nach (a) ist der Platz sofort geschlossen, obwohl Reihe 1 weiter sechs
    Feldspieler mit +0x15 ≠ 0 hat und kein Torhüter steht; ein Torhüter wird danach ohne Meldung eingesetzt (c: +0x15 = 1,
    Torhüterzelle belegt) – Reihe 1 hat dann sechs Feldspieler **und** Torhüter. Das Remake leitet `SixthSkaterOpen` aus dem
    Zustand ab (`Squad.Lineup.cs` `SixthSkaterOpen`, sechster belegt und kein Torhüter): nach (a) bleibt es offen, der
    Torhüter wird mit „Schon 6 Feldspieler / auf dem Eis !“ abgewiesen (`Squad.Lineup.cs`, Prüfung vor `TryOpenSixthSkater`).
  - **A972 – Anzeige:** Nach (a) zeigt das Original den sechsten Feldspieler in Sturmzelle 0; beim nächsten Aufbau (d) baut es das
    Gitter nach +0x15 neu: zwei Stürmer, drei Abwehrspieler in den Zellen 3–5, [bp−0x0F] = 5 → Kennzeichen 0, nur zwei
    Abwehrzellen gezeichnet (x 0x10F/0x121) – der Spieler in Zelle 5 ist unsichtbar, spielt aber (+0x15 = 1, Abrechnung zählt
    jeden Platz mit +0x15 ≠ 0, Datei 0x54918–0x54927). Das Remake lässt ihn sichtbar auf dem sechsten Platz, die Sturmzelle
    bleibt leer; +0x15 aller Plätze ist gleich.
- **Vorgabe (`aufstellung`):** (1) Einstellung „automatisch aufstellen“ als Feld des Managers (Migration), Lesen und
  Abschalten über den Manager am Zug; roter Test: Manager wechselt den Verein → Einstellung kommt mit. (2) Je Spieler die
  zuletzt zugewiesene Reihe (+0x15) führen: gesetzt nur, wenn die Neuaufstellung bzw. der Eingriff ihn aufstellt, nicht
  gelöscht beim Herausnehmen; `RollPlayedLines` liest sie. Roter Test: aufgestellter Verteidiger (Reihe 2) verletzt sich,
  Neuaufstellung, Wurf 0 → `PlayedLine` = 2. (3) A807, A165: nur Marker nachziehen (`[ORIGINAL]` mit diesen Fundstellen;
  A165 Computervereine `[APPROX]` „Ergänzung, Original ohne Kaderplätze“). (4) A971/A972: Kennzeichen „sechster Platz offen“
  als Zustand der Aufstellung führen – gesetzt beim Öffnen (`TryOpenSixthSkater`) und beim Aufbau des Bildschirms (Reihe 1
  sechs Plätze, kein Torhüter), gelöscht beim Leeren **jedes** Platzes der Reihe 1; dabei rückt der sechste Feldspieler in die
  geleerte Zelle (auch Sturm, Reihe +0x15 bleibt Abwehr). Danach ist ein Torhüter einsetzbar. Rote Tests: (a) Reihe 1 voll mit
  sechstem Feldspieler, Sturmplatz leeren → Kennzeichen aus, Spieler in der Sturmzelle angezeigt, `PositionOf` = 1;
  (c) danach Torhüter in die Torhüterreihe → eingesetzt, keine Meldung „Schon 6 Feldspieler“; (d) neuer Aufbau →
  Kennzeichen aus, sechster Feldspieler nicht angezeigt, spielt weiter (Reihe 1). Fehlt Spielraum im Datenmodell für (d), als
  `[APPROX]` mit dieser Fundstelle festhalten.

### T03 – Saisonwechsel: Zeitpunkt und Reihenfolge der Würfe (A661, A829, A843, A935)

`python3 tools/reverse-engineering/abweichungen/t03_markt.py` (Takt 0x07169 für Manager 0 bis zur zweiten Schleife Datei
0x0727C, Zähler 0x4BE17 echt; vier Markt-Plätze, Zufall einmal untere, einmal obere Grenze):

```
Plätze vorher: 0: Kraft 60, Sperre 0x01, +0x09 0x20, Bieter 5; 1: Kraft 50, Sperre 0x00, +0x09 0x00, Bieter 0; 2: Kraft 57, Sperre 0x02, +0x09 0x40, Bieter 0; 3: Kraft 40, Sperre 0x81, +0x09 0x00, Bieter 0
Lauf Zufall = untere Grenze:
   Platz 0: random(0, 2) = 0
   Platz 0: random(0, 3) = 0
   Platz 0: 0x4B270 (Gebot verfällt)
   Platz 1: random(0, 3) = 0
   Platz 2: random(0, 3) = 0
   Platz 3: random(0, 2) = 0
   Platz 3: random(0, 3) = 0
   nachher: 0: Kraft 50, Sperre 0x00, +0x09 0x00; 1: Kraft 50, Sperre 0x00, +0x09 0x00; 2: Kraft 47, Sperre 0x02, +0x09 0x40; 3: Kraft 40, Sperre 0x80, +0x09 0x00
Lauf Zufall = obere Grenze:
   Platz 0: random(0, 2) = 2
   Platz 0: random(0, 3) = 3
   Platz 1: random(0, 3) = 3
   Platz 2: random(0, 3) = 3
   Platz 3: random(0, 2) = 2
   Platz 3: random(0, 3) = 3
   nachher: 0: Kraft 50, Sperre 0x01, +0x09 0x20; 1: Kraft 50, Sperre 0x00, +0x09 0x00; 2: Kraft 47, Sperre 0x02, +0x09 0x40; 3: Kraft 40, Sperre 0x81, +0x09 0x00
```

`python3 tools/reverse-engineering/abweichungen/t03_saisonwechsel.py` (Tagesfunktion 0x54C9F ab Datei 0x56130 bis zum
Saisonzähler Datei 0x56476, alle Aufrufe als Stub; Gruppe 2 mit zwei Paaren):

```
Lauf Datei 0x56130 bis stop at 0x56476:
    1. random(36, 41) = 38
    2. Tausch 0x36169(35, 38, 0x1)
    3. random(36, 41) = 38
    4. random(36, 41) = 40
    5. Tausch 0x36169(34, 40, 0x1)
    6. Ewige Tabelle 0x5667A
    7. Wanderung 0x09D1B(10)
    8. Ligamischung 0x35EDD
    9. 0x346D5
   10. Saisonschleife 0x26F20
   11. 0x5ACA0(0)
   12. 0x5ACA0(1)
   weitere Stubs: 0x25b4a 0x51f3d 0x5d90d
   Schreibzugriffe auf Tageszähler 0x3B7/0x3B9 und Tagesindex 0xA969: 0
```

- **A829 – Markt-Verfall: Abweichung bestätigt (Strom und Zahl der Würfe).** Eine Schleife je Markt-Platz (Anzahl 0x4BE17,
  Satz 4): Kraft > 56 → −10; Sperre nur Manager 0 → random(0, 2), bei 0 frei; **random(0, 3) für jeden Platz**, erst
  danach die Prüfung „offenes Gebot“ (+0x09 > 0x1F und Pool +0x1E ≠ 0, Datei 0x07227–0x07247), bei 0 verfällt es (0x4B270).
  Das Remake rechnet erst Verfall und Sperren aller Plätze, dann je **Gebot** einen Wurf (`TransferMarket.AdvanceBids`,
  Kommentar „der Wurf fällt nur bei einem offenen Gebot“ widerlegt).
- **A661 – Oberliga-Zug: Abweichung bestätigt (Zeitpunkt).** Der Zug random(36, 41) (Datei 0x56275, Wiederholung bei
  gleichem Zug) und die Tausche 0x36169 stehen im selben Durchlauf wie Ewige Tabelle, Wanderung und Mischung; zwischen
  Datei 0x56130 und 0x56476 schreibt nichts Tageszähler oder Tagesindex – also am Tag der Mischung (29. April, Datei
  0x5644E, belegt in `SeasonFlow.cs`/`wettbewerbe.md`). Das Remake zieht am 13. April (`FinishClubSeason`).
- **A843/A935 – Wanderung vor der Mischung: Abweichung bestätigt.** Reihenfolge am 29. April: Austausch (Gruppen 0–2) →
  Kostenstufe (Datei 0x563C0) → Ewige Tabelle 0x5667A → Wanderung 0x09D1B(10) → 0x35EDD (Länder, dann Ligen) → 0x346D5 →
  0x26F20. Die Wanderung zählt die Sätze in der Folge **nach** den Tauschen von Auf- und Absteigern (0x36169 mit Schalter 1
  tauscht ganze Sätze) und **vor** der neuen Mischung. Das Remake wandert in `StartNextSeason` (`OnNewSeasonManager`) nach
  der Mischung, in deren neuer Folge (`MatchEngine.DriftOrder`).
- **Vorgabe (`saison-geld`):** (1) `DailyRunMarket`: eine Schleife über die Markt-Plätze in Satzfolge – Verfall, Sperrwurf
  (nur Sperre allein von Manager 0), `Random16(0, 3)` immer, Gebot des Platzes bei 0 löschen; roter Test: drei Plätze ohne
  Gebot → drei Würfe aus `Random` (Stromstand nach dem Takt wie mit drei `Random16(0, 3)`). (2) `ExchangeOberliga` aus
  `FinishClubSeason` in `FinishSeason` nach `ApplyForcedRelegation`, Zug `Random16(36, 41)` auf die Plätze 36–41; roter
  Test: Stromstand und Oberliga-Paare am 13. April unverändert, am 29. April gezogen. (3) `DriftTeamValues(Season)` in
  `FinishSeason` nach `RecordSeasonStatistics` und vor `MixForNextSeason`, Folge = Satzfolge der alten Saison mit den
  Aufsteigern auf den Plätzen ihrer Absteiger (wie 0x36169); in `OnNewSeasonManager` entfernen. Roter Test: Wanderung zieht
  vor den 40 Würfen der Länder (Stromstand), Reihenfolge der Vereine wie oben. Mehrsaisonprobe (4 Wechsel).

### F08 – Stadion, Zuschauer und Sommerpause (A468, A509, A512, A526, A695)

`python3 tools/reverse-engineering/abweichungen/f08_baufirma.py` (Tagesabschluss 0x3ED01 für Manager 1 bis Datei 0x3ED4A):

```
Merker vorher : Manager 0 [0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0], Manager 1 [0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0]
   Projekt  0: random(0, 10) = 7
   Projekt  1: random(0, 10) = 7
   Projekt  2: random(0, 10) = 0
   Projekt  3: random(0, 10) = 0
   Projekt  4: random(0, 10) = 7
   Projekt  5: random(0, 10) = 7
   Projekt  6: random(0, 10) = 7
   Projekt  7: random(0, 10) = 7
   Projekt  8: random(0, 10) = 7
   Projekt  9: random(0, 10) = 7
   Projekt 10: random(0, 10) = 7
Merker nachher: Manager 0 [0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0], Manager 1 [0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
```

`python3 tools/reverse-engineering/abweichungen/f08_stadionbau.py` (Bauwoche 0x3D316, Manager 0; Merkmal 2 ohne Ziel,
Merkmal 4 mit Ziel 5, Sitzplätze ohne Ziel, je eine Restwoche):

```
   Projekt 0: 0x3cfa0 (aus Datei 0x3d330)
   Projekt 2: 0x0390b (aus Datei 0x3d3ea)
   Projekt 2: 0x0389c (aus Datei 0x3d409)
   Projekt 2: 0x0389c (aus Datei 0x3d41b)
   Projekt 2: 0x0390b (aus Datei 0x3d431)
   Projekt 2: 0x4b2ba (aus Datei 0x3d447)
   Projekt 4: 0x0390b (aus Datei 0x3d3ea)
   Projekt 4: 0x0389c (aus Datei 0x3d409)
   Projekt 4: 0x0389c (aus Datei 0x3d41b)
   Projekt 4: 0x0390b (aus Datei 0x3d431)
   Projekt 4: 0x4b2ba (aus Datei 0x3d447)
   Projekt 9: 0x0390b (aus Datei 0x3d3ea)
   Projekt 9: 0x0389c (aus Datei 0x3d409)
   Projekt 9: 0x0389c (aus Datei 0x3d41b)
   Projekt 9: 0x0390b (aus Datei 0x3d431)
   Projekt 9: 0x4b2ba (aus Datei 0x3d447)
Merkmal 2: Stufe 0, Ziel 0, Restwochen 0
Merkmal 4: Stufe 5, Ziel 0, Restwochen 0
Sitzplätze (Projekt 9): Plätze 0, Ziel 0, Restwochen 0
```

`python3 tools/reverse-engineering/abweichungen/f08_sommerpause.py` (Sommerschleife 0x54C9F ab Datei 0x56476, zwei Manager,
drei Tage; Ausgabe gekürzt auf den ersten Tag, die weiteren gleich ohne Stadionbau):

```
Saisonzähler 0x3BD vor Datei 0x56476: 3
   --- 0x367B1 (Datum weiter), Zähler 0x3B7 = 7
   Manager 0: Stadionbau 0x3D316
   Manager 0: Tagesabschluss 0x3ED01 (Saisonzähler 0x3BD = 4)
   Manager 0: Heilung 0x07C12 Platz 0
   Manager 0: Heilung 0x07C12 Platz 1
   Manager 1: Stadionbau 0x3D316
   Manager 1: Tagesabschluss 0x3ED01 (Saisonzähler 0x3BD = 4)
   Manager 1: Heilung 0x07C12 Platz 0
   Manager 1: Heilung 0x07C12 Platz 1
   …
```

`python3 tools/reverse-engineering/abweichungen/f08_abrechnung.py` (Spieltag 0x36B5D, Ligaweg, bis zur ersten Minute):

```
   0x0b8c2 (aus Datei 0x36bdb)
   0x084d7 (aus Datei 0x36c4f)
   0x5ac9b (aus Datei 0x36c55)
   0x50b45 (aus Datei 0x36cd3)
   0x08365 (aus Datei 0x36d12)
   0x08365 (aus Datei 0x36d49)
   Abrechnung 0x544C0 (aus Datei 0x36dbd)
   0x08365 (aus Datei 0x36d12)
   0x08365 (aus Datei 0x36d49)
   Abrechnung 0x544C0 (aus Datei 0x36dbd)
   0x08365 (aus Datei 0x36d12)
   0x08365 (aus Datei 0x36d49)
   Abrechnung 0x544C0 (aus Datei 0x36dbd)
   0x08365 (aus Datei 0x36d12)
   0x08365 (aus Datei 0x36d49)
   Abrechnung 0x544C0 (aus Datei 0x36dbd)
   0x08365 (aus Datei 0x36d12)
   0x08365 (aus Datei 0x36d49)
   Abrechnung 0x544C0 (aus Datei 0x36dbd)
   0x08365 (aus Datei 0x36d12)
   0x08365 (aus Datei 0x36d49)
   Abrechnung 0x544C0 (aus Datei 0x36dbd)
   0x5ac9b (aus Datei 0x3700d)
   Minutenschleife 0x37A62 (aus Datei 0x377de)
Abrechnungen vor der ersten Minute: 6; Hauptmenü/Sicherung davor: nein
```

- **A512 – Baufirmen: Abweichung bestätigt (Zahl der Würfe).** random(0, 10) für **alle 11 Projekte** des Managers am Zug,
  bei 0 Merker DGROUP 0xCC09 + 11 · Manager + Projekt = 0 (auch wenn nicht gesetzt). Das Remake würfelt nur für gesetzte
  Merker (`ForgiveBuilders`); Verteilung je Merker gleich, Strom verschieden.
- **A509 – Bau ohne Ziel: Abweichung bestätigt, im Spiel nicht erreichbar.** Bei Restwoche 0 setzt 0x3D316 ohne Prüfung
  Stufe = Ziel bzw. Plätze = Zielplätze, Ziel = 0, und meldet (0x4B2BA, Datei 0x3D447) – ohne Ziel also Stufe bzw. Plätze 0.
  Das Remake lässt die Stufe stehen und meldet nichts. Beide setzen Restwochen nur mit einem Ziel (Annahme); erreichbar nur
  über einen fremden Spielstand (`ImportConstruction`).
- **A526 – Sommerpause: Abweichung bestätigt (Reihenfolge), Buchjahr gleich.** Je Kalendertag (0x367B1) und je Manager
  verschränkt: Stadionbau (Zähler ≡ 0), Tagesabschluss 0x3ED01, Heilung je Kaderplatz – dann der nächste Manager. Der
  Saisonzähler ist schon erhöht (Datei 0x56476; 3 → 4 vor der ersten Buchung): Buchen in der neuen Saison wie im Remake.
  Das Remake rechnet je Tag erst den Bau aller, dann den Tagesabschluss aller (Börse einmal), dann die Heilung aller.
- **A468, A695 – Abrechnung vor dem Spiel: keine Abweichung im Ergebnis.** 0x36B5D rechnet 0x544C0 für alle sechs Paarungen
  des Tages (Datei 0x36DBD), dann erst die Minutenschleife 0x37A62; dazwischen kein Hauptmenü, keine Sicherung. Gemerkte
  Zuschauer über einen Spielstand hinweg (A468) und eine Abrechnung ohne Vorbereitung (A695: nur Spielwertung und direkte
  Aufrufe ohne Spiel, die Aufstellung ändert sich dann nicht) kommen im Original nicht vor.
- **Vorgabe (`saison-geld`):** (1) `ForgiveBuilders`: je Manager 11 Würfe `Between(Random, 0, 10)` in Projektfolge, bei 0
  Merker löschen; roter Test: kein Merker gesetzt → 11 Würfe je Manager und Tag (Stromstand). (2) `RunSummerBreak` je Tag
  je Manager: Bau (Zähler ≡ 0), Tagesabschluss des Managers, Heilung seines Kaders; Börse weiter einmal je Tag (#188) –
  roter Test: zwei Manager, Folge der Würfe Bau/Baufirmen/Heilung wie oben. (3) A509: „wie im Original“ (Stufe = Ziel ohne
  Prüfung, Meldung) oder `[APPROX]` mit diesem Beleg; A468, A695: Marker mit Beleg nachziehen, nichts ändern.
