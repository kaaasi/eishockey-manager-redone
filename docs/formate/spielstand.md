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

Die Ablage in der Datei steht unten im Abschnitt „Datei“ (Feld `Random` usw. als `{"$id", "next", "nextP", "seed"}`).

## Spieler-IDs (Teil `-ids`)

Jeder Spieler hat eine dauerhafte ID `Player.Id` (`src/EishockeyManager.Core/Player.Identity.cs`, `[APPROX]`:
das Original kennt keine IDs). 0 heißt „noch nicht im Spiel“.

- **Vergabe:** `GameState.NextPlayerId` zählt nur aufwärts (Start 1).
  - Spielstart (`NewGame.Create`, nach dem Kaderaufbau): alle Kaderspieler in Vereins- und Kaderreihenfolge.
  - Jedes Talent über `PlayerFactory.CreateTalent(game, …)`: Kader-KI der Computervereine und Talentsuche.
  - Pool, Transfer, Leihe und Laufbahnende erzeugen keine Spieler und behalten die ID; eine ID wird nie neu vergeben,
    auch nicht nach dem Laufbahnende.
- Die Vergabe verbraucht keinen Zufall; die Mehrsaison-Abdrücke aus Teil `-rng` bleiben unverändert (ohne `Id`).
- **Prüfung:** `GameState.ValidatePlayerIdentity()` meldet Spieler, die mehrfach vorkommen; ungültige oder doppelte
  IDs; Aufstellungen mit kaderfremden Spielern; Leihen ohne Stammverein; Markt-, Gebots- und Nationalkaderverweise ins
  Leere. `FindPlayer(id)` löst eine ID über Kader, Pool und `RetiredPlayers` auf.
- **Für Teil `-format`** als ID zu sichern: Kader, Aufstellung/Blöcke, Transfermarkt (Angebote, Gebote), vertragsloser
  Pool, Leihe (Stammverein über Vereinsindex), Nationalkader und `RetiredPlayers`. Vereine über ihren Index, Manager
  über ihre Position in `Managers`. Der Posteingang hält keine Objektverweise auf Spieler, nur Text.

## Datei (Teil `-format`)

Umsetzung: `src/EishockeyManager.Core/SaveGame*.cs`. Endung `.ems`, Inhalt UTF-8-JSON mit Einrückung.

**Warum Text (JSON):** lesbar und vergleichbar (Fehlersuche, Diff zweier Spielstände), ohne zusätzliches Paket
(`System.Text.Json` gehört zu .NET). Größe ca. 1 MB je Spielstand, unkritisch.
**Warum feldweise statt eigener Datenklassen:** Der Zustand ist über viele `private`-Felder in den Dateien anderer
Spuren verteilt. Gesichert wird deshalb jedes Instanzfeld per Reflexion – neue Felder sind automatisch dabei, und der
Schemawächter (unten) erzwingt bei jeder Änderung eine neue Schemaversion. Die Dateinamen der Felder sind damit Teil
des Schemas.

```json
{
  "kennung": "EISHOCKEY-MANAGER-REDONE-SPIELSTAND",
  "schema": 1,
  "spiel":   { …Felder von GameState… },
  "vereine": [ { …Felder von Club… }, … ],     // Reihenfolge = GameState.Clubs
  "manager": [ { …Felder von Manager… }, … ],  // Reihenfolge = GameState.Managers
  "spieler": [ { …Felder von Player… }, … ]    // nach Player.Id sortiert
}
```

- **Prüfung vor dem Lesen:** zuerst `kennung`, dann `schema`. Fremde Datei → „Die Datei ist kein Spielstand dieses
  Spiels.“; höhere Version → „Der Spielstand stammt aus einer neueren Version …“. Ältere Versionen laufen durch die
  Migrationen (`SaveGame.Migrations`, je Version eine Funktion auf dem JSON-Baum; zunächst leer).
- **Felder:** alle Instanzfelder (auch `private`, auch geerbte aus Core/Formats), nach Name sortiert. Rückfelder
  automatischer Eigenschaften heißen wie die Eigenschaft (`Age`), andere Felder wie im Code (`_eternal`).
  Beim Laden muss jedes Feld vorhanden und keines unbekannt sein – sonst Meldung „beschädigt“.
- **Werte:** Zahlen als JSON-Zahl; `double`/`float`/`decimal` als Zeichenkette (`"R"`, invariant, verlustfrei);
  Aufzählungen als Name; `null` für leere Verweise.
- **Verweise:**
  - Spieler `{"$spieler": id}` (`Player.Id`), Vereine `{"$verein": index}`, Manager `{"$manager": index}`,
    der Spielstand selbst `{"$spiel": 0}`.
  - Jedes andere Objekt (auch Listen, Felder, Wörterbücher) bekommt beim ersten Auftreten `"$id"` (fortlaufend in
    Schreibreihenfolge) und wird danach als `{"$ref": n}` verwiesen – gemeinsam genutzte Objekte (z. B. ein
    Werbevertrag in Posteingang und Werbung, ein Marktangebot in Markt und Pool) bleiben gemeinsam.
  - Feldtyp abstrakt oder Schnittstelle (z. B. `ISeasonCompetition`): zusätzlich `"$type"` mit dem Klassennamen. Es
    werden nur Klassen aus Core und Formats angenommen, nie fremde Typen.
- **Sammlungen:** `{"$id", "items": [...]}`; zweidimensionale Felder zusätzlich `"dims"`; Wörterbücher
  `{"$id", "entries": [[schlüssel, wert], …]}` in Aufzählungsreihenfolge. Felder vom Typ `IReadOnlyList<T>` werden
  als Feld (`T[]`) wiederhergestellt.
- **Zufall:** `GameRandom` als `{"$id", "next", "nextP", "seed": [56 Zahlen]}`; eine noch nicht erzeugte Quelle ist
  `null`.
- **Gleiche Bytes:** gleicher Zustand ergibt dieselbe Datei; Sichern → Laden → Sichern ist bytegleich.
- **Sichern** prüft vorher `ValidatePlayerIdentity()` und verweigert fehlerhafte Spielstände; **Laden** erzeugt immer
  einen neuen Spielstand und prüft ihn ebenso – der laufende bleibt bei einem Fehler unverändert.

**Schemawächter:** `SaveGame.SchemaDescription()` listet alle erreichbaren Typen, Felder, Feldtypen und
Aufzählungsnamen; `SaveGame.SchemaFingerprint()` ist deren Hash. Der Test `SchemaFingerprint_MatchesSchemaVersion`
kennt den Hash zur aktuellen Version. Wer ein Zustandsfeld hinzufügt, umbenennt oder dessen Typ ändert, erhöht
`SchemaVersion`, trägt eine Migration ein (neues Feld mit Startwert, umbenanntes Feld verschieben) und führt den Hash
nach. `CheckSchemaCoverage()` meldet Felder, deren Typ das Format nicht kennt (z. B. Delegaten).

**Haltepunkte:** Gesichert wird zwischen zwei Aufrufen von `GameState.Continue()`. Die WM läuft nach dem Ende der
Vereinssaison in einem einzigen Schritt; der letzte Haltepunkt davor ist `SeasonPhase.ClubSeasonOver`.
