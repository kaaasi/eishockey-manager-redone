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
| `docs/` | Bestandsaufnahme, Dateiformate, Anforderungen, Reverse Engineering, Referenzvideos |

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

# Übersicht, PNG-Export (Bilder, Halle, Figuren, Fonts) und Vereinsdatenbank
dotnet run --project src/EishockeyManager.Tools -- info
dotnet run --project src/EishockeyManager.Tools -- export gamedata export
dotnet run --project src/EishockeyManager.Tools -- vereine gamedata 56
```

## Fahrplan

- [ ] **v0.1 – Asset-Viewer:** Grafiken mit originalgetreuen Farben, Datenbank, Fonts, Halle, Figuren (umgesetzt, Test mit den Originaldateien auf deinem Rechner steht aus)
- [ ] **v0.2 – Datenformate:** Sound/Musik, Torszenen, Animationstabelle
- [ ] **v0.3 – Oberfläche:** Hauptmenü und Manager-Bildschirme nachbauen
- [ ] **v0.5 – Spiellogik:** Saisonablauf, Spielberechnung, Finanzen
- [ ] **v1.0 – Parität:** verhält sich wie das Original
- [ ] **danach:** neue Funktionen nach [docs/anforderungen/zusatzfunktionen.md](docs/anforderungen/zusatzfunktionen.md)
  (moderne Grafik, weitere Sprachen, Cheatmenü, Netzwerk mit Dedicated Server, Ex-/Import)

Details zum aktuellen Analysestand: [docs/bestandsaufnahme.md](docs/bestandsaufnahme.md)
