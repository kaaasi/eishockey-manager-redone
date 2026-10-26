# Dateiformate des Eishockey Managers (DOS, V1.00 vom 10.05.1993)

Mehrbyte-Werte sind Little Endian, sofern nicht anders angegeben.

Status: **✔** = an den Originaldateien geprüft und in `EishockeyManager.Formats` umgesetzt (mit Tests),
**○** = aus dem früheren Portierungsversuch übernommen, noch nicht nachgeprüft.

## Verzeichnisstruktur

| Pfad | Inhalt |
|---|---|
| `em.exe` | Programm (Borland C++ 1991, Large Model, Real Mode, DGROUP = Segment `0x14FD`, VROOMM-Overlays). Enthält die Bitmapfonts. |
| `kb\N.vga` | Grafiken, Paletten, Datenbank, Hallenkacheln, Sounds und Musik – trotz Endung nicht alles Bilder |
| `tore\N.ht/.hv/.at/.av` | Torszenen (Kennung `TOR!`), `anzahl` = `MAXSZENE:11|2|2` |
| `high\version.txt` | Versionskennung |
| `infofile` | Liesmich-Text (CP437) |

## Bilder `kb\N.vga` ✔ — `VgaImage`

```
u16 Breite
u16 Höhe
u16 n          Länge der komprimierten Daten (inkl. Endmarker)
u8[n]          PackBits-Daten
u8[765]        optional: eigene Palette (255 Farben × RGB)
```

Lader in `em.exe` ab Code-Offset `0x0A380`, Entpacker `0BBF:45C6`.

| Steuerbyte | Wirkung |
|---|---|
| `00`–`7F` | die nächsten *n*+1 Bytes unverändert kopieren |
| `80` | Ende des Stroms |
| `81`–`FF` | das nächste Byte (*n* & `7F`)+1-mal wiederholen |

Bilder mit mehr als **200 Zeilen** bestehen aus zwei Strömen (Zeilen 0–199, dann der Rest), weil ein
Speicherblock im Real Mode höchstens 64 KB groß ist. Vollbilder: 320×240 (VGA Mode X, quadratische Pixel).
Der Entpacker kann direkt in den Bildspeicher schreiben und schaltet dabei pro Pixel die Bitplane um.

Bilder mit eigener Palette: 5, 14, 16, 151, 195, 197.

## Paletten ✔ — `Palette`

**Wichtig:** Die Paletten werden unverändert in den VGA-DAC geschrieben, der nur **6 Bit** pro Kanal auswertet.
Für die Darstellung zählt also nur `Wert & 0x3F`. Erst mit dieser Maskierung stimmen die Farben mit den
Videos des Originals überein (graublaue Menüs statt türkis).

- `kb\21.vga`: Standardpalette (765 Bytes, ohne Kopf). Die Werte sind teilweise größer als 63.
- `kb\247.vga`: entspricht `21 & 0x3F`, bis auf die Indizes 12, 30, 175, 253 und 254 –
  das sind die Farben, die das Spiel zur Laufzeit umfärbt (z. B. Index 30 als Kachel-/Kopfhintergrund).
- In Bildern eingebettete Paletten: ebenfalls maskieren.

## Komprimierte Rohdaten ✔ — `PackedData`

`kb\231–235`: `u16 Länge`, danach ein oder mehrere PackBits-Ströme ohne Bildkopf.

| Datei | Ergebnis | Inhalt | Klasse |
|---|---|---|---|
| 231, 232 | je 64 000 Bytes | je 250 Hallenkacheln 16×16, **spaltenweise** (Pixel (x, y) an x·16 + y) | `RinkTileSet` |
| 233 | 40 960 Bytes | 40 Figuren 32×32 Heim, zeilenweise, Index 0 transparent | `SpriteSheet` |
| 234 | 40 960 Bytes | 40 Figuren 32×32 Gast | `SpriteSheet` |
| 235 | 12 288 Bytes | 12 Torhüterbilder 32×32 | `SpriteSheet` |

## Hallenkarten ✔ — `RinkMap`

`kb\236`, `kb\242–245` (je 6048 Bytes): 72 × 42 Einträge, `u16` **Big Endian**.
Bit 0–14 = Kachel 0–499 (231 und 232 hintereinander), Bit 15 = waagerecht gespiegelt.
Ergebnis: Halle mit 1152 × 672 Pixeln, Tore links und rechts auf halber Höhe.

## Datenbank `kb\23.vga` ✔ — `ClubDatabase`

36 648 Bytes, kein Bild.

| Offset | Größe | Inhalt |
|---|---|---|
| `0x0000` | 150 × 2 | Farbcode je Verein (hi, lo als `sbyte` → hi·256 + lo) |
| `0x012C` | 64 × 1 | Logo: `(b + 0xB0) & 0x3F` → Bild `kb\(80 + Logo).vga` |
| `0x016C` | 150 × 3 | Stärke je Verein (drei gleiche Werte) |
| `0x04AE` | 64 × 523 | Vereine mit Kader: Name[23] + 25 × (Name[19] + Ausländer-Flag[1]) |
| `0x876E` | 86 × 23 | Namen der Europapokal-Gegner (ohne Kader) |

Kaderplätze: 0–1 Torhüter, 2–11 Verteidiger, 12–24 Stürmer. Leere Plätze beginnen mit `0x00`
(dahinter teils Restmüll); das Spiel vergibt dafür zufällige Namen anderer Datenbankspieler.

| Vereinsindex | Bedeutung |
|---|---|
| 0–11 | 1. Bundesliga 1992/93 |
| 12–23 | 2. Liga |
| 24–35 | Oberliga |
| 36–47 | weitere Oberligisten |
| 48–51 | Leerplätze (`LEER1`–`LEER4`) |
| 52–63 | Nationalmannschaften (56 = Deutschland) |
| 64–149 | Europapokal-Gegner |

○ Die Stärkewerte der Nationalmannschaften im Spiel stehen zusätzlich als Text in der EXE (DGROUP:`0x4C4E`, jeweils −2).

## Texte ✔ — `GameText`

Die Fonts haben nur die Zeichen 0x20–0x7F. Umlaute liegen auf `[ \ ] { | } ~` = `Ä Ö Ü ä ö ü ß`.
In der Datenbank kommen vereinzelt CP437-Umlaute vor (`0x99` = Ö). Das `infofile` ist CP437-kodiert.

## Fonts in `em.exe` ✔ — `BitmapFont`

Grafiksegment `0BBF` (Dateioffset = EXE-Kopfgröße + `0xBBF0`). Fonttabelle bei Segmentoffset `0x06CD` (4 Zeiger):

| Font | Kopf | Höhe | Grundlinie |
|---|---|---|---|
| `normal.font` | `0x19A3` | 16 | – (VGA-BIOS-Font, keine eigenen Glyphen) |
| `mini.font` | `0x19A9` | 6 | 1 |
| `flow.font` | `0x1D08` | 8 | 1 |
| `newbold.font` | `0x0ABD` | 20 | 4 |

Kopf: `u16 Namenszeiger, u16 Höhe, u16 Grundlinie, 96 × u16 Glyphzeiger`.
Glyph: `u8 Breite`, dann Höhe × ⌈Breite/8⌉ Bytes (1 Bit pro Pixel, MSB zuerst).
Die Breite enthält den Zeichenabstand bereits – Text wird **ohne** zusätzlichen Abstand gesetzt.

## Sound ✔

- **VOC** (`kb\237–241`, ✔ `VocFile`, Export `emtool export-sound`): Kopf „Creative Voice File“ + 0x1A, u16 Kopflänge (26),
  Version 0x010A, Prüfwert 0x1129. Blöcke Typ 1 (8 Bit unsigned, Rate = 10⁶ / (256 − TC)),
  2 (Fortsetzung), 3 (Stille, Länge = u16 + 1), 6/7 (Wiederholung, Anzahl + 1 Durchläufe, 0xFFFF = endlos);
  der Lader kennt außerdem 4/5 (übersprungen), 8 und 9. Im Spiel benutzt:

  | Datei | Blöcke | TC → Rate | Abtastwerte | Nach Gehör |
  |---|---|---|---|---|
  | 237 | 6 (0xFFFF), 1, 7 | 42 → 4673 Hz | 9146 | Publikum (Endlosschleife) |
  | 238 | 1 | 42 → 4673 Hz | 4910 | Jubel |
  | 239 | 1 | 172 → 11 905 Hz | 9194 | Pfiff |
  | 240 | 1 | 101 → 6452 Hz | 906 | Puck |
  | 241 | 1 | 101 → 6452 Hz | 2574 | Check |

  **Verwendung (Disassembly, `SoundEffect.cs`):** Effekte spielt nur die Torszene. Sie lädt `kb\(231 + i)`, i = 0–13,
  in die Zeigertabelle DGROUP 0xE2B9; Effekt-Index s (Byte 3 eines 28-Byte-Bildes in `tore\*`, ab Byte 0x54,
  Ende 0xAAAA) spielt Eintrag 6 + s = `kb\(237 + s)`. Ist die Ausgabe frei, startet 237 neu. In den 49 Szenen:
  s = 3 (Puck) 149×, 4 (Check) 53×, 1 (Jubel) 25× (24× am Ende von Treffer-Szenen), **2 (Pfiff) nie** –
  weitere Aufrufer der Abspielfunktion 99F:000A gibt es nicht, der Pfiff ist im Original also nicht zu hören.
- **CMF** (`kb\248`, ✔ `CmfFile`, Wiedergabe `CmfPlayer` über `Opl2`): Kopf „CTMF“, Version 1.1. Instrumente ab 40
  (14 × 16 Byte OPL-Register, alle Wellenformen 0), Musik ab 264, 30 Ticks je Viertel, 96 Ticks/s, Grundtempo 190.
  Kanäle 0–5 melodisch, 11 (Bass Drum), 12 (Snare), 15 (Hi-Hat). Erstes Ereignis Controller 0x67 = 1 (Rhythmusmodus),
  31 Programmwechsel, 4550 Noten (Ende als Note an mit Anschlag 0), Ende `FF 2F` bei Tick 16 491 = **171,8 s**.
- **FM-Treiber**: em.exe enthält ab Datei `0x14D70` den Creative-Treiber SBFMDRV (Kennung „FMDRV“, Sprungtabelle
  der Funktionen bei +0x261); CT-VOICE für die VOC-Effekte liegt ab `0x143B0`. Aus dem Treiber übernommen
  (Adressen relativ zu 0x14D70, Einzelheiten in `CmfPlayer.cs`): Stimmenwahl (+0B6E), Instrument laden (+0A1E),
  Anschlag → Trägerpegel `TL = 63 − ((63 − TL) · (Anschlag | 0x80) ≫ 8)`, Tonhöhe (+0E75: Oktave = Note/12 − 1,
  F-Nummern-Tabelle +0305 = `round(342,92 · 2^(i/768))`, 64 Schritte je Halbton), Controller 0x66–0x69 (+0EFA),
  Register 0xBD = 0xC0 (tiefes Tremolo/Vibrato), im Rhythmusmodus 0xE0 mit 6 Melodiestimmen.
  Tonhöhenbeugung, Aftertouch und andere Controller ignoriert der Treiber.

## Offen

- `kb\246`: vermutlich Animationstabelle.
- `tore\*`: Kopf `TOR!`, Big-Endian-`u16`, Rahmen zu 28 Bytes – Bedeutung offen.
- ○ Spielstände `*.HOK`: verschlüsselt mit `verschlüsselt = (klar XOR k) + k`, danach `k -= 0x29`;
  Startschlüssel `0x6E + random(0..255)` im Spielstand, 8-Bit-Prüfsumme der Klartextbytes.
  Nachprüfen, sobald ein Original-Spielstand vorliegt.
