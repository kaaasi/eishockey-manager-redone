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

(offen)
