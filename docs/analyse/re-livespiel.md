# Analyse em.exe: Livespiel Ov 0x130 (Spur `re-livespiel`, Runde 10)

Alle Adressen in dieser Datei sind **Dateioffsets** in `em.exe` (Schreibweise und Umrechnung wie in
[re-abdeckung.md](../re-abdeckung.md)); Werte im Datensegment stehen ausdrücklich als `DGROUP 0x…`. Overlay 0x130
(Ghidra 5E73) liegt ab Datei 0x51E90; Stub-Einträge stehen als `0x130:0x75` (= Datei 0x5353C). Hilfsfunktionen der
Laufzeit, der Fenster- und Grafikbasis stehen nur mit ihrem Fernaufruf in der Schreibweise des Plans (`30h:885h`), damit
die Funktionskarte sie nicht dieser Datei zuordnet. Bedeutung aus der Benutzung hier (Argumente in C-Reihenfolge,
erster Parameter zuletzt geschoben):

| Aufruf | Bedeutung |
|---|---|
| `30h:800h` | Rechteck füllen `(Fläche, x1, y1, x2, y2, Farbe)` |
| `30h:885h` | Text `(Fläche, Text, x1, x2, Grundlinie, Farbe, Schatten)`, zentriert zwischen x1 und x2 (x2 = −1: linksbündig); Schatten 0xFF = keiner |
| `30h:831h` | Text `(Fläche, Text, x, y, Farbe)` linksbündig |
| `30h:995h` | Linie `(Fläche, x1, y1, x2, y2, Farbe)` |
| `30h:787h` | Rahmen `(Fläche, x1, y1, x2, y2, Farbe)` |
| `30h:627h` | alle Fenster schließen („Fenster_Zu“) |
| `38h:6DDh` | Schrift wählen `(Fläche, Nr 0–3)`: 1 = Flow (Überschriften), 2 = Mittel, 3 = Mini |
| `38h:98Dh`, `38h:70Ah` | Bild `kb\N` laden (N, 0, 0, 0, Schalter) → Fläche; Fläche freigeben |
| `38h:DDEh` | Bildschirm aus- (0) bzw. einblenden (1) `(Grafikkontext DGROUP 0xC157, Schalter)` – `[APPROX]` Palettenblende, Inhalt der 347 Byte nicht gelesen |
| `38h:10Eh` | auf den Strahlrücklauf warten (Port 0x3DA Bit 3; das Argument wird nicht benutzt) |
| `38h:118h`, `38h:8A7h` | auf Mausklick warten; Mausereignisse verwerfen |
| `38h:2C2h` | Zahl als Text (Mindestbreite `DGROUP 0x374`) |
| `38h:847h`, `38h:7E5h` | `random(a, b)`, `lrandom(a, b)` (beide Grenzen inklusive) |
| `50h:19Eh` | Pause `(n)`: wartet n / 3 BIOS-Ticks (Zähler 0040:006C, 18,2 Hz), also n · 18 ms |
| `50h:A79h` | Blit `(Quelle, qx, qy, Ziel, zx, zy, Breite, Höhe, Modus 0xC0 = Farbe 0 durchsichtig)` |
| `50h:BD3h` | Rechteck kopieren `(Quellbild, qx, qy, Zielbild, zx, zy, Breite, Höhe)`, deckend |
| `48h:4675h` | Rechteck aus VGA-Seite 1 (Versatz 0x4B00) in die sichtbare Seite 0 zurückholen `(0x4B00, 0, x, y, Breite, Höhe)` |
| `48h:2EF4h` | ganze VGA-Seite kopieren `(Quelle, Ziel)`; `(0, 0x4B00)` sichert den Bildschirm als Hintergrundkopie |
| `48h:2635h`, `48h:267Eh`, `48h:2EC4h` | Grafikmodus/Schreibmodus umschalten (nur Mechanik, nicht nachgebaut) |
| `110h:7Ah` | Hintergrund `kb\22` auf den Bildschirm legen (Kopie in Seite 2, Versatz 0x9600); bei `DGROUP 0x541C` ≠ 0 vorher ausblenden |
| `120h:61h`, `0:3CAh`, `0:1A9Ch`, `0:1B0Bh`, `0:1B34h` | Betrag `\|a − b\|`; 32-Bit-Multiplikation; `strcat`, `strcpy`, `strlen` |
| `F0h:20h`, `F0h:8Eh` | Strafbank zählen (Datei 0x35540, `spielberechnung.md` §6); Kader neu zählen (0x38A36, §16) |
| `118h:43h` | Aufstellung/Taktik aus dem Menü mit Kennwert (`wettbewerbe.md`, `CupsNational.Lineup.cs`) |
| `108h:4Dh`, `18h:199Ah` | automatische Aufstellung (0x426BC); Verletzung eintragen (0x05B1A, §17) |

Belege: Disassembly mit `ndisasm` über die Funktionskarte (`tools/reverse-engineering/funktionskarte.py`, Grenzen und
Aufrufer aus der CSV), DGROUP-Texte und -Tabellen aus em.exe, Referenzbilder `bundestrainer_00200` (Anzeigetafel mit
Reporter, Freundschaftsspiel Deutschland – Schweden), `karriere_02315` („Spiele laufen“) und DOSBox
`nur-weltkugel/37` (Kasten leer, vor der ersten Minute). Ein Hilfsskript (Scratch, nicht im Repo) hat die Overlays
annotiert; `xref` über alle Spielsegmente lieferte die Schreiber der Anzeigetabellen.

## Übersicht

| Adresse (Plan) | Bereich | Rolle laut Code | Karte vorher | Ergebnis |
|---|---|---|---|---|
| Datei 0x51F3D | Ov 0x130 + 0xAD | Spielende-Bildschirm „Das wars… / Carpe diem !“ | unbekannt | zugeordnet, P3 |
| Datei 0x51FEA | Ov 0x130 + 0x15A | Spielstand einer Anzeigereihe aus dem Ergebnisbyte | unbekannt | zugeordnet, P1 |
| Datei 0x52190 | Ov 0x130 + 0x300 | zwei zweistellige Zahlen aus `kb\38`-Ziffern („HH:GG“) | unbekannt | zugeordnet, P1 |
| Datei 0x5224E | Ov 0x130 + 0x3BE | Reporter: Mundfolge aus einem Text (Vokale) | unbekannt | zugeordnet, P1 |
| Datei 0x522EC | Ov 0x130 + 0x45C | Reporter: Augenzwinkern | unbekannt | zugeordnet, P1 |
| Datei 0x52382 | Ov 0x130 + 0x4F2 | Reporter: Mundbewegung (7 Bilder) | unbekannt | zugeordnet, P1 |
| Datei 0x52430 | Ov 0x130 + 0x5A0 | Reporter: Textzeile sprechen | unbekannt | zugeordnet, P1 |
| Datei 0x52BD6 | Ov 0x130 + 0xD46 | Anzeigetafel je Managerpaarung (Reihen, Energie, Einsatz, Angriff, Strafbank, Drittelwerte, Uhr) | unbekannt | zugeordnet, P1 |
| Datei 0x53234 | Ov 0x130 + 0x13A4 | eine Ziffer 8 × 10 aus `kb\38` | unbekannt | zugeordnet, P1 |
| Datei 0x53307 | Ov 0x130 + 0x1477 | senkrechter Balken (2 Spalten) | unbekannt | zugeordnet, P1 |
| Datei 0x533D0 | Ov 0x130 + 0x1540 | waagerechter Balken (2 Zeilen) | unbekannt | zugeordnet, P1/P2 |
| Datei 0x53453 | Ov 0x130 + 0x15C3 | Lampe an/aus | unbekannt | zugeordnet, P1 |
| Datei 0x534BC | Ov 0x130 + 0x162C | Treffer-Test Schieberegler → Wert 1…n | unbekannt | zugeordnet, P2 |
| Datei 0x53508 | Ov 0x130 + 0x1678 | Treffer-Test 6 × 6 (Lampe) | unbekannt | zugeordnet, P2 |
| Datei 0x5353C | Ov 0x130 + 0x16AC | Live-Bildschirm aufbauen (`kb\19`, Paarungsreihen, Logos/Köpfe, Zuschauer) | unbekannt | zugeordnet, P1 |
| Datei 0x53BF9 | Ov 0x130 + 0x1D69 | Trikotfarben: zu ähnliche Farbe des Gastes invertieren | unbekannt | zugeordnet, P3 |
| Datei 0x5434C | Ov 0x130 + 0x24BC | Eingriff: Aufstellung/Taktik im Spiel, danach Live-Bildschirm neu | unbekannt | zugeordnet, P2 |
| Datei 0x56EDD | Ov 0x130 + 0x504D | Torszene: Kopfzeile (Namen, Tore, Minute, Zuschauerzeile) | unbekannt | zugeordnet, P3 |
| Datei 0x570E8 | Ov 0x130 + 0x5258 | Minutenzeile „NN.MINUTE“ (Torszene, Dauer-Eisfeld) | unbekannt | zugeordnet, P1/P3 |

Nebenbei gelesen, weil der Ablauf sie braucht (Status in der Karte bleibt): Datei 0x51E90 (Spieluhr, bisher nur
dokumentiert), Datei 0x52520 (Reporter-Meldung `0x130:0x48`, als Aufruf „umgesetzt“, Anzeige nie nachgebaut),
Datei 0x53C57 (Torszene, nur der Anzeigeteil), Datei 0x567C8 (Torszenen-Abspieler, bisher nur die Töne) und die
Anzeigeteile der Minutenschleife Datei 0x37A62 (Ov 0xF0).

## 1. Ablauf: wann das Overlay zeichnet

Die Spielberechnung ist unabhängig von der Anzeige (`spielberechnung.md` §1). Das Overlay 0x130 wird aus der
Minutenschleife Datei 0x37A62 (Ov 0xF0) gerufen; Schalter: `DGROUP 0x5A56` = Live-Anzeige für diesen Spieltag (gesetzt
in der Spielabrechnung Datei 0x54C63, wenn ein Verein zum Zusehen markiert ist, Vereinsfeld 0x34 Bit 7), `DGROUP 0x5A55`
= Darstellung 2 „Eisfeld bleibt an“, `DGROUP 0x226` = Darstellung des Live-Spiels 0/1/2 (Spielstand-Option 9,
`reverse-engineering.md` „Wirkung der Spielstand-Optionen“), `DGROUP 0x1DB` = Durchlauf ohne Halt, `DGROUP 0x262` =
Spielgeschwindigkeit 0–80 in Zehnerschritten (Regler der Optionen).

Je Drittel (Aufruf `0x37A62(…, Drittelanfang, Drittelende, Anzeige)`):

1. **Mit Anzeige** (Datei 0x37B35): Live-Bildschirm `0x130:0x75(Pokal, 1)` neu aufbauen. `DGROUP 0xE19D` wechselt je
   Drittel 0/1, dazu `DGROUP 0xE06A` = −`0xE06A` und `DGROUP 0xE06C` = Wort `DGROUP 0x4E81[0xE19D]` – der Seitenwechsel
   für die Torszenen.
2. **Ohne Anzeige und ohne Dauer-Eisfeld** (Datei 0x37B82–0x37C5C): Schrift 1, Text `DGROUP 0x620` → „Spiele laufen“
   zentriert über die ganze Breite auf Grundlinie 80, Farbe 30, Schatten 0; Rahmen `30h:787h` (3|183)–(123|218) Farbe 0,
   Füllung (4|184)–(122|217) Farbe 3; einblenden; dann die Spieluhr `0x130:0x20(Stil 1, Drittelanfang)`.
3. **Je Minute** (Datei 0x37C6B–0x37CBD, nur mit Anzeige, `[0x1DB]` = 0 und Geschwindigkeit < 70): mit
   `lrandom(0, 4)` = 0 ein Augenzwinkern `0x130:0x39` und Pause `50h:19Eh(30 − Geschwindigkeit / 2)`, sonst Pause
   `(45 − Geschwindigkeit / 2)` (Pause 45 ≈ 15 Ticks ≈ 0,8 s; bei 60 ≈ 0,3 s). `DGROUP 0xE19E` = Minute; bei
   Dauer-Eisfeld Strahlrücklauf und Minutenzeile `0x130:0xAC(Minute)`. Taste Leerzeichen (oder, bei `DGROUP 0x5A16` = 1,
   wenn das Fensterobjekt `0xC147` an +0 und +2 Null meldet – `[APPROX]` wohl die Maustaste) öffnet die
   Spielmodus-Optionen Datei 0x2CD1C (Geschwindigkeit in `DGROUP 0xE19B`, Darstellung in
   `0xE19C` gesichert), danach `0x130:0x75(Pokal, 1)` neu, Lampe (10|228) aus, einblenden (Datei 0x37D29–0x37D86).
4. Rechnung der Minute (§2 in `spielberechnung.md`): Chancen, Abschlüsse (Torszene `0x130:0x7F` aus Datei 0xA484),
   Strafen (Reporter `0x130:0x48` aus Datei 0x35818), Eingriffsmenü Datei 0x38D96 bei Klick in der Managerpaarung.
5. **Minutenende** (Datei 0x3854A): Anzeigetafel aller Reihen `0x130:0x52(0xFF, Pokal)` – ohne Anzeige zeichnet das
   nur die Spieluhr neu (Stil 1).
6. **Nach Minute 60 bzw. 70** mit Anzeige und Geschwindigkeit < 70: auf Mausklick warten (`38h:118h`), dann Hintergrund
   `110h:7Ah` (Datei 0x3855F–0x3858C). Nach den Dritteln 1 und 2 wird nicht gewartet.

Geschwindigkeit ≥ 70 ist der Schnelldurchlauf: keine Minutenpause, keine Reporter-Meldung (Datei 0x52534), kein Klick am
Ende. Bei 50 und 60 sind die Reporterschleifen verkürzt (§5).

## 2. Daten der Anzeige (DGROUP)

| Variable | Inhalt | Schreiber |
|---|---|---|
| `0xA762` | Zahl der Anzeigereihen (höchstens 4) | Spielabrechnung Datei 0x54C94 |
| `0xA75C + p` | Heimverein + 1 der Reihe p (0 = Reihe frei; Schleifenende in 0x5353C) | Datei 0x54C74 |
| `0xA74D + p`, `0xA747 + p` | Liga bzw. Pokal-Wettbewerb und Paarungsindex der Reihe p | Datei 0x54C82/0x54C90; Penaltyschießen Datei 0x39333/0x39341 |
| `0xA88C + 1 + p` | Heimverein der Reihe p (für die Torszene) | 0x5353C (Datei 0x53830) |
| `0x233 + 2p`, `0x234 + 2p` | Heim-/Gastverein der Reihe p, 0xFF = leer („Managerpaarung“ Datei 0x52B91) | 0x5353C (Datei 0x5359B, 0x53722, 0x538CF) |
| `0xA838` | laufendes Drittel 0–2 | Spieltagsablauf (nicht verfolgt) |
| `0xA839 + 14p + 2k + s` | Chancen der Reihe p im Drittel k (0–2) für Seite s (0 Heim, 1 Gast) | Torszene Datei 0x53CF6 (Art 0/1, Minute ≤ 60) |
| `0xA83F + 14p + 2k + s` | Tore je Drittel | Torszene Datei 0x53D1D (nur Art 1) |
| `0xA845 + 14p + s` | Strafminuten im Spiel | Strafen Datei 0x359DC |
| `0x5D99 + 6 · (Manager + 1)` | je Manager: +0 Kennbyte (2 = kein Heimspiel), +1 ausverkauft, +2 Zuschauer (long; + 200 000 bei `DGROUP 0xD0C0` = 2) | Spielabrechnung Datei 0x54A78–0x54BC3 |
| `0xE19E`, `0xE19A` | angezeigte Minute; Lampe (10\|228) | Minutenschleife; Eingriffsmenü |
| `0xE2A1`, `0xE2A2 + p` | Zahl der Manager in der Reihe; Reihe mit zwei Managervereinen | 0x5353C |
| `0xE262` ff. | Mundfolge des Reporters (bis 28 + 8 + 1 Byte) | 0x5224E |
| `0xC133`/`0xC11B` | Fläche `kb\38` und ihr Bildspeicher (Ziffern, Mund, Augen, Lampe); geladen im Spieltagsablauf Datei 0x37095 | |
| `0xC137`/`0xC123` | Hilfsfläche 113 × 35 für Texte (Datei 0x370BC, Schrift 3) | |
| `0xC143`/`0xC12B` | Arbeitsfläche = sichtbarer Bildschirm und ihr Bildspeicher (Datei 0x4365B/0x4367E) | |
| `0xC127` | Bildbogen der Managerköpfe, 38 × 38 im Abstand 39 (Datei 0x43978) | |
| `0x541C` | Schalter für `110h:7Ah`: vorher ausblenden | 0x5353C |

## 3. Bildquellen

| Bild | Ausschnitt (x\|y, Breite × Höhe) | Ziel | Fundstelle |
|---|---|---|---|
| `kb\19` | (0\|0) 313 × 44 | Reihenhintergrund (3\|y − 3) je Paarungsreihe | Datei 0x536AA |
| `kb\19` | (0\|45) 122 × 38 | Kasten unten links (3\|182): Spieluhr | Datei 0x535F9 |
| `kb\19` | (190\|60) 122 × 38 | Kasten unten rechts (194\|180): Reporter-Text | Datei 0x53627 |
| `kb\19` | (123\|45) 63 × 53 | Mitte unten (129\|187): Reporter | Datei 0x53658 |
| `kb\19` | (187\|45) 21 × 12 | (3\|226): Feld der Lampe | Datei 0x53B5E |
| `kb\38` | (Ziffer · 8\|0) 8 × 10 | Ziffern 0–9 | Datei 0x532A9 |
| `kb\38` | (0\|10) 8 × 10 | Arbeitszelle für eine Ziffer (wird überschrieben!) | Datei 0x53245–0x532FB |
| `kb\38` | (76\|10) 4 × 4 | Lampe „an“ | Datei 0x53462 |
| `kb\38` | (80\|0), (128\|0) 12 × 4 | Augen offen / geschlossen → (154\|203) | Datei 0x522FA–0x5237D |
| `kb\38` | (80\|4) 12 × 17; (92 + 12k\|4), k = 0…6 | Mund geschlossen; sieben offene Münder → (154\|207) | Datei 0x5239C–0x52429 |
| `kb\80 + (Vereinsfeld 0x34 & 0x3F)` bzw. `kb\143` | ganz, 38 × 38 | Vereinslogo in der Reihe (Vereine ≥ 0x40, also Europapokal-Gegner: 143) | Datei 0x537B3–0x5382C |
| Köpfe `DGROUP 0xC127` | (39 · Manager\|0) 38 × 38 | Managerkopf statt Logo | Datei 0x53749, 0x538F4 |
| `kb\22` | ganz | Hintergrund hinter den Reihen (`110h:7Ah`) | Datei 0x535C4 |
| `kb\231 … 247` | – | Torszenen-Bilder (Eisfeld je Hallenstufe 241–244, Figuren, Töne 237–241) | Datei 0x568BA–0x56A9C |

Texte (DGROUP, Umlaute nach DIN 66003): `0x543D` „Das wars...“, `0x5449` „Carpe diem !“, `0x5456` „Chance:“, `0x545E`
„Strafzeit:“, `0x5469` „Verletzt:“, `0x5473` „Penalty:“, `0x547C`/`0x549E` „Vergeben“, `0x5485`/`0x54A7` „TOR!“,
`0x548A` „2 Minuten“, `0x5494` „5 Minuten“, `0x54AC` „Vorlage: “, `0x54B6` „ min.“, `0x54BC` „:“, `0x54BE`
„ ZUSCHAUER“, `0x54C9` „AUSWÄRTS       “, `DGROUP 0x54D9` 15 Leerzeichen, `0x5515` „00“, `0x5518` „.MINUTE“, `0x194C`
„wegen “, Strafgründe `0x5CC` (13 Fernzeiger): „Stockschlags.“, „Ellbogenchecks.“, „hohem Stocks.“, „Haltens.“,
„Hakens.“, „Übertr. Härte.“, „Cross-Checks.“, „Faustschlags.“, „Bandenchecks.“, „Reklamierens.“, „Beinstellen.“,
„Torverschiebens.“, „Stockstichs.“; `0x5C8` Fernzeiger auf „wegen “; `0xD08` Fernzeiger auf „(ausw.)“ (Text DGROUP 0x2B9F);
`0x620` Fernzeiger auf „Spiele laufen“ (Text DGROUP 0x1A68); `0x541D` „25AEIOU“ (Vokaltabelle des Reporters, davor `0x541C`
der Blendschalter); Farbtabellen `0x5425` = 0x18, 0x13 (senkrechte Balken) und `0x5427` = 0x18, 0x13 (waagerechte
Balken).

## 4. Live-Bildschirm und Anzeigetafel

### Datei 0x5353C – Aufbau (`0x130:0x75`, 1725 B)

Aufrufer: Minutenschleife (Drittelanfang, nach den Optionen), Penaltyschießen Datei 0x3901B, Eingriff 0x5434C,
Torszenen-Abspieler 0x567C8. Argumente `(Pokal, erster Aufbau)`.

1. Ohne Live-Anzeige (`[0x5A56]` = 0) nichts. `[APPROX]` `50h:443h(Fläche, 137, 42)` = 0x11 → Rücksprung (vermutlich
   Pixelabfrage: Bildschirm steht schon).
2. „Erster Aufbau“ ≠ 0: ausblenden. Reihenliste `DGROUP 0x233` (5 Wörter) auf 0xFF. Erster Aufbau = 0: `0x541C` = 0,
   damit `110h:7Ah` nicht blendet; Hintergrund `kb\22`; `0x541C` = 1.
3. `kb\19` laden; Kästen unten (Bildtabelle §3).
4. Je Reihe p, solange `[0xA75C + p]` ≠ 0, y = 4 + 45p (Datei 0x536A9–0x53B4D): Reihenhintergrund; Heimverein =
   Pokal: `DGROUP 0x5CFB[32 · Wettbewerb + Paarung]`, Liga: `DGROUP 0x5D7B[12 · Liga + Paarung]` (Gast: `0x5CFC` bzw.
   `0x5D7C`); Vereine in `0x233`/`0x234` und `0xA88D` eintragen. Hat ein Manager m (m < `[0x386]`) den Verein, zählt
   `0xE2A1` hoch und sein Kopf (`0xC127`, 39m) kommt nach (96\|y) bzw. (182\|y); sonst das Logo `kb\80 + Logo`
   (Logo = Vereinsfeld 0x34 & 0x3F; Verein ≥ 64 → `kb\143`), geladen und gleich wieder freigegeben. Name in Schrift 3,
   Farbe 7, Schatten 0, zentriert zwischen 5 und 96 bzw. 224 und 317 auf Grundlinie y + 4.
5. Zuschauerzeile (Datei 0x53A11–0x53B37): Manager-Flag (Vereinsfeld 0x16, Manager + 1) des Heim-, sonst des
   Gastvereins → Zuschauer `DGROUP 0x5D99 + 6 · Flag + 2` (long; über 200 000 minus 200 000). Zahl ≠ 0 und keine WM
   (`DGROUP 0x12B` = 0): Zahl als Text (`DGROUP 0x38D` während der Umwandlung 0, `[APPROX]` vermutlich ohne
   Tausenderpunkt), Farbe 0x10, wenn sie gleich Sitz- + Stehplätzen des Managers ist (`DGROUP 0x94AC + 0x137 + 0x1AF · m`
   + `0x95DF + 0x1AF · m`, ausverkauft), sonst Farbe 0; andernfalls „(ausw.)“. Zentriert 48–82, Grundlinie y + 38, ohne
   Schatten. Haben beide Vereine einen Manager (`0xE2A1` = 2) → `0xE2A2 + p` = 1.
6. Lampenfeld, `kb\19` freigeben, Bildschirm als Hintergrundkopie sichern (`48h:2EF4h(0, 0x4B00)`), Anzeigetafel aller
   Reihen `0x52BD6(0xFF, Pokal)`, Lampe `0x53453(10, 228, [0xE19A])`, bei „erster Aufbau“ einblenden.

Beleg: `bundestrainer_00200` zeigt genau dieses Raster (Name/Flagge links, Spielstand oben Mitte, Drittelwerte, Kästen
unten; „(ausw.)“ unter dem linken Feld).

### Datei 0x52BD6 – Anzeigetafel je Reihe (`0x130:0x52`, 1630 B)

Aufrufer: Minutenschleife (Minutenende, 0xFF = alle Reihen), 0x5353C, Torszene 0x53C57 (nur der betroffene Verein).
Argumente `(Verein oder 0xFF, Pokal)`.

- Ohne Live-Anzeige: bei Dauer-Eisfeld nichts, sonst Strahlrücklauf und nur die Spieluhr `0x51E90(1, [0xE19E])`.
- Reihe p suchen (`0x233`/`0x234`); je Reihe y0 = 15 + 45p, x0 = 13 (Heim) bzw. 240 (Gast); Vereinssatz
  `DGROUP 0x9657 + 0x3D · Verein`. Zuerst (nur Heimseite) Strahlrücklauf und Spielstand `0x51FEA(p, Pokal)`.
- Je Seite und je Teil t (0 Abwehr, 1 Sturm): Reihe auf dem Eis r = Vereinsfeld 0x2E + 2t; **Lampen** k = 0…3 bei
  (x0 + 8 + 8t \| y0 + 7k − 1), an für k = r (`0x53453`); **Energiebalken** `0x53307(x0 + 24t, y0, 25,
  max(0, (Vereinsfeld 0x1E + 4t + r − 50) / 2))` (Energie der Reihe, `spielberechnung.md` §7).
- **Einsatz** Vereinsfeld 0x26 · 25 / 100 und **Angriff** (Vereinsfeld 0x2C Bits 4–6) · 25 / 7 als waagerechte Balken
  `0x533D0(x0 + 39, y0 + 9, 25, Einsatz)` und `(x0 + 39, y0 + 16, 25, Angriff)` – nach der Spieluhr gezeichnet.
- Hilfsfläche `0xC137` (Schrift 3, Farbe 7, `DGROUP 0x374` = 1): **Spalte A** (0\|5), (0\|12), (0\|19): Feldspieler
  (Vereinsfeld 0x32), Strafbank Platz 0 Restminuten (0x33 & 7), Platz 1 (0x33 >> 4 & 7) → Bildschirm
  (139 + 34s \| y0 + 6), 4 × 20. **Spalte B** (20\|5 + 8k): Chancen je Drittel `min(9, [0xA839 + 14p + 2k + s])` →
  (88 + 136s \| y0 + 5), 4 × 23. **Strafminuten** `[0xA845 + 14p + s]` + „ min.“ in Schrift 1, Farbe 0x10, zentriert
  40–74, Grundlinie 5 → (48 + 227s \| y0 − 3), 34 × 6. Nur Heimseite: **Tore je Drittel** „H:G“ aus `0xA83F`/`0xA840`
  (je min 9) bei (75\|5 + 8k) → (153 \| y0 + 4), 14 × 23. Jedes Zielrechteck wird vorher aus der Hintergrundkopie
  geholt (`48h:4675h`) und dann deckend kopiert (`50h:BD3h`).
- Danach Spieluhr `0x51E90(0, [0xE19E])` (einmal je Reihe). Bei Verein 0xFF weiter mit der nächsten Reihe, bis
  `0x233[2p]` = 0xFF (höchstens 4).

Beleg: im Bild `bundestrainer_00200` stehen „5 0 0“ senkrecht in Rot bei x ≈ 139/173, „0 0 0“ bei x ≈ 88/224, „0:0“
dreimal um x = 153, „0 min.“ oben links in beiden Feldern.

### Datei 0x51FEA – Spielstand aus dem Ergebnisbyte (`0x130:0x2A`, 422 B)

Aufrufer: Abschluss Datei 0xA484 (nach einem Tor), Penaltyschießen 0x3901B, 0x52BD6. Argumente `(Reihe p, Pokal)`; bei
p > 3 oder ohne Anzeige nichts. Je Seite s: Byte = Liga: `DGROUP 0xA0BF[0x210 · Liga + 12 · Spieltag(0x7A9 + Liga) +
Paarung + s]`, Pokal: `DGROUP 0x5C4B[32 · Wettbewerb + Paarung + s]` (laufende Pokal-Spielstände, `spielberechnung.md`
§2). Byte 0x5A = gewertetes Spiel: in der Liste `DGROUP 0xA958` (Liga) / `0xA938` (Paarung), Länge `0xA968`, die
Wertung `0xA928` (Heim) / `0xA918` (Gast) nachschlagen; sonst so lange 30 abziehen, bis der Wert ≤ 29 ist (die
Marken 0x1E in `spielberechnung.md` §4). Dann `0x52190(0, Heim, Gast, 139, 45p + 6)`.

### Datei 0x52190, 0x53234 – Ziffern (`0x130:0x2F`, `0x130:0x57`)

`0x52190(Stil, a, b, x, y)`: Ziffern a/10 bei x, a mod 10 bei x + 9, b/10 bei x + 21, b mod 10 bei x + 30 (der
Doppelpunkt steht im Hintergrund). `0x53234(Fläche, Stil, Ziffer, x, y)`: Arbeitszelle (0\|10) von `kb\38`
vorbereiten – Stil 0: Hintergrund der Zielstelle hineinkopieren (`50h:A79h` von `0xC12B`), Stil ≠ 0: mit Farbe 3 füllen
–, dann die Ziffer (Ziffer · 8\|0) 8 × 10 deckend in die Zelle (`50h:BD3h`), Zelle mit Durchsichtigkeit 0xC0 ans Ziel.
Das Remake liest die Ziffern schon so (`MatchDayScreen.DrawNumber`), setzt sie aber im Abstand 8 statt 10.

### Datei 0x51E90 – Spieluhr (`0x130:0x20`, 173 B, bisher nur dokumentiert)

`(Stil, Minute)`: drei waagerechte Balken `0x533D0(9, 192 + 8k, 79, 4 · (Minute − 20k))` (negativ → 0, über 79 →
79), also je Drittel ein Balken, der in 20 Schritten zu 4 Pixel vollläuft; Ziffern Minute/10 bei (96\|196) und
Minute mod 10 bei (106\|196). Beleg `karriere_02315`: zwei volle Balken, dritter halb, „08“ rechts daneben.

### Datei 0x53307, 0x533D0, 0x53453 – Balken und Lampe

- `0x53307(x, y, max, wert)` senkrecht: wert ≤ max; zwei Spalten x + 2 + k (k = 0, 1): von (x + 2 + k \| y + max)
  bis (… \| y + max − wert − 1) in Farbe `0x5425[k]` (0x18, 0x13), darüber bis y in Farbe 0x12.
- `0x533D0(x, y, max, wert)` waagerecht: Rechteck (x \| y)–(x + max \| y + 1) Farbe 0x11, darauf zwei Linien
  (x \| y + k)–(x + wert \| y + k) in `0x5427[k]` (0x18, 0x13). Das Remake malt im Kasten „Spiele laufen“ je Balken
  eine Linie in einer Farbe (gelb/rot nach Drittel fertig/offen) – im Original laufen die Balken je Minute voll und
  sind zweizeilig.
- `0x53453(x, y, an)`: an → `kb\38` (76\|10) 4 × 4 durchsichtig nach (x + 1 \| y + 2); aus → Rechteck
  (x \| y + 1)–(x + 4 \| y + 5) Farbe 0x10.

### Datei 0x534BC, 0x53508 – Treffer-Tests des Eingriffsmenüs

Beide nur aus dem Eingriffsmenü Datei 0x38D96 (Ov 0xF0, nicht Teil dieser Spur). `0x534BC(mx, my, x, y, breite, n)`:
liegt (mx, my) in x…x + breite, y…y + 6, Rückgabe `(mx − x) · n / breite + 1`, sonst 0 (Schieberegler, 1…n).
`0x53508(mx, my, x, y)`: 1, wenn (mx, my) in x…x + 5, y…y + 5 (Lampenfeld).

### Datei 0x5434C – Eingriff im Spiel (`0x130:0x89`, 212 B)

Aufrufer: Eingriffsmenü 0x38D96 und Torszene 0x53C57 (nach einer Verletzung, Modus 1). `(Manager, Pokal, Modus)`:
`DGROUP 0xA70E` = Manager (alter Wert gesichert), Hintergrund `110h:7Ah`, Maus leeren, `118h:43h(Modus,
DGROUP 0x2D6[Manager], 1)` – der Aufstellungs-/Taktikbildschirm des Menüs mit Kennwert –, ausblenden, Fenster schließen,
Bildschirm schwarz, Kader des Managervereins neu zählen (`F0h:8Eh`), Live-Bildschirm `0x5353C(Pokal, 1)`, Schrift 3,
Maus leeren, einblenden, `0xA70E` zurück. Wirkung auf den Spielzustand: keine eigene, alles im Aufstellungsbildschirm.

## 5. Reporter

### Datei 0x52520 – Meldung (`0x130:0x48`, 1649 B, als Aufruf „umgesetzt“)

Aufrufer: Strafen Datei 0x35C73 (`spielberechnung.md` §6) und Torszene 0x53C57. Argumente `(Verein, Ereignis,
Spieler, Zweiter, Text)`: Ereignis 0/1 Chance vergeben/Tor, 2/3 Strafe 2/5 Minuten, 4 Verletzung, 5/6 Penalty
vergeben/Tor (Datei 0x52B75/0x52B83 Sprungtabellen); Spieler und Zweiter sind Spielerindizes (Satz
`DGROUP 0x8253 + 0x21 · i`; 0xFF bzw. ≥ 200 = keiner); Text = Fernzeiger (Verletzungsart). Ohne Live-Anzeige oder bei
Geschwindigkeit ≥ 70 nichts.

1. Hilfsfläche löschen (0\|0)–(111\|31). Nur bei Darstellung `[0x226]` = 0: Schrift 1; Kopftext „Chance:“ (0, 1),
   „Strafzeit:“ (2, 3), „Verletzt:“ (4), „Penalty:“ (5, 6); Mundfolge `0x5224E(Text)`; mit `lrandom(0, 1)` = 0
   Zwinkern. Schleife i = 0 … 3 (bei Geschwindigkeit 60: 0 … 1): i gerade → Fläche löschen, i ungerade → Text zentriert
   20–111, Grundlinie 24, Farbe 0; bei 50 Pause 5; Strahlrücklauf; Kasten (199\|183) 111 × 32 aus der Hintergrundkopie
   und dann die Hilfsfläche deckend; Pause 2; Mundfolge[i] = 0 → Mundbewegung `0x52382`, sonst Pause 4. (Der Kopftext
   blinkt also zweimal auf.)
2. Schrift 3, Pause `80 − Geschwindigkeit`; Vereinsname (`0x9657 + 0x3D · Verein`) sprechen `0x52430(Name, 0–111,
   Grundlinie 30, Farbe 7, Schatten 0)`; bei 50 und 60 Pause 25; Schrift 1. Ereignis 4 → Schritt 4.
3. Ereignistext „Vergeben“, „TOR!“, „2 Minuten“, „5 Minuten“, (4: keiner), „Vergeben“, „TOR!“. Bei Darstellung 0 wie
   Schritt 1 mit Schleife i = 0 … 3 (bei 60: nur i = 0), aber i gerade → Text zentriert 22–111, Grundlinie 24, i
   ungerade → löschen; Pause `(50 ? 1 : 0) + 1` statt 2.
4. Schrift 3, Fläche löschen, zuletzt kopierter Text (Ereignis- bzw. „Verletzt:“-Text) zentriert 10–111, Grundlinie
   24, Farbe 0. Zeile 1 = Spielername, wenn Spieler ≠ 0xFF. Zeile 2: Ereignis 0/1 mit Zweitem < 200 → „Vorlage: “ +
   Name des Zweiten; Ereignis 4 mit Zweitem < 200 → der übergebene Text; Ereignis 2 → „wegen “ + Strafgrund
   `0x5CC[random(0, 12)]`; Ereignis 3 → „wegen “ + `0x5CC[5]` („Übertr. Härte.“). Zeile 1 sprechen auf Grundlinie 18,
   Zeile 2 auf 26 (jeweils `0x52430`, 0–111, Farbe 0, Schatten 0). Pause `45 − 10 · (Geschwindigkeit > 49)`,
   Strahlrücklauf, Kasten (199\|183) 112 × 32 aus der Hintergrundkopie.

Beleg `bundestrainer_00200`: Kasten rechts unten „Chance:“ groß, darunter „SCHWEDEN“ klein – Schritt 2 (Darstellung 0).

### Datei 0x52430 – Zeile sprechen (`0x130:0x43`, 240 B)

`(Fläche, Text, x1, x2, Grundlinie, Farbe, Schatten)`: Text mit `30h:885h` auf die Hilfsfläche, Kasten (199\|183)
111 × 32 aus der Hintergrundkopie, Hilfsfläche (0\|0) 111 × 32 deckend dorthin; Mundfolge `0x5224E(Text)`;
`lrandom(0, 1)` = 0 → Zwinkern; bei Geschwindigkeit > 40 fertig, sonst je Eintrag der Folge: 0 → Mundbewegung,
9 → Pause 6, 0xFF Ende.

### Datei 0x5224E – Mundfolge (`0x130:0x34`, 158 B)

Zeichen des Textes der Reihe nach (Kleinbuchstaben → Großbuchstaben): ist das Zeichen eines aus `0x541D` „25AEIOU“ und
sind noch keine 28 Einträge da, wird ein Eintrag 0 angehängt und das **folgende Zeichen übersprungen**; danach acht
Einträge 9 und 0xFF. Rückgabe Fernzeiger `DGROUP 0xE262`. (Die Ziffern 2 und 5 zählen als Vokale für „2 Minuten“,
„5 Minuten“.)

### Datei 0x52382, 0x522EC – Mundbewegung und Zwinkern

`0x52382`: bei Geschwindigkeit > 40 nichts; sieben Bilder (92 + 12k \| 4) 12 × 17 aus `kb\38` nach (154\|207), je
Bild nach einem Strahlrücklauf, zum Schluss der geschlossene Mund (80\|4). `0x522EC` (auch je Minute aus der Schleife):
ohne Anzeige nichts; Strahlrücklauf, Augen (128\|0) 12 × 4 nach (154\|203), Pause 3, Strahlrücklauf, Augen (80\|0)
zurück.

## 6. Torszene (Anzeigeteil) und Spielende

### Datei 0x53C57 – Torszene (`0x130:0x7F`, umgesetzt; hier nur der Anzeigeteil)

Argumente `(Art, Liga/Pokal, Paarung, Verein, Gegner, Seite, Minute, Wettbewerb bzw. Manager, Kaderplatz-Flag,
Penalty)`; bei Art 4 (Verletzung) stehen an +0x12/+0x14 Kaderplatz und Manager (Satz `DGROUP 0x5DC3 + 0x618 · Manager +
0x34 · Kaderplatz`, Spielerindex bei +0xF). Reihe p = Index des Vereins in `0xA88D`; Statistik `0xA839`/`0xA83F` (§2).
Für den Managerverein mit weniger als 4 fitten Spielern (`120h:66h`) Rücksprung 0 (§17 in `spielberechnung.md`).
Anzeigeteil (Datei 0x53DCA–0x5406A, Art 0/1, Darstellung `[0x226]` ≠ 0): ohne Durchlauf Pause 30; Spielstand aus dem
Ergebnisbyte wie 0x51FEA (bei einem Tor vorher um 1 vermindert, weil das Byte schon zählt); Zuschauerzeile: Manager des
Vereins → Zahl + „ ZUSCHAUER“ und Kennbyte `0x5D99 + 6 · Flag`, ohne Manager „AUSWÄRTS“, Verein ≥ `DGROUP 0x6AD`
(Länderteam) Leerzeichen; Trikotfarben beider Vereine (Vereinsfeld 0x3B) durch `0x53BF9` entzerrt; Abspieler
`0x567C8(Verein, Gegner, Tore, Feldspieler beider, Farben, Seite, Art, Minute [0xE19E], Wettbewerb, Flag, 2 · [0x5A55],
Kennbyte, Zuschauertext)`; Pause 20; Taste `r` oder `w` wiederholt die Szene bis zu dreimal (`DGROUP 0xCC35`). Danach
Torschütze `0x541D9`, Reporter `0x52520(Verein, Art + 5 · Penalty, Schütze, Vorlage, 0)`; Art 4: Verletzung
`18h:199Ah`, Reporter mit Verletzungstext, dann Eingriff `0x5434C(Manager, Seite, …, 1)` bzw. ohne Anzeige
automatische Aufstellung `108h:4Dh`. Am Ende Anzeigetafel des Vereins (außer Art 4). Rückgabe Reihe + 1.

### Datei 0x567C8 – Torszenen-Abspieler (`0x130:0xA2`, umgesetzt nur für die Töne)

Modus 0 (Datei 0x56816–0x56AA7): freier Speicher `50h:D4h` < 0x412F8 → Torszenen aus (`[0x226]` = 0, `0xE19C` = 0),
Live-Bildschirm neu und Meldung „Sie haben zu wenig Speicher …“ (`DGROUP 0x13E5` ff.); sonst 14 Bilder `kb\231 + i`
laden, i = 5 durch die Hallenstufe ersetzt (`kb\241 … 244`), i = 12 → `kb\246`, i = 13 → `kb\247`, nur wenn
`DGROUP 0x5400[2i]` ≠ 0 (Größentabelle), Zeiger in `0xE2B9 + 4i`. Modus 1: Kopfzeile `0x56EDD(0, …)` und Eisfeld
einblenden. Modus 2 (Datei 0x56B4D–0x56E41): Szene über `138h:25h(Art, Seite, Flag, Unterzahl)` aus `tore\` wählen
(−1 = keine), Trikotfarben als Palettenindizes 0xFE/0xFD (`50h:63Fh`, Komponenten `(c >> 6) & 63`, `(c >> 2) & 63`,
`(c << 2) & 63` des 16-Bit-Farbcodes), Kopfzeile, Bilder zu 28 Byte ab `0xE2E5 + 0x54` bis 0xAAAA abspielen
(Zeichnen `48h:2C08h`/`48h:5316h`, Warten `48h:4884h`, Töne `38h:Ah`, `dateiformate.md` „Verwendung“), bei einem
Treffer die Tore um 1 erhöhen und mit `0x56EDD(2, …)` neu schreiben, Pause 6. Modus 3: Bilder freigeben,
`DGROUP 0x49A9` = 1, Live-Bildschirm neu.

### Datei 0x56EDD, 0x570E8 – Kopfzeile der Torszene und Minutenzeile

`0x56EDD(Modus, Heimname, Gastname, ToreH, ToreG, Minute, Zuschauertext)`: Modus 0 setzt die Palette
(`48h:54BFh(DGROUP 0xE2E9)`); Modus ≠ 2: Schrift 2, Kästen Farbe 4 mit Text Farbe 7, Schatten 0: Heimname (9\|6)–(152\|14)
zentriert 9–151 auf Grundlinie 13, Gastname (169\|6)–(312\|14), Zuschauertext (8\|26)–(188\|34) linksbündig ab 8 auf
Grundlinie 33, Minutenzeile `0x570E8`. Immer: `DGROUP 0x374` = 2 (zweistellig), Breite w = Textbreite „00“
(`50h:2B8h`), Torkästen (80 − w/2 \| 16)–(80 + w/2 \| 24) und (240 − w/2 \| 16)–… mit den Toren zentriert 9–151 bzw.
169–311 auf Grundlinie 23. `0x570E8(Minute)`: `0x374` = 2, Kasten (232\|26)–(301\|34) Farbe 4, Text Minute + „.MINUTE“
linksbündig ab 232 auf Grundlinie 33, Farbe 7, Schatten 0 – auch je Minute aus der Minutenschleife bei Dauer-Eisfeld.

### Datei 0x53BF9 – Trikotfarben entzerren (`0x130:0x7A`, 94 B)

`(Zeiger Heimfarbe, Zeiger Gastfarbe)`: Summe über die drei Halbbytes (Bits 0–3, 4–7, 8–11) von |Heim − Gast|; ist
sie < 6, wird die Gastfarbe durch Heimfarbe XOR 0xFFF ersetzt.

### Datei 0x51F3D – Spielende (`0x130:0x25`, 173 B)

Aufrufer: Saisonwechsel in der Tagesfunktion Datei 0x56438, wenn das Endjahr erreicht ist (`DGROUP 0x5A14` =
`0x5A18`, `highscore.md`: erst die Bestenliste 0x5D90D, dann dieser Bildschirm, dann Programmende `C0h:61h(0x63)`).
Bildschirm schwarz, Schrift 1, „Das wars...“ zentriert 8–319 auf Grundlinie 100 in Farbe 7, „Carpe diem !“ auf
Grundlinie 140 in Farbe 30, beide ohne Schatten; einblenden; auf Mausklick warten; Argument ≠ 0 → Endlosschleife. Das
Remake zeigt stattdessen eine Zusammenfassung (`SeasonEndGameOverScreen`, `[APPROX]`).

## 7. Abgleich mit dem Remake

| Original | Remake | Stand |
|---|---|---|
| Live-Bildschirm 0x5353C/0x52BD6, Reporter 0x52520 | kein Bildschirm („Tag weiter“ ohne Live-Spiel, `MatchDayScreen`) | fehlt (Paket `livespiel`) |
| Statistik je Drittel `0xA839`/`0xA83F`, Strafminuten `0xA845` | `MatchEngine.Live.cs` liefert Ereignisse, keine Drittelzähler | fehlt |
| „Spiele laufen“ 0x37B82–0x37C5C mit Uhr 0x51E90 | `MatchDayScreen`: Kasten (3\|183) 121 × 36 richtig; Text Grundlinie 65 statt 80, Balken 1 Zeile einfarbig und nur je Drittel, Ziffernabstand 8 statt 10 | `[APPROX]`-Abweichungen |
| Geschwindigkeit `DGROUP 0x262`, Pausen, Klick nach Minute 60/70 | `FramesPerMinute` = 3 fest | `[APPROX]` |
| Spielende 0x51F3D | `SeasonEndGameOverScreen` (eigener Wortlaut) | `[APPROX]` |
| Torszenen 0x567C8 | nur Töne (`SoundEffect.cs`), `tore\`-Format offen | fehlt (zuletzt) |
| Eingriff 0x5434C, Treffer-Tests | Eingriffsmenü 0x38D96 nicht analysiert (Ov 0xF0) | offen |

## 8. Kandidaten Runde 11

| Nr | Ziel | Dateien | Testidee | Priorität |
|---|---|---|---|---|
| L1 | Drittelzähler der Anzeige im Core: Chancen/Tore je Drittel und Seite, Strafminuten je Spiel, Reihenliste (bis 4 Paarungen: Managervereine und markierte Vereine, Vereinsfeld 0x34 Bit 7) nach §2 | `src/EishockeyManager.Core/MatchEngine.Live.cs`, neue `MatchEngine.Scoreboard.cs` | Spieltag mit Managerpaarung spielen, Summe der Drittelzähler = Chancen/Tore der Ereignisliste; Strafminuten = Vereinsminuten | P1 |
| L2 | Live-Bildschirm `LiveMatchScreen`: `kb\19`/`kb\22`, Reihen y = 4 + 45p, Logos/Köpfe, Namen, Spielstand (139 \| 45p + 6), Lampen/Balken/Spalten nach §4, Zuschauerzeile | neue `src/EishockeyManager.UI/Screens/Live/LiveMatchScreen.cs`, Szenario `Tools/Scenarios/Live.cs` | Bildschirmfoto gegen `bundestrainer_00200`/`duell_s2a_01126` (zwei Reihen), Pixelprüfung der Kästen und Ziffernpositionen | P1 |
| L3 | Spieluhr und Balken als Bausteine (`0x51E90`, `0x53307`, `0x533D0`, `0x53453`, `0x53234` Stil 0/1) | neue `src/EishockeyManager.UI/LiveWidgets.cs`; `MatchDayScreen` darauf umstellen (Balken zweizeilig 0x18/0x13 auf 0x11, 4 px je Minute, Ziffern Abstand 10, Text Grundlinie 80) | `karriere_02315`: zwei volle Balken, dritter 32 px, „08“ ab x = 96 | P1 (Bausteine) / P2 („Spiele laufen“) |
| L4 | Reporter: Meldungsablauf §5 mit Mundfolge, Zwinkern, Strafgründen `0x5CC` (random(0, 12) bzw. Index 5), „Vorlage:“ | `LiveMatchScreen`, `LiveReporter.cs`; Strafgrund-Zufall im Core, damit die Zufallsfolge stimmt | Mundfolge für „Chance:“ = [0, 9 × 8, 0xFF], „2 Minuten“ = [0, 0, 0, …]; Strafgrund bei 5 Minuten immer „Übertr. Härte.“ | P1 |
| L5 | Zeitsteuerung: Geschwindigkeit 0–80 (Optionen), Pausen `(45 − g/2) · 18 ms`, Zwinkern mit 1/5, Schnelldurchlauf ≥ 70, Klick nach Minute 60/70, Leertaste → Optionen, Seitenwechsel `0xE19D` je Drittel | `LiveMatchScreen`, `GameOptionsScreen` (Regler) | Ablauftest mit festem Zufall: Zahl der Frames je Minute bei g = 0 und 60 | P1 |
| L6 | Eingriff im Spiel: Aufstellung/Taktik öffnen und Bildschirm neu aufbauen (0x5434C); vorher Eingriffsmenü Datei 0x38D96 und Treffer-Tests (0x534BC, 0x53508) belegen | Analyse-Nachtrag zu Ov 0xF0, dann `LiveMatchScreen` | Klick in der Managerpaarung öffnet das Menü; Aufstellung im Spiel ändert die Reihe auf dem Eis | P2 |
| L7 | Torszenen: `tore\`-Format (Bilder zu 28 Byte, Effektbyte 3), Szenenwahl `138h:25h`, Kopfzeile §6, Trikotfarben 0x53BF9 und Palette 0xFE/0xFD, Eisfeld je Hallenstufe `kb\241…244` | neue Spur `torszenen` (Formate + UI) | Szene 1 abspielen und Bild gegen `eisbaeren_f01_01150` | P3 (letzter Schritt vor v1.0) |
| L8 | Spielende-Bildschirm: schwarz, „Das wars...“ (100) / „Carpe diem !“ (140), Klick | `SeasonEndGameOverScreen` | Bildschirmfoto, Textpositionen | P3 |

## 9. Offen

- `38h:DDEh` (Aus-/Einblenden, 347 B) und die Seitenkopien `48h:…` sind Mechanik der Grafikbasis und nicht gelesen;
  für das Remake genügt „Bild steht / Bild weg“.
- Das Eingriffsmenü Datei 0x38D96 (Ov 0xF0, Klick in der Managerpaarung, Regler für Einsatz/Angriff) gehört nicht zu
  dieser Spur und ist nicht gelesen; es ruft 0x534BC, 0x53508, 0x53453 und 0x5434C.
- `50h:443h(…, 137, 42)` = 0x11 in 0x5353C ist als Pixelabfrage gedeutet, nicht belegt.
- Die Szenenwahl `138h:25h` (Ov 0x138, „tore\“) und das Bildformat in `tore\*` bleiben offen (Kandidat L7).
- Im Spielstand-Original ist keine der Anzeigetabellen enthalten (sie werden je Spieltag neu aufgebaut); die
  Schreibstelle von `DGROUP 0xA838` (Drittelzähler) wurde nicht gesucht.
