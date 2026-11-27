# Review PR #801 – Spur `pruefung-karte` Teil 2 (Runde 17)

**Reviewer:** Claude Code (Claude Opus 5.5), geplanter Lauf, eigene Sitzung ohne Implementierungskontext · **Datum:** 13.11.2026
**Base:** `e947eb51757915a0a1e8d8584d17b5ff999bef4b` · **Head:** `d8332d6263eb6dba1abbc852f6da533f31baeb44`
(`git rev-parse HEAD` = Head, `git status --porcelain` leer; Originaldaten aus `kaaasi/eishockey-manager-referenz`, `original/`)

## 1. Merge-Empfehlung

**Ja** für genau `d8332d6`. Alle drei Läufe von `tools/pruefen.sh` grün, CI `build` grün auf dem Head. Jede Rückmutation der
drei neuen bzw. verschärften Tests wird rot. Die Inventur deckt alle 26 hinzugekommenen und alle 33 entfallenen
`[APPROX]`-Zeilen der Runde ab. Die Gegenprobe der Markerzählung stimmt (`195 573` → `196 566`). Die Begründung „#800 kein
Befund“ ist im Code und in einer eigenen Probe belegt. Doku außerhalb der Spur ändert der PR nur nach den genannten Hinweisen.
Offen bleibt nur ein P3.

## 2. Befunde

**P3-1 – Börsentest erkennt nur das Fehlen des Wachstums, nicht seine Rate.**
[`PruefungKarteRunde17Tests.cs` Z. 37](https://github.com/kaaasi/eishockey-manager-redone/blob/d8332d6263eb6dba1abbc852f6da533f31baeb44/tests/EishockeyManager.Core.Tests/PruefungKarteRunde17Tests.cs#L37)
prüft den Anteil im Fenster 55–70 %. Probe: Ich habe die Wachstumsrate in `FinanceStocks.cs` Z. 102 (`stock.Base * r / 10_000`)
verändert und die Assertion auf `InRange(…, -1, -1)` gesetzt, damit der Fehlschlag den Istwert ausgibt. Gemessene Anteile:

| r (je 10.000 und Tag) | 0 | 1 | 3 (Original) | 6 |
|---|---:|---:|---:|---:|
| Anteil gewachsen | 43 % | 55 % | 62 % | 74 % |

Bei r = 1 (ein Drittel der Rate aus 0x57854) bleibt der Test grün (Mutation `m1b`: `Passed! 1/1`), genau an der unteren
Grenze. Der Kommentar („ohne das tägliche Wachstum … weit darunter“) stimmt. Vorschlag: Fenster etwa 58–67 % (Ist 62) oder
zusätzlich der mittlere Basiswert gegen `Base · (1 + 3/10.000)^924` mit Toleranz. Kein Mergehindernis; nach §5 Punkt 5 als
Issue `P3` sammeln.

Keine P0–P2.

**Erwartet offen, bestätigt:**

- Import/Export 0x26/0x2C: im PR als eigenes Paket genannt, kein Code geändert.
- A370: Abweichung der Hinweiszeile `Math.Clamp(p.Strain - 50, 0, 100)`
  ([`PlayerHintLine.cs` Z. 131](https://github.com/kaaasi/eishockey-manager-redone/blob/d8332d6263eb6dba1abbc852f6da533f31baeb44/src/EishockeyManager.UI/Screens/Team/PlayerHintLine.cs#L131))
  gegenüber `+0x13 · 10 / 15` in `reverse-engineering.md` ist in der Zeile A370 vermerkt. Dieselbe Formel steht auch in
  `PlayerInfoPopup.cs` Z. 85; für das Paket mitnehmen.
- #730 und #773 sind offen (`issue_read` → `state: open`).

## 3. Ausgeführte Prüfungen

**`tools/pruefen.sh`** (ohne Option, alle drei Läufe; SDK 10.0.402):

| Lauf | bestanden | fehlgeschlagen | übersprungen |
|---|---:|---:|---:|
| Mit Originaldaten (`EM_REQUIRE_GAMEDATA=1`) | 3044 | 0 | 0 |
| Ohne Originaldaten (`EM_SKIP_GAMEDATA=1`) | 2241 | 0 | 516 |
| Pflichtlauf ohne Daten (scheitert wie gewollt) | 2242 | 802 | 0 |

Commit `d8332d6`, Ergebnis: bestanden.

**Rückmutationen** (temporärer Worktree des Heads, `dotnet test tests/EishockeyManager.Core.Tests --filter …`, mit Daten). Unmutiert
sind alle vier Fälle grün.

| Mutation | Test | Ergebnis |
|---|---|---|
| Tageswachstum entfernt (`FinanceStocks.cs` Z. 102 `+= 0`) | `StockModel_BaseValuesGrow_LargeSeedSet` | rot (`InRange`, Ist 43) |
| `MatchEngine.Preparation.cs` von e1476e6 (Regel vor #784) | `ComputerMatch_KeepsFieldsAcrossSaveAndLoad` | rot (`Assert.Equal`, Gastwert) |
| #752 zurück: `(sbyte)(byte)` in `Squad.Training.cs` Z. 346 entfernt | `Strain730_DeviatesAtMostSeeds` (beide) | rot: HOCK 4 von 6 (1993, 1, 3, 77), KUHNEKATH 0 von 6 |

Der Bereichstest #730 erkennt damit nicht nur eigene Parameter (PR: `minSeeds` 6, HOCK 128–150), sondern auch eine
Produktänderung an der Belastung. Er bleibt auch bei den neuen Gastwerten rot, die die Paarungswahl von P3-3 erzwingt.

**Funktionskarte:** `python3 -I tools/reverse-engineering/funktionskarte.py ~/referenz/original/gamedata/em.exe --write`
zweimal im Head-Worktree. Danach `git status --porcelain` leer, md5 von `docs/re-abdeckung.md` jeweils `f02c5867…`: Der Head
ist schon der erzeugte Stand, der zweite Lauf erzeugt keinen Diff.

**(1) Inventur „Stand Runde 17“:** Geprüft habe ich die `[APPROX]`-Zeilen im Diff `git diff -U0 3a8070c e947eb5 -- src/`
(der PR ändert keinen Produktcode).

- **Hinzugekommen (26):** Jede Stelle hat eine Zeile. A418–A437 sind 20 Zeilen für 23 Stellen: A422 deckt `GameState.Spielplan.cs:11`,
  `NewGame.cs:46` und `Playoffs.cs:189`, A427 deckt `Player.Factory.cs:42/192`. Dazu kommen drei Rückblicke ohne Zeile:
  `StatisticsClub.cs:351`, `Transfer.Talent.cs:86`, `Transfer.cs:534`. Alle drei sind gelesen und nur Rückblicke („bis dahin
  bzw. bis Runde 17 `[APPROX]`“).
- **Entfallen (33):** Jede Stelle ist mit ID und Datei:Zeile im Absatz genannt, darunter `CupsNational.cs:184/200`,
  `Transfer.Squads.cs:24/30/57`, `LeaguePairingsScreen.cs:28–30` und `ContractsScreen.cs:81` (ohne eigene Zeile, jetzt `[ORIGINAL]`).
- **Zählung:** `git grep -c '\[APPROX\]' <rev> -- src/` ergibt `195 573` (3a8070c), `196 566` (e947eb5) und `196 566` (Head).
- **Übertragene IDs:** Alle 41 im PR genannten IDs (A004 … A410, A092, A370) haben eine Tabellenzeile mit „Stand Runde 17“.

**(2) #800:** Die Begründung ist im Code belegt: `GameState.FinanceRandom` = `new GameRandom(Season · 7919 + Σ(ClubIndex · 31 + Head))`
([`GameState.Manager.cs` Z. 40](https://github.com/kaaasi/eishockey-manager-redone/blob/d8332d6263eb6dba1abbc852f6da533f31baeb44/src/EishockeyManager.Core/GameState.Manager.cs#L40)),
ohne Spielsaat. Eigene Probe mit dem Aufbau von `BoerseStartTests.RunStockProbe` und den Saaten 1, 2, 25, 77, 192 und 1993:
Alle Läufe starten mit den Basiswerten 6448/11273/13864/17455, den Kursen 47/79/99/101 und Saison 1993. Gleiche
Startbörse in allen Saaten, wie im PR angegeben.

**(3) Doku außerhalb der Spur:**

- `belastung-staerke.md` §7 (0x38F40 → Angriffsbits) und `dateiformate.md` „Offen: tore\“ folgen dem Hinweis im
  Übergabekommentar #793 („belastung-staerke §7, dateiformate „Offen“, F084“) und `re-livespiel.md` Z. 458–460. Die
  Verweise §10 und §11 stimmen mit den Überschriften überein.
- `abgleich-original.md` #730 folgt R17 P3-1, das Z. 54 nennt.
- `arbeitspakete.md` ändert nur den eigenen Status.

**Teständerungen:** Kein Produktcode (`--stat`: 6 Doku-Dateien, 3 Testdateien).

- `AbgleichOriginalPaareTests` ändert nur Kommentare und den neuen Test.
- `AbgleichRestPreparationTests` verschärft P3-3 auf den exakten Wert für Heim und Gast. Der Gast wird gegen den schon
  geschriebenen Grundwert des Heimvereins verglichen. Zusätzlich prüft der Test jetzt auch Angriff (5, 5) und den Gast nach
  dem Laden. Der P3-2-Kommentar folgt dem Review.

**Akzeptierte `[APPROX]`:** keine neuen (bestätigt: kein Produktcode, Markerzählung unverändert).
**Annahmen (nicht selbst geprüft):** PR-Messungen „406 von 768“ und „60,0 % ab Startstand“. Der Probe-Lauf „Import 0x26/0x2C
allein → zwei Tests rot“ (erwartet offen).
