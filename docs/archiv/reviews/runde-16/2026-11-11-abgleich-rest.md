# Review PR #766 – Spur abgleich-rest (Belastung, Modus „nur Weltkugel“, #772, #773)

Erstreview, Claude Opus 5.5, frische Sitzung ohne Implementierungskontext (geplanter Lauf, 11.11.2026).
Base `d6fa442c6552213e9e810c9505117eb6ab457680` · Head `8f3554a12a55dae84979d53a3269df02b3e0c20f` (Checkout nachgewiesen:
`git rev-parse HEAD` = Head, `git status --porcelain` leer).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. Die drei Fixes (#732, #699 Rest, #772) sind gegen em.exe nachgelesen, die Gegenbeispiele
reproduzieren mit dem Head, jede neue Prüfung wird bei Rückmutation rot, alle drei Läufe von `tools/pruefen.sh` sind grün,
CI auf dem Head grün. Offen bleiben nur P3. Vor dem Merge beachten: **Schema 48 kollidiert mit `pool` #765** (dort ebenfalls
`SchemaVersion = 48`, `[47] = MigratePool`); `git merge-tree pr766 pr765` meldet 13 Konfliktdateien (u. a. `SaveGame.cs`,
`OriginalExport.Squads.cs`, `OriginalImport.Market.cs`, `OriginalExportRoundTripTests.cs`, Abdrucktests). Der Zweitgemergte
nummeriert auf 49 um und erneuert Abdrücke (nur benannt, kein Befund an diesem PR).

## 2. Befunde

**P0/P1/P2:** keine.

**P3-1 (#773 „Zufall“ für FILE02→03 nicht gedeckt).** Bericht §8 und Kommentar in `AbgleichRestPenaltyTests` erklären die
Liga-Strafminuten FILE02→03 (Original +1447) als Zufall. Probe (temporärer Test im Klon, `HokCompare.PlayTo` FILE02→FILE03,
Saaten 1–20): Remake 1218 … 1377, Mittel 1301,9 – **das Original liegt über allen 20 Saaten** (−10 %). FILE05→24 und
FILE16A→22 passen dagegen (Wächtertest grün). Am ehesten ein Effekt der ersten Spieltage eines neuen Spiels (z. B. Einsatz 0x26
= 0 vor dem ersten Spiel in der Stärke 0x9C66, Reihen 0x2E/0x30 aus dem Vorspiel), nicht belegt. Vorschlag: #773 mit diesem
Beleg wieder öffnen oder neues P3-Issue; kein Merge-Hindernis (Wirkung nur auf Strafminuten der ersten Spieltage).
[Head: `tests/EishockeyManager.Core.Tests/AbgleichRestPenaltyTests.cs` Z. 15](https://github.com/kaaasi/eishockey-manager-redone/blob/8f3554a12a55dae84979d53a3269df02b3e0c20f/tests/EishockeyManager.Core.Tests/AbgleichRestPenaltyTests.cs#L15)

**P3-2 (Pflicht → Sägezahn ohne Fix).** In `AbgleichOriginalPaareTests` wandern J.MEYER (FILE13), A.RAUBAL und PYLE (FILE16A)
von Pflicht #730 in den optionalen Sägezahn, weil sie mit der neuen Zufallsfolge (#772) nicht mehr bei allen Prüfsaaten
abweichen. Das ist kein Fix (#730 offen), aber erzwungen und im Code begründet; #730 bleibt durch KUHNEKATH und ZIMMERMANN als
Pflicht verankert. Nur festhalten: Die Entfernungen sind **keine** echten Fixes. Alle übrigen Entfernungen (FILE20→21
`DayOpened`, `Managers[0].Money`, `MonthlySalary`, `PurchasePrice`) sind durch Fixes gedeckt (Rückmutationen M3, M4, M7 unten).
Die Umdeutung Form/Belastung FILE20→21 von #732 zu „ein Wertesatz“ ist belegt: Probe auf FILE20 (Kaderplätze `5DC3`) –
Schnittmenge Satz 0 ∩ Satz 5 = K]NAST, BUKOWSKI, PODLAHA, DAFFNER, BLEICHER, PURVES; genau diese stehen im Remake auf
Belastung 70 (Wert des Satzes 5), alle übrigen ±1 am Original.
[Head: `AbgleichOriginalPaareTests.cs` Z. 110–123](https://github.com/kaaasi/eishockey-manager-redone/blob/8f3554a12a55dae84979d53a3269df02b3e0c20f/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L110-L123)

**P3-3 (Verdacht, ohne Beleg).** `SaisonwechselSchema22Tests`: „beide Manager verlieren in Saison 4 ihren Kader bis auf 0/1“
wird mit `extendExpired: true` überdeckt statt erklärt. Nicht nachgestellt; nur Hinweis für `pruefung-karte`.

**Erwartet offen, bestätigt:** #730 zurückgestellt (R16-E2 = C): `[APPROX]` an `OriginalMinutes.ChangeLines`
(`MatchEngine.Managed.cs` Z. 504–508), Pflichteinträge KUHNEKATH/ZIMMERMANN bleiben, DOSBox-Liste
`docs/analyse/belastung-staerke.md` §7.3 vorhanden.

## 3. Ausgeführte Prüfungen

**em.exe (`ndisasm -b16`, Dateioffsets):**
- 0x8365: Abbruch bei Verein > 0x95; zweimal 0x368B1 (Segmenteintrag 0x149D) mit (Verein, 1, 0, 0, 1), `shl ax,1`; eigene ≥
  gegnerische → 0x2C Bit 7; Long-Vergleich der Differenz < 25 → 60, sonst 40 (0x8400–0x841C); ohne Manager (0x16 = 0) oder
  DGROUP 0xA6FA + Manager = 0 (Block `A6FB`: „0 = AUTOM.“) → 0x27–0x2B aus 0x4912 und 0x26 = 0x2D = Grundwert, **kein
  Zuschlag**; Angriff nur Zeile 0x35–0x3A aus 0x491C, keine Bits 4–6 (0x8481–0x84BD); immer 0x32 = 5, 0x33 = 0. Deckt sich mit
  `MatchEngine.PrepareMatch`/`Tactics.PreparedEffort`.
- Aufrufer `call 0x28:0x2a5` (Segmenttabelle Eintrag 5 → 0x62C → Datei 0x8365): 0x36D12/0x36D49 (Liga, je Paarung beide
  Richtungen) und 0x36F27/0x36F5A (Pokaltabelle 0x5CFB) – also auch Pokalspiele, wie das Remake (Aufruf vor dem Schnellweg in
  `PlayWithEvents`). Nationalteams: `RatedNation` ohne Spieler → `IsNation` → übersprungen, wie bisher.
- WM-Schleife 0x5568B–0x5569F: Managerschleife bis [0x386] mit `call 0x100:0x4d`/`0x100:0x57` ohne Modusprüfung, 0x100:0x25 bei
  Zähler ≡ 0 (mod 7); Sommerpause 0x56504–0x56512 ebenso. Deckt `AdvanceFinance(day, clubDay: false)` und `SummerDayClose` ohne
  `WorldOnly`.

**Gegenbeispiele:**
- #732: `emtool abgleich FILE20 FILE21 --seed 1|2|3|77 --alle`: `Managers[0].Money` Original −1.530.327, Remake −1.532.012 bei
  allen vier Saaten; Gehalt/Preis ohne Abweichung; verbleibende Abweichungen nur Belastung BLEICHER/DAFFNER/PURVES (s. P3-2) und
  `Invariante.PoolMittel.Strain` (Pool #731, außerhalb).
- #699 Rest: dieselben Läufe `DayOpened True/True`, `FinanceDay −1/−1`, kein Eintrag mehr.
- #772: Rückmutation ohne `PrepareMatch` → `AbgleichRestPreparationTests` 6 von 7 rot (wie im PR).
- #773: Rückmutation Einsatz Grundwert + 20/15 → `AbgleichRestPenaltyTests` rot (Mittel 497,5, Bereich 405,7–476,3).

**Rückmutationen (temporärer Klon, `dotnet test --filter`, mit Originaldaten):**

| Mutation | rot |
|---|---|
| M3 `if (!WorldOnly) SummerDayClose` + ohne `AdvanceFinance` in der WM-Schleife | 15 von 18: alle `AbgleichRestWorldOnlyTests` zu Geld/Kader, `GameModeTests.WorldOnly_MultiSeason…` (3), `NationalLineupTests.WorldOnly…` (3), `SaisonwechselProbenTests.WorldOnly…` (2), `SpielberechnungAnschlussTests.FourSeasonChanges_WorldOnly` |
| M4 ohne `OpenDay(CurrentDay)` im Sprung (`SeasonFlow.cs` Z. 150) | beide `…JumpToWorldCup_OpensDay…`, `AbgleichOriginalPaareTests` FILE20→21 (6) |
| M5 ohne Finanztag-Sprung | `WorldOnly_FirstSeason_ClosesSummerMonthsOnly` |
| M6 ohne `[47] = MigrateAbgleichRest` | `AbgleichRestMigrationTests.Schema47_…` |
| M7 Import `if (!WorldOnly) ApplySquad` | `File20To21_…` (3), `OriginalExportRoundTripTests` (9 Stände) |
| M8 Export ohne Kader im Modus 1 | `OriginalExportRoundTripTests` (9 Stände) |
| M10 Angriff nicht festgehalten | `ComputerMatch_KeepsFieldsAcrossSaveAndLoad` |
| M1 ohne `PrepareMatch`, fremde Tests | Abdrücke `RngTwinProbeTests` (6), `SpielberechnungAnschluss…CountersPerSeason` (2), `GameModeTests.Mode4…`, `JugendTeamsTests` (2), `AbgleichFixMoneyTests.File16A` rot; `AbgleichFix…File13` und `AbgleichFixStrainTests` grün – wie im PR angegeben |

**`tools/pruefen.sh` (alle drei Läufe), Commit `8f3554a`, SDK 10.0.402:**

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2937 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2160 | 0 | 495 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2161 | 776 | 0 |

CI auf dem Head: `build` success.

**Akzeptierte `[APPROX]`:** Werte des Satzes 5 zugleich für Spieler aus Satz 0 im Modus 1 (ein Wertesatz je Spieler; Wirkung
auf sechs Spieler, Form/Belastung optional); Reihe 0 statt Vorspielreihe in der Vorbereitungsstärke; Managerverein aus den
Spielern (Liste 0x233 nicht belegt); SEMI/MANUELL behalten die eigenen Werte (Original lässt 0x26 stehen); Migration 47 → 48
mit 0; Modus 1 ohne Verträge/Training/Aufstellung für Satz 0 in der WM-Schleife; #730 an `ChangeLines`.

**Annahmen (nicht geprüft):** Rolle der Funktion 0x14CF:0x61 als Betrag der Differenz (nur aus dem Long-Vergleich `< 25`
geschlossen, unverändert gegenüber `main`); Pixel/Anzeige nicht betroffen (kein UI-Code geändert außer Abdrucktest).

---
Co-Authored-By: Claude Opus 5.5 · Sitzung https://claude.ai/code/session_01GhACvTT1e8mdNhGmXqSpjX
