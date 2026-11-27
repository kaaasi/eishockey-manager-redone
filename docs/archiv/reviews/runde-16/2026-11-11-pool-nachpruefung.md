# Nachprüfung Review PR #765 – Spur pool (Delta `2738e8b..30d000d`)

Geprüft: Head `30d000d66682d5ed3b61aa28361b9575476fafe6` (`git rev-parse` im Worktree, `git status --porcelain` leer), Base
`12d8fdef3417d5de5d2288f816c5b02bfdfff2c5` per Merge eingebunden (Eltern `2738e8b`, `12d8fde`). Reviewer: Claude Code (Opus 5.5),
geplanter Lauf, eigene Sitzung ohne Implementierungskontext. Mutationen und Proben nur in temporären Worktrees, danach verworfen.
Vorreview: `docs/reviews/2026-11-11-pool.md` (Branch `review/pool`).

## 1. Merge-Empfehlung

**Ja** für genau diesen Head. `tools/pruefen.sh --schnell` grün (2952/0/0). R765-01 ist behoben (Kaderklausel jetzt durch einen
Test belegt, Listenklausel gestrichen), R727N2-01 wird weiter rot, die Rundreise FILE20/FILE21 im Modus 1 vergleicht Satz 0 jetzt
ohne Ausnahmeregel und ist grün. Die Rückschaltprobe zeigt, dass alle erneuerten Abdrücke und umgestellten Saaten allein an der
Platzvormerkung hängen; ein Schema-47-Stand lädt über beide Migrationen. Ein P3 (neues `[APPROX]` ohne Test) blockiert nicht.
`main` ist seit der Base weiter (`d076e7b`, #767): Merge textuell konfliktfrei, betroffene Tests im Merge-Ergebnis grün (s. 3).

## 2. Befunde

**R765N-01 (P3) – `AddUnseatedSquadPlayers` (neues `[APPROX]`) hat keinen wirksamen Test.**
[OriginalExport.Squads.cs:123–124](https://github.com/kaaasi/eishockey-manager-redone/blob/30d000d66682d5ed3b61aa28361b9575476fafe6/src/EishockeyManager.Core/OriginalExport.Squads.cs#L123-L124),
[Z. 135 ff.](https://github.com/kaaasi/eishockey-manager-redone/blob/30d000d66682d5ed3b61aa28361b9575476fafe6/src/EishockeyManager.Core/OriginalExport.Squads.cs#L135):
Mutation `if (missing.Count > 0)` → `if (false)`: gesamte `EishockeyManager.Core.Tests` 2099/2099 grün. Der Pfad greift nur beim
Export eines **neuen** Spiels im Modus „nur Weltkugel“ (Startkader nicht im Pool, Importe haben die Plätze aus dem Stand). Probe
(Datenbank `kb\23`, 1 Manager Verein 2, Modus 1, Saat 5, Export → `SquadSlot(…, 0, k)[0x0F]`):
- Head: Satz 0 → 2–3, 22–31, 98–110, alle 25 verschieden, Besitzer 0, Namen gleich (bis auf die Umlautkodierung), über zwei
  Saisonwechsel unverändert; Re-Import `ValidatePlayerIdentity` leer. Das entspricht den Plätzen von FILE01 (Vorreview).
- Mutation: alle 25 Kaderplätze mit Poolindex 0, Besitzer unbelegt.

Verhalten also richtig, nur ungesichert. Vorschlag: diese Probe als Test (Modus 1, neues Spiel, Export) aufnehmen.

**Hinweise (kein Befund):**
- Import `taken = true` ([OriginalImport.Market.cs:102](https://github.com/kaaasi/eishockey-manager-redone/blob/30d000d66682d5ed3b61aa28361b9575476fafe6/src/EishockeyManager.Core/OriginalImport.Market.cs#L102)):
  Rückmutation auf `player is not null || !WorldOnly` → Core 2100/2100 grün. Seit #732 übernimmt der Import den Kader von Satz 0
  auch im Modus 1, `player` ist für Besitzer 0–3 in allen vorliegenden Ständen gesetzt; beide Fassungen sind dort gleichwertig.
  Die neue Fassung ist die folgerichtige (vergeben unabhängig vom Modus), ein unterscheidender Stand fehlt.
- `SeasonChange_KeepsMarketList_PlayersAgeOneYear` prüft jetzt `StartNextSeasonWithRenewals` (ohne Tag 0). Der Tag-0-Wurf
  (Datei 0x54E65/0x54E7C) ist ein eigener, im Kommentar genannter Aufrufer des Listenwechsels, R688-05 betrifft nur den
  Saisonwechsel selbst – akzeptiert.

**Gegenbeispiele erneut ausgeführt (Head `30d000d`):**
- **R765-01 (#775):** Kaderklausel `managed.Any(c => c.Players.Contains(slot.Player)) ||` in `StartPoolSeason`
  ([Transfer.Pool.cs:701](https://github.com/kaaasi/eishockey-manager-redone/blob/30d000d66682d5ed3b61aa28361b9575476fafe6/src/EishockeyManager.Core/Transfer.Pool.cs#L701))
  entfernt → `PoolManagerSquadTests` 3 rot: `LoanBetweenManagers_OverSeasonChange_KeepsPoolSlot`,
  `FourSeasonChanges_ManagerSquadsStayInPool` (aktiv/passiv). Listenklausel gestrichen (tot, s. Vorreview).
- **R727N2-01:** `if (!marketTwins.Contains(index))` → `if (true)` → 7 rot (Rundreise FILE41/43/43A, `MarketTwin_…`
  FILE41/43/43A/44), unverändert gegenüber dem Vorreview.
- **Rundreise FILE20/FILE21 (Modus 1, Satz 0 aus #732):** `OriginalStand_ExportMatchesOriginal_ExceptAllowedFields` FILE20 und
  FILE21 grün, ebenso `File20To21_WorldOnly_ManagerMoneyAndSquadLikeOriginal` (3 Saaten). Die entfallenen Regeln („Kader des
  Managervereins“ 0x00–0x33, „Poolsätze des Managervereins“, „zweiter Poolsatz“, Besitzer 0 frei) decken nichts mehr ab; übrig
  sind nur Regeln mit `Records: NationalSlots` (Satz 5). Export-Mutation „gemerkter Manager“ (`?? 6` statt `slot.Manager`) →
  Rundreise FILE46 rot.

**Besonders geprüft:**
1. **Modus 1 + Pool:** s. R765N-01 und Hinweis 1. `PoolIndices` nimmt Kaderspieler zuerst über ihren Platz, dann den Marktzwilling,
   erst danach `AddUnseatedSquadPlayers`; dort wird jeder belegte Index dem Vorbesitzer entzogen (`map.Remove`), Eindeutigkeit in
   der Probe bestätigt.
2. **Migration 48 → 49:** `[48] = MigratePool`, `PoolSeatedSchema = 49`, `AfterLoadPool(game, loadedSchema)` mit dem
   Ausgangsschema. Probe: mit `d6fa442` (Schema 47) erzeugter Stand (2 Manager, Vereine 5/7, Saat 4, 10 Spieltage) unter Head
   geladen: kein Fehler, 50/50 Kaderspieler auf genau einem `Taken`-Platz, `ValidatePlayerIdentity` leer, Sichern → Schema 49,
   Sichern/Laden byte-gleich; erster Saisonwechsel ohne Platzverlust. Nach dem zweiten Wechsel sind die Kader leer – ebenso im
   frischen Spiel auf `d6fa442` (25/25 Verträge laufen ab, passiv, `StartNextSeason` ohne Verlängerung): kein Migrationsfehler.
3. **Rückschaltprobe (R15-E2 = C):** Head mit abgeschalteter Platzvormerkung (`BuildPool` ohne `ManagerSquadIndices`, ohne
   `SeatStartSquads` in `Initialize`, ohne `SeatManagerSquads` beim Pool-Neubau und in `AfterLoadPool`) und den Fassungen von
   `12d8fde` aller 13 geänderten fremden Testdateien (GameMode, JugendTeams, KaderTransferTeil4/Teil5Pool, PlayerIdentity,
   RngTwinProbe, SpielberechnungAnschluss, TagestaktHauptwegReview, TransferVertraegeKomplettTeil5, WettbewerbeSpiel2, WorldCup;
   UI DebCupDrawFlow, LifecycleScreenRegression): Core 131/131, UI 8/8 grün – auch mit den alten Saaten 3/21/5 und
   `StartNextSeason`. Gegenrichtung: Head unverändert mit den alten Fassungen von WorldCup/KaderTransferTeil4/TagestaktHauptwegReview
   → 3 rot. Begründung WorldCup geprüft: Saat 3 ernennt den Manager zum Bundestrainer (Vorbedingung „kein Manager spielt die WM“
   verletzt), Saat 4 nicht. Keine verdeckte Verhaltensänderung außer der Platzvormerkung.

**Erwartet offen – bestätigt:** #776 (R765-02, Leihe Manager → Manager, Ursache in `Player.Contract.cs`) nicht Teil des Deltas;
Leihklausel in `StartPoolSeason` als Schutz kommentiert (Z. 700), Mutation „Leihklausel entfernt“ weiter grün (177/177, Filter
Pool|Leih|Loan|Saisonwechsel) – wie benannt.

## 3. Ausgeführte Prüfungen

`tools/pruefen.sh --schnell` auf Head `30d000d`, SDK 10.0.402:

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2952 | 0 | 0 |

Ergebnis: bestanden. Mutationen/Proben: `dotnet test tests/EishockeyManager.Core.Tests -c Release --filter …` mit
`EM_REQUIRE_GAMEDATA=1` in Worktrees; Schema-47-Stand mit Worktree `d6fa442`. Merge-Ergebnis `30d000d` + `d076e7b` (lokal,
konfliktfrei): `Tagestakt|StockPriceZero|AbgleichOriginalPaare|AbgleichFile13` 105/105 grün (volle Prüfung des Merge-Ergebnisses
bleibt bei der Leitstelle).

**Akzeptierte `[APPROX]`:** `AddUnseatedSquadPlayers` (Modus 1, neues Spiel: Platz des Namens beim eigenen Verein, sonst erster
freier Ligaplatz der Art; Ergebnis in der Probe = Plätze von FILE01); übrige wie im Vorreview. **Annahmen:** keine Stände mit
Besitzer 0–3 ohne Spieler im Modus 1 vorhanden (Hinweis 1).
