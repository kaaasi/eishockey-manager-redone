# Review PR #932 – Spur `kader-regeln` (Erstreview)

Geprüft: Base `c58e738` (= `origin/main`), Head `0e0b02aad5a0ab9c8e9affc629477f79e7238f1c` (sauberer Checkout, `git rev-parse HEAD` =
Head, `git status --porcelain` leer). Reviewer: Claude Opus 5.5, geplanter Lauf, eigene Sitzung (nicht an #932 beteiligt).
Originaldateien aus `kaaasi/eishockey-manager-referenz` (`original/`), SDK 10.0.402 aus dem Release `claude-werkzeuge`.

## 1. Merge-Empfehlung

**Ja für `0e0b02a`.** Alle drei Läufe von `tools/pruefen.sh` grün, CI-Build auf dem Head grün. Alle genannten Gegenbeispiele
erneut ausgeführt und auf dem Head behoben (eigene Proben, Abschnitt 3). Jede neue Testgruppe wird unter gezielter Rückschaltung
im Head rot (7 von 8 Mutationen; die achte ist unter der neuen Invariante gleichwertig). Die vier angepassten fremden Tests mit
geänderter Erwartung sind nötig (Base-Fassung auf dem Head rot) und mit neuem Inhalt auf der Base rot bzw. – wie im PR angegeben –
`A010` auf beiden grün (R15-E2 = C / R21-D2 = A erfüllt). Migration 56 → 57 und Rundlauf geprüft. Keine Befunde über P3.

## 2. Befunde

**P3-1 (Testlücke, kein Fehler) – Highscore-Schlüssel ohne trennenden Test.** Mutation in
[Highscore.cs:260](https://github.com/kaaasi/eishockey-manager-redone/blob/0e0b02aad5a0ab9c8e9affc629477f79e7238f1c/src/EishockeyManager.Core/Highscore.cs#L260)
`setup.Level` → `setup.Managers[0].Level`: `KaderRegelnTeil1Tests` bleibt grün (7/7). Weil `GameSetup.Level` den Wert von Manager 0
liest und Setzen für alle gilt, ist das gleichwertig; die Umstellung ist Aufräumen, kein Verhalten. Kein Handlungsbedarf.

**P3-2 (Hinweis) – Mehrsaisonprobe der Leihe prüft die Rückkehr nicht.**
`KaderRegelnTeil2Tests.MultiSeason_FourChanges_LoanSalaries` prüft Spanne des Leihgehalts, gemeinsamen Level und Rundlauf über 4
Wechsel (aktiv ≥ 3 Leihen, passiv 0), aber nicht den Spieler nach Ablauf der Leihe. Eigene Probe P2 (unten): Nach dem Saisonwechsel
ist der Leihspieler aus dem Managerkader, `OnLoan = false`, das Gehalt wieder das gesicherte aus `PlayerLoan.MonthlySalary` (hier 0,
Poolspieler). Verhalten korrekt; ein Assert in der Mehrsaisonprobe wäre billig.

**Beobachtung (kein Befund):** A631 – der Nachfolger trägt den Namen des Ruheständlers; in `RetiredPlayers` und im Pool steht damit
derselbe Name zweimal (verschiedene IDs). Das ist originalgetreu (Pooleintrag bleibt) und betrifft nur Ruheständler; Suchen nach
Namen in aktiven Kadern sind nicht berührt.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh` (alle drei Läufe, Erstreview) auf dem Head:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3666 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2564 | 0 | 601 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2565 | 1101 | 0 |

**Gegenbeispiele** (eigene Probedatei, nicht committet; `dotnet test tests/EishockeyManager.Core.Tests --filter
FullyQualifiedName~ReviewProbeKaderRegeln`, 7/7 grün):

- **P1 A054/A518/A462/A060:** `GameSetup` mit zwei Managern über `AddManager`, `Step(Level, +1, B)` und `Step(StartStrength, −1, B)`
  → beide Manager 4/OberesDrittel; ohne Manager `Step(Level)` = true, neuer Manager übernimmt 4; `NewGame.Create` → alle
  `Manager.Level` = 4. Highscore-Schlüssel folgt dem Spielwert (`HighscoreKey_FollowsGameWideLevel`, Mutation s. P3-1).
- **P2 A118/A120:** Saat 8, Leihe vom Computermarkt: `Demand` = 2.600, Wert(Gehalt) = 46.200 Drittel, Leihgehalt 13.860 DM
  (= 46.200 · 90 / 100 / 3, in Spanne 13.860–17.710); Speichern/Laden behält Leihgehalt und gesichertes Gehalt; Rückkehr s. P3-2.
  Aufrufer von `Contracts.Demand`: nur `PlayerFactory.Join` (Computervereine bzw. von Aufrufern überschrieben), `BaseSalary`
  (Startkader, danach `ApplyStartSalaries`) und `Transfer.Squads.cs:248` (Computerkader) – passt zur neuen Marke A120.
- **P3 A683/A490:** Saat 5, Managerverein, Abwehr Reihe 3 geleert, Sturmhälfte voll: Stürmer-Klick → `Assigned` auf
  `Defender(2, 0)`; nach Speichern/Laden steht er dort, `LiveSquad.PositionOf` = Abwehrposition, ein Spieltag läuft ohne Fehler,
  Platz bleibt. Vorprüfungen (22 Mann, verletzt, gesperrt, Automatik aus) liegen in beiden `ClickPlayer` vor der Platzsuche.
- **P4 A827:** Saat 11, `JoinScoutedTalent(manager, 1)`: `PurchasePrice` 88.000 = `Contracts.ValuePrice`. Schleife ohne Grenze
  ([FinanceTalent.cs:223](https://github.com/kaaasi/eishockey-manager-redone/blob/0e0b02aad5a0ab9c8e9affc629477f79e7238f1c/src/EishockeyManager.Core/FinanceTalent.cs#L223))
  endet sicher: nur bei mindestens einem freien Platz in 1–179.
- **P5 A394** (Originaldaten): EC HARZ BRAUNLAGE (leer 7, 9–11), EHC STRAUBING (9, 11), 1.EV WEIDEN (7, 9–11), KÖLNER EC (11):
  jeder belegte Datenbankplatz steht auf seinem Kaderplatz, leere Plätze tragen einen gezogenen Namen an ihrer Stelle.
- **P6 A631:** Saat 29, Pooleintrag Alter 60: Nachfolger mit Name, Talent 2 und Form = `PoolForm` 51 des Eintrags, neue ID,
  genau 5 Würfe im gemeinsamen Strom (37–42, Alter, s, zwei Grundstärken).
- **P7 Migration:** Zwei Manager (Level 2/4, Stärke Abstiegskampf/Mittelfeld), Stand auf Schema 56 gesetzt, `_level` und
  `_startStrength` entfernt → lädt, `Setup` = 2/Abstiegskampf, alle `Manager.Level` = 2, Startgeld unverändert. Rundlauf
  zusätzlich in `Migration_56To57_UnifiesOnFirstManager` und in beiden Mehrsaisonproben je Wechsel (bytegleich).

**Rückschaltproben** (Mutation im Head, betroffene Testklasse, danach `git checkout`):

| Mutation | Ergebnis |
|---|---|
| M1 `Transfer.cs` Leihgehalt nicht zuweisen | Teil2 rot 5/8 |
| M2 `Lineup.SearchSlots` nur eigene Hälfte | Teil3 rot 5/6 |
| M3 `FinanceTalent.cs` ohne `PurchasePrice` | Teil4 rot 3/10 |
| M4 `Squad.Setup.cs` Platz = Anfang der Art + j | Teil4 rot 3/10 (Originaldaten) |
| M5 `CreateRenewal` Form statt `PoolForm` | Teil4 rot 2/10 |
| M6 `GameSetup.AddManager` ohne `m.Level = Level` | Teil1 rot 2/7 |
| M7 Migration ohne Angleich der Setup-Manager | Teil1 rot 1/7 |
| M8 Highscore `Managers[0].Level` | grün (gleichwertig, P3-1) |

**Fremde Tests (R21-D2 = A):** Base-`src` + Head-Fassung von `KaderStartRunde13Teil2Tests`, `ZufallsstromTeil1Tests`,
`GameModeTests`, `NationalteamRunde13Tests` → 3 rot (`LevelAndStrength…`, `PoolSeason_CareerEndOfFreeSlot…`,
`Mode4_ReferenceValues…`), `A010` grün (wie im PR). Head-`src` + Base-Fassung → alle 4 rot, Anpassungen also nötig. `Mode4`: nur der
Zufallszustand am Ende geändert, Titel und Konten aller Saisons gleich (Diff). `KaderTransferNachtraegeTests`,
`ZufallsstromTeil2Tests`, `DataFormatTests` nur neue Signatur/neues Feld; `SaveGameTests` Schema 57 und Abdruck.

**Erwartet offen – bestätigt:** sechster Feldspielerplatz (A683/A490 Rest, Datei 0x413E8–0x414B4) bleibt `[APPROX]` in
`Squad.Lineup.cs` und `CupsNational.Lineup.cs`, Entscheidung Nutzer offen; Datenbankplätze im Pool (`CaptureDatabaseNames`,
`transfer-pool`) zählen weiter ab Anfang der Art; Schemanummer 57 vorläufig (Base hat 56, Leitstelle nummeriert beim Merge).

**Akzeptierte `[APPROX]`:** `LoanSalary` ganze DM (Drittel abgerundet, mindestens `MinSalary`); `Contracts.Demand` nur
Computervereine (ohne Gegenstück); Migration 56 → 57 auf Manager 0; Talent ohne freien Poolplatz/ohne Pool → neuer Spieler;
`CreateRenewal` neue ID, Energie/Tendenz; Spieler ohne gemerkten Datenbankplatz (Import, Aufbauten) wie bisher.

**Annahmen, nicht geprüft:** Inhalt der em.exe-Fundstellen (0x2890A, 0x477E7/0x42F1F, 0x4783A/0x47929, 0x414D3, 0x06F5E,
0x5B0CC, 0x27947) nicht mit `ndisasm` nachgelesen; Belegbild kb\23 nicht angesehen (Datenbank-Leerplätze per Probe P5 bestätigt).
