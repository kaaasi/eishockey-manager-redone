# Eishockey Manager Redone

Portierung des **Eishockey Managers** (Software 2000; Amiga 1991, DOS 1993) nach C#/.NET,
mit dem Ziel, das Spiel originalgetreu nachzubauen und anschließend um neue Funktionen zu erweitern.

> **Wichtig:** Dieses Repository enthält **keine** Dateien des Originalspiels.
> Grafiken, Sounds und Daten werden zur Laufzeit aus einer eigenen Kopie des Spiels gelesen.

## Projektstruktur

| Ordner | Inhalt |
|---|---|
| `src/EishockeyManager.Desktop` | **Das Spiel** (MonoGame-Fenster) |
| `src/EishockeyManager.UI` | Bildschirme des Spiels – zeichnen in einen 320×240-Palettenpuffer, ohne Fensterabhängigkeit |
| `src/EishockeyManager.Core` | Spielzustand und Regeln (Vereine, Manager, Spielplan, …) |
| `src/EishockeyManager.Formats` | Lesen/Schreiben der Originalformate (Grafiken, Paletten, Datenbank, Fonts, …) |
| `src/EishockeyManager.Tools` | `emtool` – Untersuchen, Exportieren, Bildschirme als PNG |
| `src/EishockeyManager.Viewer` | Grafik-Betrachter (MonoGame) |
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
# Das Spiel (F11 = Vollbild, Esc/rechte Maustaste = zurück)
dotnet run --project src/EishockeyManager.Desktop

# Grafik-Betrachter (Pfeiltasten: Bild wechseln, 1–4: Zoom, P: Standardpalette, Esc: Ende)
dotnet run --project src/EishockeyManager.Viewer

# Übersicht, PNG-Export (Bilder, Halle, Figuren, Fonts) und Vereinsdatenbank
dotnet run --project src/EishockeyManager.Tools -- info
dotnet run --project src/EishockeyManager.Tools -- export gamedata export
dotnet run --project src/EishockeyManager.Tools -- vereine gamedata 56

# Testabläufe als PNG-Serie (für den Vergleich mit dem Original); ohne Namen: alle
dotnet run --project src/EishockeyManager.Tools -- screens gamedata screens setup spieltag tabellen
```

## Fahrplan

- [x] **v0.1 – Asset-Viewer:** Grafiken mit originalgetreuen Farben, Datenbank, Fonts, Halle, Figuren
- [x] **v0.2 – Rahmen:** Titel, Spielvorbereitung, Hauptmenü
- [x] **v0.3 – Manager & Mannschaft:** Finanzen, Werbung, Stadion, Jugend, Posteingang, Aufstellung, Taktik, Verträge, Transfer, Training (Formeln teils angenähert)
- [ ] **v0.4 – Saison:** Spieltage, Tabellen, Play-Offs, Aufstiegsrunden, Auf-/Abstieg, Saisonende, Spielende ✔ – Paarungen, Stärken, Statistiken, DEB-Pokal, Europapokal, WM folgen (Paket `wettbewerbe`)
- [ ] **v0.5 – Laden/Sichern, Einstellungen, Sound und Musik**
- [ ] **v0.9 – Live-Spiel (optional abschaltbar):** Anzeigetafel, Reporter, Torszenen – bewusst als letzter Schritt vor v1.0
- [ ] **v1.0 – Parität:** verhält sich wie das Original
- [ ] **danach:** neue Funktionen nach [docs/anforderungen/zusatzfunktionen.md](docs/anforderungen/zusatzfunktionen.md)
  (moderne Grafik, weitere Sprachen, Cheatmenü, Netzwerk mit Dedicated Server, Ex-/Import)

Details zum aktuellen Analysestand: [docs/bestandsaufnahme.md](docs/bestandsaufnahme.md) ·
Parallele Arbeit in Paketen: [docs/arbeitspakete.md](docs/arbeitspakete.md) · Leitfaden für Claude: [CLAUDE.md](CLAUDE.md)
