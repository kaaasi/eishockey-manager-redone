# Bestandsaufnahme der Originaldateien

Stand: 25.10.2026

## Versionen

| | DOS | Amiga |
|---|---|---|
| Version | V1.00 vom 10.05.1993 | v1.00 (1991) |
| Programm | `em.exe` – Borland C++ (1991), 16-Bit Real Mode, VROOMM-Overlays | `eh` – 68000, 154 KB |
| Grafiken | `kb\*.vga`, 114 Dateien | `kleinbilder\`, `bilder\` (weniger Dateien) |
| Torszenen | `tore\` – Szenen 2–11 | `tore\` – Szenen 2–3 |
| Sound | Sound Blaster (VOC + CMF) | – |
| Hinweise | `infofile` (Spielmodus „nur Eisfläche“, Korrektur Handbuch S. 37) | `infofile` |

Die DOS-Version ist umfangreicher und dient als Hauptvorlage. Die Amiga-Version
ist als Referenz nützlich (gleiche Verzeichnisstruktur, ältere Datenstände).

## DOS: Dateien in `kb\`

| Nummern | Inhalt | Status |
|---|---|---|
| 0–20, 22, 24, 25, 33, 36, 38 | Bilder und Bildschirm-Elemente (z. B. 22 = Einstellungsmenü, 5 = Wochenkalender, 16 = Siegerfoto mit Pokal) | ✔ dekodierbar ([Format](formate/vga.md)) |
| 80–143 | 64 Symbole/Porträts à 38×38 | ✔ dekodierbar |
| 150–152, 195, 197 | Vollbilder 320×240 | ✔ dekodierbar |
| 21 | Standardpalette (8 Bit) | ✔ |
| 247 | Palette (6 Bit) | ✔ erkannt, Verwendung offen |
| 23 | Zeichensatz (wird per Name geladen) | offen |
| 231–235 | PackBits-Daten ohne Bild-Header (erstes Wort = Dateilänge − 2) | offen |
| 236, 242–245 | je 6048 Bytes, 16-Bit-Werte – vermutlich Kachel-/Bildschirmkarten | offen |
| 246 | unbekanntes PackBits-ähnliches Format | offen |
| 237–241 | Creative Voice Files (VOC) – Soundeffekte | erkannt |
| 248 | Creative Music File (CMF) – Musik | erkannt |

## DOS: Torszenen `tore\`

- `anzahl` enthält `MAXSZENE:11|2|2`.
- Dateien `<nr>.<variante>` mit Kennung `TOR!` am Anfang – vermutlich Animationsskripte
  (Positionen/Bewegungen). Varianten: `ht`, `hv`, `at`, `av` (Heim/Auswärts × t/v?),
  für Szene 2 zusätzlich `…e` und `…j`.

## Spieldaten (Vereine, Spieler)

Vereins- und Spielernamen liegen **nicht** im Klartext in `em.exe`. Kandidaten sind die
Overlay-Daten der EXE (≈ 235 KB) und die noch unbekannten Dateien 231–236/242–246.
Texte in der EXE verwenden einen eigenen Zeichensatz: `]` = Ü, `~` = ß.

## Nächste Schritte

1. Paletten-/Farbabgleich mit Videos des Originals
2. Zeichensatz `23.vga` entschlüsseln (für originalgetreue Texte)
3. Speicherort der Vereins- und Spielerdaten finden
4. Format der Torszenen (`TOR!`)
