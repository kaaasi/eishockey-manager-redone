# Vorgehen bei der Portierung

## Aufbau

```
Desktop (MonoGame)  →  UI (Bildschirme, 320×240-Palettenpuffer)  →  Core (Spielzustand, Regeln)
                                   ↘                    ↙
                                 Formats (Originaldateien)
```

- **UI** kennt kein Fenster: jeder Bildschirm (`Screen`) verarbeitet ein `InputState` und zeichnet in eine
  `Surface` (320 × 240 Byte + Palette) – genau wie das Original in den VGA-Speicher. Dadurch lassen sich alle
  Bildschirme ohne Fenster rendern (`emtool screens`) und Pixel für Pixel mit Aufnahmen des Originals vergleichen.
- **Desktop** skaliert den Puffer ganzzahlig und reicht Maus/Tastatur an die UI weiter.
- **Core** enthält keine Darstellung. Nicht rekonstruierte Regeln sind im Code mit `[APPROX]` markiert.

## Bildschirme nachbauen

1. Videobild des Originals auf 320 × 240 bringen (Ausschnitt 960 × 720 ab x = 320 im 720p-Video, Faktor 3).
2. Die Videobilder sind gegenüber dem Original um etwa (−0,25 | +0,5) Pixel verschoben und leicht weichgezeichnet;
   beim Vergleich wird das eigene Bild genauso verschoben.
3. Grafikteile per Template-Matching im Videobild suchen → exakte Koordinaten.
4. `emtool screens` rendern und Abweichungsbild erzeugen; Rest-Abweichung entsteht fast nur an Kanten (Videounschärfe).

Das Original setzt Rahmen aus Einzelteilen zusammen (`kb\2`: 9 Teile à 24 × 19, grau und orange),
Kachelsymbole und Lampen stammen aus `kb\3`, Pfeile und Rollbalken aus `kb\4`.

## Stand

| Bildschirm | Status |
|---|---|
| Titel (`kb\197`) | ✔ |
| Spielvorbereitung (`kb\150`): Manager, Kopfwahl, Namenseingabe, Vereinswahl, Modus, Level, Dauer, Liga, Stärke | ✔ – Reihenfolge der Vereine in der Auswahl `[APPROX]`, Laden/Netzwerk fehlen |
| Hauptmenü: Infoleiste, große und kleine Kacheln | ✔ – Bedeutung der Lampen und Markierung der gewählten Kachel `[APPROX]` |
| Alle Untermenüs | Platzhalter |
