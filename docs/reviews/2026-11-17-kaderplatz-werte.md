# Review PR #923 – Spur kaderplatz-werte (Erstreview)

Reviewer: Claude Opus 5.5, geplanter Lauf (frische Sitzung, nicht an der Umsetzung beteiligt) · Base `191f3ee` · Head
`c3ea6c2fc2e83124266afe9f278c01f913e0852f` (`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Nein** für genau diesen Head – wegen eines P2 (R923-01): Die Darstellung „Poolwert = null, solange gleich dem Kaderplatz“
lässt Training (und jede andere Änderung am Kaderplatz) in den Poolwert durchschlagen, sobald beide zufällig gleich sind. Das
Original ändert den Poolsatz beim Training nicht. Damit ist A714/A674 (Verkauf an Computerverein „behielt trainierte Werte“,
Export Pool +0x15/+0x16) nur teilweise behoben. Alles andere ist belegt und sauber: Gegenbeispiele reproduziert, Leser und Wurf
gegen em.exe nachgelesen, fremde Testwerte mit eigener Rückschaltprobe bestätigt, alle drei Prüfläufe grün. Nach dem Fix genügt
die Delta-Prüfung der Leitstelle (nur P2/P3).

## 2. Befunde

### R923-01 (P2) – Training verschiebt den Poolwert, wenn Pool und Kaderplatz gleich sind

[`Squad.SlotValues.cs:112`](https://github.com/kaaasi/eishockey-manager-redone/blob/c3ea6c2fc2e83124266afe9f278c01f913e0852f/src/EishockeyManager.Core/Squad.SlotValues.cs#L112)
setzt `PoolCondition`/`PoolTechnique` auf null, wenn der Poolwert dem Kaderplatz gleicht; `PoolStrengths` liefert dann
`Condition`/`Technique`. Das Training schreibt nur den Kaderplatz
([`Squad.Training.cs:336`](https://github.com/kaaasi/eishockey-manager-redone/blob/c3ea6c2fc2e83124266afe9f278c01f913e0852f/src/EishockeyManager.Core/Squad.Training.cs#L336)),
also wandert der Poolwert mit – und bleibt danach dauerhaft „gleich“ (null), bis zur nächsten Drift.

- **Original (Probe E, Rohbytes):** zwischen zwei Ständen derselben Saison ändert sich Kaderplatz +0x10/+0x11, Pool +0x15/+0x16 nie
  – auch nicht bei vorher gleichen Werten:
  FILE50→FILE63: Kaderwerte geändert 3, Poolwerte 0 (vorher gleich 8, davon Kader geändert 1, Pool gefolgt 0);
  FILE02→FILE03: 10 / 0 (6, 1, 0); FILE71→FILE74: 20 / 0 (18, 3, 0).
- **Remake (Probe B):** `TestGames.Create(4)`, 4 × `TestSeason.PlayMatchDay`: 12 Spieler trainiert, bei einem wandert der Poolwert
  mit – SPIELER 15 Kader 82/80 → 82/79, Pool (80, 80) → (80, **79**). Rund ein Fünftel der Werte ist am Saisonanfang gleich
  (FILE71: 18 von 100), der Fehler wächst über die Saison.
- **Folgen:** Export schreibt Pool +0x15/+0x16 falsch; Verkauf an einen Computerverein bzw. Join (`AdoptPoolValues`) übernimmt
  trainierte Werte; die Drift am Saisonwechsel startet vom falschen Wert.
- **Vorschlag:** Poolwerte für Spieler mit Kaderplatz explizit führen (nicht „null = gleich“), oder vor jedem Schreiben in den
  Kaderplatz eines Spielers mit `PoolCondition is null` den alten Wert festhalten. Regressionstest wie Probe B (Spieler mit
  Pool = Kader, ein Trainingsschritt → `PoolStrengths` unverändert). Dieselbe Darstellung hat `PoolForm` (A066, vor diesem PR);
  dort wirkt sie nur, wenn ein Schreiber die Kaderform ändert – im Fix mit ansehen, nicht Gegenstand dieses Befunds.

Probenquelltext (temporär, nicht committet; in `tests/EishockeyManager.Core.Tests/` kopieren):
Probe E liest je Paar `Block("5DC3")`/`Block("far:8253")` und vergleicht für Plätze mit gleichem Poolindex +0x10/+0x11 gegen
Pool +0x15/+0x16; Probe B misst `(Condition, Technique)` und `PoolStrengths` vor/nach vier Spieltagen.

### R923-02 (P3) – Mehrsaisonprobe ohne Aussage zur Drift der Poolwerte

Die neue Mehrsaisonprobe (`KaderplatzWerteTeil2Tests.MultiSeason_…`) prüft nur `PlayedLine`; die Poolwerte über vier
Saisonwechsel sind nur indirekt über die Fingerabdrücke (`RngTwinProbeTests`, `SpielberechnungAnschlussTests`) abgedeckt. Eigene
Probe F (Originaldatenbank, Saat 7, passiv/aktiv): Poolwerte aller Poolplätze bleiben in 1 … 99, Abstand Pool – Kader wächst auf
bis zu 53/60, Spielstand-Rundlauf und Export → Import nach vier Wechseln tragen `PoolStrengths`, Kader- und Reihenwerte
unverändert – kein Fehler, aber eine solche Aussage gehört als Test zur Spur. Mit R923-01 mitnehmen.

### Bestätigt (keine Befunde)

- **A093 Stand a093:** `OriginalStand_A093_ImportAndExportPlayedLine` grün (FILE71/72 0, FILE73 5, FILE74 3 Plätze). Leser em.exe
  Datei 0x09A65–0x09A8A per `ndisasm` nachgelesen: `cmp [0xED8],2 / jnl`, `[bp+0xC]` (im Spiel), `(+0x02) − 1 ≠ [bp+8]`,
  `si > 12 → − 10`, kein Torhütertest, danach erst die − 15-Prüfung – wie im Remake (auch bei +0x02 = 0, #899 P3-1). Wurf Datei
  0x548A8–0x548E7: je Kaderplatz bis Anzahl aus 0x120:0x66, `random(0,4)`, bei 0 `+0x02 = +0x15 & 0x7F` – wie `RollPlayedLines`.
- **Kauf FILE05 → FILE07 (Probe G):** FILE05 importiert, DAFFNER gekauft (`Bid` + `Sign`), exportiert, Kaderplatz gegen FILE07:
  gleich in +0x00–+0x09, +0x0D, +0x10–+0x13, +0x19 = 0, +0x06 = 2. Abweichend nur Tendenz +0x0E/+0x14 (Zufall), Gehalt/Gebot
  +0x2C–+0x2E (anderes Gebot), Reihe +0x0A/+0x15 (automatische Aufstellung im Original) und +0x1B (Export-`[APPROX]` 90, vor
  diesem PR).
- **Migration 54 → 55 (Probe C):** Remake-Stand gespeichert, Schema auf 54 gesetzt, `PoolCondition`/`PoolTechnique`/`PlayedLine`
  aus allen 2697 Feldern entfernt → lädt, alle Spieler 0/null/null, Kaderwerte unverändert, zweiter Rundlauf bytegleich. Die
  Fixtures Schema 19/22 laden weiter (volle Prüfung grün).
- **Rundlauf Export → Import (Probe D):** FILE05 importiert, Drift (`PoolQuotas`), Export → Import: 25 Managerspieler, alle 25 mit
  abweichendem Poolwert, Kader-, Pool- und Reihenwerte gleich; Markt 7/7 gleich. FILE23, FILE05, FILE73 importiert → exportiert:
  Pool +0x15/+0x16 aller 22/25/50 belegten Plätze bytegleich. Rückmutation „Import liest Pool +0x15/+0x16 nicht“ → 3 Tests rot
  (`OriginalExportRoundTripTests.MultiSeason_SecondRoundTrip_IsIdentical` ×2, `RemakeStand_SecondRoundTrip_IsIdentical`).
- **Fremde Testwerte (R21-D2 = A):** eigene Rückschaltprobe `src/` und Tests von `191f3ee`, nur die 12 geänderten Dateien ohne neue
  Typen vom Head: rot wie behauptet – `GameModeTests.Mode4…`, `JugendTeamsTests` ×2, `RngTwinProbeTests` ×6,
  `SpielberechnungAnschlussTests` ×2, `TeamwertAufstellungTests.MultiSeason` (passiv), `DebCupDrawFlowTests`,
  `PruefungKarteRunde16UiTests`, `PruefungKarteRunde17UiTests`; grün wie behauptet (Saat-Vorbedingungen) –
  `NationalCoachMode4Tests`, `SeasonEndStatusTests`, `PlayerFactoryTests.TalentSearch`, `KaderTransferTeil7Tests.Offer_OnlyLastYear`.
  Alle Begründungen nennen die Ursache (Wurf 0x548A8 bzw. Zufall 0x42CB1), keine Originalsollwerte überschrieben. Die Zwillinge in
  `TransferOriginalTests`/`KaderTransferTeil3Tests` ziehen genau die neuen Würfe (0x42CB1, Leihgehalt) nach; nachgelesen.
- **Rückmutation neuer Code:** Aufruf `RollPlayedLines` in `PrepareMatchGate` entfernt → 3 rot
  (`MatchDay_RollsPlayedLinesOfManagerSquad`, Mehrsaisonprobe ×2).

**Erwartet offen – bestätigt:** Satz 5 ohne +0x02 (`!squad.NationalCoach`, `[APPROX]`), Spielwertung ohne Wurf (`MatchGate.cs`,
`[APPROX]`), Startgehalt/Verhandlung am Markt-Platz (`Transfer.cs`, `[APPROX]`), Zurückziehen ohne Zufall von 0x42CB1 (PR
Restlücken), Schema 55 vorläufig (`SaveGame.cs`, Migrationsdatei).

## 3. Ausgeführte Prüfungen

`source ~/claude-tools/env.sh` (SDK 10.0.402), Originaldaten aus `kaaasi/eishockey-manager-referenz` (`11c3095`), `tools/pruefen.sh`:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3599 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2522 | 0 | 586 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2523 | 1076 | 0 |

Commit `c3ea6c2`, Ergebnis: bestanden. Proben B–G in einem temporären Worktree des Heads (`ReviewProbeTests.cs`,
`dotnet test tests/EishockeyManager.Core.Tests --filter ReviewProbe…`), Rückschaltprobe in einem zweiten Worktree; `ndisasm -b16
-e 0x9A40` bzw. `-e 0x54820` auf `gamedata/em.exe`.

**Akzeptierte `[APPROX]`:** Bundestrainer ohne +0x02; Spielwertung ohne Wurf; Verletzter ohne Platz spielt Reihe 0 ein;
Computervereine tragen Poolwerte; Platzhalter 150/7 ohne Leser; `SlottedPlayers` (Verkauf bis Saisonwechsel); Migration
null/0 für Altstände; Startgehalt/Verhandlung am Markt-Platz.
**Annahmen (nicht geprüft):** Reihenfolge Zuschauer → Wurf innerhalb der Managerrunde 0x544C0 (nur die Schleife 0x548A8 gelesen,
nicht die Zuschauerstelle 0x5464A); Kopierliste beim Kauf vom Manager 0x4787D–0x478CB nicht nachgelesen.
