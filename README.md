# Eishockey Manager Redone

Portierung des **Eishockey Managers** (Software 2000; Amiga 1991, DOS 1993) nach C#/.NET,
mit dem Ziel, das Spiel originalgetreu nachzubauen und anschließend um neue Funktionen zu erweitern.

> **Wichtig:** Dieses Repository enthält **keine** Dateien des Originalspiels.
> Grafiken, Sounds und Daten werden zur Laufzeit aus einer eigenen Kopie des Spiels gelesen.

## Projektstruktur

| Ordner | Inhalt |
|---|---|
| `src/EishockeyManager.Formats` | Lesen/Schreiben der Originalformate (Grafiken, Paletten, …) |
| `src/EishockeyManager.Tools` | `emtool` – Kommandozeilenwerkzeug zum Untersuchen und Exportieren |
| `src/EishockeyManager.Viewer` | Grafik-Betrachter (MonoGame) – Meilenstein v0.1 |
| `tests/` | Unit-Tests (arbeiten nur mit synthetischen Daten) |
| `docs/` | Bestandsaufnahme und Dokumentation der Dateiformate |

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Eine eigene Kopie der DOS-Version des Spiels

## Originaldateien bereitstellen

Den Inhalt der DOS-Version in den Ordner `gamedata/` im Repo-Stamm kopieren, sodass
z. B. `gamedata/em.exe`, `gamedata/kb/0.vga` und `gamedata/tore/anzahl` existieren.
Der Ordner ist per `.gitignore` vom Einchecken ausgeschlossen.

## Starten

```bash
# Grafik-Betrachter (Pfeiltasten: Bild wechseln, 1–4: Zoom, P: Standardpalette, Esc: Ende)
dotnet run --project src/EishockeyManager.Viewer

# Übersicht und PNG-Export aller Grafiken
dotnet run --project src/EishockeyManager.Tools -- info
dotnet run --project src/EishockeyManager.Tools -- export gamedata export
```

## Fahrplan

- [ ] **v0.1 – Asset-Viewer:** alle Grafiken korrekt anzeigen (Bilder ✔, Paletten prüfen, Sonderformate offen)
- [ ] **v0.2 – Datenformate:** Vereine, Spieler, Ligen, Torszenen, Sounds/Musik entschlüsseln
- [ ] **v0.3 – Oberfläche:** Hauptmenü und Manager-Bildschirme nachbauen
- [ ] **v0.5 – Spiellogik:** Saisonablauf, Spielberechnung, Finanzen
- [ ] **v1.0 – Parität:** verhält sich wie das Original
- [ ] **danach:** neue Funktionen

Details zum aktuellen Analysestand: [docs/bestandsaufnahme.md](docs/bestandsaufnahme.md)
