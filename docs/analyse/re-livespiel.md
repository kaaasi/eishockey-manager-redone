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
   (`DGROUP 0x12B` = 0): Zahl als Text (`DGROUP 0x38D` während der Umwandlung 0, mit Tausenderpunkt – belegt
   durch `folge01_00615` „4.335“, Spur `nutzer-befunde` #837), Farbe 0x10, wenn sie gleich Sitz- + Stehplätzen des Managers ist (`DGROUP 0x94AC + 0x137 + 0x1AF · m`
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

Beide nur aus dem Eingriffsmenü Datei 0x38D96 (Ov 0xF0, Nachtrag in §10). `0x534BC(mx, my, x, y, breite, n)`:
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
einblenden. Modus 2 (Datei 0x56B4D–0x56E41): Szene über `138h:25h(Art, Penalty, Seite, sechs Feldspieler)` (§11) aus `tore\` wählen
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
| Torszenen 0x567C8, Lader 0x575C3 | nur Töne (`SoundEffect.cs`); Szenenwahl und `tore\`-Format in §11 | fehlt (L7, zuletzt) |
| Eingriff 0x5434C, Treffer-Tests | Eingriffsmenü 0x38D96 analysiert (§10, Runde 17), kein Bildschirm | fehlt (Runde 18 L6) |

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
- Das Eingriffsmenü Datei 0x38D96 (Ov 0xF0, Klick in der Managerpaarung, Regler für Einsatz/Angriff) ist in §10
  nachgetragen (Runde 17); offen bleiben dort nur drei Stellen der Aufstellung im Spiel.
- `50h:443h(…, 137, 42)` = 0x11 in 0x5353C ist als Pixelabfrage gedeutet, nicht belegt.
- Szenenwahl `138h:25h` und Format `tore\*` sind in §11 nachgetragen (Runde 17); offen bleiben dort die Bildtafel der
  Figurnummern und das Zeichnen in der Grafikbasis (`48h:4ECFh` ff.).
- Im Spielstand-Original ist keine der Anzeigetabellen enthalten (sie werden je Spieltag neu aufgebaut); die
  Schreibstelle von `DGROUP 0xA838` (Drittelzähler) wurde nicht gesucht.

## 10. Eingriff im Spiel (L6; Spur `re-livespiel-2`, Runde 17, Teil 1)

Nachtrag zu §4 (Treffer-Tests, 0x5434C) und §9 (Eingriffsmenü offen). Belege: `ndisasm -b16 -e <von> -o <von>
~/referenz/original/gamedata/em.exe | head -n 200` bzw. annotiert `python3 tools/reverse-engineering/disasm.py
~/referenz/original/gamedata/em.exe 0x38D96 0x3901B --cache <scratch>/an.pickle`; Bytes in Klammern zum Nachprüfen mit
`xxd -s <adresse> -l <n> em.exe`. Mauskoordinaten: Fensterobjekt `DGROUP 0xC147` (Fernzeiger) **+0 = Zeile y, +2 = Spalte x**
(abgeleitet aus dem Lampenfeld: x 3 … 20, y 226 … 238 passt nur so zu (3\|226) 21 × 12, §3); Maustaste `DGROUP 0x5A16`
1 = links, 3 = rechts (`finanzen-rekonstruktion.md`).

### Datei 0x38D96 – Eingriffsmenü (Ov 0xF0 `0xF0:0x98`, 645 B)

**Aufruf:** nur aus der Minutenschleife 0x37A62, je Paarung, wenn sie eine Managerpaarung ist (`0x130:0x4D` = Datei
0x52B91 ≠ 0) und `[0x5A16]` = 1 oder 3: Liga-Paarungen (Tabelle `DGROUP 0xA0BF`) Datei 0x37E77–0x37EB5, Pokal-Paarungen
(`DGROUP 0x5C4C`) Datei 0x38273–0x382B3 (Nahaufruf `0E E8 …`). Erstes Argument = Pokal-Schalter der
Schleife (`[bp+8]` von 0x37A62), nur er wird gelesen (`[bp+6]` bei Datei 0x38FE5); die übrigen sieben Bytes nicht.
Rückgabe immer 0 (`[bp-0xD]`, Datei 0x38D9E/0x39012). Ohne Live-Anzeige (`[0x5A56]` = 0) sofort zurück (Datei 0x38DA2).

1. **Lampe „Schnelldurchlauf“** (Datei 0x38DBB–0x38E19): 2 < x < 21 und 225 < y < 239, also x 3 … 20, y 226 … 238 (`83 FE 02`,
   `83 FE 15`, `81 7E FC E1 00`, `81 7E FC EF 00`) → `DGROUP 0xE19A` ^= 1 (`80 36 9A E1 01`), Maus leeren
   (`38h:8A7h`), Lampe `0x53453(10, 228, [0xE19A])`. An: Geschwindigkeit `[0x262]` = 70 (`C7 06 62 02 46 00`, also
   Schnelldurchlauf ≥ 70, §1) und Darstellung `[0x226]` = 0 (Torszenen aus); aus: beide aus der Sicherung `0xE19B`/`0xE19C`
   zurück. Die Sicherung schreibt der Spieltagsanfang Datei 0x36B65–0x36B77 (dort auch Lampe = 0), zurückgeschrieben
   wird sie am Spieltagsende Datei 0x37833–0x3783E und vor den Spielmodus-Optionen (Leertaste, Datei 0x37D29–0x37D34;
   danach neue Sicherung und Lampe = 0, Datei 0x37D41–0x37D5B). Danach läuft die Reihenschleife trotzdem weiter (keine
   Reihe reicht bis y = 225).
2. **Reihenschleife** p = 0 … 4, solange `0x233[2p]` ≠ 0xFF (Datei 0x38E1C–0x3900F): Zeile trifft, wenn
   45p < y < 45p + 43 (`[bp-2]` += 0x2D, Datei 0x38FFB). Mindestens einer der beiden Vereine muss einen Manager haben
   (Vereinsfeld 0x16 ≠ 0, Datei 0x38E5F/0x38E7B). Seite: x > 160 (`81 FE A0 00`) → Gast, Basis x0 = 13 + 227 = 240 und
   Kopf-/Logospalte xk = 182 (`81 46 FA E3 00`, `C7 46 F8 B6 00`); sonst Heim, x0 = 13, xk = 96. Hat der Verein der
   angeklickten Seite keinen Manager (`[bp-0xB]` = Feld 0x16 = 0), nichts. Alle Felder dieses Vereinssatzes
   (`far:9657 + 0x3D · Verein`):
   - **Einsatz** `0x534BC(x, y, x0 + 39, 45p + 22, 25, 100)` = r ≠ 0 → Vereinsfeld 0x26 = r − 1 (Datei 0x38ECD–0x38EFE,
     `26 88 47 26`). Wertebereich 0, 4, 8 … 100 (Breite 25 inklusive beider Ränder, s. u.).
   - **Angriff** `0x534BC(x, y, x0 + 39, 45p + 29, 25, 7)` = r ≠ 0 → Vereinsfeld 0x2C = (0x2C & 0x8F) | ((r − 1) << 4)
     (Datei 0x38F02–0x38F40, `24 8F`, `D2 E2` mit cl = 4): Bits 4–6, Modus (Bits 0–2) und Bit 3 (Torhüter draußen) bleiben.
     r − 1 = 0 … 7; 7 nur am rechten Randpixel x = x0 + 64 ((25 · 7) / 25 + 1 = 8).
   - **Reihe auf dem Eis** je Teil t = 0 (Abwehr), 1 (Sturm) und Reihe j = 0 … 3: `0x53508(x, y, x0 + 8 + 8t, 45p + 14 +
     7j)` = 1 (Datei 0x38F44–0x38FBE, `BA 07 00`, `83 C2 0E`, `B1 03`, `83 C2 08`) → Vereinsfeld 0x2E + 2t = j
     (`26 88 47 2E`); für j = 3 (vierte Reihe) nur, wenn `0x28:0xD9F` = Datei 0x08E5F (Manager, t) ≠ 0 („vierte Reihe des
     Teils besetzt“, `LiveSquad.HasFourthLine`). Keine Neuzählung der Feldspieler (0x38A36) und kein Neuzeichnen hier; die
     Anzeigetafel zeigt den neuen Stand am Minutenende (§4, Lampen k = Reihe).
   - **Kopf/Logo** xk ≤ x ≤ xk + 39 (Datei 0x38FC0–0x38FCD) → Eingriff `0x130:0x89` = Datei 0x5434C(Manager = Feld 0x16 − 1,
     Pokal, 0, `[0x5A16]` = 3 ? 2 : 1) (Datei 0x38FCF–0x38FED), danach Maus leeren. Linke Taste öffnet also die
     Aufstellung, rechte die Taktik (s. 0x48E28).

   Die Felder decken sich mit der Anzeigetafel 0x52BD6 (§4: y0 = 15 + 45p, Lampen (x0 + 8 + 8t \| y0 + 7k − 1), Balken
   Einsatz/Angriff bei (x0 + 39 \| y0 + 9) und (x0 + 39 \| y0 + 16)), Kopf/Logo mit 0x5353C ((96\|y) bzw. (182\|y), 38 × 38).
   Nach Slider- und Lampenklick wird die Maus **nicht** geleert; ruft die Schleife das Menü in derselben Minute für eine
   weitere Managerpaarung erneut, wirkt derselbe Klick ein zweites Mal (gleiches Ergebnis).

**Wirkung im Spielkern:** Einsatz und Angriff gelten ab sofort für die Minutenrechnung (`spielberechnung.md` §5–§7), bis
das Nachführen 0x939A sie in einer geraden Minute der Managerpaarung überschreibt (Einsatz nur bei Modus ≠ 2, Angriff
nur bei Modus < 2; A176 „MANUELL“). Die Reihe auf dem Eis gilt bis zum nächsten Reihenwechsel 0x8EF2 (`ChangeLines`).
Damit ist die Lesart in `analyse/belastung-staerke.md` §7 („0x38F40 (Modus)“) zu berichtigen: Datei 0x38F40 schreibt die
**Angriffsbits** 4–6, einen Modus schreibt das Eingriffsmenü nicht (Hinweis für `pruefung-karte`, Datei außerhalb der
Spur).

### Datei 0x534BC, 0x53508 – Treffer-Tests (Nachtrag zu §4)

- `0x534BC(mx, my, x, y, breite, n)` (Datei 0x534D7–0x534FC): Treffer, wenn x ≤ mx ≤ x + breite **und** y ≤ my ≤ y + 6
  (`3B D3`/`7C`, `03 C7`, `05 06 00`, alle Grenzen einschließend); Rückgabe (Byte) `(mx − x) · n / breite + 1`
  (`F7 6E 10`, `F7 FF` vorzeichenbehaftet, `FE C0`), sonst 0. Für breite 25 also 26 Pixelspalten, Werte 1 … n + 1.
- `0x53508(mx, my, x, y)` (Datei 0x53518–0x53535): 1, wenn x ≤ mx ≤ x + 5 und y ≤ my ≤ y + 5 (`05 05 00` zweimal), sonst 0
  – 6 × 6 Pixel, also einen Pixel größer als die gezeichnete Lampe (x \| y + 1)–(x + 4 \| y + 5).

### Datei 0x5434C, 0x48E28 – Neuaufbau nach dem Eingriff (Nachtrag zu §4)

- Argumente `(Manager, Pokal, unbenutzt, Modus)`: `[bp+0xA]` wird nie gelesen (0x38D96 übergibt 0, die Torszene
  Datei 0x54181 ihr `[bp-2]`); Modus (Wort `[bp+0xC]`) 1 = Aufstellung zuerst (linke Taste, Torszene nach Verletzung), 2 = Taktik zuerst
  (rechte Taste).
- `118h:43h` = Datei 0x48E28(Modus, `DGROUP 0x2D6[Manager]`, 1) als Langwörter (Datei 0x54368–0x54385): Schleife
  „Modus = 1 → Aufstellung 0x108:0x2A (Datei 0x40364) mit dem zweiten Argument, sonst Taktik Datei 0x47F2B mit dem
  dritten; deren Rückgabe (Byte, vorzeichenbehaftet) = nächster Modus; Hintergrund `110h:7Ah`; bis Modus 0“ (Datei
  0x48E2B–0x48E5E). Aus dem Hauptmenü (Datei 0x4578D–0x4579C) sind zweites und drittes Argument 0. Im Spiel bekommt die
  Taktik 1 (Kopfzeile Datei 0x40290 mit Vereins- und Managername, `re-rest.md`; bei Datei 0x47F5E für den ersten
  Eintrag der Schleife `[bp-6]` = 0 der Wert 7 statt 0x1E – `[APPROX]` vermutlich Farbe; Datei 0x486F1 nicht gelesen),
  die Aufstellung `DGROUP 0x2D6[Manager]` (einziger direkter Schreiber laut `--xref`: Spielabrechnung Datei 0x54821, Wert 5;
  Rücksetzen nicht gefunden – `[APPROX]`, für die Umsetzung genügt „≠ 0 = im Spiel“).
- Die Aufstellung prüft das Argument an acht Stellen (Datei 0x40529, 0x40CFD, 0x40E2A, 0x40F35, 0x4115F, 0x41298,
  0x4135E, 0x413E8). Gelesen: 0x40529 Kopfzeile 0x40290; 0x40CFD (nach Kaderplatz +0x15 = 0, Datei 0x40CF8) und
  0x4115F setzen „eingesetzt“ (Kaderplatz +0x0A) = 1 im Spiel, sonst 0 – ein aus der Reihe genommener Spieler bleibt im
  Spiel also eingesetzt; 0x40E2A die Grenze „22 eingesetzte Spieler“ (`120h:66h(Manager, 3)` = 0x16 → Meldung
  `DGROUP 0x11D5`) gilt nur außerhalb des Spiels; 0x40F35 im Spiel nur mit der linken Taste. Die übrigen drei
  (0x41298, 0x4135E, 0x413E8) sind nicht gelesen – `[APPROX]` für die Umsetzung (Runde 18 L6: dort zuerst lesen).
- Vorher (0x5434C, vor `118h:43h` Datei 0x54385): `110h:7Ah` (Datei 0x5435E; Deutung „Hintergrund“ aus Review #795,
  `[APPROX]`) und `38h:8A7h` (Datei 0x54363) – für B3 in dieser Reihenfolge übernehmen (Review #795 P3-3).
- Danach (Datei 0x5438D–0x54419): ausblenden, Fenster schließen, Bildschirm schwarz (0\|0)–(319\|239), Feldspieler des
  Managervereins (`DGROUP 0x966F + 0x1AF · Manager`) neu zählen `0xF0:0x8E` = Datei 0x38A36, Live-Bildschirm
  `0x5353C(Pokal, 1)` (Nahaufruf `E8 53 F1`), Schrift 3, Maus leeren, einblenden, `0xA70E` zurück.

**Remake-Zuordnung (für Runde 18):** Treffer-Tests und Schleife → `LiveMatchScreen` (Klick → Befehl), die Wirkung als
Spielkern-Befehle auf `LiveTeam` (`Effort` = 0x26, `AttackLevel` = 0x2C Bits 4–6, `DefenseLine`/Sturmreihe = 0x2E/0x30,
`LiveSquad.HasFourthLine`), die Lampe als Schalter im Live-Ablauf (Geschwindigkeit 70, Torszenen aus, Rücknahme aus
der Spieltagssicherung). Der Eingriff öffnet die vorhandenen Bildschirme Aufstellung/Taktik mit „im Spiel“-Kennzeichen;
danach `RecountSkaters` und Neuaufbau. Testideen in §12.

## 11. Torszenen-Lader und Format `tore\` (L7; Spur `re-livespiel-2`, Runde 17, Teil 2)

Ov 0x138 (Stub-Segment 63A2) beginnt mit zwei Funktionen für die Torszenen; der Rest des Overlays ist Finanzen/Aktien.
Belege wie §10 (`disasm.py … 0x57480 0x57768`); DGROUP liegt ab Datei 0x16DD0, Texte also bei Datei 0x16DD0 + Adresse.

### Datei 0x57480 – Szenenzahl lesen (`0x138:0x20`, 323 B, Programmstart 0x437D5)

`DGROUP 0x554A` „tore\anzahl“ öffnen (`50h:17h`), bis 200 Byte lesen (`50h:5Ah`), schließen. Von hinten: Nichtziffern
überspringen, dann drei Dezimalzahlen rückwärts lesen (Ziffer · 1, 10, 100 …, zwischen zwei Zahlen genau **ein**
Trennzeichen, Datei 0x574FC–0x5759E): letzte Zahl → `DGROUP 0xE2F4`, vorletzte → `0xE2F2`, drittletzte → `0xE2F6`.
Ist `0xE2F6` < 1 oder `0xE2F2` < 1 → Fehler 2 (`C0h:61h(2)`, „DISK-Error“). Mit `MAXSZENE:11|2|2` (`dateiformate.md`):
`0xE2F6` = 11 (normale Szenen), `0xE2F2` = 2 (Penalty), `0xE2F4` = 2 (sechs Feldspieler). Der Text vor der ersten Zahl
wird nicht geprüft.

### Datei 0x575C3 – Szene wählen und laden (`0x138:0x25`, 421 B, einziger Aufrufer 0x567C8)

Argumente `(Art, Penalty, Seite, sechs)` (Datei 0x56BAC–0x56BBC): Art = 0 vergeben / 1 Tor (`[bp+0x18]` des Abspielers,
dort auch „Tor → Torzahl + 1“, Datei 0x56DD9), Penalty = Kennzeichen der Torszene 0x53C57 (`[bp+0x18]` dort, derselbe
Wert wie in „Art + 5 · Penalty“ für den Reporter, Datei 0x53DD2), Seite = 0 Heim / 1 Gast (Seite der Chance), sechs = 1,
wenn der Verein dieser Seite Feldspieler (Vereinsfeld 0x32) + belegte Strafbankplätze (`F0h:20h`, Datei 0x35540) = 6 hat,
also ohne Torhüter spielt (Datei 0x56B4D–0x56BA8). Die frühere Lesart „(Art, Seite, Flag, Unterzahl)“ in §6 ist damit
ersetzt.

1. `DGROUP 0x38D` = 1 und `0x374` = 1 (Zahl ohne Tausenderpunkt, Mindestbreite 1; Datei 0x575D3/0x575D8), Fehlversuche
   `[bp-1]` = 0.
2. n = `random(2, [0xE2F6])` (Datei 0x575FA–0x57609). Danach `random(0, Art ? 210 : 255)` = 0 → Penalty = 1 (Datei
   0x575E9–0x5761A, `BF FF 00` / `BF D2 00`): auch ohne Penalty kommt also mit 1/256 (vergeben) bzw. 1/211 (Tor) eine
   Penalty-Szene. Penalty ≠ 0 → n = `random(2, [0xE2F2])`; sonst sechs ≠ 0 → n = `random(2, [0xE2F4])` und
   Merker `[bp-2]` = 1 (Datei 0x5761E–0x57643). `38h:847h` ist `random(a, b)` mit beiden Grenzen; es zieht aus dem
   **Spielzufall** – jede gezeigte Torszene verschiebt also die Zufallsfolge der Spielrechnung. `random` hat keinen
   Sonderfall a = b (Datei 0xC037: Grenzentausch, dann immer `call 0x0:0x6df`): mit `MAXSZENE:11|2|2` ergibt die
   Penalty- bzw. Sechser-Wahl stets n = 2, verbraucht aber trotzdem einen Zug – je Szene also drei Züge mit Penalty oder
   sechs, sonst zwei (Review #795 P3-2).
3. Dateiname (`0:1B0Bh` kopieren, `0:1A9Ch` anhängen): `DGROUP 0x5556` „tore\“ + n (`38h:2C2h`) + „.h“/„.a“ (Seite 0/1,
   `0x555C`/`0x555F`) + „v“/„t“ (Art 0/1, `0x5562`/`0x5564`) + „e“, wenn Penalty und nicht `[bp-2]` (`0x5566`) +
   „j“, wenn `[bp-2]` (`0x5568`). Beispiele: Heim vergibt `tore\7.hv`, Gast trifft per Penalty
   `tore\2.ate`, Heim trifft ohne Torhüter `tore\2.htj`.
4. Öffnen (`50h:17h`); misslingt es, `[bp-1]` + 1 und mit neuem Zug ab Schritt 2, nach 50 Fehlversuchen Rückgabe −1
   (Datei 0x575DD–0x575E6, 0x576FC–0x57709). Länge `0:131Ah`, Puffer `38h:F39h(Länge, 0)` in
   `DGROUP 0xE2E5` (Fernzeiger; null → Fehler 4 „SPEICHER-Error“), ganze Datei lesen, schließen.
5. Rückgabe 1 bei Penalty-Szene, sonst 0 (Datei 0x57755–0x57762). Der Abspieler wertet nur −1 aus (Datei 0x56BC7: dann
   keine Szene, Bilder freigeben, Live-Bildschirm neu).

Alle 48 Szenendateien sind vorhanden (`ls tore`: n = 2 … 11 je `.ht .hv .at .av`, dazu `2.hte 2.hve 2.ate 2.ave 2.htj 2.hvj
2.atj 2.avj`), Fehlversuche kommen mit den Originaldateien also nicht vor.

### Format `tore\*` (gelesen aus `48h:5250h` Szene starten und `48h:5316h` Bild anwenden)

Alle Wörter **Big-Endian** (`86 E0` = `xchg ah,al` nach jedem Lesen). Probe über alle 48 Dateien (Skript im Scratch,
Ergebnis hier): Länge = 0x54 + 28 · Bilder + 2, letztes Wort 0xAAAA, sonst nirgends am Bildanfang.

| Bereich | Inhalt | Fundstelle |
|---|---|---|
| +0x00 | „TOR!“ (nicht geprüft) | – |
| +0x04, +0x06 | Kamera x, y; beim Start x − 16, y − 32, jeweils ≥ 0 (Grafikbasis `cs:0x30`/`0x32`) | `48h:5250h` +0x1F–0x45 |
| +0x08, +0x0A | Puck x, y (`cs:0x34`/`0x36`); der Puck ist ein dreizehntes Objekt mit Figur 1 | `48h:5250h` +0x49–0x59; Figur 1 setzt `48h:51EBh` |
| +0x0C + 6k, k = 0 … 11 | Objekt k: Byte +1 = Figur (Byte +0 in allen Dateien 0), Wort +2 = x, Wort +4 = y | `48h:5250h` +0x60–0x80 |
| +0x54 + 28i | Bild i, bis zum Wort 0xAAAA | 0x567C8 Datei 0x56CDA–0x56DC3 |

Bild (28 Byte): Byte 0 = Kamerabewegung, Byte 1 = Puckbewegung (addiert auf x/y), Byte 2 = 0 in allen 6 600 Bildern
(unbenutzt), Byte 3 = Toneffekt s (0 keiner; 1 Jubel, 3 Puck, 4 Check, `dateiformate.md` „Verwendung“), dann je Objekt k
zwei Byte: Figur (immer übernommen) und Bewegung (nur wenn Figur oder Bewegung ≠ 0). **Bewegungsbyte** (`48h:51ADh`):
Bit 7 Vorzeichen y, Bit 6 Vorzeichen x, Bits 5–3 |dx|, Bits 2–0 |dy| (also höchstens ±7 Pixel je Bild); Kamera: Werte
ersetzen eine Geschwindigkeit (`cs:0x1A`/`0x1C`; `[APPROX]` Deutung als Kamera aus dem Kopf +0x04), Puck und Objekte: Werte werden addiert. Figurnummern in den Dateien
0 … 93; welche Bildtafel sie zeichnet (`kb\246`/`kb\247` oder die Halle `kb\241…244`, Zeichnen `48h:4ECFh` ff.) ist
nicht gelesen – `[APPROX]` offen für L7 (Kandidat: die 92 Figuren 32 × 32 in `kb\233`/`kb\234`/`kb\235`,
`SpriteSheet.cs`, über eine Tabelle in `kb\246`). Koordinaten (Startwerte aus dem Kopf, alle 48 Dateien): Kamera bis
(304\|880), Puck bis (639\|1 051), Objekte bis (791\|1 205), also etwa 800 × 1 210 – das Eisfeld ist **hoch**, nicht breit,
größer als der Bildschirm und scrollt; Heimszenen (`.h*`) liegen oben (Objekt-y 158 … 905), Gastszenen (`.a*`) reichen
bis unten (y bis 1 205, z. B. `6.at`; Kamera-y 880 in `8.at`). Die Bewegungen kommen noch hinzu. Größe der Eisfläche und
Zuordnung zur Halle `kb\241…244` in Spur C prüfen (Review #795 P2-1).

Ablauf je Bild im Abspieler (Datei 0x56CEA–0x56DC3): Ende bei 0xAAAA; `48h:2C08h(1)`, Bild anwenden, warten, solange
`48h:4884h` ≠ 0 (dabei Publikum `kb\237` neu, wenn Ton an `DGROUP 0x4A44` und die Ausgabe frei `0x59E4` = 0 ist);
Toneffekt s ≠ 0 → laufenden Ton stoppen, `0xE2D1 + 4s` spielen; dann `48h:2C4Eh` abwarten, nächstes Bild. Danach Puffer
freigeben, bei einem Tor Torzahl + 1 und Kopfzeile `0x56EDD(2, …)`, Pause 6 (nur Modus 0, Datei 0x56E14–0x56E29).
Modus = `2 · [0x5A55]` (Dauer-Eisfeld, Datei 0x53FCE): bei 2 springt der Abspieler gleich zur Szenenwahl (Datei 0x56804).

## 12. Umsetzungsplan Live-Spiel für Runde 18 (L1–L8)

Vorschlag der Spur `re-livespiel-2`; Zuschnitt und Reihenfolge entscheidet die Leitstelle im Rundenplan. Grundsätze:
der Spielkern bleibt ohne Bildschirm vollständig (Live-Anzeige ist nur Darstellung, `spielberechnung.md` §1), **außer**
den Zufallszügen, die das Original nur mit Anzeige macht (Strafgrund §5, Szenenwahl §11 Schritt 2) – sie gehören in
den Kern, geschaltet über „Live-Anzeige an“, sonst verschiebt sich die Zufallsfolge. Die Zwinker-Züge `lrandom`
(`38h:7E5h`, Datei 0xBFD5) verschieben sie **nicht**: eigener Generator mit Zustand `DGROUP 0xE017/0xE019` (Langwort),
`x = ((x << 4) + 0xA7)` mit `0:48Bh` (Langwort-Rest) gegen 0x3D0900 (4 000 000), danach Skalierung auf `(b − a + 1)`;
`random` (`38h:847h`, Datei 0xC037) ruft dagegen `0:6DFh` = Datei 0x24DF (Borland-`rand`, Zustand `DGROUP 0x5984/0x5986`,
Faktor 0x015A4E35). Für A4/A2: `lrandom` als eigener Generator (Kern bzw. `LiveClock`), getrennt vom Spielzufall
(Review #795 P3-1). Zeit vergeht weiter nur
über `GameState.Continue()`; der Live-Bildschirm treibt die Minuten, ändert aber keinen Zustand außer über Kern-Befehle.

**Spur A `livespiel-kern`** (Core und Formats; zuerst, weil B die Werte zeigt)

| Teil | Inhalt | Dateien | Abnahme |
|---|---|---|---|
| A1 | L1 Anzeigezähler: Reihenliste (bis 4 Paarungen, §2 `0xA75C`/`0x233`), Chancen/Tore je Drittel und Seite (`0xA839`/`0xA83F`, nur Minute ≤ 60), Strafminuten (`0xA845`) | neu `Core/MatchEngine.Scoreboard.cs`; Anschluss in `MatchEngine.Live.cs` (Absprache mit dem Besitzer) | Spieltag mit Managerpaarung: Summe der Drittelzähler = Ereignisliste; Strafminuten = Vereinsminuten; Reihenfolge der Reihen wie 0x5353C |
| A2 | L5 Ablaufmodell: Geschwindigkeit 0–80, Pausen je Minute `(45 − g/2)` bzw. `(30 − g/2)` mit Zwinkern 1/5, Schnelldurchlauf ≥ 70, Warten nach Minute 60/70, Spieltagssicherung `0xE19B`/`0xE19C`, Lampe §10 Schritt 1; nur als Modell (Bilder je Minute), keine Wanduhr | neu `Core/LiveClock.cs` | Ablauftest: Bilder je Minute bei g = 0/60/70; Lampe an → g = 70 und Torszenen aus, aus → Sicherung zurück; Leertaste-Optionen setzen Lampe = 0 |
| A3 | L6 Eingriff als Befehle: Einsatz (0, 4 … 100), Angriff (0–7), Reihe auf dem Eis je Teil mit Prüfung 4. Reihe (0x08E5F), Eingriff öffnen (Modus 1/2) mit „im Spiel“-Kennzeichen; danach `RecountSkaters` | neu `Core/MatchEngine.Intervention.cs` | Rechnung je Minute mit geändertem Einsatz/Angriff/Reihe; Nachführen 0x939A überschreibt in gerader Minute (A176); 4. Reihe ohne Spieler → keine Änderung |
| A4 | L4/L7 Zufall im Kern: Strafgrund `random(0, 12)` bzw. Index 5; Szenenwahl §11 (Züge in genau dieser Reihenfolge, auch `random(2, 2)` zieht – mit Penalty oder sechs **drei** Züge, sonst zwei; Dateiname); Format-Leser `tore\` (Kopf, Bilder, Bewegungsbyte) | neu `Formats/GoalScene.cs`, `Core/MatchEngine.GoalScene.cs` | `[GameDataFact]`: alle 48 Dateien lesbar, Bildzahl = (Länge − 86) / 28, Effektzählung 149/53/25, Byte 2 = 0; Szenenwahl mit festem Zufall ergibt den erwarteten Namen; Zufallsfolge mit/ohne Anzeige getrennt geprüft |

**Spur B `livespiel-ui`** (nach A1; Bildschirme, Szenario `Tools/Scenarios/Live.cs`)

| Teil | Inhalt | Dateien | Abnahme |
|---|---|---|---|
| B1 | L3 Bausteine (Spieluhr 0x51E90, Balken 0x53307/0x533D0, Lampe 0x53453, Ziffern 0x53234) und „Spiele laufen“ in `MatchDayScreen` darauf umstellen | neu `UI/LiveWidgets.cs`; `Screens/MatchDayScreen.cs` | `karriere_02315`: zwei volle Balken, dritter 32 px, „08“ ab x = 96, Text Grundlinie 80 |
| B2 | L2 Live-Bildschirm (Aufbau 0x5353C, Anzeigetafel 0x52BD6) und L4 Reporter (Meldung 0x52520, Mund/Augen) | neu `Screens/Live/LiveMatchScreen.cs`, `Screens/Live/LiveReporter.cs` | Bildschirmfoto gegen `bundestrainer_00200` und `duell_s2a_01126`; Mundfolge „Chance:“ = [0, 9 × 8, 0xFF] |
| B3 | L6 Klickwege §10 (Treffer-Tests 0x534BC/0x53508, Lampe, Kopf/Logo links = Aufstellung, rechts = Taktik), Neuaufbau 0x5434C; Aufstellung/Taktik mit „im Spiel“ (die drei offenen Stellen §10 vorher lesen) | `LiveMatchScreen.cs`; Lineup-/Tactics-Bildschirm nur mit Kennzeichen-Parameter (Absprache) | Klick (52 + d \| 45p + 22), d = 0 … 25, setzt Einsatz 4d; Randpixel x0 + 64 ergibt Angriff 7; Lampenklick (21 + 8t \| 45p + 14 + 7j) setzt Reihe j |
| B4 | L8 Spielende 0x51F3D (schwarz, „Das wars...“ 100, „Carpe diem !“ 140, Klick) | `Screens/SeasonEndGameOverScreen.cs` | Bildschirmfoto, Textpositionen |

**Spur C `torszenen`** (L7 Anzeige, nach A4, P3 – kann nach Runde 19 rutschen): Abspieler 0x567C8 (Laden `kb\231…247`,
Kopfzeile 0x56EDD/0x570E8, Trikotfarben 0x53BF9 und Palette 0xFE/0xFD, Bildschleife §11), vorher Analyse der
Figurzuordnung und des Zeichnens (`48h:4ECFh`, `kb\246`). Dateien: neu `Screens/Live/GoalSceneScreen.cs`. Abnahme: Szene
`2.ht` abspielen, Bild gegen `eisbaeren_f01_01150`; Töne je Bild wie Byte 3.

Abhängigkeiten: B2 braucht A1, B3 braucht A3, C braucht A4. A2 und B1 sind unabhängig. Gemeinsame Dateien:
`MatchEngine.Live.cs` (A1–A4 nur Anschlüsse), `MatchDayScreen.cs` (B1), `GameOptionsScreen.cs` (Regler der
Geschwindigkeit, nur falls A2 es braucht).

## 13. Spur `livespiel-kern` (Runde 18)

Umsetzung des Plans §12 Spur A; Belege mit `disasm.py` (Dateiadressen).

### A1 Anzeigezähler (`Core/MatchEngine.Scoreboard.cs`)

- **Reihenliste:** 0x36B5D löscht vor den Spielen `0x233` (0xFF), `0xA88D` (0xFF), `0xA75C` (0), `0xA762` und `0x5A56`
  (Datei 0x36B8E–0x36BC9) sowie 0x54 Byte ab `0xA839` (Datei 0x36BCC–0x36BDB). Die Spielabrechnung 0x544C0 (je Paarung vor
  dem Spiel, Datei 0x36DBD/0x36F9C) hängt nach der Managerschleife eine Reihe an, wenn Heim- oder Gastverein Vereinsfeld
  0x34 Bit 7 hat (Datei 0x54BF7–0x54C23), und setzt `0x5A56` = 1 (Datei 0x54C63–0x54C94); Spielwertungen kehren vorher
  zurück (Datei 0x54599, 0x547BA). Keine Grenze bei vier.
- **Markierung (Vereinsfeld 0x34 Bits 6–7):** 0xAE2B setzt den Modus 0/1/2; im Modus 2 höchstens vier Vereine (getrennt für
  Vereine 0 … 0x95 und Länderteams ab `DGROUP 0x6AD`), mit Verdrängen (Argument 2 ≠ 0) wird ein markierter Verein ohne
  Manager auf Modus 1 gesetzt, sonst abgelehnt (Datei 0xAE5F–0xAF4C). Aufrufer: Spielstart 0x28165 je Manager Modus 2 mit
  Verdrängen (Datei 0x298EA, 0x29C1C), Vereinsfenster dritter Knopf (Datei 0x4E46A, Verdrängen nur für Managervereine),
  WM-Option 12 für das eigene Land (Datei 0x2D3FD), Ernennung zum Bundestrainer (Datei 0x55457). Der Lader setzt die Bits
  auf 0 (Datei 0x2EFA8). `[APPROX]` Remake: markiert = `MatchEngine.IsManaged` (Umschalter fehlen).
- **Zähler:** Torszene 0x53C57 sucht die Reihe in `0xA88D` und zählt nur für p < 4, Minute ≤ 60 und Art < 2 (Datei
  0x53C9F–0x53D1D); Drittel `0xA838` = 0/1/2 aus 0x36B5D (Datei 0x37023, 0x3719D, 0x371C4). Der Abschluss 0xA484 ruft die
  Szene mit Art = Tor/vergeben, wenn die Paarung eine Reihe hat (Datei 0xAB47–0xAB90). Strafminuten: 0x35818 addiert
  Belegung & 7 für jede Reihe mit dem Verein in `0x233` (Datei 0x3598A–0x359EE), vor der Spielerwahl.
- **Anschluss:** `MatchEngine.PlayWithEvents` legt die Reihe nach der Spielwertung an; der einzige Haken in
  `MatchEngine.Minutes.cs` ist `OriginalMinutes.GoalScene` an der Stelle 0xAB90 (Teil A4 wählt dort die Szene).

### A2 Ablaufmodell (`Core/LiveClock.cs`)

- **Minute** (Datei 0x37C6B–0x37CBD): nur mit Anzeige (Argument der Drittelschleife), `[0x1DB]` = 0 und Wort `[0x262]` < 0x46:
  `lrandom(0, 4)` (Langwort, Ergebnis `dx:ax` = 0) → Zwinkern 0x522EC und Pause 30 − `[0x262]` sar 1, sonst Pause 45 − … .
  Pause 0x130EE: Argument (Langwort) / 3 per 0x227C, dann `0xBBF:0x2C08`/`0xBBF:0x2C4E` (Ticks abwarten). `DGROUP 0x1DB`
  wird im Spielcode nur gelesen (`--xref`, zehn Leser, kein Schreiber), Anfangswert 0.
- **Drittelende** (Datei 0x3855F–0x3858C): mit Anzeige und Drittelende 60 oder 70: unter 70 Klick `38h:118h`, dann immer
  `110h:7Ah`.
- **Sicherung:** Spieltagsanfang Datei 0x36B65–0x36B77 (Lampe 0, `0xE19B` = `[0x262]`, `0xE19C` = `[0x226]`), vor den
  Optionen zurück und danach neu gesichert, Lampe 0 (Datei 0x37D29–0x37D5B).
- **lrandom** (Datei 0xBFD5): Zustand `DGROUP 0xE017/0xE019` (Anfangswert 0 in em.exe), `x = (x << 4) + 0xA7`, Rest
  `0:48Bh` gegen 0x3D0900, Ergebnis `x · (b − a + 1)` (`0:3CAh`) / 0x3D0900 (`0:47Ch`) + a. **Berichtigung zu §12:** lrandom
  ist zwar vom Borland-`random` getrennt, aber **nicht** von der Spielrechnung – die Funktionskarte nennt als Aufrufer u. a.
  0x87F7 (Energie), 0x9F5C (Torchancen), 0xA484 (Abschluss) und 0x35818 (Strafen). Im Original verschiebt jedes Zwinkern
  also die lrandom-Folge der Spielrechnung. `[APPROX]` Das Remake hält den Zwinkerzufall getrennt (Rundenplan 18, Review
  #795 P3-1); die Spielrechnung zieht ohnehin aus `GameState.Random`, eine zuggenaue Folge gibt es dort nicht.

### A3 Eingriff als Befehle (`Core/MatchEngine.Intervention.cs`)

- `MatchIntervention.SetEffort`/`SetAttack`/`SetLine` schreiben `LiveTeam.Effort` (Vereinsfeld 0x26), `AttackLevel` (0x2C
  Bits 4–6) und `DefenseLine`/`AttackLine` (0x2E/0x30) wie Datei 0x38ECD–0x38FBE, nur für Vereine mit Manager; die vierte
  Reihe nur mit eingesetztem Spieler (0x08E5F, Datei 0x38F80–0x38F99). Kein Neuzählen nach Schieber und Lampen.
- `MatchIntervention.AfterLineup` = Neuzählung 0x38A36 nach Aufstellung/Taktik im Spiel (Datei 0x5438D–0x54419);
  `ModeFor` = Modus 1/2 aus der Maustaste (Datei 0x38FCF–0x38FED); Kennzeichen „im Spiel“ `DGROUP 0x2D6` = 5 (Datei 0x54821).
- **Offen für B2/B3:** Das Remake rechnet eine Paarung in `MatchEngine.PlayWithEvents` am Stück; das Original rechnet je
  Minute alle Paarungen des Spieltags (0x37A62). Damit Eingriffe während des Spiels wirken, braucht der Live-Bildschirm einen
  Minutenschritt im Kern (Paarungen vorbereiten, je Minute `OriginalMinutes.PlayMinute`, Abschluss wie `PlayWithEvents`);
  nicht Teil von A1–A4 laut Plan – Entscheidung Leitstelle. **Entschieden:** R18-E1 = A (Nutzer 13.11.2026) → Teil A5.

### A4 Zufall der Anzeige und Format (`Core/MatchEngine.GoalScene.cs`, `Formats/GoalScene.cs`)

- **Szenenwahl:** Die Torszene 0x53C57 ruft den Abspieler 0x567C8 (Datei 0x5402A) nur für Art 0/1 und mit `[0x226]` ≠ 0
  (Datei 0x53DB1–0x53DE3, 0x53E02), dort mit `[0x5A56]` oder `[0x5A55]` (Datei 0x53F75–0x53F83). Der Abspieler zieht vor
  0x575C3 nichts; 0x575C3 zieht in dieser Reihenfolge (Datei 0x575E9–0x57643): `random(2, [0xE2F6])`, `random(0, Art ? 0xD2 :
  0xFF)`, dann bei Penalty `random(2, [0xE2F2])`, sonst bei sechs `random(2, [0xE2F4])`. Das Kennzeichen Penalty kommt aus dem
  Abschluss immer als 0 (Datei 0xAB69). Der Kern zieht das an der Szenenstelle 0xAB90 (`OriginalMinutes.GoalScene`), also
  nach `GoalScored` und vor dem Torschützen 0x541D9 (Datei 0x5407F).
- **Strafgrund:** 0x35818 ruft den Reporter nach der Spielerwahl, wenn Verein oder Gegner einen Manager hat (Datei
  0x35C1C–0x35C73); 0x52520 kehrt ohne `[0x5A56]` oder bei `[0x262]` ≥ 70 zurück (Datei 0x5252A–0x5253B), sonst
  `random(0, 12)` nur für Ereignis 2 (Datei 0x52A9A), Ereignis 3 Index 5 (Datei 0x52A94). Der Kern zieht das in `Penalty` nach
  dem Ereignis, vor dem Kettenzug (`OriginalMinutes.PenaltyReason`).
- **Format:** Effektzählung über alle 6 600 Bilder: Ton 3 = 149, Ton 4 = 53, Ton 1 = 25 (andere Töne kommen nicht vor).
- **Wirkung auf die Zufallsfolge:** Mit den Anfangswerten (Geschwindigkeit 70, Darstellung 0) zieht der Kern nichts zusätzlich;
  Darstellung ≠ 0 bzw. Geschwindigkeit < 70 verschieben die Folge der Managerpaarung wie im Original.

### A5 Minutenschritt im Kern (`Core/MatchEngine.LiveStep.cs`, R18-E1 = A)

- **Ablauf:** `LiveMatchDay.Begin` bereitet alle Paarungen in ihrer Reihenfolge vor wie 0x36B5D (Spielabrechnung 0x544C0 je
  Paarung vor den Minuten, Datei 0x36DBD/0x36F9C); `Step` rechnet je Minute alle laufenden Paarungen über
  `OriginalMinutes.PlayMinute` wie die Minutenschleife 0x37A62 (Drittelpausen 0x84D7 bei 21/41/61/66, Verlängerung 61–70 nur
  im K.-o.-Spiel bei Gleichstand); `Finish` rechnet den Rest und schließt je Paarung ab wie `PlayWithEvents`.
- **Gemeinsamer Teil:** Vorbereitung (`BeginMatch`: Spielwertung 0x36DC5, Anzeigereihe 0x544C0, Mannschaften, 0x8365,
  schneller Weg 0x3731F), Ende der Minutenschleife (`FinishMinuteLoop`: Penaltyschießen 0x3901B) und Abschluss (`EndMatch`:
  Vereinsfelder, Spielerzähler, Tagesform 0x50283) aus `PlayWithEvents` (`MatchEngine.Live.cs`) herausgelöst, Züge und
  Reihenfolge unverändert.
- **Eingriff:** Befehle aus A3 zwischen zwei Schritten schreiben die `LiveTeam`; die nächste Minute liest sie (Datei
  0x38ECD–0x38FBE schreibt die Vereinsfelder, gelesen wird in der nächsten Rechnung, §10).
- **Zufall:** Jede Paarung zieht aus dem Generator ihrer Vorgabe. Mit einem Generator je Paarung ist das Ergebnis gleich
  `PlayWithEvents`; mit einem gemeinsamen Generator ziehen die Paarungen verschränkt wie im Original (0x37A62), also anders
  als die Spieltage des Remakes (Paarung für Paarung). `[APPROX]` Wie ein Spieltag über den Minutenschritt den Zufall
  verteilt, legt der Anschluss fest (B2/B3) – Abnahme dort „Spieltag über den Live-Bildschirm = Ergebnis ohne Anzeige“;
  der Minutenschritt selbst ändert keinen Spieltag.

## 14. Umsetzung Spur `livespiel-ui` (Runde 18, B1 und B4)

- **B1:** Bausteine in `src/EishockeyManager.UI/LiveWidgets.cs` (Spieluhr 0x51E90, Balken 0x533D0/0x53307, Lampe 0x53453,
  Ziffer 0x53234); „Spiele laufen“ in `MatchDayScreen.DrawRunning` nach Datei 0x37B82–0x37C5C (Text 0x30:0x885 zentriert
  x 0–319, Grundlinie 80, Farbe 30, Schatten 0; Rahmen Farbe 0, Füllung Farbe 3; Uhr Stil 1). Die Farben der Balken
  stehen in DGROUP 0x5425 und 0x5427 je als 0x18, 0x13. Pixelvergleich mit `karriere_02315` (Szenario `live-bausteine`):
  Text, Kasten, Balken und Ziffern je bei Versatz (0 | 0) am besten.
- **Korrektur der Abnahme:** `karriere_02315` zeigt Minute **08**: oberer Balken 32 Pixel gelb (x 9–41, Linie mit beiden
  Enden), die anderen beiden rot – wie 0x51E90 mit Minute 8 rechnet. „Zwei volle Balken, dritter 32 px“ gilt erst bei
  Minute 48 (Szenario-Bild `02_spiele_laufen_minute_48`). Der Text stand im Remake schon auf Grundlinie 80 (Oberkante 65);
  abweichend waren die Mitte (160 statt 159 − Breite/2), Schatten 53 statt 0, Rahmen 241 statt 0, Füllung 2 statt 3,
  Balken und Ziffern.
- **0x53234:** Die Ziffern in kb\38 haben Farbe 0 als Grund; `karriere_02315` zeigt das Innere der „0“ in Farbe 3: 0x50:0xBD3
  lässt Farbe 0 durch (`[ORIGINAL]` Datei 0x1134C, Fixlauf #820 wie `LiveWidgets.Digit`).
- **B4:** Spielende 0x51F3D war bereits umgesetzt (`SeasonEndGameOverScreen.DrawGameEnd`, Tests `SpielendeOriginalTests`);
  neu nur das Szenario `live-spielende`. Ein Videobild des Spielendes gibt es in den Reihen nicht.

## 15. Umsetzung Spur `livespiel-ui-2` (Runde 18, B2 und B3)

- **Anschluss (B2, `Core/MatchEngine.LiveDriver.cs`):** Mit eingesetztem `ILiveMatchDriver` (je Thread) läuft jede Paarung mit
  Anzeigereihe in `PlayWithEvents` über `LiveMatchDay` mit derselben Vorgabe und demselben Zufall; der Treiber rechnet die
  Minuten. `[APPROX]` Das Remake behält die Reihenfolge „Paarung für Paarung“: Ohne Eingriff ist der Spieltag über den
  Live-Bildschirm damit genau der ohne Anzeige (Tests `LivespielUi2DriverTests`, `LivespielUi2Tests`); mehrere
  Managerpaarungen eines Tages laufen nacheinander statt gleichzeitig (Original 0x37A62: alle Paarungen je Minute).
- **Oberfläche:** `LiveSession` führt `GameState.Continue()` auf eigenem Thread; es rechnet immer nur ein Thread (Übergabe per
  Semaphore). `MatchDayScreen.Start` zeigt je Halt Banner und `LiveMatchScreen`; „Spiele laufen“ entfällt für live gezeigte
  Spiele (§1 Schritt 1/2).
- **Textargumente 0x30:0x885:** Reihenfolge auf dem Stapel Schatten, Farbe, Grundlinie, x2, x1 (Vergleich Datei 0x37BAA
  „Spiele laufen“). Damit berichtigt sich §5: Kopftext x 0–111, Grundlinie 20, Farbe 0x18 (Datei 0x525F3); Ereignistext
  Grundlinie 22 (Datei 0x52871); Zusammenfassung in Schrift 3 auf den Grundlinien 10/18/26, Farbe 0x18 (Datei 0x529BC,
  0x52AD4, 0x52B00). Strafminuten „n min.“ ohne Schatten (0xFF), Farbe 0x10, x 40–74, Grundlinie 5 (Datei 0x52F2F). Videobild
  `bundestrainer_00247/00248`: drei gelbe Zeilen.
- **Mundfolge 0x5224E:** aufgefüllt wird mit 9 nur **bis Eintrag 8** (Datei 0x522C2–0x522D5), nicht acht Einträge angehängt;
  „Chance:“ = [0, 0, 9 × 6, 0xFF] (A und E öffnen den Mund, je das folgende Zeichen übersprungen). Die Abnahme aus §12
  ([0, 9 × 8, 0xFF]) ist damit berichtigt.
- **Pause ≤ 0 (#823):** 0x130EE teilt n mit Vorzeichen durch 3 und übergibt das Wort an 0xBBF:0x2C08 (Ziel = BIOS-Ticks
  0040:006C + Wort, vorzeichenlos); 0xBBF:0x2C4E wartet bis zum Ziel. n = −1/−2 → keine Pause, n = −3/−4 → 0xFFFF Ticks
  (≈ 1 h). Geschwindigkeiten 62–69 kommen im Original nicht vor (Regler in Zehnern, Lampe 70); das Remake nimmt `[APPROX]`
  „≤ 0 = keine Pause“.
- **Abgleich:** Szenario `live-spiel` gegen `duell_s2a_01126` (erste Reihe): Kästen, Reihenhintergrund, Ziffern, Spalten,
  „0 min.“, Namen und Uhr deckungsgleich (Sichtvergleich); Reporter gegen `bundestrainer_00200/00247/00248`.
  `[APPROX]` Durchsichtigkeit des Reporterbilds (Farbe 0) aus dem Video; Zuschauerzahl im Heimspiel leer (Remake rechnet sie
  erst nach dem Spieltag); vergebene Chancen ohne Schützen; Reihenfolge der Meldungen einer Minute Chancen, Strafen,
  Verletzungen.
- **B3 Klickwege:** Treffer-Tests 0x534BC/0x53508 und Lampe „Schnelldurchlauf“ wie §10 (`LiveMatchScreen.SliderHit`,
  `LampHit`, `FastLampHit`), Befehle über `MatchIntervention`. Regler (Teil 3) nicht nötig (`GameOptionsScreen` hat ihn).
- **B3 Kopf/Logo (Fortsetzung 1):** die drei offenen Stellen aus §10 gelesen (`python3 tools/reverse-engineering/disasm.py
  em.exe 0x40D80 0x41440`, `0x41533 0x41683`):
  - **Datei 0x41298** (Aufstellweg 0x41273, erreicht von Linksklick auf einen Nicht-Aufgestellten bzw. Nicht-Eingesetzten,
    Datei 0x40F1E–0x40F32, und im Spiel von **jedem Rechtsklick**, Datei 0x40F35–0x40F42): ist der Spieler eingesetzt und
    aufgestellt (+0x0A ≠ 0, +0x15 ≠ 0) und der Kennwert ≠ 0, endet der Klick ohne Wirkung. Im Spiel nimmt die rechte Taste also
    keinen Aufgestellten heraus und setzt niemanden ein oder aus; sie stellt wie die linke in die gewählte Reihe (Meldung
    „Keine Reihe“ nur für die linke Taste, Datei 0x40E63).
  - **Datei 0x4135E** und **0x413E8** (erster freier Platz bzw. sechster Feldspielerplatz) reichen den Kennwert als letztes
    Argument an das Aufstellen **0x41533** weiter: Platz +0x15, eingesetzt +0x0A = 1 (Datei 0x41569–0x415B2); mit Kennwert ≠ 0
    und ohne Spielerfeld +0x1A Bit 7 wird Bit 7 gesetzt und sofort ein Spiel gezählt (+0x06 + k und Wort +0x1C + 2k, k nach
    DGROUP 0xD0C2: 6 → 1, 7 → 0 und Zähler `far:0x8253 + 0x21 · Verein +0x1C`, sonst 2; Datei 0x415B7–0x41622).
  - Zusammen mit §10: Grenze 22 nur ohne Spiel (0x40E2A), Herausgenommene bleiben eingesetzt (0x40CFD, 0x4115F).
  - **Umsetzung:** `Club.ClickPlayerInMatch`/`ClearLineupSlotInMatch` (neu `Core/Squad.Lineup.InMatch.cs`), Übernahme in den
    Spielkader `LiveLineup.Apply` (neu `Core/MatchEngine.LiveLineup.cs`: Platz +0x15, eingesetzt, Energie und Tore bleiben,
    danach `MatchIntervention.AfterLineup` = 0x38A36). `LiveMatchScreen`: Kopf/Logo → `LiveIntervention.Open` (Aufstellung bzw.
    Taktik mit `InMatch`, Wechsel über den Knopf unten rechts wie die Schleife 0x48E28), `GameState.CurrentManager` = Manager
    des Vereins und zurück (DGROUP 0xA70E), Kopfzeile 0x40290 (Schrift 2, Farbe 30, Verein ab x 1, Manager bis x 317,
    Grundlinie 10). Szenario `live-eingriff`.
  - `[APPROX]`: Spiele zählt das Remake am Spielende aus der Aufstellung (0x5494C) statt beim Aufstellen im Spiel; Linienwechsel-
    Einstellung (DGROUP 0xA700) aus der Taktik im Spiel wirkt erst im nächsten Spiel (`LiveTeam.LineChange` beim Anpfiff
    kopiert), Einsatz-/Angriffsmodi und Spielstandswerte sofort (gleiche `Tactics`); Bestrafte bleiben bis zum Freiwerden
    draußen, auch wenn die Aufstellung sie neu setzt; Tasten A/C im Spiel aus; Nationalteam des Bundestrainers ohne Eingriff;
    Farbe 7 statt 0x1E beim ersten Eintrag der Taktik im Spiel (Datei 0x47F5E) nicht übernommen.

