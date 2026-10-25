# Bestandsaufnahme der Originaldateien

Stand: 25.10.2026 · Formatdetails: [formate/dateiformate.md](formate/dateiformate.md)

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
| 0–20, 22, 24, 25, 33, 36, 38 | Bilder und Bildschirm-Elemente (z. B. 0 = Managerköpfe, 5 = Training, 22 = Sternenhintergrund, 150 = Spielvorbereitung) | ✔ |
| 80–143 | 64 Vereinslogos à 38×38 | ✔ |
| 150–152, 195, 197 | Vollbilder 320×240 | ✔ |
| 21, 247 | Paletten | ✔ (6-Bit-Maskierung) |
| 23 | Vereins- und Spielerdatenbank | ✔ |
| 231, 232 | Hallenkacheln | ✔ |
| 233–235 | Figuren der Torszenen | ✔ |
| 236, 242–245 | Hallenkarten | ✔ |
| 237–241 | Soundeffekte (VOC) | erkannt, Wiedergabe offen |
| 248 | Musik (CMF, OPL2) | erkannt, Wiedergabe offen |
| 246 | vermutlich Animationstabelle | offen |

Dazu die Bitmapfonts mini/flow/newbold in `em.exe` (✔).

## Torszenen `tore\`

`anzahl` enthält `MAXSZENE:11|2|2`. Dateien `<nr>.<variante>` mit Kennung `TOR!` – Positions-/Bewegungsdaten,
Bedeutung der Rahmen (28 Bytes) noch offen.

## Nächste Schritte

1. Sound: VOC-Wiedergabe, CMF über OPL2-Emulation
2. Torszenen-Format `tore\*` und `kb\246`
3. Bildschirme nach [anforderungen/bildschirme.md](anforderungen/bildschirme.md) nachbauen, beginnend mit Titel → Setup → Hauptmenü
4. Spielregeln aus [reverse-engineering.md](reverse-engineering.md) an `em.exe` nachprüfen
