# Review PR #752 – Spur abgleich-fix (Erstreview)

Head `2e5372c10104d778c2387b286dc05cdfa022ea4e` (geprüft: `git rev-parse HEAD` = Head, `git status --porcelain` leer), Base
`263753d` (= aktueller `main`, Merge-Basis bestätigt). Reviewer: Claude Opus 5.5, geplanter Lauf, frische Sitzung ohne
Implementierungskontext (Sitzung nicht unter den im PR genannten).

## 1. Merge-Empfehlung

**Nein** für diesen Head – wegen eines P2 zu Prüfpunkt (1). Die Fixes selbst sind belegt und richtig: alle Fundstellen in em.exe
stimmen (Teil 3), alle Gegenbeispiele sind weg, jede Rückmutation macht ihren Regressionstest rot, alle drei Läufe grün. Aber für
#730 ersetzt der PR die bisherigen Pflichteinträge je Spieler durch das optionale Muster `Players[*].Strain` („Zufall“), obwohl
mehrere Spieler weiter **systematisch** abweichen (B1). Damit ist „Fixes #730“ zu weit und der Abgleich verliert genau das Netz,
das ihn rot halten soll. Nachbesserung klein (B1), danach Nachprüfung nur dieses Punkts.

## 2. Befunde

**B1 (P2) – #730 nur teilweise behoben; `StrainSawtooth` weicht systematische Abweichungen als Zufall auf.**
[`AbgleichOriginalPaareTests.cs:68`](https://github.com/kaaasi/eishockey-manager-redone/blob/2e5372c10104d778c2387b286dc05cdfa022ea4e/tests/EishockeyManager.Core.Tests/AbgleichOriginalPaareTests.cs#L68),
Verwendung Z. 79 (FILE02→03), Z. 96 ff. (FILE16A→17), außerdem 13→14, 24→25, 38→39, 39→40. Das Muster deckt jede Belastung jedes
Spielers des Managervereins optional ab; der Ersatz `Pairs_ManagerClubStrainMean_LikeOriginal` prüft nur das Vereinsmittel ±10.
Beleg (`emtool abgleich <a> <b> --gamedata gamedata --seed n`, Release-Build des Heads):

| Paar | Spieler (Original) | weicht ab bei Saaten |
|---|---|---|
| FILE02→03 | HOCK (50) | **15 von 16** (1, 2, 3, 77, 1993, 4711, 4–12; nur 13 nicht), Remake 119–127 |
| FILE02→03 | SIKORA (50) | 11 von 16 |
| FILE16A→17 | A.RAUBAL, TRAVNICEK, ZIMMERMANN (50–57) | 4 von 4 (1, 2, 3, 77), Remake 97–127 |
| FILE16A→17 | BLAZYCEK, DOLL, HORSTMANN, M.SCHMIDT, NOACK (50–72) | 3 von 4 |
| FILE16A→17 | PYLE (120, Gegenrichtung) | 4 von 4 |

Bei 15/16 Saaten ist das kein „Sägezahn mit zufälliger Phase“: Im Original kippen die Vielspieler der Reihen 1/2 vor dem Training
über 127, im Remake fast nie (BRANDL, Reihe 3, kippt dagegen im Remake bei 4/16 Saaten, im Original nicht) – Verdacht auf eine
zweite Ursache beim Belastungszuwachs je Reihe/Einsatz, nicht Teil des Belegs. Vorher waren genau diese Spieler Pflichteinträge
(`StrainOf`, „über alle sechs Saaten“). Rückmutation: Mit `StrainSawtooth` bliebe auch ein Remake grün, in dem HOCK/SIKORA nie
auf 50 fallen. **Erwartet:** systematisch abweichende Spieler (über alle Prüfsaaten) wieder als Pflichteinträge unter #730 (oder
einem Folge-Issue), das Muster nur für die übrigen; #730 bleibt offen bzw. „Refs“ statt „Fixes“, bis die Restursache geklärt ist.
Der eigentliche Fix (`cbw`, 0x4BD56) ist richtig und bleibt.

**B2 (P3, vorbestehend) – Kurs 0 führt zu Division durch null.** Der PR lässt in
[`BoerseStartTests.cs:248`](https://github.com/kaaasi/eishockey-manager-redone/blob/2e5372c10104d778c2387b286dc05cdfa022ea4e/tests/EishockeyManager.Core.Tests/BoerseStartTests.cs#L248) Kurs 0 zu (tritt seit dem anderen
Zufallsverbrauch bei Saat 8 auf). [`FinanceStocks.cs:190`](https://github.com/kaaasi/eishockey-manager-redone/blob/2e5372c10104d778c2387b286dc05cdfa022ea4e/src/EishockeyManager.Core/FinanceStocks.cs#L190) (`BuyStock`) und
`FinanceScreen.cs:524` teilen durch `Price`. Probe (temporärer Test): `NewGame.Create(TestGames.CreateDatabase(), …, 8)`,
`Stocks[0].Price = 0`, `BuyStock(m, 0, 10)` → `System.DivideByZeroException`. Nicht durch #752 verursacht, aber durch die
Testanpassung jetzt erreichbar und unbewacht; eigenes Issue (Verhalten des Originals bei Kurs 0 klären, 0x596C2).

Sonst keine Befunde. Geprüft und in Ordnung:

- **Prüfpunkt (1), übrige Entfernungen:** #747 (FILE18→48 0 Abweichungen, 4 Saaten), #749 und `PoolMittel.Condition/Technique`
  #731 (FILE44→46 nur noch `PoolMittel.Strain`, kein Originalfeld), #733 `_autoLineup` FILE24→25 (0 bzw. nur Belastung),
  #738 Form KÖLN, #732 Alter/Laufzeit FILE20→21 (keine `Age`/`ContractYears`-Zeile bei 1, 2, 3, 77) – jeweils durch echten Fix
  gedeckt. Umwidmungen: FILE13→14 `_autoLineup` als Eingriff (Schreiber von 0x375 nur Oberfläche/Tageslauf, nachvollzogen an
  0x54D84/0x54FC6 ff.), FILE20→21 Gehalt/Preis als Importfolge (`emtool hok dump` FILE20/21: Satz-0-Preise unverändert, z. B.
  ENGLBRECHT 444000, ABSTREITER 277000) – plausibel begründet.
- **Erwartet offen bestätigt:** #732 Konto (FILE20→21 `Managers[0].Money` −1.530.327 ↔ 500.000), #734 (FILE13→14 Konto weicht ab),
  #737 als Zufall (optional), #743 offen (nicht Teil des PRs). Alle Issues noch offen.
- **Prüfpunkt (3):** Anpassungen fremder Tests einzeln gelesen (Abdrücke, `WettbewerbeSpiel12Teil2Tests` 150→50 folgt direkt aus
  `cbw`, `RefillLineupDressedTests` folgt aus 0x426E4, `SquadLifecycleTests`/`KaderStartRunde13Tests` begründet, B2 siehe oben).
  Mehrsaisonprobe passiv/aktiv vorhanden (`AbgleichFixPoolTests.File44_FourSeasonChanges_PoolMeansStable`,
  `AbgleichFixWorldOnlyTests.WorldOnly_MultiSeason_…`, je 4 Wechsel) und durch Rückmutation scharf.

## 3. Ausgeführte Prüfungen

**Fundstellen em.exe** (`ndisasm -b16`, Overlay-Stubs über Segmenttabelle 0x163C0):

- #730: Datei 0x6B08 `sub al,[bp-8]`, Aufruf 0x14CF:0x57 → Stub 0x16B47 → **0x4BD56** `mov al,[es:bx]; cbw`, Grenzen cx = 0x32,
  si = 0x96, Rückschreiben Byte (0x4BD78). ✔
- #736: 0x5B0CC ff. laufender Datenbankplatz `[bp-0x10]` über alle Arten, leerer Name → 0x5AAA0; dort Verein random(0, [0x7B1]−1),
  Managerverein verworfen, nach doppeltem Poolnamen (`[0x55EE]` = 1) Obergrenze +0x18 (0–35). ✔
- #733: 0x54D84–0x54D9A `[0x375+2m] := [0x376+2m]`, dann 0x108:0x4D und 0x54DAA; 0x54CC2 `jnz 0x54CEE`. ✔
- #748: 0x28:0x146C → Segment 0x62C → **0x952C** (Satz ≤ 5, Vereinsbyte ≤ 0x95 bei 0x954C); Aufrufe 0x54DAA (Modus 0), 0x54FBF (Modus 1). ✔
- #738: 0x426C4–0x426E4 `[0x375+2m] − 1 = 0xFF` → `jmp 0x42832` (Ende). ✔
- #732: 0x26F2F–0x26F4C Start = [0x386] · ([0xD0B9] = 1), 0x26FAD → 0x279F1 `cmp [0xA70E],[0x386]` → Schleife läuft im Modus 1 nicht. ✔

**Gegenbeispiele** (`emtool abgleich`, Saaten 1, 2, 3, 77): FILE02→03 MENDE nicht mehr abweichend (nur HOCK/SIKORA/BRANDL, B1);
FILE24→25 0–1 Abweichungen (nur Belastung); FILE44→46 nur `PoolMittel.Strain`; FILE18→48 0; FILE20→21 ohne `Age`/`ContractYears`,
LANDSHUT-Teamwerte weg; FILE13→14/FILE16A→17 wie in der erwarteten Liste. #736 über `AbgleichFixNamesTests` (FILE01 UVIRA/LALA
Platz 12/25 per `hok dump` bestätigt).

**Rückmutationen** (Worktree, `dotnet test tests/EishockeyManager.Core.Tests --filter …`, jeweils rot):

| Issue | Mutation | rot |
|---|---|---|
| #730 | `(fatigue - recovery) & 0xFF` | 11/21 `AbgleichFixStrainTests` (auch Mittel 02/13/16A) |
| #731 | Tausch Kondition/Technik entfernt | 4/4 `AbgleichFixPoolTests` |
| #733 | `[2 * m]` statt `[2 * m + 1]` | 3/5 `Import_AutoLineup_UsesSavedByte` |
| #736 | `NameDraw.Take` → nur Platzhalter | 8/8 `AbgleichFixNamesTests` |
| #738 | Bedingung `!dressed` entfernt | 9/28 `AbgleichFixLineupTests` |
| #747 | alte Bedingung `EnsureTeamValues` | 1/1 `ImportWorldOnly_FirstMatch_…` |
| #748 | `UpdateManagerTeamValues()` im Tageslauf entfernt | 6/28 `AbgleichFixLineupTests` |
| #749 | `taken = player is not null` | 1/1 `Import_LostPlayerPoolSlot_StaysTaken` |
| #732 | Jugend-Sperre bzw. Pool-Altern-Sperre entfernt | 2/6 bzw. 6/6 `AbgleichFixWorldOnlyTests` |

**`tools/pruefen.sh`** (alle drei Läufe, Head 2e5372c, SDK 10.0.402, Originaldaten aus dem Referenz-Repo):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 2903 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2133 | 0 | 492 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2134 | 769 | 0 |

CI auf dem Head: `build` success.

**Akzeptierte `[APPROX]`:** Werte­tausch Pool ↔ Computerkader; „nur Weltkugel“ Poolplatz ohne Managerkader frei; #736
Datenbankplatz = Anfang der Art + laufende Nummer und Namensquelle Computerkader; #733 Laden an Tag ohne Ereignis; #748 sofortiges
0x952C beim Verlassen der Aufstellung; #732 Modus 1 ohne Vereinsfinanzen (Entscheidung Nutzer).
**Annahmen (nicht geprüft):** Pixel­vergleich entfällt (kein Bildschirm geändert); Ursache der Restabweichung in B1 nur Verdacht.
