# Format: Grafiken `kb\<nr>.vga` (DOS)

Ermittelt aus dem Lader in `em.exe` (Borland C++ 1991, 16-Bit Real Mode).
Grafik-Ladefunktion ab Code-Offset `0x0A380`, Dekompression in Segment `0BBF:45C6`
(Image-Offset `0x101B6`).

## Aufbau

| Offset | Typ | Bedeutung |
|---|---|---|
| 0 | UInt16 LE | Breite in Pixeln |
| 2 | UInt16 LE | Höhe in Pixeln |
| 4 | UInt16 LE | Länge der komprimierten Daten (inkl. Endmarker) |
| 6 | Bytes | PackBits-Daten |
| 6 + Länge | 765 Bytes | *optional:* eigene Palette (255 Farben × RGB) |

Die Pixel liegen zeilenweise von links oben vor, ein Byte pro Pixel (Palettenindex).
Bilder mit mehr als **200 Zeilen** bestehen aus **zwei** komprimierten Strömen:
zuerst die Zeilen 0–199, dann der Rest. (Der Lader legt dafür zwei Speicherblöcke an,
weil ein Block im Real Mode maximal 64 KB groß sein kann.)

Vollbilder sind 320×240 – das Spiel läuft also im VGA-„Mode X“ mit quadratischen Pixeln.
Der Dekompressor kann direkt in den Bildspeicher (`A000h`) schreiben und schaltet dabei
pro Pixel die Bitplane über Port `3C4h` um.

## Kompression

| Steuerbyte | Wirkung |
|---|---|
| `00`–`7F` | die nächsten *n*+1 Bytes unverändert kopieren |
| `80` | Ende des Stroms |
| `81`–`FF` | das nächste Byte (*n* & `7F`)+1-mal wiederholen |

## Paletten

- Gespeichert werden 255 Farben (Index 0–254) mit **8-Bit-Werten** (VGA-DAC-Wert × 4).
- `kb\21.vga` ist keine Grafik, sondern die **Standardpalette** (765 Bytes, ohne Header).
- `kb\247.vga` ist ebenfalls eine reine Palette, aber mit **6-Bit-Werten** (0–63).
- Bilder mit eingebetteter Palette: 5, 14, 16, 151, 195, 197.

## Offene Punkte

- Farbtreue mit Screenshots/Videos des Originals abgleichen (insbesondere, welche Palette
  bei welchem Bildschirm aktiv ist).
- Transparenzfarbe der Sprites (vermutlich Index 0) bestätigen.
