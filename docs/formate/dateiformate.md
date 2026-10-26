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
| `0x016C` | 150 × 3 | Stärken je Verein: Abwehr, Sturm, dritter Wert (siehe unten) |
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

### Stärketripel (`0x016C`) – Rohwerte und belegte Bedeutung

`ClubDatabase` liefert alle drei Bytes unverändert (`DatabaseClub.Strengths`); `Strength` ist weiterhin Byte 0.
In der Originaldatenbank sind die drei Werte bei den Vereinen 0–63 gleich und bei allen **86 Europapokal-Gegnern
(64–149) verschieden**, z. B. Index 64: `79, 72, 74`.

**Beleg im Original** – Lader der Datenbank in `em.exe`, Datei `0x2EF1B` (Code ab `0x2EFDE`, Vereinsstruktur 61 Byte
über den Zeiger DGROUP `0x9657`). Je Verein `i` (0–149), `v = Byte[0x016C + 3·i + k]`, `t = v / 10`:

| Byte `k` | Vereinsfeld | Wert |
|---|---|---|
| 0 | `0x18` (Kondition Abwehr) und `0x1A` (Technik Abwehr) | je `v − random(0, t) + random(0, t)` |
| 1 | `0x19` (Kondition Sturm) und `0x1B` (Technik Sturm) | dito |
| 2 | – | **wird vom Lader nicht gelesen** (Schleife nur `k = 0, 1`, danach Zeiger + 3) |

Außerdem in derselben Schleife: Feld `0x17 = random(47, 53)`, Form `0x1C/0x1D = random(45, 55)`, für `i > 64`
um 5 vermindert (Vergleich `i ≤ 0x40`, d. h. Index 64 selbst ohne Abzug). Für die Nationalmannschaften
(`i` = DGROUP `[0x6AD]` = 52 bis 63) werden Kondition und Technik von Abwehr/Sturm mit den Paaren aus DGROUP `0x4C4E`
(jeweils −2) überschrieben – die Datenbankwerte der Nationalteams wirken also nicht.

**Offen:** Bedeutung von Byte 2. Im Code wurde kein weiterer Zugriff auf den Stärkebereich gefunden (Suche nach den
Konstanten `0x016C`–`0x016E` als Additionswert: nur dieser Lader). Der Datenpuffer stammt aus dem gemeinsamen
Grafikladepuffer (`call 0x140:0x3E`); ob er später noch gelesen wird, ist nicht geprüft. Nicht raten – erst bei einem Beleg
(z. B. Stärkeanzeige der Europapokal-Gegner im Video) belegen.

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

## Sound ○

- **VOC** (`kb\237–241`): Kopf „Creative Voice File“ (✔ erkannt). Blöcke Typ 1 (8 Bit unsigned, Rate = 10⁶ / (256 − TC)),
  2 (Fortsetzung), 3 (Stille), 6/7 (Wiederholung). Nach Gehör: 237 = Publikum (Schleife), 238 = Jubel, 239 = Pfiff,
  240 = Puck, 241 = Check.
- **CMF** (`kb\248`): Kopf „CTMF“ (✔ erkannt). Instrumente ab 40 (14 × 16 Byte OPL-Register), Musik ab 264, 96 Ticks/s,
  MIDI-artige Ereignisse; Controller 0x67 = Rhythmusmodus, 0x68/0x69 Transponieren, Ende mit `FF 2F`.

## Offen

- `kb\246`: vermutlich Animationstabelle.
- `tore\*`: Kopf `TOR!`, Big-Endian-`u16`, Rahmen zu 28 Bytes – Bedeutung offen.
- ○ Spielstände `*.HOK`: verschlüsselt mit `verschlüsselt = (klar XOR k) + k`, danach `k -= 0x29`;
  Startschlüssel `0x6E + random(0..255)` im Spielstand, 8-Bit-Prüfsumme der Klartextbytes.
  Nachprüfen, sobald ein Original-Spielstand vorliegt.
