# Review: Integration Runde 9 – transfer-vertraege-komplett (PR #336)

- **PR:** #336 · **Base:** `4f8cbc3f3e9a5746eb7716ba2ec85559202e7a9d` · **Head:** `34eeec85ed69daeb49e67b7fc2198ea775839cb5`
  (geprüft nur das Delta Base..Head, 3 Commits erster Elternlinie, 20 Commits insgesamt)
- **Reviewer:** Claude Opus 5.5, geplanter Lauf (frische Sitzung ohne Implementierungskontext, unbeaufsichtigt)
- **Checkout:** Klon, `git rev-parse HEAD` = Head, `git status --porcelain` leer; Originaldaten aus `kaaasi/eishockey-manager-referenz`
  (Zugriff per `git ls-remote` geprüft); SDK 10.0.402.

## 1. Merge-Empfehlung

**Nein** für genau diesen Head. Grund: P1-01 – jeder vorhandene Spielstand von Schema 18 und 19 (also alle Stände vom
aktuellen `main`) lässt sich nicht mehr laden („Feld _databaseNames in TransferMarket fehlt“). Die Migration 21 → 22 legt
nur `_pool` an; das Feld `_databaseNames` kam erst mit `f273954` dazu, ohne Migration, und der Migrationstest entfernt nur
`_pool` aus einem frischen Stand. Mit einer Zeile in der Migration (Diagnose in einer temporären Kopie, s. u.) laden alle
geprüften Stände, spielen zwei Saisonwechsel weiter und sichern bytegleich. Alles Übrige (Fundstellen, Mehrsaisonprobe,
Gegenbeispiel, drei Läufe) trägt die Empfehlung „ja“ nach Behebung von P1-01 und einem Regressionstest mit echtem Altstand;
P2-01 sollte im selben Zug geschlossen werden.

## 2. Befunde

### P1-01 Spielstände von Schema 18/19 laden nicht (Migration 21 → 22 unvollständig)

[`SaveGame.Migration.TransferVertraegeKomplett5.cs` Z. 18–20](https://github.com/kaaasi/eishockey-manager-redone/blob/34eeec85ed69daeb49e67b7fc2198ea775839cb5/src/EishockeyManager.Core/SaveGame.Migration.TransferVertraegeKomplett5.cs#L18-L20)
ergänzt nur `_pool`; [`TransferMarket._databaseNames`](https://github.com/kaaasi/eishockey-manager-redone/blob/34eeec85ed69daeb49e67b7fc2198ea775839cb5/src/EishockeyManager.Core/Transfer.Pool.cs#L233)
wird aber mitgespeichert und der strenge Leser verlangt es.

Gegenbeispiel (reproduzierbar):

1. Worktree auf Base `4f8cbc3` (Schema 19) und auf `6504333` (Schema 18, vor optionen-wirkung). Dort mit einem temporären
   Test Stände schreiben: `TestGames.Create(8)` zu Spielbeginn, nach 10 Spieltagen (zwei Spieler versichert, ein eigener
   Spieler auf dem Markt), nach `LifecycleRegressionTests.FinishSeason` (Phase `SeasonOver`), nach dem ersten Saisonwechsel;
   dazu Originaldaten (`NewGame.Create(…, Verein 2, Seed 5)`) nach 12 Spieltagen und am Saisonende.
2. Am Head `SaveGame.Load(bytes)` → alle 12 Stände: `Success = false`,
   `Der Spielstand ist beschädigt: spiel.TransferMarket: Feld _databaseNames in TransferMarket fehlt.`

Warum die Tests grün sind: [`Migration_FromSchema21_BuildsPoolOnFirstChange`](https://github.com/kaaasi/eishockey-manager-redone/blob/34eeec85ed69daeb49e67b7fc2198ea775839cb5/tests/EishockeyManager.Core.Tests/TransferVertraegeKomplettTeil5Tests.cs#L356)
entfernt aus einem **aktuellen** Stand nur `_pool` und lässt `_databaseNames` stehen; die übrigen Migrationstests setzen
`schema` auf einen frischen Stand zurück. Keiner lädt einen am alten Code erzeugten Stand.

Diagnose (temporäre Kopie, nicht Teil des Heads): zusätzlich
`market["_databaseNames"] = new JsonObject { ["$id"] = NewObjectId, ["items"] = new JsonArray() };` in der Migration →
alle 12 Stände laden, `ValidatePlayerIdentity()` leer, Sichern/Laden bytegleich, nach zwei Saisonwechseln Pool 199 Plätze ohne
doppelte Namen im Pool; Versicherung wird zu 10 % des Gehalts übernommen (z. B. 2.500 DM → 250 DM, wie in der Migration 19 → 20
beschrieben). Empfehlung: Migration ergänzen und den Migrationstest auf einen Stand umstellen, der beide Felder entfernt (oder
einen am Base-Code erzeugten Stand als JSON-Vorlage ohne Originaldaten einchecken).

### P2-01 Kein Test sichert den Tausch Pool ↔ Computerkader („jeder Name nur einmal“)

[`OriginalDatabase_FourChanges_NamesUniqueAndIdsDistinct`](https://github.com/kaaasi/eishockey-manager-redone/blob/34eeec85ed69daeb49e67b7fc2198ea775839cb5/tests/EishockeyManager.Core.Tests/TransferVertraegeKomplettTeil5Tests.cs#L339)
vergleicht nur Poolspieler mit `Id < startIds`. Ein ohne Tausch umbenannter Poolplatz bekommt aber eine **neue** ID
(`Rename(game, slot, name)`), fällt also genau aus der Prüfung heraus.

Gegenprobe: In `Transfer.Pool.cs` `if (squads.Remove(name, out var holder))` → `if (false && …)` (Tausch abgeschaltet). Alle 13
Tests von `TransferVertraegeKomplettTeil5Tests` bleiben grün (mit `EM_REQUIRE_GAMEDATA=1`). Dieselbe Mutation erzeugt mit
Originaldaten (Verein 2, Seed 5) **152 / 134 / 131 / 113** Poolspieler, deren Name zugleich bei einem Spieler mit alter ID in
einem Kader steht (Head ohne Mutation: 0 / 8 / 10 / 13, alle gegen Talente mit neuer ID – die bekannte Lücke
`PlayerFactory.TalentName`). Vorschlag: Pool gegen Spieler mit `Id < startIds` **außerhalb** des Pools prüfen, unabhängig von
der ID des Poolspielers (oder Talente über ihre Herkunft ausnehmen).

### P3-01 Weitere Mutationen ohne roten Test (Stichprobe je Teil)

| Teil | Mutation | Ergebnis |
|---|---|---|
| 1 | Ablöse am Saisonende Wert(Preis) / 2 → / 3 | rot ✓ |
| 2 | Prämie Wert(Gehalt) / 2 → / 3 | rot ✓ |
| 2b | `ValueSalary` + 1 DM | rot ✓ |
| 3 | Tageswurf random(0, 450) → random(0, 449) | grün (Wahrscheinlichkeitsgrenze, kaum testbar – hinnehmbar) |
| 4 | `HoverEntryColor` 201 → 17 | grün: der Test vergleicht mit der Konstante selbst ([Z. 31–32](https://github.com/kaaasi/eishockey-manager-redone/blob/34eeec85ed69daeb49e67b7fc2198ea775839cb5/tests/EishockeyManager.UI.Tests/TransferVertraegeKomplettTeil4ScreenTests.cs#L31-L32)); Lage der Linien ist gesichert, die Farbe nicht |
| 5 | Ablöse Angebot / 7 → / 8 | rot ✓ |
| 5 | `DrawIndex` random(0, 5) → random(0, 4) (Anteil Nationalspieler) | grün: kein Test hält den Zug mit festem Zufall fest |

### P3-02 Widersprüchliche Belegung Pool +0x15/+0x16

`docs/reverse-engineering.md` (Tabelle „Spielerstruktur im Original“, unverändert seit vor diesem PR) nennt +0x15 = Technik
(aus Vereinsfeld 0x19) und +0x16 = Kondition; der neue Code und `docs/transfer.md` Teil 5 nennen +0x15 = Kondition, +0x16 = Technik.
Die Disassembly (0x4981E: +0x15 aus Vereinsfeld 0x19, +0x16 aus 0x1B mit dem Zuschlag random(12, 20)) zusammen mit der
Vereinstabelle (0x18/0x19 Kondition, 0x1A/0x1B Technik) und Kaderplatz +0x10/+0x11 (Kondition, Technik) stützt den neuen
Code. Die alte Tabelle sollte angeglichen werden (nur Doku).

### Erwartet offen – bestätigt

- Knöpfe im Verhandlungsfenster 1 Pixel zu weit rechts (`docs/transfer.md`, „Noch offen“; Ursache offen) – bestätigt, nicht geändert.
- „Dieser Spieler hört am Saisonende auf.“ beim Anklicken (0x46DD8/0x46E04, Bit 0x80) – als nicht abgebildet geführt, bestätigt.
- Verjüngung des Pools nach v1.0 – `docs/anforderungen/zusatzfunktionen.md` Abschnitt C, bestätigt; der Code altert Poolspieler
  ohne Laufbahnende (`StartPoolSeason`).

## 3. Ausgeführte Prüfungen

**Gegenbeispiel `CoachWarning_Boundary`:** auf `b21c7b3` rot (2 von 2: `players: 9/True`, `10/False`), am Head grün (im Lauf
mit und ohne Originaldaten). `1a27c39` ändert nur den Test (ausdrückliche Ernennung des Bundestrainers) – sachlich richtig,
die Warnung selbst wird weiter an der Grenze 9/10 geprüft.

**Fundstellen gegen `em.exe` (ndisasm -b 16), Teil 5:**

- 0x49332–0x49371: Angebot / 7 (0:0x47C), · Restlaufzeit (0:0x3CA, Laufzeit aus Kaderplatz +0x0B, Aufruf 0x473E8–0x473FF), bei
  Argument 0 vom Angebot abgezogen → `MarketTransferFeeThirds` = Angebot − Angebot / 7 · Laufzeit, in Drittel-DM. ✓
- 0x495D8–0x9620: random(1, 179), dann random(0, 5) = 0 → random(180, 199), Prüfung Besitzer 6 → `DrawIndex`. ✓
- 0x5AFB2–0x5B0B6: Reihenfolge Alter random(18, 33), b = random(30, 89) mit Nachwurf random(40, 89), +0x16, Form random(45, 55),
  +0x15, +0x19 = 0, Besitzer 6, Schleife bis 200 → `RollStartValues` (Reihenfolge der Züge gleich). ✓
- 0x4981E–0x498D0: +0x15 aus Vereinsfeld 0x19, +0x16 aus 0x1B, random(10t, 14), Begrenzung 0x120:0x52 (10 … 99), Zuschlag
  random(0, 4) = 0 auf +0x16 → `RollMarketStrengths`. ✓ (Vereinsfelder aus Lader-Abweichung `[APPROX]`, s. u.)

**Pool ↔ Computerkader:** Head mit Originaldaten, 4 Saisonwechsel: keine Namensgleichheit Pool ↔ Spieler mit alter ID, IDs
eindeutig (bestätigt eigener Zähler; Gleichheiten nur gegen Talente, bekannt). Testlücke siehe P2-01.

**Migrationen 18/19 → 22:** siehe P1-01 (12 Stände, alle scheitern am Head; mit Diagnose-Zeile alle grün). Abdruck
`SchemaFingerprint_MatchesSchemaVersion` (22, `5DEB24A5736D8951`) grün.

**Mehrsaisonprobe:** `MultiSeason_FourChanges_PoolMarket(passiv/aktiv)` (4 Saisonwechsel, Pool, Markt, Kauf, Gutschrift,
Sichern/Laden bytegleich) und `FinanceCalibrationTests` (4 Jahre, Ablöse am Saisonende als Transfers der neuen Saison) grün.
`Mode4Reference`: Verlauf der Werte über die Teil-Commits nachvollzogen – Saison 1 Konto 2.225.350 → 808.550 mit Teil 2b
(Startgehalt = Wert(Gehalt), `a702d07`) → 711.150 mit Teil 5 (`82cb6d8`); die Begründung im Kommentar trifft zu.

**Mutationen:** Tabelle P3-01 und P2-01 (je in temporärer Kopie, danach zurückgesetzt).

**`tools/pruefen.sh` (alle drei Läufe) am Head:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 1586 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 1309 | 0 | 248 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 1309 | 277 | 0 |

Commit `34eeec85ed69daeb49e67b7fc2198ea775839cb5`, SDK 10.0.402, Ergebnis: bestanden.

**Akzeptierte `[APPROX]`** (begründet, Entscheidungen Nutzer 01.11.2026): Computerkader bleiben mit Tausch beim Umbenennen;
flexible Poolgröße; Listenwechsel je Ligaspieltag statt je Tag; Vereinsfelder aus Datenbankstärke und Lader-Abweichung,
Nationalteams ohne Ländertabelle 0x4C4E; Energie auf 99 begrenzt; Ablöse am Saisonende als Transfereinnahme gebucht;
Versicherungsprämie in ganzen DM, Altstände 10 % des Gehalts; Vertragslose aus Managervereinen bleiben auf der Liste.

**Annahmen (nicht geprüft):** Pixelvergleiche der Teile 1 und 4 (Tabellen in `docs/transfer.md`) nicht nachgemessen; übrige
Fundstellen der Teile 1–3 (0x4781B, 0x27D05, 0x07972, 0x059B8) nur über die Regressionstests und Mutationen, nicht erneut in
der Disassembly.
