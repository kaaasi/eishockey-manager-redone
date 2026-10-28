# Spielstand (eigenes Format)

Spur `speichern` (Runde 3, Welle B, Audit G11). Dieses Dokument wächst mit den Teilen der Spur
(`-rng`, `-ids`, `-format`, `-dialog`). Import der Original-Spielstände `*.HOK` ist nicht Teil dieses Formats.

## Zufallszustand (Teil `-rng`)

Der Spielstand hat vier Zufallsquellen: `GameState.Random`, `FinanceRandom`, `CupRandom`, `ContractRandom`.
Alle sind vom Typ `GameRandom` (`src/EishockeyManager.Core/Rng.cs`).

- **Gleiche Folge wie `System.Random`:** `GameRandom(seed)` bildet den subtraktiven Generator nach Knuth nach, den
  `System.Random(seed)` in .NET verwendet („Net5CompatSeedImpl“). Für jeden Seed sind `Next()`, `Next(max)`,
  `Next(min, max)`, `NextDouble()`, `NextSingle()`, `NextInt64(…)`, `NextBytes(…)` und `Shuffle(…)` wertgleich
  (`RngTests`). Dadurch bleiben alle bisherigen Spielverläufe und Mehrsaison-Referenzwerte unverändert
  (`RngTwinProbeTests`: Abdrücke nach 4 Saisonwechseln, auf `main` `5f8e02a` ermittelt).
- **Zustand:** `RngState` = 56 ganze Zahlen (Feld 0 unbenutzt) und die beiden Zeiger `Next` (0–55) und
  `NextP` (0–55). `GameRandom.Capture()` legt eine Kopie an, `Restore(state)` setzt dieselbe Instanz zurück,
  `GameRandom.FromState(state)` erzeugt eine neue. Danach setzt sich die Folge identisch fort.
- **Verzögerte Erzeugung:** `FinanceRandom`, `CupRandom` und `ContractRandom` entstehen beim ersten Zugriff aus einem
  Saison-Seed (`??=`). `GameState.CaptureRng()` liefert ein `RngSnapshot` mit `null` für eine noch nicht erzeugte
  Quelle und erzeugt dabei nichts; `RestoreRng(snapshot)` stellt „nicht erzeugt“ wieder her, sodass die Quelle
  später wie im ununterbrochenen Spiel aus dem dann gültigen Seed entsteht.
- Ein `GameRandom()` ohne Seed nimmt einen zufälligen Seed (`[APPROX]`: `System.Random()` ohne Seed nutzt in .NET
  einen anderen Algorithmus; dessen Folge ist ohnehin nicht wiederholbar).

Die Ablage in der Datei (Kennung, Schemaversion, Feldreihenfolge) legt Teil `-format` fest.
