# Analyse em.exe: Meldungen, Tagesablauf, Fenster (Spur `re-meldungen`, Runde 10)

Alle Adressen sind **Dateioffsets** von `em.exe` (Basissegmente: 0x1E00 + Segment · 16 + Offset; Overlays: 0x255C0 +
Versatz aus dem Stub, siehe `docs/reverse-engineering.md`). DGROUP-Variablen stehen immer mit dem Wort „DGROUP“ davor.
Fernaufrufe heißen wie im Original `Selektor:Offset` (0x30 = Basis 0x902, 0x38 = Basis 0x99F, 0x50 = Basis 0x1115,
0x48 = Basis 0xBBF). Beleg ist, wo nicht anders gesagt, die Disassembly (`ndisasm -b16` über die Bereiche der
Funktionskarte; Hilfsskripte und Ausgaben nur im Scratch). Spielstände und DOSBox-Bilder wurden in diesem Lauf nicht
benutzt.

## Übersicht

| Funktion | Rolle | Remake | Kandidat |
|---|---|---|---|
| Datei 0x0AF8D | Bildstreifen kopieren, wahlweise zeilenweise eingerollt | – | – |
| Datei 0x0B019 | Fensterfehler anzeigen und Programm beenden | – | K10 (P3) |
| Datei 0x0B0E6 | Maus in Fenster k? | Treffertests je Bildschirm | – |
| Datei 0x0B447 | Fenster schließen (Hintergrund zurück) | Bildschirme zeichnen neu | – |
| Datei 0x0B7B5 | Linie in einer Farbe | `Surface.HLine`/eigene Linien | – |
| Datei 0x0B830 | Speicher kopieren (überlappend) | – | – |
| Datei 0x0B8C2 | Speicher füllen | – | – |
| Datei 0x0B8FE | auf Strahlrücklauf warten | – | – |
| Datei 0x0BE71 | Zeichen auf darstellbar prüfen | Font-Abbildung | – |
| Datei 0x0BECD | Schrift wählen | `Assets`-Fonts | – |
| Datei 0x0BEFA | Bildpuffer freigeben | – | – |
| Datei 0x0BF64 | EMS-Bildcache: Seiten einblenden | – | – |
| Datei 0x0C0B8 | Kurzmeldung ohne Knopf, schließt nach Zeit | `ShortMessage`, `MessageBox.ShowShort` (Runde 11) | K2 (erledigt) |
| Datei 0x0C5CE | Bildschirm aus/ein (Seitenwechsel, Palette) | – | K9 (P3) |
| Datei 0x0C729 | Speicher anfordern, sonst SPEICHER-Error | – | – |
| Datei 0x0C789 | Speicher freigeben | – | – |
| Datei 0x4C246 | Meldung/Frage (Stub 0x120:0x7A) | `OriginalDialog` (Runde 11); Aufrufer `QuitQuestion`, `DebCupDrawLive`, `AdvertisingScreen`, `TransferDialog`, `MessageBox` | K1, K3 (erledigt, Rest im Abschnitt Kandidaten) |
| Datei 0x4BA58 | Pfeilpaar (Stub 0x120:0x3E) | `AdvertisingScreen` | K4 (P3) |
| Datei 0x432EC | Infofile anzeigen (Klick auf Pixel 319 \| 0) | `InfoFileScreen` (Runde 11) | K5 (erledigt) |
| Datei 0x445E0 | Tabellenkurve | `RankCurve` (Runde 11) | K6 (erledigt) |
| Datei 0x07C7E | Saisonwechsel: freie Poolspieler auf Ligen verteilen | `Transfer.PoolDistribution.cs` (Runde 11) | K7, K8 (erledigt) |
| Datei 0x4F325 | Tabellenfelder sichern/herstellen | keine Sicherung nötig | – |
| Datei 0x25B4A | Fehlerhalt mit ERROR.HOK, Programmende | Ausnahme | K10 (P3) |
| Datei 0x25764 | EMS-Bildcache beim Spielstart füllen | – (alles im Speicher) | – |

## A. Fensterbasis, Segment 0x902

**Fenstertabelle** DGROUP 0x5A57, 50 Plätze zu 10 Byte: +0 x1 (Wort, −1 = frei), +2 x2 (Wort), +4 y1 (Byte), +5 y2
(Byte), +6 Fernzeiger auf den gesicherten Hintergrund. Geöffnet wird mit 0x30:0x335 (Datei 0x0B155, schon
dokumentiert; Fehlertexte „Zu viele Fenster auf !“ und „Fenster-Koordinaten falsch !“ → 0x0B019).

### Datei 0x0AF8D – Bildstreifen kopieren (140 B)

- **Aufrufer:** 0x28165, 0x2A9AD, 0x47F2B. **Ruft:** 0x0B8FE (Strahlrücklauf, Datei 0x10AA5), Kopie `0x50:0xA79`
  (Datei 0x139C9).
- **Argumente:** (Quelle far, Breite, Höhe h, Quell-x, Bildnummer k, Ziel-x, Ziel-y, Schritt s).
- **Regel:** Quellzeile = h · k (senkrechter Streifen gleich großer Bilder). s = 0: einmal kopieren (Maske 0xC0) auf den
  Bildschirm DGROUP 0xC143. s ≠ 0: h + 1 Durchgänge, je Durchgang auf den Strahlrücklauf warten, kopieren, Quellzeile + s –
  das Bild rollt zeilenweise durch das Zielfeld.
- **Remake:** keine eigene Entsprechung nötig (Aufrufer gehören zu anderen Bildschirmen). Kein Kandidat.

### Datei 0x0B019 – Fensterfehler (205 B)

- **Aufrufer:** 0x0B155, 0x0B447 (nur in deren Fehlerzweigen). **Ruft:** Schrift 0x0BECD, Text 0x30:0x885 (Datei
  0x0B6A5), Zahl 0x38:0x2C2 (Datei 0x0BAB2), Bildschirm ein 0x0C5CE, Warten 0x38:0x118 (Datei 0x0B908), Fehlerhalt
  0x25B4A mit Code 99.
- **Regel:** Schrift 2; Fehlertext zentriert x 0 … 319 auf Grundlinie 90, darunter DGROUP 0x7A9 (Spieltag) auf 100 und
  DGROUP 0xD0C2 (Wettbewerbscode) auf 110, Farbe 1, Schatten 0; Bildschirm ein, auf Klick warten, Programmende ohne
  ERROR.HOK (Code 99, siehe 0x25B4A).
- **Remake:** Fehler sind dort Ausnahmen. Kandidat K10 nur zusammen mit 0x25B4A.

### Datei 0x0B0E6 – Maus in Fenster k (111 B)

- **Aufrufer:** 0x25E30, 0x2FC2F, 0x31CD5, 0x32E23, 0x40364, 0x44E22, 0x4E8A3, 0x4F78D, 0x5DBE5.
- **Regel:** k außerhalb 0 … 49 → 0. Sonst 1, wenn x1 ≤ Maus-x ≤ x2 und y1 ≤ Maus-y ≤ y2 (Fenstertabelle, Grenzen
  einschließlich; Maus über Fernzeiger DGROUP 0xC147: +0 = y, +2 = x).
- **Remake:** Treffertests stehen je Bildschirm als `Rect`; die Einschluss-Regel ist dieselbe. Kein Kandidat.

### Datei 0x0B447 – Fenster schließen (352 B)

- **Aufrufer:** 28 Stellen (z. B. 0x4C246, 0x432EC, 0x44E22, 0x26F20); 18-mal mit 127, sonst mit einer Fensternummer.
- **Regel:** Nummer n ≥ 200 → n − 200 (gleiche Wirkung). n = 127: alle Plätze 0 … 48 (Platz 49 wird nicht erfasst).
  Sonst n ≥ 50 → „Fenster_Zu: Fehler !“ über 0x0B019. Je belegtem Platz mit Puffer: Hintergrund mit 0x139C9 (Maske 0xC0)
  zurückschreiben, Puffer über 0x0BEFA freigeben, Platz auf −1.
- **Befund:** Die Lage und Größe des Zurückschreibens nimmt der Code aus dem Platz **n**, nicht aus dem Schleifenplatz.
  Bei n = 127 sind das die Bytes DGROUP 0x5F4D ff. (hinter der Tabelle, kein direkter Schreiber gefunden) – das
  „Alle schließen“ stellt also keinen Hintergrund verlässlich her; die Aufrufer zeichnen danach den Bildschirm neu.
- **Remake:** Bildschirme zeichnen jedes Bild vollständig; ein Zurückschreiben gibt es nicht und ist nicht nötig.

### Datei 0x0B7B5 – Linie (69 B)

- **Aufrufer:** 40 Stellen (Meldungen, Infoleiste, Tabellen, Statistik). **Ruft:** `0x50:0x545` (Farbe, Feld +4 des
  Zeichenkontexts), `0x50:0x2DA` (Stift setzen, +7/+9), `0x50:0x2F3` (Linie ziehen, Datei 0x13243).
- **Argumente:** (Ziel, x1, y1, x2, y2, Farbe); Linie von (x1 | y1) nach (x2 | y2) einschließlich.
- **Remake:** `Surface.HLine`/`VLine` und eigene Linien (z. B. `MainMenuScreen.Line`). Kein Kandidat.

## B. Segment 0x99F (Bildlader, Texte, Speicher)

### Datei 0x0B830 und Datei 0x0B8C2 – Speicher kopieren und füllen

- 0x0B830 (146 B), `0x38:0x40`, 17 Aufrufer: (Quelle far, Ziel far, Anzahl long). Liegt der Ziel-Offset über dem
  Quell-Offset, wird von hinten kopiert (nur die Offsets werden verglichen, nicht die Segmente) – `memmove`.
- 0x0B8C2 (60 B), `0x38:0xD2`, 7 Aufrufer: (Ziel far, Anzahl long, Byte) – `memset`; z. B. 0x5661F löscht vor 0x07C7E
  0x46 Byte ab DGROUP 0xCCB2.
- **Remake:** Sprachmittel. Kein Kandidat.

### Datei 0x0B8FE – Strahlrücklauf (10 B)

- 22 Aufrufer (Fenster, Börse, Livespiel). Fernhülle um Datei 0x10AA5 (wartet, bis Port 0x3DA Bit 3 gesetzt ist).
- **Remake:** MonoGame taktet selbst. Kein Kandidat.

### Datei 0x0BE71 – darstellbares Zeichen (92 B)

- **Aufrufer:** nur die Textausgabe 0x0BAFB. **Regel:** a–z, A–Z, 0–9 → 1. Sonst wird das Zeichen in der Liste
  DGROUP 0x4A45 (Leerzeichen, CP437 Ä Ö Ü ä ö ü ß, dann `.,':_\`, 14 Zeichen) gesucht; gefunden → durch den Eintrag
  derselben Stelle aus DGROUP 0x4A54 ersetzt (in der Datei dieselbe Liste, also unverändert) und 1, sonst 0 (Zeichen wird
  nicht gezeichnet).
- **Remake:** Fonts zeichnen fehlende Zeichen nicht; Texte kommen ohnehin aus em.exe bzw. der Datenbank. Kein Kandidat.

### Datei 0x0BECD – Schrift wählen (45 B)

- Über 60 Aufrufer, `0x38:0x6DD`. (Kontext far, Schrift k): k < 4 → Fontzeiger DGROUP 0xDE65 + 4 · k in Feld +0x0B des
  Zeichenkontexts (0x1315D; Font mit erstem Wort −1 wird abgelehnt). Schrift 1 = Zahlen/Knöpfe, 2 = Überschriften,
  3 = Fließtext (Zuordnung wie in `docs/formate/dateiformate.md`).
- **Remake:** Fonts in `Assets`. Kein Kandidat.

### Datei 0x0BEFA – Bildpuffer freigeben (106 B)

- Viele Aufrufer (z. B. 0x0B447, 0x0C5CE, 0x25B4A). Gibt die beiden Fernzeiger +4 und +8 des inneren Satzes, den inneren
  Satz und den äußeren Satz über 0x0C789 frei (Kennungen 0, 0, 15, 12 – nur Merker).
- **Remake:** Speicherverwaltung von .NET. Kein Kandidat.

### Datei 0x0BF64 – EMS-Bildcache einblenden (113 B)

- **Aufrufer:** Bildlader 0x0C17D, 0x567C8, 0x5AC03. Sucht in der Liste ab DGROUP 0xDE45 (Glied +0x10) den Eintrag mit
  +4 = Bildnummer; blendet dessen logische EMS-Seiten +0x0E … +0x0E + (+0x0F) − 1 der Kennung DGROUP 0xDE27 in die
  physischen Seiten 0, 1, … ein (`INT 67h`, AH = 0x44, Datei 0x10D4B) und liefert den Eintrag, sonst 0.
- **Remake:** alle Bilder liegen im Speicher. Kein Kandidat.

### Datei 0x0C0B8 – Kurzmeldung (197 B)

- **Aufrufer:** 0x3B8F7, 0x3D7F8, 0x40364 (fünfmal), 0x42CB1, 0x44E22 (Bundestrainer-Warnung 0x454EA), 0x465F1
  (Transfermarkt, siebenmal), 0x5C3E3. **Ruft:** 0x03953 (`strncmp`), 0x0C5CE (Bildschirm ein), Meldung 0x4C246.
- **Argumente:** (Zeile 1, Zeile 2). Fehlt Zeile 2, steht dort „einlegen.“ (Zeiger DGROUP 0x14ED). Der Zweig, der die
  Zeilen tauscht und 100 aufschlägt, läuft nur bei DGROUP 0x6AE = 0; der Wert ist 1 und wird nirgends geschrieben (toter
  Zweig).
- **Regel:** Aufruf 0x4C246 (Manager DGROUP 0xA70E, Zeile 1, Zeile 2, –, –, „Na gut.“, Modus m) mit
  m = 75 − 30 · [Zeile 1 beginnt mit „Auto-“]. Modus ≥ 2 heißt in 0x4C246: **kein Knopf, keine Eingabe**, Pause m
  (`0x50:0x19E`, Datei 0x130EE = m / 3 BIOS-Takte, also 25 bzw. 15 Takte ≈ 1,4 s bzw. 0,8 s bei 18,2 Hz), dann
  schließt die Meldung von selbst. Rückgabe immer 1.
- **Remake (Runde 11, Spur `ui-meldungen`, K2):** `ShortMessage` (`UI/OriginalDialog.cs`) ohne Knopf und ohne Eingabe,
  schließt nach Modus / 3 Takten (75 bzw. 45 bei „Auto-…“); `MainMenuScreen.CoachWarning` und über
  `MessageBox.ShowShort` die Kurzmeldungen der Aufstellung (0x40AF3, 0x40B55, 0x40EB9), der Transferliste (0x46A4D ff.,
  0x42CE3) und der Freundschaftsspiele (0x3C2C5). Test `OriginalDialogCallersTests`. Nicht angeschlossen: Aufrufer
  ohne Entsprechung im Remake (Stadion „Die Baufirma / ist noch beleidigt...“ 0x3DE9F, „Kein / Platz mehr !“ 0x414C6,
  „Schon 6 Feldspieler / auf dem Eis !“ 0x40EF7, Spielstand-Meldungen 0x5C559/0x5C59A).

### Datei 0x0C5CE – Bildschirm aus/ein (347 B)

- **Aufrufer:** über 30 Bildschirme (immer mit DGROUP 0xC157 und 0 = aus bzw. 1 = ein). Zustand DGROUP 0x4A69 (gleicher
  Wert → nichts tun).
- **Regel mit Seitenwechsel** (DGROUP 0x22A ≠ 0; gesetzt beim Spielstart Datei 0x439C0, gelöscht beim Laden 0x5687A):
  - ein: auf den Strahlrücklauf warten, ausstehende Palette (DGROUP 0x49B8 Einträge aus DGROUP 0xC153) setzen
    (`INT 10h` 0x1012, Datei 0x121E0), Anzeigeseite 0 (CRTC-Startadresse 0, Datei 0x10819 mit 1).
  - aus: steht DGROUP 0x49A9 (gesetzt vom Bildlader 0x0C17D), erst alle 256 Paletteneinträge aus DGROUP 0xE1A0 setzen. Beim ersten Mal (DGROUP 0x512D
    = 0) kb\22 laden und die Seite 0 nach VRAM 0x9600 kopieren (Datei 0x108E4, Maus dabei aus); dann Anzeigeseite
    0x9600 (Datei 0x10819 mit 3). Gezeichnet wird weiter auf Seite 0 – der Spieler sieht so lange den Hintergrund kb\22.
- **Regel ohne Seitenwechsel:** ein = Palette wie oben, dann Bild an (`INT 10h` AX 0x1200 BL 0x36, Datei 0x1050D);
  aus = Bild aus (AX 0x1201, Datei 0x10505), dann Palette DGROUP 0xE1A0 wie oben.
- **Nebenbefund Datei 0x45D46** (Ov 0x110, 126 B, unbekannt): stellt bei DGROUP 0x541C ≠ 0 den Hintergrund kb\22 her –
  aus VRAM 0x9600, beim ersten Mal kb\22 laden und dorthin sichern. Aufrufer u. a. 0x432EC, 0x26F20, 0x2F5B6.
- **Remake:** Bildschirmwechsel sind sofort fertig, ohne Zwischenbild. Kandidat K9 (P3, nur falls Übergänge
  originalgetreu werden sollen).

### Datei 0x0C729 und Datei 0x0C789 – Speicher anfordern und freigeben

- 0x0C729 (96 B, `0x38:0xF39`): Anforderung über 0x13030; gelingt sie nicht, erst den Bildcache räumen (0x2598F mit 0),
  sonst Fehlerhalt 0x25B4A mit Code 4 („SPEICHER-Error“).
- 0x0C789 (22 B, `0x38:0xF99`): (Zeiger, Kennung) → 0x130A5 sucht den Zeiger in der Liste DGROUP 0xE43A (200 Einträge),
  gibt frei und trägt 0 ein; die Kennung wird nicht benutzt.
- **Remake:** Speicherverwaltung von .NET. Kein Kandidat.

## C. Meldungen, Overlay 0x120

### Datei 0x4C246 – Meldung und Frage (Stub 0x120:0x7A, 1136 B)

- **Aufrufer:** 0x063BE, 0x0C0B8, 0x26F20, 0x2B0C0, 0x33B83, 0x3D7F8, 0x45C33, 0x465F1, 0x4BCCC, 0x5B9DD, 0x5DBE5.
- **Ruft:** 0x0390B (`strcpy`), 0x0C097 (bis keine Maustaste ansteht), Fenster 0x0B155, Schrift 0x0BECD, Text
  0x0B6A5, Fläche 0x0B620, Umriss 0x0B5A7, Linie 0x0B7B5, Knopf anlegen/freigeben 0x4C196 (`0x120:0x75`),
  Knopf unter der Maus 0x4BEBD (`0x120:0x6B`), Mausereignis 0x0C068, Pause 0x130EE, Fenster zu 0x0B447.
- **Argumente:** (Manager m, Z1, Z2, Z3, Z4, Z5, Modus). Überschrift: Name des Managers m (DGROUP 0x965B + 0x1AF · m);
  m ≥ Anzahl Manager (DGROUP 0x386) und < 5 → Manager am Zug DGROUP 0xA70D; m = 5 → Name des Managers DGROUP 0x9FAA
  (Bundestrainer); leerer Name → Zeiger DGROUP 0xCC4 („An Alle:“).
- **Modus:** 0 = Meldung, 1 = Frage, ≥ 2 = Meldung mit Zeit (Wert > 100 → Wert − 100) ohne Knöpfe (siehe 0x0C0B8).
- **Aufbau** (Bildschirm DGROUP 0xC143, alle Koordinaten einschließlich):

  | Teil | Original |
  |---|---|
  | Fenster | 7 × 4 Rahmenteile ab (75 \| 107), Stil 0, Merker 3 – = `TransferDialog.QuestionBox` (75, 107, 168, 76) |
  | Überschrift | Schrift 2, zentriert x 105 … 210, Grundlinie 125, Farbe 7, Schatten 0 |
  | Symbolfelder | i = 0, 1, x0 = 83 + 129 · i: Fläche (x0 \| 112)–(x0 + 21 \| 133), Umriss (x0 + 1 \| 113)–(x0 + 20 \| 132); Meldung Fläche 17, Umriss 16, Zeichen „!“; Frage Fläche 14, Umriss 12, Zeichen „?“; Zeichen Schrift 1, zentriert zwischen x0 und x0 + 23 (Frage: + 24), Grundlinie 129, Farbe 0, ohne Schatten |
  | Linie oben | (109 \| 131)–(207 \| 131), Farbe 7 |
  | Textzeilen | Schrift 3, zentriert x 105 … 210, Farbe 7, Schatten 0. Grundlinien 141 + 8 · i (Z1, Z2, Z3); ohne Z3 um 4 tiefer (145, 153). Meldung mit Z4: Z4 als vierte Zeile, Abstand 7, Beginn 2 höher (139, 146, 153, 160; ohne Z3 um 4 tiefer) |
  | Linie unten | (80 \| 163)–(237 \| 163), Farbe 0 |
  | Knöpfe | nur wenn (Z3 oder Z4 nicht leer) und Modus < 2: links (95 \| 166) mit Z4 (Frage; fehlt Z4: Z5) bzw. Z5 (Meldung – dann beide Knöpfe „Z5“), rechts (165 \| 166) mit Z5 |

- **Eingabe:** nur Mausereignis 1 (linke Taste) über 0x0C068 → 0x0FECF (Merker der Maustasten, keine Tasten). Meldung:
  jeder Linksklick schließt; Frage: nur ein Klick auf einen Knopf. Ende auch bei DGROUP 0x1DB ≠ 0 (Durchlauf ohne Halt).
  **Tastatur und rechte Maustaste werden nicht ausgewertet.**
- **Rückgabe:** Frage: linker Knopf 1, rechter Knopf 0; Meldung: 0 (Klick auf den linken Knopf: 1); Meldung mit Zeit:
  −1 (0xFF). Danach Fenster zu und
  beide Knöpfe freigeben.
- **Remake (Runde 11, Spur `ui-meldungen`, K1/K3):** `OriginalDialog` (`UI/OriginalDialog.cs`) als ein Baustein nach der
  Tabelle oben (Symbolfelder 83/212 | 112 mit eingerücktem Umriss, „!“ 16/17, „?“ 12/14, untere Linie Farbe 0, nur
  Linksklick). Aufrufer umgestellt (Test `OriginalDialogCallersTests`): `QuitQuestion`, `DebCupDrawLive` (Frage 0x4C246,
  A270, A273), `AdvertisingScreen` (Verlängerungsfrage, `RenewalDialog`, A304, A308), `TransferDialog` (Talent,
  Sponsorzuschuss, Frage an den Verkäufer, `AsDialog`) und `TeamUi.MessageBox` (Überschrift = Manager am Zug, Knöpfe nur
  bei Zeile 3/4). **[APPROX]:** Knopflage 94/164 statt 95/165 (Video), Knopftext „Na gut.“, wo der Aufrufer keine Zeile 5
  übergibt, Komfort-Tasten (Escape, Enter, rechte Maustaste) der Aufrufer; Meldungen mit mehr als vier Zeilen
  (Spielstand-Fehler, Spielmodus) und die Nationalaufstellung (0x54C9F) behalten den alten Rahmen.

### Datei 0x4BA58 – Pfeilpaar (Stub 0x120:0x3E, 628 B)

- **Aufrufer:** 0x2B0C0, 0x2C1EE, 0x2E356, 0x3D7F8, 0x5DBE5. **Ruft:** 0x0C068, Kopie 0x139C9.
- **Argumente:** (x1, y1, x2, y2, Halten). Trefferflächen 8 × 9: (x1 \| y1) → −1, (x2 \| y2) → +1 (Maus x1 ≤ x < x1 + 8,
  y1 ≤ y < y1 + 9). Quelle der Grafik: Bild kb\4 (Puffer DGROUP 0xC117, geladen beim Spielstart Datei 0x438B5).
- **Regel:** Linksklick auf einen Pfeil → gedrückten Pfeil zeichnen: Quelle (22 \| 19 + 9 · j), 8 × 7, Ziel
  (x + 2 \| y + 1), j = 0 links/oben, 1 rechts/unten. Halten = 0: Richtung in DGROUP 0x5350 merken und sofort ±1
  liefern (der Aufrufer ruft weiter, solange die Taste unten ist). Halten ≠ 0: warten bis Ereignis 2 (Taste los), dann
  ungedrückt zeichnen (Quelle (21 \| 1 + 9 · j), Ziel (x + 1 \| y + 1)) und ±1 liefern. Ereignis 2 bei gemerkter Richtung:
  ungedrückt zeichnen (Quelle (22 \| 1 + 9 · j), Ziel (x + 2 \| y + 1)), Merker 0, Rückgabe 0. Sonst 0.
- **Remake:** `AdvertisingScreen` (Trefferflächen 0x120:0x3E bei (138 | 183)/(138 | 192), gedrückte Darstellung
  [APPROX] fehlt). → K4.

## D. Infofile

### Datei 0x432EC – Infofile anzeigen (Ov 0x110, 682 B)

- **Aufrufer:** nur das Hauptmenü 0x44E22, Datei 0x453E5–0x453F8: **Linksklick genau auf Pixel (319 \| 0)** (Maus-y = 0
  und Maus-x = 319), sonst nichts – ein verstecktes Feld ohne Kachel.
- **Ruft:** freier Speicher 0x13024, Fenster zu 0x0B447 (127), Datei öffnen/lesen/schließen 0x12F67/0x12FAA/0x12F59,
  Speicher 0x0C729/0x0C789, Hintergrund 0x45D46, Schrift 0x0BECD, Text 0x0B6A5/0x0B651, Linie 0x0B7B5, Zeichenbreite
  0x13208, Umlautersatz 0x43596, Bildschirm 0x0C5CE, Warten 0x0B908.
- **Regel:**
  1. Weniger als 21 000 Byte frei → zurück. Alle Fenster zu. Datei `infofile` (DGROUP 0x51C2) öffnen; fehlt sie:
     „KEIN INFOFILE VORHANDEN !“ zentriert x 0 … 319, Grundlinie 100, Farbe 7, Schatten 0; Klick abwarten; zurück.
  2. Bis 20 000 Byte lesen. Seitenkopf: Hintergrund kb\22 (0x45D46), Schrift 3, „Neueste Informationen zum
     Eishockey-Manager“ zentriert x 0 … 319, Grundlinie 7, Farbe 7, Schatten 0; Linie (0 \| 9)–(319 \| 9) Farbe 0;
     Bildschirm ein; Schreibstelle x = 0, Grundlinie 19.
  3. Je Byte: LF → nächste Zeile (Grundlinie + 8); TAB bei x < 285 → x auf das nächste Vielfache von 20; „#“ →
     Seitenende; sonst Zeichen über 0x43596 umsetzen (Latin-1 ä Ä ü Ü ß ö Ö → `{ [ } ] ~ \ \`, Tabelle DGROUP 0x512E/
     0x5136; ö wird so wie Ö gezeichnet) und linksbündig ab x zeichnen (Farbe 7, Schatten 0), x + Breite. x > 310 →
     nächste Zeile. Grundlinie > 230 → Seitenende. CR wird wie ein Zeichen behandelt.
  4. Seitenende: „TASTE!“ ab x 275, Grundlinie 238, Klick abwarten, neuer Seitenkopf. Dateiende: „TASTE!“, Klick,
     Bildschirm aus, Puffer frei.
- **Beleg Datei:** `infofile` des Originals hat 1153 Byte, 34 Zeilen (CRLF), CP437-Umlaute (0x81, 0x84, 0x94, 0xE1),
  kein „#“ → zwei Seiten (27 Zeilen je Seite). Die CP437-Umlaute treffen die Latin-1-Tabelle nicht und gehen
  unverändert an den Font.
- **Remake:** `UI/Screens/InfoFileScreen.cs` (Runde 11, Spur `ui-meldungen` Teil 2, K5); Seitenaufbau nach Regel oben, Test `InfoFileAndRankCurveTests`. Offen: Bildaufbau im Video nicht belegt; Schatten [APPROX].

## E. Tabellenkurve

### Datei 0x445E0 – Tabellenkurve (Ov 0x110, 1128 B)

- **Aufrufer:** Infoleiste 0x43E21 (Datei 0x445A6: Ursprung x 3, Grundwert y −2, eigener Verein, Gegner) und
  Vereinsinfo 0x4D385 (Datei 0x4D588: x + 30, y − 30). **Ruft:** Nationalteam? 0x4D2F0, Platz am Spieltag 0x43BED,
  Linie 0x0B7B5, Farbe/Stift/Linie 0x13495/0x1322A/0x13243, |a − b| 0x4BDE6, Text 0x0B6A5.
- **Daten:** Platz am Spieltag k = Halbbyte der Tabelle DGROUP 0x828B + 0x50 · Verein + k / 2 (gerade k: Bit 0–3,
  ungerade: Bit 4–7), 0 = Platz 1 (Vereinssatz der Ligatabelle +0x31, siehe `reverse-engineering.md`, „Ligatabelle“).
- **Regel** (Ursprung X, Grundwert Y; Infoleiste: X = 3, Y = −2):
  - Ist einer der beiden Vereine ein Nationalteam → nichts.
  - Achsen: senkrecht (X + 2 \| Y + 48)–(X + 2 \| Y + 71) Farbe 6; waagrecht (X + 3 \| Y + 72)–(X + 98 \| Y + 72) Farbe 8.
  - n = DGROUP 0x7A9 (Spieltag). n ≤ 1 → „Vor dem ersten Spieltag.“ (Zeiger DGROUP 0x674).
  - Schritt d = (8500 / (n − 1)) / 100 (ganzzahlig, also ⌊85 / (n − 1)⌋); d = 1 → 2.
  - Eigener Verein Farbe 24, Gegner Farbe 7 (nur wenn Gegner ein Verein ist, < DGROUP 0x6AD): für k = 0 … n − 2 mit
    p = Platz(k): k = 0 Stift auf (X + 3 \| Y + 49 + 2 p); jeweils Linie bis (X + 2 + (k + 1) · d \| Y + 49 + 2 p).
  - Platzzahlen (p + 1 mit „.“) rechts; Verschiebung bei |Δy| < 7 (Datei 0x44801–0x44891, schon dokumentiert).
- **Folgerung für die Infoleiste:** Plätze 1 … 12 auf y 47 … 69 (2 Pixel je Platz), x ab 6, Länge ⌊85 / (n − 1)⌋ je
  Spieltag (bei 43 Spieltagen 2 Pixel, also über die Achse hinaus). Die Kurve endet beim Platz nach dem vorletzten
  Spieltag – der letzte Punkt liegt eine Schrittweite rechts davon.
- **Remake:** `UI/Screens/RankCurve.cs` (Runde 11, Teil 2, K6): Achsen, Schritt ⌊85 / (n − 1)⌋, y 47 + 2 p, am Bild `folge06_00000` und `oberflaeche_0842` vermessen. [APPROX] bleibt: Lage der Platzzahl (±1 px) und Verschiebung bei |Δy| < 7; Vereinsinfo 0x4D385 (x + 30, y − 30) nicht angeschlossen.

## F. Tagesablauf

### Datei 0x07C7E – freie Poolspieler auf die Ligen verteilen (Basis 0x46E, 627 B)

- **Aufrufer:** nur die Tagesfunktion 0x54C9F im Saisonwechsel (Datei 0x56631; vorher Pokale 0x30E7B/0x3139F/0x341AB,
  DGROUP 0xCCB2 löschen, DGROUP 0xCFFA = 0xFF, DGROUP 0xD074 = 0). Gegenstück beim Spielstart: Datei 0x057E3 (Aufruf
  0x43A83, gleiche Bausteine; nicht Teil der Spur).
- **Ruft:** Datei 0x07F6B, Datei 0x05521, `random` 0x0C037, Datei 0x07EF1.
- **Daten:** Spielerpool far DGROUP 0x8253 (33 Byte, Index 1 … 179 geprüft): +0x1A Besitzer, +0x1D Verein. Ligagrenzen
  DGROUP 0x7C6/0x7C7 + 2 · l: 0 … 11, 12 … 23, 24 … 35, 36 … 42.
- **Regel:**
  1. Für l = 0 … 2 Vereinsliste der Liga nach 0x07F6B (Vereine nach Stärke 0x368B1 absteigend sortiert; das Ergebnis
     wird **nicht benutzt**, siehe 0x07EF1).
  2. Soll je Liga s[l] aus 0x05521 (s[3] = 0). Für jede Liga l und jeden Poolspieler mit Verein in l: s[l] − 1 (nicht
     unter 0); hat er Besitzer > 3 (Transfermarkt 4 oder frei), kommt er in die Liste frei[l].
  3. Für l = 0 … 3, solange s[l] > 0 (höchstens 1000 Durchgänge): für q = 0 … 3, q ≠ l, der Reihe nach: zufälliger
     Listenplatz von frei[q] (`random(0, Anzahl − 1)`, bis 1000 Versuche auf einen belegten Platz), Spieler aus der Liste
     nehmen, Verein +0x1D = 0x07EF1(l); Anzahl q − 1, s[l] − 1; dieselbe Liga q weiter, solange beides > 0.
- **Datei 0x07EF1** (122 B, auch aus 0x057E3): l > 2 → `random(64, 150)`; sonst Verein = DGROUP 0x7C0[l] +
  DGROUP 0x48FE[z] mit dem Zähler z = DGROUP 0x48FB[l] (Anfang 0; danach z + 1, nach 16 wieder 0); Managerverein
  (Vereinsfeld +0x16 ≠ 0) → nächster Zählerwert. Die 17 Einträge DGROUP 0x48FE sind 0 0 1 1 2 2 3 3 4 4 5 6 7 8 9 10 11 –
  die ersten fünf Vereine einer Liga (Datenbankreihenfolge, nicht Stärke) bekommen doppelt so viele Spieler. Das zweite
  Argument (die sortierte Liste) wird nicht gelesen.
- **Datei 0x05521** (Basis 0x238, 706 B; Fundstelle in `spielberechnung.md` nur zufällig): Kadergröße je Liga
  k[l] = Σ der belegten Kaderplätze (0x4BE17 Modus 0) der Manager in Liga l; je Poolspieler 0x054D9 auf +0x15 und +0x16
  (nicht geprüft); Spieler mit Verein ≥ 64 (Ausland, ≠ 0xFF) zählen; random(5, 15)-mal einen zufälligen Spieler
  1 … 179 nehmen und, solange mehr als 4 im Ausland sind, einem Auslandsspieler einen neuen Verein über 0x059B8 geben;
  dann s[l] = k[l] · 100 / Σk · (179 − Auslandsspieler) / 100 (LXMUL/LDIV; Rundungsfolge nicht im Einzelnen geprüft).
- **Remake (Runde 11, Spur `kader-transfer`, Teil 5, PR #453):** `Core/Transfer.PoolDistribution.cs`: Soll je Liga, Drift
  (0x054D9) und Auslandsrückholung (0x05521, 0x059B8), Zielverein reihum mit doppelten ersten fünf (0x07EF1, Zähler
  DGROUP 0x48FB im Spielstand), Spielstart 0x057E3, Saisonwechsel 0x07C7E (nach den neuen Namen, nicht im Modus „nur
  Weltkugel“). `TransferMarket` erneuert daneben weiter Namen und Vereine der Plätze mit Besitzer 6 (0x5ACA0 aus
  0x56467, `Transfer.Pool.cs`). Test `KaderTransferTeil5PoolTests` (Goldtest `Start_OriginalDatabase_MatchesFile05Distribution`,
  Mehrsaisonprobe). → K7, K8 erledigt.

### Datei 0x4F325 – Tabellenfelder sichern/herstellen (Ov 0x128, 463 B)

- **Aufrufer:** nur 0x50B45 (Datei 0x50C2C/0x50C3D: Modus 1, von 0 nach 2 für beide Vereine einer Paarung;
  Datei 0x50C57/0x50C68: Modus 0, von 2 nach 0). **Ruft:** 0x0390B (Bilanz).
- **Argumente:** (Verein, von, nach); Satz DGROUP 0x825A + 0x50 · Verein.
- **Regel:** kopiert die Bytes +0/+1 (Punkte), +0x16/+0x17 (Tore), +0x1A/+0x1B (Gegentore), +0x26 (Siege Heim), +0x2A
  (Niederlagen Heim), jeweils von +von nach +nach, und die Bilanz +4 + 9 · (von / 2) nach +4 + 9 · (nach / 2).
- **Befund:** Nicht gesichert werden Spiele (+0x1E/+0x1F), Siege auswärts (+0x27) und Niederlagen auswärts (+0x2B).
  Das ist unschädlich, solange zwischen Sichern (Tagesbeginn) und Herstellen (Spielende) kein Ergebnis eingetragen wird –
  das ist im Original der Fall (je Spieltag ein Modus-0-Aufruf).
- **Remake:** Die Tabelle wird aus den Ergebnissen gebildet; eine Sicherung ist nicht nötig. Kein Kandidat.

## G. Fehler und Bildlader, Overlay 0xC0

### Datei 0x25B4A – Fehlerhalt (Stub 0xC0:0x61, 615 B)

- **Aufrufer:** 15 Stellen; Codes: 0 (2×), 1, 2 (5×), 3, 4 (10×), 99 (4×). **Ruft:** 0x0390B, Bildschirm löschen
  0x134C2, Schrift 0x0BECD, Palette 0x1358F, Text 0x0B6A5, 0x0C5CE, Pause 0x130EE, 0x0B830, Speichern 0x5B9DD,
  Warten 0x0B908, Freigaben 0x0BEFA/0x0C789, Cache 0x2598F/0x25A6F/0x255ED, EMS frei 0x10D84, `exit(0)` 0x0218F.
- **Regel:** Code 99 = Programmende ohne Meldung und ohne Sichern (Sprung zu Datei 0x25CC4). Sonst Text nach Code
  (Sprungtabelle cs:0x7E5): 0 „GRAFIK-Error“, 1 „LIB-Error“, 2 „DISK-Error“, 3 „FONT-Error“, 4 „SPEICHER-Error“,
  5 „BildLade-Error“, größer „Alles klar!“. Bildschirm löschen, Schrift 1, Palette 17 = (60, 0, 0), Text zentriert
  x 0 … 320, Grundlinie 120, Farbe 17; Bildschirm ein, Pause 10. Dann Manager am Zug 0 (DGROUP 0xA70D/0xA70E), Tagesstand
  herstellen (DGROUP 0x3B7 ← 0xE2B5, 0xA969 ← 0xE2AC, 8 Byte DGROUP 0xE2AD → 0x7A9; gesichert am Tagesbeginn Datei
  0x5507A–0x5508F) und als `ERROR.HOK` speichern; Schrift 3, „SPIELSTAND WURDE GESICHERT (ERROR.HOK) - <TASTE
  DR]CKEN>“ (Zeiger DGROUP 0x14E9; „]“ ist Ü im Font) auf Grundlinie 140; Klick abwarten. Ende (auch Code 99): Bilder
  DGROUP 0xC13F, 0xC12F, 0xC113 frei, Cache räumen, Spielerpool und Vereine frei, Bildschirm ein, Maus und Puffer frei,
  EMS-Kennung frei, `exit(0)`.
- **Remake:** Ausnahmen beenden das Programm ohne Spielstand; `AutoSave` (autosave.hok) besteht. → K10 (P3).

### Datei 0x25764 – EMS-Bildcache füllen (Ov 0xC0, 459 B)

- **Aufrufer:** nur der Spielstart 0x437D5. **Ruft:** EMS 0x10C80/0x10CD2/0x10D10, Cacheeintrag 0x25AB5, Datenbank
  0x5AC03, Dateien 0x12F67/0x12FAA/0x12F59, Speicher 0x0C729/0x0C789, Kopie ins EMS 0x25644, Räumen 0x2598F, 0x25613,
  Bildlader 0x0C17D (Modus 2 = in den Cache), Fehlerhalt 0x25B4A (Codes 2, 4).
- **Regel:** Ohne EMS (DGROUP 0xDE29 = 0) nichts. Sonst Seitenrahmen DGROUP 0xDE2D/0xDE31 und Kennung DGROUP 0xDE27
  holen und die Liste DGROUP 0x49DA (105 Nummern bis 0xFF) laden: Nummer ≤ 229 → Bild kb\n über 0x0C17D in den Cache;
  230 → Datenbankpuffer (0x5AC03, 0x9088 Byte); sonst Datei `kb\n.vga` roh: 236 … 247 mit fester Größe aus der Tabelle
  DGROUP 0x4A96 (12 Wörter), 231 … 235 mit der Größe aus den ersten 2 Byte; Eintrag +4 = n, Inhalt ins EMS; reicht
  der Platz nicht, Cache räumen. Fehlende Datei → Code 2, kein Speicher → Code 4.
- **Folgerung:** kb\231–kb\247 sind Rohdaten (Klänge 237 … 241 laut `docs/formate/dateiformate.md`); für `re-livespiel`
  ist die Liste DGROUP 0x49DA die Übersicht der beim Spielstart gebrauchten Dateien.
- **Remake:** lädt alle Dateien in den Speicher; kein Cache nötig. Kein Kandidat.

## Kandidaten Runde 11

| Nr. | Prio | Ziel | Dateien | Testidee |
|---|---|---|---|---|
| K1 | P2 | Meldung/Frage 0x4C246 als ein Baustein: Aufbau nach Tabelle in Abschnitt C (Symbolfelder 83/212 \| 112 mit eingerücktem Umriss, Meldung „!“ 16/17, Frage „?“ 12/14, Textgrundlinien, untere Linie Farbe 0), Rückgabe 1/0, nur Linksklick | neue Datei `src/EishockeyManager.UI/OriginalDialog.cs`; `QuitQuestion.cs`, `DebCupDrawLive.cs`, `AdvertisingScreen.cs` umstellen | Pixeltest gegen Video 3:46 (DEB-Pokal-Frage); Test: rechte Taste/Escape schließen nicht |
| K2 | P2 | Kurzmeldung 0x0C0B8: Meldung ohne Knopf, schließt nach 75 (bzw. 45 bei „Auto-…“) / 3 Takten ≈ 1,4 s (0,8 s), keine Eingabe | `MainMenuScreen.cs` (Bundestrainer-Warnung), Transfermarkt- und Aufstellungsmeldungen der Aufrufer 0x40364/0x465F1/0x3B8F7 | Test: Meldung ohne Knopf, nach Ablauf zu; Klick verlängert/verkürzt nicht |
| K3 | P3 | `TeamUi.MessageBox` durch K1 (Modus 0) ersetzen, wo das Original 0x4C246 ruft | `TeamUi.cs` und Aufrufer | Bildschirmfoto je Aufrufer |
| K4 | P3 | Pfeilpaar 0x4BA58: gedrückte Darstellung aus kb\4 (22 \| 19/28), Loslassen (22 \| 1/10), Wiederholung bei gehaltener Taste | `AdvertisingScreen.cs` (und andere Pfeile mit 0x120:0x3E) | Pixeltest gedrückt/losgelassen |
| K5 | P2 | Infofile 0x432EC: versteckter Klick (319 \| 0) im Hauptmenü, Seitenaufbau nach Abschnitt D | neuer Bildschirm `src/EishockeyManager.UI/Screens/InfoFileScreen.cs`; Anschluss im Hauptmenü (gemeinsame Datei, minimal) | Test: Seitenumbruch nach 27 Zeilen, TAB-Raster 20, „#“; fehlende Datei → Meldung |
| K6 | P2 | Tabellenkurve 0x445E0 genau: Achsen, y 47 + 2 p, Schritt ⌊85 / (n − 1)⌋ (mindestens 2), Endpunkt-Versatz | `MainMenuScreen.cs` (DrawRankCurve), Vereinsinfo (0x4D385) | Pixeltest gegen Videobild des Hauptmenüs (z. B. 13:07) |
| K7 | P2 | Saisonwechsel 0x07C7E (mit 0x05521, 0x07EF1): freie Poolspieler in Ligen mit Fehlbedarf umhängen | `src/EishockeyManager.Core/Transfer.Pool.cs` bzw. eigene Datei, Anschluss in `GameState.Integration.cs` (Saisonwechsel) | Mehrsaisonprobe (4 Wechsel): Verteilung der Poolvereine je Liga; Vereine 0–4 einer Liga doppelt so oft Ziel |
| K8 | P3 | Offene Teile von 0x05521 nachprüfen (0x054D9 auf +0x15/+0x16, Rundung) vor K7 | nur Doku | Spielstandvergleich vor/nach Saisonwechsel (`emtool hok`) |
| K9 | P3 | Bildschirmwechsel mit Zwischenbild kb\22 (0x0C5CE/0x45D46) | `ScreenHost` | Bildfolge beim Wechsel |
| K10 | P3 | Fehlerhalt: bei Absturz Spielstand des Tagesbeginns als `ERROR.HOK` sichern und Meldung zeigen | `src/EishockeyManager.Desktop`, `SaveLoad` | Test mit erzwungener Ausnahme |

**Stand nach Integration Runde 11:** K1, K2, K3, K5, K6 durch die Spur `ui-meldungen` (PR #421, #426, #430) umgesetzt
(Remake-Zeilen in den Abschnitten C, D und E); K7 und K8 durch die Spur `kader-transfer` Teil 5 (PR #453). Offen bleiben
K4, K9 und K10.

## Offen

- 0x0B447: welche Werte bei „Alle schließen“ in DGROUP 0x5F4D ff. stehen (Laufzeit, DOSBox).
- 0x05521: Wirkung von 0x054D9 auf die Spielerfelder +0x15/+0x16 (geklärt in Runde 11, PR #453: Kondition/Technik des Poolspielers, Drift ±; Tabelle in `reverse-engineering.md` berichtigt) und genaue Rundung der Sollzahl (offen).
- 0x130EE: Takt 18,2 Hz angenommen (BIOS-Zähler 0040:006C); ob 0x11FD den PIT umprogrammiert und den BIOS-Zähler
  schneller zählen lässt, ist nicht geprüft.
- 0x0BE71: Ersatzliste DGROUP 0x4A54 ist in der Datei gleich der Suchliste; ein Schreiber zur Laufzeit wurde nicht gesucht.

## Selbstprüfung

`python3 tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --csv <scratch>/karte.csv`
(ohne `--write`): alle 24 Funktionen der Spur nennen in der Spalte `fundstellen` diese Datei (Ergebnis im PR).
